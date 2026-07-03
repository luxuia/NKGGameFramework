using Cysharp.Threading.Tasks;

namespace NKGGameFramework.Runtime;

public interface IEntityView
{
    long ViewId { get; }
}

public interface IPresentationService
{
    UniTask<IEntityView> BindAsync(object logicalEntity, string viewLocation, CancellationToken cancellationToken = default);
}

public readonly record struct PresentationObjectId(long Value)
{
    public static PresentationObjectId Root { get; } = new(0);
}

public readonly record struct PresentationResourceId(long Value);

public readonly record struct PresentationVector2(double X, double Y);

public readonly record struct PresentationColor(double R, double G, double B, double A);

public readonly record struct PresentationTransform2D(
    double X,
    double Y,
    double Rotation = 0,
    double ScaleX = 1,
    double ScaleY = 1);

public enum PresentationValueKind
{
    Bool,
    Integer,
    Float,
    String,
    Color,
    Vector2,
    Vector2Array,
    Resource
}

public readonly struct PresentationValue : IEquatable<PresentationValue>
{
    private readonly IReadOnlyList<PresentationVector2>? _vector2Array;

    private PresentationValue(
        PresentationValueKind kind,
        bool boolean = false,
        long integer = 0,
        double number = 0,
        string? text = null,
        PresentationColor color = default,
        PresentationVector2 vector2 = default,
        IReadOnlyList<PresentationVector2>? vector2Array = null,
        PresentationResourceId resourceId = default)
    {
        Kind = kind;
        Boolean = boolean;
        Integer = integer;
        Number = number;
        Text = text;
        Color = color;
        Vector2 = vector2;
        _vector2Array = vector2Array;
        ResourceId = resourceId;
    }

    public PresentationValueKind Kind { get; }

    public bool Boolean { get; }

    public long Integer { get; }

    public double Number { get; }

    public string? Text { get; }

    public PresentationColor Color { get; }

    public PresentationVector2 Vector2 { get; }

    public IReadOnlyList<PresentationVector2>? Vector2Array => _vector2Array;

    public PresentationResourceId ResourceId { get; }

    public static PresentationValue FromBool(bool value) => new(PresentationValueKind.Bool, boolean: value);

    public static PresentationValue FromInteger(long value) => new(PresentationValueKind.Integer, integer: value);

    public static PresentationValue FromFloat(double value) => new(PresentationValueKind.Float, number: value);

    public static PresentationValue FromString(string value) => new(PresentationValueKind.String, text: value ?? throw new ArgumentNullException(nameof(value)));

    public static PresentationValue FromColor(PresentationColor value) => new(PresentationValueKind.Color, color: value);

    public static PresentationValue FromVector2(PresentationVector2 value) => new(PresentationValueKind.Vector2, vector2: value);

    public static PresentationValue FromVector2Array(IReadOnlyList<PresentationVector2> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new PresentationValue(PresentationValueKind.Vector2Array, vector2Array: value.ToArray());
    }

    public static PresentationValue FromResource(PresentationResourceId value) => new(PresentationValueKind.Resource, resourceId: value);

    public bool Equals(PresentationValue other)
    {
        if (Kind != other.Kind ||
            Boolean != other.Boolean ||
            Integer != other.Integer ||
            !Number.Equals(other.Number) ||
            Text != other.Text ||
            !Color.Equals(other.Color) ||
            !Vector2.Equals(other.Vector2) ||
            !ResourceId.Equals(other.ResourceId))
        {
            return false;
        }

        return SequenceEqual(_vector2Array, other._vector2Array);
    }

    public override bool Equals(object? obj) => obj is PresentationValue other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        hash.Add(Boolean);
        hash.Add(Integer);
        hash.Add(Number);
        hash.Add(Text);
        hash.Add(Color);
        hash.Add(Vector2);
        hash.Add(ResourceId);
        if (_vector2Array is not null)
        {
            foreach (var point in _vector2Array)
            {
                hash.Add(point);
            }
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(PresentationValue left, PresentationValue right) => left.Equals(right);

    public static bool operator !=(PresentationValue left, PresentationValue right) => !left.Equals(right);

    private static bool SequenceEqual(IReadOnlyList<PresentationVector2>? left, IReadOnlyList<PresentationVector2>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}

[Flags]
public enum PresentationDirtyFlags
{
    None = 0,
    Created = 1 << 0,
    Destroyed = 1 << 1,
    Parent = 1 << 2,
    Transform = 1 << 3,
    Visible = 1 << 4,
    Properties = 1 << 5,
    Calls = 1 << 6
}

public readonly record struct PresentationFrameState(int Frame, int Score, int Lives, bool IsTerminal);

public readonly record struct PresentationResourceChange(
    PresentationResourceId Id,
    string Location,
    bool Release);

public readonly record struct PresentationMethodCall(
    string MethodName,
    IReadOnlyList<PresentationValue> Arguments);

public sealed class PresentationObjectChange
{
    public PresentationObjectChange(
        PresentationObjectId id,
        string typeName,
        string name,
        PresentationDirtyFlags dirty,
        PresentationObjectId? parent,
        PresentationTransform2D transform,
        bool visible,
        IReadOnlyDictionary<string, PresentationValue> properties,
        IReadOnlyList<PresentationMethodCall> calls)
    {
        Id = id;
        TypeName = typeName;
        Name = name;
        Dirty = dirty;
        Parent = parent;
        Transform = transform;
        Visible = visible;
        Properties = properties;
        Calls = calls;
    }

    public PresentationObjectId Id { get; }

    public string TypeName { get; }

    public string Name { get; }

    public PresentationDirtyFlags Dirty { get; }

    public PresentationObjectId? Parent { get; }

    public PresentationTransform2D Transform { get; }

    public bool Visible { get; }

    public IReadOnlyDictionary<string, PresentationValue> Properties { get; }

    public IReadOnlyList<PresentationMethodCall> Calls { get; }
}

public sealed class PresentationSyncBatch
{
    public PresentationSyncBatch(
        PresentationFrameState frame,
        IReadOnlyList<PresentationResourceChange> resources,
        IReadOnlyList<PresentationObjectChange> objects)
    {
        Frame = frame;
        Resources = resources;
        Objects = objects;
    }

    public PresentationFrameState Frame { get; }

    public IReadOnlyList<PresentationResourceChange> Resources { get; }

    public IReadOnlyList<PresentationObjectChange> Objects { get; }

    public bool HasChanges => Resources.Count > 0 || Objects.Count > 0;
}

public sealed class PresentationScene
{
    private readonly Dictionary<PresentationObjectId, PresentationObject> _objects = [];
    private readonly Dictionary<PresentationResourceId, string> _resources = [];
    private readonly List<PresentationResourceChange> _resourceChanges = [];

    public PresentationObject GetOrCreateObject(PresentationObjectId id, string typeName, string name = "")
    {
        if (_objects.TryGetValue(id, out var existing))
        {
            existing.EnsureType(typeName, name);
            return existing;
        }

        var created = new PresentationObject(id, typeName, name);
        _objects.Add(id, created);
        return created;
    }

    public PresentationObject GetObject(PresentationObjectId id)
    {
        return _objects.TryGetValue(id, out var existing)
            ? existing
            : throw new KeyNotFoundException($"Presentation object '{id.Value}' has not been created.");
    }

    public void DestroyObject(PresentationObjectId id)
    {
        if (_objects.TryGetValue(id, out var existing))
        {
            existing.Destroy();
        }
    }

    public void LoadResource(PresentationResourceId id, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        if (_resources.TryGetValue(id, out var existing) && existing == location)
        {
            return;
        }

        _resources[id] = location;
        _resourceChanges.Add(new PresentationResourceChange(id, location, Release: false));
    }

    public void ReleaseResource(PresentationResourceId id)
    {
        if (!_resources.Remove(id))
        {
            return;
        }

        _resourceChanges.Add(new PresentationResourceChange(id, string.Empty, Release: true));
    }

    public PresentationSyncBatch Flush(PresentationFrameState frame)
    {
        var resources = _resourceChanges.ToArray();
        _resourceChanges.Clear();

        var objects = new List<PresentationObjectChange>();
        var destroyed = new List<PresentationObjectId>();
        foreach (var item in _objects)
        {
            var change = item.Value.CreateChange();
            if (change is null)
            {
                continue;
            }

            objects.Add(change);
            if (change.Dirty.HasFlag(PresentationDirtyFlags.Destroyed))
            {
                destroyed.Add(item.Key);
            }
        }

        foreach (var id in destroyed)
        {
            _objects.Remove(id);
        }

        return new PresentationSyncBatch(frame, resources, objects);
    }
}

public sealed class PresentationObject
{
    private readonly Dictionary<string, PresentationValue> _properties = [];
    private readonly HashSet<string> _dirtyProperties = [];
    private readonly List<PresentationMethodCall> _calls = [];
    private PresentationDirtyFlags _dirty = PresentationDirtyFlags.Created;
    private PresentationObjectId? _parent;
    private PresentationTransform2D _transform = new(0, 0);
    private bool _visible = true;
    private bool _destroyed;

    internal PresentationObject(PresentationObjectId id, string typeName, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        Id = id;
        TypeName = typeName;
        Name = name ?? string.Empty;
    }

    public PresentationObjectId Id { get; }

    public string TypeName { get; private set; }

    public string Name { get; private set; }

    public PresentationObjectId? Parent => _parent;

    public PresentationTransform2D Transform => _transform;

    public bool Visible => _visible;

    public void SetParent(PresentationObjectId parent)
    {
        ThrowIfDestroyed();
        if (_parent == parent)
        {
            return;
        }

        _parent = parent;
        _dirty |= PresentationDirtyFlags.Parent;
    }

    public void SetTransform2D(double x, double y, double rotation = 0, double scaleX = 1, double scaleY = 1)
    {
        SetTransform2D(new PresentationTransform2D(x, y, rotation, scaleX, scaleY));
    }

    public void SetTransform2D(PresentationTransform2D transform)
    {
        ThrowIfDestroyed();
        if (_transform == transform)
        {
            return;
        }

        _transform = transform;
        _dirty |= PresentationDirtyFlags.Transform;
    }

    public void SetVisible(bool visible)
    {
        ThrowIfDestroyed();
        if (_visible == visible)
        {
            return;
        }

        _visible = visible;
        _dirty |= PresentationDirtyFlags.Visible;
    }

    public void SetProperty(string name, PresentationValue value)
    {
        ThrowIfDestroyed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_properties.TryGetValue(name, out var existing) && existing == value)
        {
            return;
        }

        _properties[name] = value;
        _dirtyProperties.Add(name);
        _dirty |= PresentationDirtyFlags.Properties;
    }

    public void Call(string methodName, IReadOnlyList<PresentationValue> arguments)
    {
        ThrowIfDestroyed();
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(arguments);
        _calls.Add(new PresentationMethodCall(methodName, arguments.ToArray()));
        _dirty |= PresentationDirtyFlags.Calls;
    }

    public void Destroy()
    {
        _destroyed = true;
        _dirty = PresentationDirtyFlags.Destroyed;
        _dirtyProperties.Clear();
        _calls.Clear();
    }

    internal void EnsureType(string typeName, string name)
    {
        ThrowIfDestroyed();
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        if (TypeName != typeName)
        {
            throw new InvalidOperationException($"Presentation object '{Id.Value}' was created as '{TypeName}' and cannot be reused as '{typeName}'.");
        }

        var normalizedName = name ?? string.Empty;
        if (Name != normalizedName)
        {
            Name = normalizedName;
            _dirty |= PresentationDirtyFlags.Created;
        }
    }

    internal PresentationObjectChange? CreateChange()
    {
        if (_dirty == PresentationDirtyFlags.None)
        {
            return null;
        }

        IReadOnlyDictionary<string, PresentationValue> properties = new Dictionary<string, PresentationValue>();
        if (_dirty.HasFlag(PresentationDirtyFlags.Properties) || _dirty.HasFlag(PresentationDirtyFlags.Created))
        {
            var changedProperties = _dirty.HasFlag(PresentationDirtyFlags.Created)
                ? _properties
                : _properties.Where(item => _dirtyProperties.Contains(item.Key));
            properties = changedProperties.ToDictionary(item => item.Key, item => item.Value);
        }

        var change = new PresentationObjectChange(
            Id,
            TypeName,
            Name,
            _dirty,
            _parent,
            _transform,
            _visible,
            properties,
            _calls.ToArray());

        if (!_destroyed)
        {
            _dirty = PresentationDirtyFlags.None;
            _dirtyProperties.Clear();
            _calls.Clear();
        }

        return change;
    }

    private void ThrowIfDestroyed()
    {
        if (_destroyed)
        {
            throw new ObjectDisposedException(nameof(PresentationObject), $"Presentation object '{Id.Value}' has been destroyed.");
        }
    }
}
