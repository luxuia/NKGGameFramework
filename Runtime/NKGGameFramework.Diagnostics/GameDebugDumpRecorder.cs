using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using NKGGameFramework.Diagnostics;

namespace NKGGameFramework.Diagnostics
{

    public sealed class GameDebugDumpRecorder : IDisposable
    {
        private const string DumpFormat = "nkg.debug.dump";
        private const int DumpDocumentVersion = 4;
        private const int MaxCachedPlaybackBlocks = 32;

        private readonly object _gate = new();
        private readonly IGameDebugSnapshotProvider _snapshots;
        private readonly GameDebugController _control;
        private readonly GameDebugFramePublisher _frames;
        private readonly GameDebugOptions _options;
        private readonly GameDebugSession? _session;
        private readonly GameDebugFrameCapturePipeline _captures;
        private readonly BlockingCollection<DumpCaptureWork> _captureQueue;
        private readonly Task _captureWorker;
        private ActiveDumpRecording? _recording;
        private FinalizingDumpRecording? _finalizing;
        private ActiveDumpPlayback? _playback;
        private string? _lastDumpName;
        private string? _lastDumpPath;
        private string? _lastDumpError;
        private bool _disposed;

        public GameDebugDumpRecorder(
            IGameDebugSnapshotProvider snapshots,
            GameDebugController control,
            GameDebugFramePublisher frames,
            GameDebugOptions options,
            GameDebugSession? session = null,
            object? debugStateGate = null)
        {
            _snapshots = snapshots;
            _control = control;
            _frames = frames;
            _options = options;
            _session = session;
            _captures = new GameDebugFrameCapturePipeline(snapshots, control, session, debugStateGate);
            // 原 Channel.CreateUnbounded（SingleReader）的等价改写：
            // 无界队列 + 单消费者 GetConsumingEnumerable。
            _captureQueue = new BlockingCollection<DumpCaptureWork>(new ConcurrentQueue<DumpCaptureWork>());
            _captureWorker = Task.Run(ProcessCaptureQueue);
        }

        public GameDebugDumpRecordingState GetState()
        {
            ActiveDumpRecording? recording;
            lock (_gate)
            {
                recording = _recording;
            }

            recording?.WaitForPendingCaptures();

            lock (_gate)
            {
                return CreateState();
            }
        }

        public GameDebugDumpRecordingResult Execute(GameDebugDumpRecordingRequest request)
        {
            NkgThrow.IfNull(request);

            return request.Command.Trim().ToLowerInvariant() switch
            {
                "start" => Start(request.Name, request.DumpDirectory),
                "stop" or "end" => Stop(),
                _ => new GameDebugDumpRecordingResult(
                    false,
                    $"Unknown dump recording command '{request.Command}'.",
                    GetState()),
            };
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _frames.FramePublished -= OnFramePublished;
            _captureQueue.CompleteAdding();
            try
            {
                _captureWorker.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
            }
        }

        private GameDebugDumpRecordingResult Start(string? name, string? dumpDirectory)
        {
            ActiveDumpRecording recording;
            lock (_gate)
            {
                if (_recording is not null)
                {
                    return new GameDebugDumpRecordingResult(
                        false,
                        "A debug dump recording is already running.",
                        CreateState());
                }

                if (_finalizing is not null)
                {
                    return new GameDebugDumpRecordingResult(
                        false,
                        "A debug dump recording is still being saved.",
                        CreateState());
                }

                var now = DateTimeOffset.UtcNow;
                recording = new ActiveDumpRecording(
                    NormalizeDumpName(name, now),
                    ResolveDumpDirectory(dumpDirectory, _options.DumpDirectory),
                    now);
                _recording = recording;
                _frames.FramePublished += OnFramePublished;
            }

            return new GameDebugDumpRecordingResult(
                true,
                "Debug dump recording started.",
                GetState());
        }

        private GameDebugDumpRecordingResult Stop()
        {
            ActiveDumpRecording recording;
            DateTimeOffset endedAt;
            lock (_gate)
            {
                if (_recording is null)
                {
                    if (_finalizing is not null)
                    {
                        return new GameDebugDumpRecordingResult(
                            true,
                            "Debug dump recording is still being saved.",
                            CreateState());
                    }

                    return new GameDebugDumpRecordingResult(
                        false,
                        "No debug dump recording is running.",
                        CreateState());
                }

                recording = _recording;
                _recording = null;
                _frames.FramePublished -= OnFramePublished;
                endedAt = DateTimeOffset.UtcNow;
                _finalizing = new FinalizingDumpRecording(
                    recording.Name,
                    recording.StartedAt,
                    endedAt,
                    recording.FrameCount,
                    recording.CreateMetricsSnapshot());
                _lastDumpName = recording.Name;
                _lastDumpPath = null;
                _lastDumpError = null;
            }

            _ = Task.Run(() => FinalizeRecording(recording, endedAt));

            return new GameDebugDumpRecordingResult(
                true,
                "Debug dump recording is being saved.",
                GetState());
        }

