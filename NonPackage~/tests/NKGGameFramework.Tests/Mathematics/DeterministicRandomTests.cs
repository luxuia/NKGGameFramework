using NKGGameFramework.Mathematics;

namespace NKGGameFramework.Tests.Mathematics;

public sealed class DeterministicRandomTests
{
    [Fact]
    public void Same_seed_reproduces_identical_sequence()
    {
        var first = new DeterministicRandom(42);
        var second = new DeterministicRandom(42);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
        }
    }

    [Fact]
    public void Different_seeds_produce_different_sequences()
    {
        var first = new DeterministicRandom(1);
        var second = new DeterministicRandom(2);

        Assert.NotEqual(first.NextUInt64(), second.NextUInt64());
    }

    [Fact]
    public void Range_result_stays_within_half_open_interval()
    {
        var random = new DeterministicRandom(7);

        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(random.Range(-3, 5), -3, 4);
        }
    }

    [Fact]
    public void NextDouble_is_within_unit_interval()
    {
        var random = new DeterministicRandom(1);

        for (var i = 0; i < 1000; i++)
        {
            var value = random.NextDouble();
            Assert.True(value >= 0 && value < 1);
        }
    }

    [Fact]
    public void Fork_is_reproducible_from_same_parent_state()
    {
        var parent = new DeterministicRandom(99);
        var child = parent.Fork();

        var parentAgain = new DeterministicRandom(99);
        var childAgain = parentAgain.Fork();

        Assert.Equal(child.NextUInt64(), childAgain.NextUInt64());
    }

    [Fact]
    public void Zero_seed_is_not_stuck()
    {
        var random = new DeterministicRandom(0);

        Assert.NotEqual(0UL, random.NextUInt64());
    }
}
