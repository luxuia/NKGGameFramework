using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OdinSerializer;

namespace NKGGameFramework.Gameplay
{

    // Identifies an attribute by an immutable, case-sensitive name (e.g.
    // "MaxHealth"). Mirrors GameplayTag's Odin-serializable string wrapper so
    // attribute references can live in definitions and save payloads.
    public readonly struct GameplayAttribute : IComparable<GameplayAttribute>, IEquatable<GameplayAttribute>
    {

        public static readonly GameplayAttribute Empty = default;

        [OdinSerialize]
        private readonly string? _name;

        private GameplayAttribute(string name)
        {
            _name = name;
        }

        public string Name => _name ?? string.Empty;

        public bool IsValid => !string.IsNullOrEmpty(_name);

        public static GameplayAttribute From(string name)
        {
            NkgThrow.IfNullOrWhiteSpace(name);
            return new GameplayAttribute(name);
        }

        public static bool TryFrom(string? name, out GameplayAttribute attribute)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                attribute = default;
                return false;
            }

            attribute = new GameplayAttribute(name);
            return true;
        }

        public bool Equals(GameplayAttribute other) => string.Equals(_name, other._name, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is GameplayAttribute other && Equals(other);

        public override int GetHashCode() => _name is null ? 0 : _name.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(GameplayAttribute left, GameplayAttribute right) => left.Equals(right);

        public static bool operator !=(GameplayAttribute left, GameplayAttribute right) => !left.Equals(right);

        public int CompareTo(GameplayAttribute other) => string.CompareOrdinal(Name, other.Name);

        public override string ToString() => Name;
    }

    // Optional registry for validating attribute names used by definitions or
    // tooling. Core evaluation does not depend on it, matching GameplayTagRegistry.
    public sealed class GameplayAttributeRegistry
    {
        private readonly Dictionary<string, GameplayAttribute> _attributes = new(StringComparer.Ordinal);

        public GameplayAttribute Register(GameplayAttribute attribute)
        {
            if (!attribute.IsValid)
            {
                throw new ArgumentException("Attribute must be valid.", nameof(attribute));
            }

            _attributes[attribute.Name] = attribute;
            return attribute;
        }

        public GameplayAttribute Register(string name) => Register(GameplayAttribute.From(name));

        public bool TryGet(string name, out GameplayAttribute attribute)
        {
            NkgThrow.IfNullOrWhiteSpace(name);
            return _attributes.TryGetValue(name, out attribute);
        }

        public bool Contains(string name) => !string.IsNullOrWhiteSpace(name) && _attributes.ContainsKey(name);
    }
}
