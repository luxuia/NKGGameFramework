namespace NKGGameFramework.Mathematics;

// Uniform-grid spatial hash for broad-phase neighbor queries in 2D. Keyed by an
// opaque TKey (e.g. an ECS entity id). Engine-agnostic: systems own
// insert/remove/update and call QueryCircle to gather candidate neighbors.
public sealed class SpatialHash2D<TKey>
    where TKey : notnull
{
    private readonly Dictionary<(int X, int Y), List<Entry>> _cells = [];
    private readonly Dictionary<TKey, Entry> _entries = [];
    private readonly double _invCellSize;
    private int _queryStamp;

    public SpatialHash2D(double cellSize)
    {
        if (double.IsNaN(cellSize) || double.IsInfinity(cellSize) || cellSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be a positive finite number.");
        }

        _invCellSize = 1.0 / cellSize;
    }

    public int Count => _entries.Count;

    public bool Contains(TKey key) => _entries.ContainsKey(key);

    public void Insert(TKey key, Vector2 position, double radius = 0)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_entries.TryAdd(key, new Entry(key, position, radius)))
        {
            throw new InvalidOperationException($"Key '{key}' already exists in the spatial hash.");
        }

        HashEntry(_entries[key]);
    }

    public bool Remove(TKey key)
    {
        if (!_entries.Remove(key, out var entry))
        {
            return false;
        }

        UnhashEntry(entry);
        return true;
    }

    public bool Update(TKey key, Vector2 position, double radius = 0)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        UnhashEntry(entry);
        entry.Position = position;
        entry.Radius = radius;
        HashEntry(entry);
        return true;
    }

    public void QueryCircle(Vector2 center, double radius, List<TKey> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");
        }

        var stamp = ++_queryStamp;
        var minCell = CellAt(center.X - radius, center.Y - radius);
        var maxCell = CellAt(center.X + radius, center.Y + radius);

        for (var cy = minCell.Y; cy <= maxCell.Y; cy++)
        {
            for (var cx = minCell.X; cx <= maxCell.X; cx++)
            {
                if (!_cells.TryGetValue((cx, cy), out var list))
                {
                    continue;
                }

                foreach (var entry in list)
                {
                    if (entry.Stamp == stamp)
                    {
                        continue;
                    }

                    entry.Stamp = stamp;

                    var reach = radius + entry.Radius;
                    if (Vector2.DistanceSquared(center, entry.Position) <= reach * reach)
                    {
                        results.Add(entry.Key);
                    }
                }
            }
        }
    }

    public void Clear()
    {
        _cells.Clear();
        _entries.Clear();
    }

    private (int X, int Y) CellAt(double x, double y) =>
        ((int)Math.Floor(x * _invCellSize), (int)Math.Floor(y * _invCellSize));

    private void HashEntry(Entry entry)
    {
        entry.Cells.Clear();

        var minCell = CellAt(entry.Position.X - entry.Radius, entry.Position.Y - entry.Radius);
        var maxCell = CellAt(entry.Position.X + entry.Radius, entry.Position.Y + entry.Radius);

        for (var cy = minCell.Y; cy <= maxCell.Y; cy++)
        {
            for (var cx = minCell.X; cx <= maxCell.X; cx++)
            {
                var cell = (cx, cy);
                entry.Cells.Add(cell);

                if (!_cells.TryGetValue(cell, out var list))
                {
                    list = [];
                    _cells.Add(cell, list);
                }

                list.Add(entry);
            }
        }
    }

    private void UnhashEntry(Entry entry)
    {
        foreach (var cell in entry.Cells)
        {
            if (_cells.TryGetValue(cell, out var list))
            {
                list.Remove(entry);
                if (list.Count == 0)
                {
                    _cells.Remove(cell);
                }
            }
        }

        entry.Cells.Clear();
    }

    private sealed class Entry
    {
        public Entry(TKey key, Vector2 position, double radius)
        {
            Key = key;
            Position = position;
            Radius = radius;
        }

        public TKey Key { get; }

        public Vector2 Position;

        public double Radius;

        public int Stamp;

        public List<(int X, int Y)> Cells { get; } = [];
    }
}