        private void FinalizeRecording(ActiveDumpRecording recording, DateTimeOffset endedAt)
        {
            try
            {
                recording.WaitForPendingCaptures();
                var path = SaveRecording(recording, endedAt);

                lock (_gate)
                {
                    _lastDumpName = recording.Name;
                    _lastDumpPath = path;
                    _lastDumpError = null;
                    _finalizing = null;
                }
            }
            catch (Exception exception)
            {
                lock (_gate)
                {
                    _lastDumpError = exception.Message;
                    _finalizing = null;
                }
            }
        }

        private string SaveRecording(ActiveDumpRecording recording, DateTimeOffset endedAt)
        {
            var recordedFrames = recording.SnapshotFrames();
            var frames = new GameDebugSnapshotMessage[recordedFrames.Count];
            List<GameDebugDumpFrameBlocks>? blockFrames = null;
            for (var index = 0; index < recordedFrames.Count; index++)
            {
                var frame = recordedFrames[index];
                frames[index] = frame.Message;
                if (frame.Blocks is not { } worlds)
                {
                    continue;
                }

                blockFrames ??= new List<GameDebugDumpFrameBlocks>();
                blockFrames.Add(new GameDebugDumpFrameBlocks(index, worlds));
            }

            var dump = new GameDebugDumpDocument(
                DumpFormat,
                DumpDocumentVersion,
                recording.Name,
                endedAt,
                recording.StartedAt,
                endedAt,
                frames,
                blockFrames?.ToArray(),
                recording.CreateMetricsSnapshot());
            return SaveDump(dump, recording.DumpDirectory);
        }

        public GameDebugDumpPlaybackManifest OpenPlayback(GameDebugDumpPlaybackOpenRequest request)
        {
            NkgThrow.IfNull(request);

            var path = string.IsNullOrWhiteSpace(request.Path)
                ? GetLastDumpPath()
                : Path.GetFullPath(request.Path.Trim());
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidDataException(_finalizing is null
                    ? "No debug dump file has been recorded yet."
                    : "The debug dump recording is still being saved.");
            }

            var dump = GameDebugDumpFile.Deserialize(File.ReadAllBytes(path));
            return SetPlayback(dump, path);
        }

        public GameDebugDumpPlaybackManifest OpenPlayback(byte[] payload)
        {
            var dump = GameDebugDumpFile.Deserialize(payload);
            return SetPlayback(dump, null);
        }

        public GameDebugDumpAnalysisReport AnalyzeDump(GameDebugDumpPlaybackOpenRequest request)
        {
            NkgThrow.IfNull(request);

            var path = string.IsNullOrWhiteSpace(request.Path)
                ? GetLastDumpPath()
                : Path.GetFullPath(request.Path.Trim());
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidDataException(_finalizing is null
                    ? "No debug dump file has been recorded yet."
                    : "The debug dump recording is still being saved.");
            }

