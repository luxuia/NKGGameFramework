using NKGGameFramework.Mathematics;

namespace NKGGameFramework.Tests.Mathematics;

public sealed class SpatialHash2DTests
{
    [Fact]
    public void Query_circle_returns_keys_within_radius()
    {
        var hash = new SpatialHash2D<int>(10);
        hash.Insert(1, new Vector2(0, 0));
        hash.Insert(2, new Vector2(5, 0));
        hash.Insert(3, new Vector2(50, 50));

        var results = new List<int>();
        hash.QueryCircle(new Vector2(0, 0), 10, results);

        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.DoesNotContain(3, results);
    }

    [Fact]
    public void Query_deduplicates_entries_spanning_multiple_cells()
    {
        var hash = new SpatialHash2D<int>(1);
        hash.Insert(1, new Vector2(0.5, 0.5), radius: 2);

        var results = new List<int>();
        hash.QueryCircle(new Vector2(0.5, 0.5), 3, results);

        Assert.Single(results);
        Assert.Equal(1, results[0]);
    }

    [Fact]
    public void Remove_excludes_key_from_queries()
    {
        var hash = new SpatialHash2D<int>(10);
        hash.Insert(1, new Vector2(0, 0));

        Assert.True(hash.Remove(1));
        Assert.False(hash.Contains(1));

        var results = new List<int>();
        hash.QueryCircle(Vector2.Zero, 10, results);

        Assert.Empty(results);
    }

    [Fact]
    public void Update_moves_key_to_new_cell()
    {
        var hash = new SpatialHash2D<int>(10);
        hash.Insert(1, new Vector2(0, 0));

        hash.Update(1, new Vector2(100, 100));

        var nearOrigin = new List<int>();
        hash.QueryCircle(Vector2.Zero, 10, nearOrigin);
        Assert.Empty(nearOrigin);

        var nearNew = new List<int>();
        hash.QueryCircle(new Vector2(100, 100), 10, nearNew);
        Assert.Single(nearNew);
        Assert.Equal(1, nearNew[0]);
    }

    [Fact]
    public void Inserting_duplicate_key_throws()
    {
        var hash = new SpatialHash2D<int>(10);
        hash.Insert(1, Vector2.Zero);

        Assert.Throws<InvalidOperationException>(() => hash.Insert(1, Vector2.One));
    }
}
