using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Gameplay
{

    public sealed record GameplayTagDefinition(
        GameplayTag Tag,
        string Source = "",
        string DevComment = "",
        bool IsExplicit = true,
        bool IsRestricted = false,
        bool AllowNonRestrictedChildren = false);

    public readonly struct GameplayTagRedirect : IEquatable<GameplayTagRedirect>
    {
        public readonly GameplayTag OldTag;
        public readonly GameplayTag NewTag;

        public GameplayTagRedirect(GameplayTag OldTag, GameplayTag NewTag)
        {
            this.OldTag = OldTag;
            this.NewTag = NewTag;
        }

        public bool Equals(GameplayTagRedirect other) => EqualityComparer<GameplayTag>.Default.Equals(OldTag, other.OldTag) && EqualityComparer<GameplayTag>.Default.Equals(NewTag, other.NewTag);

        public override int GetHashCode() => HashCode.Combine(OldTag, NewTag);

        public override bool Equals(object? obj) => obj is GameplayTagRedirect other && Equals(other);

        public static bool operator ==(GameplayTagRedirect left, GameplayTagRedirect right) => left.Equals(right);
        public static bool operator !=(GameplayTagRedirect left, GameplayTagRedirect right) => !left.Equals(right);

        public void Deconstruct(out GameplayTag oldTag, out GameplayTag newTag)
        {
            oldTag = OldTag; newTag = NewTag;
        }
    }
}