            return GameDebugDumpAnalyzer.Analyze(File.ReadAllBytes(path));
        }

        public GameDebugDumpAnalysisReport AnalyzeDump(byte[] payload)
        {
            return GameDebugDumpAnalyzer.Analyze(payload);
        }

        public GameDebugSnapshotMessage GetPlaybackFrame(string? playbackId, int frameIndex)
        {
            lock (_gate)
            {
                if (_playback is null)
                {
                    throw new InvalidDataException("No debug dump playback is loaded.");
                }

                if (!string.IsNullOrWhiteSpace(playbackId) &&
                    !StringComparer.Ordinal.Equals(_playback.Id, playbackId))
                {
                    throw new InvalidDataException("The requested debug dump playback is no longer loaded.");
                }

                if (frameIndex < 0 || frameIndex >= _playback.Document.Frames.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(frameIndex), "The debug dump frame index was out of range.");
                }

                return _playback.Document.Frames[frameIndex];
            }
        }

        public ComponentDebugSnapshot GetPlaybackComponent(GameDebugDumpPlaybackComponentRequest request)
        {
            NkgThrow.IfNull(request);

            ActiveDumpPlayback playback;
            lock (_gate)
            {
                playback = GetPlayback(request.PlaybackId);
            }

            _ = GetPlaybackFrame(playback, request.FrameIndex);
            var component = playback.TryGetComponent(request, out var foundComponent)
                ? foundComponent
                : throw new InvalidDataException($"The debug dump component '{request.ComponentTypeFullName}' was not found.");

            if (playback.TryGetBlock(request, out var block, out var row, out var blockKey))
            {
                return MaterializeBlockComponent(playback, component, block, request, row, blockKey);
            }

            return GameDebugComponentValueMaterializer.MaterializeStructured(
                component,
                new OdinGameDebugComponentValueSerializer(),
                new GameDebugStructuredComponentValueCaptureOptions
                {
                    MaxCollectionItems = 64,
                    CaptureElementTemplate = false,
                });
        }

        private string? GetLastDumpPath()
        {
            lock (_gate)
            {
                return _lastDumpPath;
            }
        }

        private GameDebugDumpPlaybackManifest SetPlayback(GameDebugDumpDocument dump, string? path)
        {
            var playback = new ActiveDumpPlayback(Guid.NewGuid().ToString("N"), dump, path);
            lock (_gate)
            {
                _playback = playback;
            }

            return CreatePlaybackManifest(playback);
        }

        private static GameDebugDumpPlaybackManifest CreatePlaybackManifest(ActiveDumpPlayback playback)
        {
            var dump = playback.Document;
            return new GameDebugDumpPlaybackManifest(
                playback.Id,
                dump.Format,
                dump.Version,
                dump.Name,
                dump.CreatedAt,
                dump.StartedAt,
                dump.EndedAt,
                dump.Frames
                    .Select((message, index) => new GameDebugDumpPlaybackFrame(index, message.Frame))
                    .ToArray());
        }

        private void OnFramePublished(GameDebugFrameInfo info)
        {
            var started = Stopwatch.GetTimestamp();
            ActiveDumpRecording? recording;
            lock (_gate)
            {
                recording = _recording;
                if (recording is null)
                {
                    return;
                }

                recording.RecordPublishedFrame();
                recording.BeginCapture();
            }

            try
            {
                if (_session is not null)
                {
                    CaptureQueuedFrame(new DumpCaptureWork(recording, info));
                    return;
                }

                try
                {
                    _captureQueue.TryAdd(new DumpCaptureWork(recording, info));
                }
                catch (InvalidOperationException)
                {
                    // 队列已完成添加（对应原 Channel Writer.TryWrite 的 false 分支）。
                    recording.CompleteCapture();
                }
            }
            finally
            {
                var elapsed = GetElapsedMilliseconds(started);
                lock (_gate)
                {
                    if (ReferenceEquals(_recording, recording))
                    {
                        recording.RecordFrameCallback(elapsed);
                    }
                }
            }
        }

        private void ProcessCaptureQueue()
        {
            foreach (var work in _captureQueue.GetConsumingEnumerable())
            {
                CaptureQueuedFrame(work);
            }
        }

        private void CaptureQueuedFrame(DumpCaptureWork work)
        {
            var started = Stopwatch.GetTimestamp();
            var allocatedBefore = TryGetAllocatedBytesForCurrentThread();
            try
            {
                var captured = CaptureFrame(work);
                var elapsed = GetElapsedMilliseconds(started);
                var allocatedBytes = GetAllocatedByteDelta(allocatedBefore, TryGetAllocatedBytesForCurrentThread());
                var (storeCount, entityRowCount) = CountSerializedBlocks(captured.Blocks);
                lock (_gate)
                {
                    work.Recording.AddFrame(captured);
                    work.Recording.RecordCapture(elapsed, storeCount, entityRowCount, allocatedBytes);
                }
            }
            catch (Exception exception)
            {
                var elapsed = GetElapsedMilliseconds(started);
                lock (_gate)
                {
                    _lastDumpError = exception.Message;
                    work.Recording.RecordCapture(elapsed, storeCount: 0, entityRowCount: 0, allocatedBytes: null);
                }
            }
            finally
            {
                work.Recording.CompleteCapture();
            }
        }

        private GameDebugDumpRecordedFrame CaptureFrame(DumpCaptureWork work)
        {
            var captured = _captures.CaptureDumpFrame(
                work.Frame,
                CreateDumpCaptureOptions(),
                includeBlocks: _session is not null);
            var blocks = captured.Blocks is null
                ? null
                : SerializeCapturedFrameBlocks(captured.Blocks);

            return new GameDebugDumpRecordedFrame(captured.Message, blocks);
        }

        private static GameDebugDumpWorldBlocks[] SerializeCapturedFrameBlocks(
            CapturedDumpFrameBlocks frame)
        {
            var worlds = new GameDebugDumpWorldBlocks[frame.Worlds.Length];
            for (var worldIndex = 0; worldIndex < frame.Worlds.Length; worldIndex++)
            {
                var world = frame.Worlds[worldIndex];
                var scenes = new GameDebugDumpSceneBlocks[world.Scenes.Length];
                for (var sceneIndex = 0; sceneIndex < world.Scenes.Length; sceneIndex++)
                {
                    var scene = world.Scenes[sceneIndex];
                    var stores = new List<GameDebugDumpComponentStoreBlock>(scene.ComponentStores.Length);
                    for (var storeIndex = 0; storeIndex < scene.ComponentStores.Length; storeIndex++)
                    {
                        var store = scene.ComponentStores[storeIndex];
                        stores.Add(GameDebugComponentStoreBlockSerializer.Serialize(store));
                    }

                    scenes[sceneIndex] = new GameDebugDumpSceneBlocks(scene.Name, stores.ToArray());
                }

                worlds[worldIndex] = new GameDebugDumpWorldBlocks(world.Name, scenes);
            }

            return worlds;
        }

        private string SaveDump(GameDebugDumpDocument dump, string dumpDirectory)
        {
            Directory.CreateDirectory(dumpDirectory);
            var fileName = $"{SanitizeFileName(dump.Name)}{GameDebugDumpFile.FileExtension}";
            var path = Path.Combine(dumpDirectory, fileName);
            File.WriteAllBytes(path, GameDebugDumpFile.Serialize(dump));
            return path;
        }

        private GameDebugDumpRecordingState CreateState()
        {
            if (_recording is null && _finalizing is { } finalizing)
            {
                return new GameDebugDumpRecordingState(
                    false,
                    finalizing.StartedAt,
                    finalizing.FrameCount,
                    _lastDumpName ?? finalizing.Name,
                    _lastDumpPath,
                    IsFinalizing: true,
                    LastDumpError: _lastDumpError,
                    Metrics: finalizing.Metrics);
            }

            return _recording is { } recording
                ? new GameDebugDumpRecordingState(
                    true,
                    recording.StartedAt,
                    recording.FrameCount,
                    _lastDumpName,
                    _lastDumpPath,
                    IsFinalizing: false,
                    LastDumpError: _lastDumpError,
                    Metrics: recording.CreateMetricsSnapshot())
                : new GameDebugDumpRecordingState(
                    false,
                    null,
                    0,
                    _lastDumpName,
                    _lastDumpPath,
                    IsFinalizing: false,
                    LastDumpError: _lastDumpError);
        }

        private GameDebugSnapshotCaptureOptions CreateDumpCaptureOptions()
        {
            return new GameDebugSnapshotCaptureOptions
            {
                Profile = GameDebugSnapshotCaptureProfile.DumpRecording,
                IncludeComponentPayloads = false,
                IncludeStructuredComponentValues = false,
                IncludeRuntimeDetails = false,
                IncludeSceneSystems = false,
                IncludeComponentStoreSummaries = false,
                IncludeEntitySummaries = false,
                IncludeComponentGraphs = false,
            };
        }

        private ActiveDumpPlayback GetPlayback(string? playbackId)
        {
            if (_playback is null)
            {
                throw new InvalidDataException("No debug dump playback is loaded.");
            }

            if (!string.IsNullOrWhiteSpace(playbackId) &&
                !StringComparer.Ordinal.Equals(_playback.Id, playbackId))
            {
                throw new InvalidDataException("The requested debug dump playback is no longer loaded.");
            }

            return _playback;
        }

        private static GameDebugSnapshotMessage GetPlaybackFrame(ActiveDumpPlayback playback, int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= playback.Document.Frames.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex), "The debug dump frame index was out of range.");
            }

            return playback.Document.Frames[frameIndex];
        }

        private static ComponentDebugSnapshot MaterializeBlockComponent(
            ActiveDumpPlayback playback,
            ComponentDebugSnapshot component,
            GameDebugDumpComponentStoreBlock block,
            GameDebugDumpPlaybackComponentRequest request,
            int row,
            PlaybackBlockKey blockKey)
        {
            if (row < 0)
            {
                return component with
                {
                    Value = new ComponentValueDebugSnapshot(
                        block.Format,
                        Payload: null,
                        $"Entity {request.EntityId} was not present in component store '{block.Type.Name}'."),
                };
            }

            if (StringComparer.Ordinal.Equals(block.Format, GameDebugComponentStoreBlockSerializer.StructuredFormat))
            {
                if (!playback.TryGetStructuredBlockValues(blockKey, block, out var structuredValues, out var structuredError))
                {
                    return component with
                    {
                        Value = new ComponentValueDebugSnapshot(
                            block.Format,
                            Payload: null,
                            structuredError),
                    };
                }

                return component with
                {
                    Value = structuredValues[row],
                };
            }

            if (!playback.TryGetBlockValues(blockKey, block, out var values, out var error))
            {
                return component with
                {
                    Value = new ComponentValueDebugSnapshot(
                        GameDebugComponentStoreBlockSerializer.Format,
                        Payload: null,
                        error),
                };
            }

            object value;
            try
            {
                value = values.GetValue(row)
                    ?? throw new InvalidDataException("The component store block contained a null row.");
            }
            catch (Exception exception)
            {
                return component with
                {
                    Value = new ComponentValueDebugSnapshot(
                        GameDebugComponentStoreBlockSerializer.Format,
                        Payload: null,
                        exception.Message),
                };
            }

            var serializer = new OdinGameDebugComponentValueSerializer();
            return component with
            {
                Value = serializer.Serialize(
                    value,
                    new GameDebugComponentValueSerializationOptions
                    {
                        IncludePayload = false,
                        IncludeStructured = true,
                        StructuredCaptureOptions = new GameDebugStructuredComponentValueCaptureOptions
                        {
                            MaxCollectionItems = 64,
                            CaptureElementTemplate = false,
                        },
                    }),
            };
        }

        private static string NormalizeDumpName(string? name, DateTimeOffset now)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            return $"nkg-debug-{now.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars().ToHashSet();
            var sanitized = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "nkg-debug-dump" : sanitized;
        }

        private static string ResolveDumpDirectory(string? requestedDirectory, string fallbackDirectory)
        {
            var directory = string.IsNullOrWhiteSpace(requestedDirectory)
                ? fallbackDirectory
                : requestedDirectory.Trim();

            return Path.GetFullPath(directory);
        }

        private static double GetElapsedMilliseconds(long startedTimestamp)
        {
            // .NET 7 Stopwatch.GetElapsedTime 的 netstandard2.1 等价实现。
            return (double)(Stopwatch.GetTimestamp() - startedTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        private static long? TryGetAllocatedBytesForCurrentThread()
        {
            try
            {
                return GC.GetAllocatedBytesForCurrentThread();
            }
            catch (NotImplementedException)
            {
                return null;
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }

        private static long? GetAllocatedByteDelta(long? before, long? after)
        {
            return before is { } start && after is { } end && end >= start
                ? end - start
                : null;
        }

        private static (int StoreCount, int EntityRowCount) CountSerializedBlocks(GameDebugDumpWorldBlocks[]? blocks)
        {
            if (blocks is null)
            {
                return (0, 0);
            }

            var storeCount = 0;
            var entityRowCount = 0;
            foreach (var world in blocks)
            {
                foreach (var scene in world.Scenes)
                {
                    foreach (var store in scene.ComponentStores)
                    {
                        storeCount++;
                        entityRowCount += store.EntityIds.Length;
                    }
                }
            }

            return (storeCount, entityRowCount);
        }

        private sealed class ActiveDumpRecording
        {
            private readonly Queue<GameDebugDumpRecordedFrame> _frames = new();
            private readonly ManualResetEventSlim _capturesDrained = new(initialState: true);
            private readonly object _captureGate = new();
            private int _pendingCaptureCount;
            private int _publishedFrameCount;
            private int _capturedFrameCount;
            private double _lastFrameCallbackMilliseconds;
            private double _maxFrameCallbackMilliseconds;
            private double _totalFrameCallbackMilliseconds;
            private double _lastCaptureMilliseconds;
            private double _maxCaptureMilliseconds;
            private double _totalCaptureMilliseconds;
            private int _lastCapturedStoreCount;
            private int _lastCapturedEntityRowCount;
            private int _maxCapturedStoreCount;
            private int _maxCapturedEntityRowCount;
            private long _totalCapturedStoreCount;
            private long _totalCapturedEntityRowCount;
            private long? _lastCaptureAllocatedBytes;
            private long _totalCaptureAllocatedBytes;

            public ActiveDumpRecording(
                string name,
                string dumpDirectory,
                DateTimeOffset startedAt)
            {
                Name = name;
                DumpDirectory = dumpDirectory;
                StartedAt = startedAt;
            }

            public string Name { get; }

            public string DumpDirectory { get; }

            public DateTimeOffset StartedAt { get; }

            public int FrameCount => _frames.Count;

            public void RecordPublishedFrame()
            {
                _publishedFrameCount++;
            }

            public void RecordFrameCallback(double elapsedMilliseconds)
            {
                _lastFrameCallbackMilliseconds = elapsedMilliseconds;
                _totalFrameCallbackMilliseconds += elapsedMilliseconds;
                if (elapsedMilliseconds > _maxFrameCallbackMilliseconds)
                {
                    _maxFrameCallbackMilliseconds = elapsedMilliseconds;
                }
            }

            public void RecordCapture(
                double elapsedMilliseconds,
                int storeCount,
                int entityRowCount,
                long? allocatedBytes)
            {
                _capturedFrameCount++;
                _lastCaptureMilliseconds = elapsedMilliseconds;
                _totalCaptureMilliseconds += elapsedMilliseconds;
                if (elapsedMilliseconds > _maxCaptureMilliseconds)
                {
                    _maxCaptureMilliseconds = elapsedMilliseconds;
                }

                _lastCapturedStoreCount = storeCount;
                _lastCapturedEntityRowCount = entityRowCount;
                _maxCapturedStoreCount = Math.Max(_maxCapturedStoreCount, storeCount);
                _maxCapturedEntityRowCount = Math.Max(_maxCapturedEntityRowCount, entityRowCount);
                _totalCapturedStoreCount += storeCount;
                _totalCapturedEntityRowCount += entityRowCount;
                _lastCaptureAllocatedBytes = allocatedBytes;
                if (allocatedBytes is { } bytes)
                {
                    _totalCaptureAllocatedBytes += bytes;
                }
            }

            public GameDebugDumpRecordingMetrics CreateMetricsSnapshot()
            {
                int pendingCaptureCount;
                lock (_captureGate)
                {
                    pendingCaptureCount = _pendingCaptureCount;
                }

                return new GameDebugDumpRecordingMetrics(
                    _publishedFrameCount,
                    _capturedFrameCount,
                    pendingCaptureCount,
                    _lastFrameCallbackMilliseconds,
                    _maxFrameCallbackMilliseconds,
                    _publishedFrameCount > 0 ? _totalFrameCallbackMilliseconds / _publishedFrameCount : 0d,
                    _lastCaptureMilliseconds,
                    _maxCaptureMilliseconds,
                    _capturedFrameCount > 0 ? _totalCaptureMilliseconds / _capturedFrameCount : 0d,
                    _lastCapturedStoreCount,
                    _lastCapturedEntityRowCount,
                    _maxCapturedStoreCount,
                    _maxCapturedEntityRowCount,
                    _totalCapturedStoreCount,
                    _totalCapturedEntityRowCount,
                    _lastCaptureAllocatedBytes,
                    _totalCaptureAllocatedBytes > 0 ? _totalCaptureAllocatedBytes : null);
            }

            public void AddFrame(GameDebugDumpRecordedFrame frame)
            {
                _frames.Enqueue(frame);
            }

            public IReadOnlyList<GameDebugDumpRecordedFrame> SnapshotFrames()
            {
                return _frames.ToArray();
            }

            public void BeginCapture()
            {
                lock (_captureGate)
                {
                    _pendingCaptureCount++;
                    _capturesDrained.Reset();
                }
            }

            public void CompleteCapture()
            {
                lock (_captureGate)
                {
                    _pendingCaptureCount--;
                    if (_pendingCaptureCount <= 0)
                    {
                        _pendingCaptureCount = 0;
                        _capturesDrained.Set();
                    }
                }
            }

            public void WaitForPendingCaptures()
            {
                _capturesDrained.Wait();
            }

        }

        private sealed record GameDebugDumpRecordedFrame(
            GameDebugSnapshotMessage Message,
            GameDebugDumpWorldBlocks[]? Blocks);

        private sealed record DumpCaptureWork(
            ActiveDumpRecording Recording,
            GameDebugFrameInfo Frame);

        private sealed record FinalizingDumpRecording(
            string Name,
            DateTimeOffset StartedAt,
            DateTimeOffset EndedAt,
            int FrameCount,
            GameDebugDumpRecordingMetrics Metrics);

        private readonly struct PlaybackComponentKey : IEquatable<PlaybackComponentKey>
        {
            public readonly int FrameIndex;
            public readonly string WorldName;
            public readonly string SceneName;
            public readonly int EntityId;
            public readonly string ComponentTypeFullName;
            public readonly string ComponentAssemblyName;

            public PlaybackComponentKey(int FrameIndex, string WorldName, string SceneName, int EntityId, string ComponentTypeFullName, string ComponentAssemblyName)
            {
                this.FrameIndex = FrameIndex;
                this.WorldName = WorldName;
                this.SceneName = SceneName;
                this.EntityId = EntityId;
                this.ComponentTypeFullName = ComponentTypeFullName;
                this.ComponentAssemblyName = ComponentAssemblyName;
            }

            public bool Equals(PlaybackComponentKey other) => EqualityComparer<int>.Default.Equals(FrameIndex, other.FrameIndex) && EqualityComparer<string>.Default.Equals(WorldName, other.WorldName) && EqualityComparer<string>.Default.Equals(SceneName, other.SceneName) && EqualityComparer<int>.Default.Equals(EntityId, other.EntityId) && EqualityComparer<string>.Default.Equals(ComponentTypeFullName, other.ComponentTypeFullName) && EqualityComparer<string>.Default.Equals(ComponentAssemblyName, other.ComponentAssemblyName);

            public override int GetHashCode() => HashCode.Combine(FrameIndex, WorldName, SceneName, EntityId, ComponentTypeFullName, ComponentAssemblyName);

            public override bool Equals(object? obj) => obj is PlaybackComponentKey other && Equals(other);

            public static bool operator ==(PlaybackComponentKey left, PlaybackComponentKey right) => left.Equals(right);
            public static bool operator !=(PlaybackComponentKey left, PlaybackComponentKey right) => !left.Equals(right);

            public void Deconstruct(out int frameIndex, out string worldName, out string sceneName, out int entityId, out string componentTypeFullName, out string componentAssemblyName)
            {
                frameIndex = FrameIndex; worldName = WorldName; sceneName = SceneName; entityId = EntityId; componentTypeFullName = ComponentTypeFullName; componentAssemblyName = ComponentAssemblyName;
            }

            /// <summary>替代原 record 的 with 表达式。</summary>
            public PlaybackComponentKey WithComponentAssemblyName(string assemblyName)
            {
                return new PlaybackComponentKey(FrameIndex, WorldName, SceneName, EntityId, ComponentTypeFullName, assemblyName);
            }
        }

        private readonly struct PlaybackBlockKey : IEquatable<PlaybackBlockKey>
        {
            public readonly int FrameIndex;
            public readonly string WorldName;
            public readonly string SceneName;
            public readonly string ComponentTypeFullName;
            public readonly string ComponentAssemblyName;

            public PlaybackBlockKey(int FrameIndex, string WorldName, string SceneName, string ComponentTypeFullName, string ComponentAssemblyName)
            {
                this.FrameIndex = FrameIndex;
                this.WorldName = WorldName;
                this.SceneName = SceneName;
                this.ComponentTypeFullName = ComponentTypeFullName;
                this.ComponentAssemblyName = ComponentAssemblyName;
            }

            public bool Equals(PlaybackBlockKey other) => EqualityComparer<int>.Default.Equals(FrameIndex, other.FrameIndex) && EqualityComparer<string>.Default.Equals(WorldName, other.WorldName) && EqualityComparer<string>.Default.Equals(SceneName, other.SceneName) && EqualityComparer<string>.Default.Equals(ComponentTypeFullName, other.ComponentTypeFullName) && EqualityComparer<string>.Default.Equals(ComponentAssemblyName, other.ComponentAssemblyName);

            public override int GetHashCode() => HashCode.Combine(FrameIndex, WorldName, SceneName, ComponentTypeFullName, ComponentAssemblyName);

            public override bool Equals(object? obj) => obj is PlaybackBlockKey other && Equals(other);

            public static bool operator ==(PlaybackBlockKey left, PlaybackBlockKey right) => left.Equals(right);
            public static bool operator !=(PlaybackBlockKey left, PlaybackBlockKey right) => !left.Equals(right);

            public void Deconstruct(out int frameIndex, out string worldName, out string sceneName, out string componentTypeFullName, out string componentAssemblyName)
            {
                frameIndex = FrameIndex; worldName = WorldName; sceneName = SceneName; componentTypeFullName = ComponentTypeFullName; componentAssemblyName = ComponentAssemblyName;
            }

            public PlaybackBlockKey WithComponentAssemblyName(string assemblyName)
            {
                return new PlaybackBlockKey(FrameIndex, WorldName, SceneName, ComponentTypeFullName, assemblyName);
            }
        }

        private readonly struct PlaybackBlockEntityKey : IEquatable<PlaybackBlockEntityKey>
        {
            public readonly PlaybackBlockKey Block;
            public readonly int EntityId;

            public PlaybackBlockEntityKey(PlaybackBlockKey Block, int EntityId)
            {
                this.Block = Block;
                this.EntityId = EntityId;
            }

            public bool Equals(PlaybackBlockEntityKey other) => EqualityComparer<PlaybackBlockKey>.Default.Equals(Block, other.Block) && EqualityComparer<int>.Default.Equals(EntityId, other.EntityId);

            public override int GetHashCode() => HashCode.Combine(Block, EntityId);

            public override bool Equals(object? obj) => obj is PlaybackBlockEntityKey other && Equals(other);

            public static bool operator ==(PlaybackBlockEntityKey left, PlaybackBlockEntityKey right) => left.Equals(right);
            public static bool operator !=(PlaybackBlockEntityKey left, PlaybackBlockEntityKey right) => !left.Equals(right);

            public void Deconstruct(out PlaybackBlockKey block, out int entityId)
            {
                block = Block; entityId = EntityId;
            }
        }

        private sealed class ActiveDumpPlayback
        {
            private readonly object _indexGate = new();
            private readonly object _blockValueGate = new();
            private readonly Dictionary<PlaybackBlockKey, Array> _blockValues = new();
            private readonly Queue<PlaybackBlockKey> _blockValueKeys = new();
            private readonly Dictionary<PlaybackComponentKey, ComponentDebugSnapshot> _components = new();
            private readonly Dictionary<PlaybackBlockKey, GameDebugDumpComponentStoreBlock> _blocks = new();
            private readonly Dictionary<PlaybackBlockEntityKey, int> _blockRows = new();
            private readonly Dictionary<int, GameDebugDumpFrameBlocks> _blockFramesByIndex = new();
            private readonly HashSet<int> _indexedComponentFrames = new();
            private readonly HashSet<int> _indexedBlockFrames = new();

            public ActiveDumpPlayback(
                string id,
                GameDebugDumpDocument document,
                string? path)
            {
                Id = id;
                Document = document;
                Path = path;
                if (document.BlockFrames is not { Count: > 0 } blockFrames)
                {
                    return;
                }

                foreach (var frame in blockFrames)
                {
                    _blockFramesByIndex.TryAdd(frame.Index, frame);
                }
            }

            public string Id { get; }

            public GameDebugDumpDocument Document { get; }

            public string? Path { get; }

            public bool TryGetComponent(
                GameDebugDumpPlaybackComponentRequest request,
                out ComponentDebugSnapshot component)
            {
                lock (_indexGate)
                {
                    EnsureComponentFrameIndexed(request.FrameIndex);
                    return _components.TryGetValue(CreateComponentKey(request), out component!);
                }
            }

            public bool TryGetBlock(
                GameDebugDumpPlaybackComponentRequest request,
                out GameDebugDumpComponentStoreBlock block,
                out int row,
                out PlaybackBlockKey blockKey)
            {
                lock (_indexGate)
                {
                    for (var frameIndex = request.FrameIndex; frameIndex >= 0; frameIndex--)
                    {
                        var key = CreateBlockKey(request, frameIndex);
                        EnsureBlockFrameIndexed(frameIndex);
                        if (!_blocks.TryGetValue(key, out block!))
                        {
                            continue;
                        }

                        row = _blockRows.TryGetValue(new PlaybackBlockEntityKey(key, request.EntityId), out var foundRow)
                            ? foundRow
                            : -1;
                        blockKey = key;
                        return true;
                    }
                }

                block = null!;
                row = -1;
                blockKey = default;
                return false;
            }

            public bool TryGetBlockValues(
                PlaybackBlockKey cacheKey,
                GameDebugDumpComponentStoreBlock block,
                out Array values,
                out string? error)
            {
                if (TryGetCachedBlockValues(cacheKey, out values!))
                {
                    error = null;
                    return true;
                }

                try
                {
                    values = GameDebugComponentStoreBlockSerializer.DeserializeValues(block);
                    AddBlockValues(cacheKey, values);
                    error = null;
                    return true;
                }
                catch (Exception exception)
                {
                    values = null!;
                    error = exception.Message;
                    return false;
                }
            }

            public bool TryGetStructuredBlockValues(
                PlaybackBlockKey cacheKey,
                GameDebugDumpComponentStoreBlock block,
                out ComponentValueDebugSnapshot[] values,
                out string? error)
            {
                if (TryGetCachedBlockValues(cacheKey, out var cachedValues) &&
                    cachedValues is ComponentValueDebugSnapshot[] structuredValues)
                {
                    values = structuredValues;
                    error = null;
                    return true;
                }

                if (!GameDebugComponentStoreBlockSerializer.TryDeserializeStructuredValues(block, out values, out error))
                {
                    return false;
                }

                AddBlockValues(cacheKey, values);
                return true;
            }

            private bool TryGetCachedBlockValues(PlaybackBlockKey cacheKey, out Array values)
            {
                lock (_blockValueGate)
                {
                    return _blockValues.TryGetValue(cacheKey, out values!);
                }
            }

            private void AddBlockValues(PlaybackBlockKey cacheKey, Array values)
            {
                lock (_blockValueGate)
                {
                    if (_blockValues.ContainsKey(cacheKey))
                    {
                        return;
                    }

                    _blockValues.Add(cacheKey, values);
                    _blockValueKeys.Enqueue(cacheKey);

                    while (_blockValues.Count > MaxCachedPlaybackBlocks)
                    {
                        var oldest = _blockValueKeys.Dequeue();
                        _blockValues.Remove(oldest);
                    }
                }
            }

            private void EnsureComponentFrameIndexed(int frameIndex)
            {
                if (!_indexedComponentFrames.Add(frameIndex))
                {
                    return;
                }

                var frame = Document.Frames[frameIndex];
                foreach (var world in frame.Snapshot.Worlds)
                {
                    foreach (var scene in world.Scenes)
                    {
                        foreach (var entity in scene.Entities)
                        {
                            foreach (var component in entity.Components)
                            {
                                AddComponentIndex(
                                    frameIndex,
                                    world.Name,
                                    scene.Name,
                                    entity.Id,
                                    component);
                            }
                        }
                    }
                }
            }

            private void EnsureBlockFrameIndexed(int frameIndex)
            {
                if (!_indexedBlockFrames.Add(frameIndex))
                {
                    return;
                }

                if (!_blockFramesByIndex.TryGetValue(frameIndex, out var frame))
                {
                    return;
                }

                foreach (var world in frame.Worlds)
                {
                    foreach (var scene in world.Scenes)
                    {
                        foreach (var store in scene.ComponentStores)
                        {
                            AddBlockIndex(frame.Index, world.Name, scene.Name, store);
                        }
                    }
                }
            }

            private void AddComponentIndex(
                int frameIndex,
                string worldName,
                string sceneName,
                int entityId,
                ComponentDebugSnapshot component)
            {
                var key = new PlaybackComponentKey(
                    frameIndex,
                    worldName,
                    sceneName,
                    entityId,
                    component.Type.FullName,
                    component.Type.AssemblyName);
                _components.TryAdd(key, component);
                if (!string.IsNullOrWhiteSpace(component.Type.AssemblyName))
                {
                    _components.TryAdd(key.WithComponentAssemblyName(string.Empty), component);
                }
            }

            private void AddBlockIndex(
                int frameIndex,
                string worldName,
                string sceneName,
                GameDebugDumpComponentStoreBlock store)
            {
                var key = new PlaybackBlockKey(
                    frameIndex,
                    worldName,
                    sceneName,
                    store.Type.FullName,
                    store.Type.AssemblyName);
                AddBlockIndex(key, store);
                if (!string.IsNullOrWhiteSpace(store.Type.AssemblyName))
                {
                    AddBlockIndex(key.WithComponentAssemblyName(string.Empty), store);
                }
            }

            private void AddBlockIndex(
                PlaybackBlockKey key,
                GameDebugDumpComponentStoreBlock store)
            {
                _blocks.TryAdd(key, store);
                for (var row = 0; row < store.EntityIds.Length; row++)
                {
                    _blockRows.TryAdd(new PlaybackBlockEntityKey(key, store.EntityIds[row]), row);
                }
            }

            private static PlaybackComponentKey CreateComponentKey(GameDebugDumpPlaybackComponentRequest request)
            {
                return new PlaybackComponentKey(
                    request.FrameIndex,
                    request.WorldName,
                    request.SceneName,
                    request.EntityId,
                    request.ComponentTypeFullName,
                    request.ComponentAssemblyName ?? string.Empty);
            }

            private static PlaybackBlockKey CreateBlockKey(GameDebugDumpPlaybackComponentRequest request)
            {
                return CreateBlockKey(request, request.FrameIndex);
            }

            private static PlaybackBlockKey CreateBlockKey(
                GameDebugDumpPlaybackComponentRequest request,
                int frameIndex)
            {
                return new PlaybackBlockKey(
                    frameIndex,
                    request.WorldName,
                    request.SceneName,
                    request.ComponentTypeFullName,
                    request.ComponentAssemblyName ?? string.Empty);
            }
        }
    }
}
