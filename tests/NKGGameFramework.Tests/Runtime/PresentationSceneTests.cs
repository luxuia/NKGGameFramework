using NKGGameFramework.Runtime;

namespace NKGGameFramework.Tests.Runtime;

public sealed class PresentationSceneTests
{
    [Fact]
    public void Flush_EmitsCreatedObjectOnceAndThenOnlyDirtyChanges()
    {
        var scene = new PresentationScene();
        var ball = scene.GetOrCreateObject(new PresentationObjectId(10), "Sprite2D", "Ball");
        ball.SetParent(PresentationObjectId.Root);
        ball.SetTransform2D(12, 24);
        ball.SetProperty("texture", PresentationValue.FromResource(new PresentationResourceId(4)));
        ball.SetVisible(true);

        var created = scene.Flush(new PresentationFrameState(1, 2, 3, IsTerminal: false));

        var createdObject = Assert.Single(created.Objects);
        Assert.True(createdObject.Dirty.HasFlag(PresentationDirtyFlags.Created));
        Assert.True(createdObject.Dirty.HasFlag(PresentationDirtyFlags.Parent));
        Assert.True(createdObject.Dirty.HasFlag(PresentationDirtyFlags.Transform));
        Assert.Single(createdObject.Properties);

        var unchanged = scene.Flush(new PresentationFrameState(2, 2, 3, IsTerminal: false));

        Assert.False(unchanged.HasChanges);

        scene.GetOrCreateObject(new PresentationObjectId(10), "Sprite2D", "Ball").SetTransform2D(18, 30);
        var moved = scene.Flush(new PresentationFrameState(3, 2, 3, IsTerminal: false));

        var movedObject = Assert.Single(moved.Objects);
        Assert.Equal(PresentationDirtyFlags.Transform, movedObject.Dirty);
        Assert.Empty(movedObject.Properties);
        Assert.Equal(18, movedObject.Transform.X);
        Assert.Equal(30, movedObject.Transform.Y);
    }

    [Fact]
    public void Flush_EmitsResourceAndDestroyChanges()
    {
        var scene = new PresentationScene();
        var resourceId = new PresentationResourceId(7);
        var objectId = new PresentationObjectId(15);
        scene.LoadResource(resourceId, "res://ball.png");
        scene.LoadResource(resourceId, "res://ball.png");
        scene.GetOrCreateObject(objectId, "Sprite2D", "Ball");

        var first = scene.Flush(new PresentationFrameState(1, 0, 1, IsTerminal: false));

        Assert.Single(first.Resources);
        Assert.False(first.Resources[0].Release);
        Assert.Single(first.Objects);

        scene.DestroyObject(objectId);
        scene.ReleaseResource(resourceId);
        var second = scene.Flush(new PresentationFrameState(2, 0, 1, IsTerminal: false));

        Assert.Single(second.Resources);
        Assert.True(second.Resources[0].Release);
        var destroyed = Assert.Single(second.Objects);
        Assert.Equal(PresentationDirtyFlags.Destroyed, destroyed.Dirty);

        var third = scene.Flush(new PresentationFrameState(3, 0, 1, IsTerminal: false));

        Assert.False(third.HasChanges);
    }
}
