using NKGGameFramework.Runtime;

namespace NKGGameFramework.Adapter.Godot;

public sealed class GodotPresentationScene
{
    private readonly PresentationScene _scene = new();
    private PresentationFrameState _frame;

    public void BeginFrame(int frame, int score, int lives, bool isTerminal)
    {
        _frame = new PresentationFrameState(frame, score, lives, isTerminal);
    }

    public GodotPresentationNode CreateNode(int id, string typeName, string name = "")
    {
        var objectId = new PresentationObjectId(id);
        var node = _scene.GetOrCreateObject(objectId, typeName, name);
        return new GodotPresentationNode(node);
    }

    public GodotPresentationNode GetNode(GodotObjectId id)
    {
        var node = _scene.GetObject(new PresentationObjectId(id.Value));
        return new GodotPresentationNode(node);
    }

    public GodotResourceId LoadResource(int id, string path)
    {
        var resourceId = new GodotResourceId(id);
        _scene.LoadResource(new PresentationResourceId(resourceId.Value), path);
        return resourceId;
    }

    public void ReleaseResource(GodotResourceId id)
    {
        _scene.ReleaseResource(new PresentationResourceId(id.Value));
    }

    public GodotPresentationNode InstantiateScene(int id, GodotResourceId resourceId, string name = "")
    {
        var node = CreateNode(id, "PackedScene", name);
        node.SetProperty("__scene_resource", GodotVariant.FromResource(resourceId));
        return node;
    }

    public void Destroy(GodotObjectId id)
    {
        _scene.DestroyObject(new PresentationObjectId(id.Value));
    }

    public PresentationSyncBatch Flush()
    {
        return _scene.Flush(_frame);
    }

    public byte[] BuildBytes(bool captureText = false)
    {
        return GodotPresentationEncoder.Encode(Flush(), captureText).BuildBytes();
    }

    public string Build(bool captureText = false)
    {
        return GodotPresentationEncoder.Encode(Flush(), captureText).Build();
    }
}

public readonly struct GodotPresentationNode
{
    private readonly PresentationObject _object;

    internal GodotPresentationNode(PresentationObject presentationObject)
    {
        _object = presentationObject;
    }

    public GodotObjectId Id => new(checked((int)_object.Id.Value));

    public void SetParent(GodotObjectId parent)
    {
        _object.SetParent(new PresentationObjectId(parent.Value));
    }

    public void SetTransform2D(double x, double y, double rotation = 0, double scaleX = 1, double scaleY = 1)
    {
        _object.SetTransform2D(x, y, rotation, scaleX, scaleY);
    }

    public void SetVisible(bool visible)
    {
        _object.SetVisible(visible);
    }

    public void SetProperty(string propertyName, GodotVariant value)
    {
        _object.SetProperty(propertyName, ToPresentationValue(value));
    }

    public void Call(string methodName, params GodotVariant[] arguments)
    {
        _object.Call(methodName, arguments.Select(ToPresentationValue).ToArray());
    }

    public void Destroy()
    {
        _object.Destroy();
    }

    private static PresentationValue ToPresentationValue(GodotVariant value)
    {
        return value.Kind switch
        {
            GodotVariantKind.Color => PresentationValue.FromColor(new PresentationColor(
                value.Color.R,
                value.Color.G,
                value.Color.B,
                value.Color.A)),
            GodotVariantKind.PackedVector2Array => PresentationValue.FromVector2Array(
                (value.Vector2Array ?? throw new ArgumentException("PackedVector2Array variant requires points.", nameof(value)))
                    .Select(point => new PresentationVector2(point.X, point.Y))
                    .ToArray()),
            GodotVariantKind.String => PresentationValue.FromString(value.Text ?? throw new ArgumentException("String variant requires text.", nameof(value))),
            GodotVariantKind.Bool => PresentationValue.FromBool(value.Boolean),
            GodotVariantKind.Integer => PresentationValue.FromInteger(value.Integer),
            GodotVariantKind.Float => PresentationValue.FromFloat(value.Number),
            GodotVariantKind.Vector2 => PresentationValue.FromVector2(new PresentationVector2(value.Vector2.X, value.Vector2.Y)),
            GodotVariantKind.Resource => PresentationValue.FromResource(new PresentationResourceId(value.ResourceId.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unsupported Godot variant kind.")
        };
    }
}

public static class GodotPresentationEncoder
{
    public static GodotHostCommandBuffer Encode(PresentationSyncBatch batch, bool captureText = false)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var commands = new GodotHostCommandBuffer(captureText: captureText);
        commands.BeginFrame(batch.Frame.Frame, batch.Frame.Score, batch.Frame.Lives, batch.Frame.IsTerminal);

        foreach (var resource in batch.Resources)
        {
            var id = new GodotResourceId(checked((int)resource.Id.Value));
            if (resource.Release)
            {
                commands.ReleaseResource(id);
            }
            else
            {
                commands.LoadResource(id, resource.Location);
            }
        }

        foreach (var change in batch.Objects)
        {
            var id = checked((int)change.Id.Value);
            if (change.Dirty.HasFlag(PresentationDirtyFlags.Destroyed))
            {
                commands.DestroyObject(id);
                continue;
            }

            if (change.Dirty.HasFlag(PresentationDirtyFlags.Created))
            {
                commands.CreateNode(id, change.TypeName, change.Name);
            }

            if (change.Dirty.HasFlag(PresentationDirtyFlags.Parent) && change.Parent is { } parent)
            {
                commands.SetParent(id, checked((int)parent.Value));
            }

            if (change.Dirty.HasFlag(PresentationDirtyFlags.Transform))
            {
                commands.SetTransform2D(
                    id,
                    change.Transform.X,
                    change.Transform.Y,
                    change.Transform.Rotation,
                    change.Transform.ScaleX,
                    change.Transform.ScaleY);
            }

            if (change.Dirty.HasFlag(PresentationDirtyFlags.Visible))
            {
                commands.SetVisible(id, change.Visible);
            }

            foreach (var property in change.Properties)
            {
                commands.SetProperty(id, property.Key, ToGodotVariant(property.Value));
            }

            foreach (var call in change.Calls)
            {
                commands.CallMethod(id, call.MethodName, call.Arguments.Select(ToGodotVariant).ToArray());
            }
        }

        return commands;
    }

    private static GodotVariant ToGodotVariant(PresentationValue value)
    {
        return value.Kind switch
        {
            PresentationValueKind.Color => GodotVariant.FromColor(new GodotColor(
                value.Color.R,
                value.Color.G,
                value.Color.B,
                value.Color.A)),
            PresentationValueKind.Vector2Array => GodotVariant.FromPackedVector2Array(
                (value.Vector2Array ?? throw new ArgumentException("Vector2Array presentation value requires points.", nameof(value)))
                    .Select(point => new GodotVector2(point.X, point.Y))
                    .ToArray()),
            PresentationValueKind.String => GodotVariant.FromString(value.Text ?? throw new ArgumentException("String presentation value requires text.", nameof(value))),
            PresentationValueKind.Bool => GodotVariant.FromBool(value.Boolean),
            PresentationValueKind.Integer => GodotVariant.FromInteger(checked((int)value.Integer)),
            PresentationValueKind.Float => GodotVariant.FromFloat(value.Number),
            PresentationValueKind.Vector2 => GodotVariant.FromVector2(new GodotVector2(value.Vector2.X, value.Vector2.Y)),
            PresentationValueKind.Resource => GodotVariant.FromResource(new GodotResourceId(checked((int)value.ResourceId.Value))),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unsupported presentation value kind.")
        };
    }
}
