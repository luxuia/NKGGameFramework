using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Gameplay
{

    public readonly struct GameplayTagConfigParseResult : IEquatable<GameplayTagConfigParseResult>
    {
        public readonly int TagsRegistered;
        public readonly int RedirectsRegistered;

        public GameplayTagConfigParseResult(int TagsRegistered, int RedirectsRegistered)
        {
            this.TagsRegistered = TagsRegistered;
            this.RedirectsRegistered = RedirectsRegistered;
        }

        public bool Equals(GameplayTagConfigParseResult other) => EqualityComparer<int>.Default.Equals(TagsRegistered, other.TagsRegistered) && EqualityComparer<int>.Default.Equals(RedirectsRegistered, other.RedirectsRegistered);

        public override int GetHashCode() => HashCode.Combine(TagsRegistered, RedirectsRegistered);

        public override bool Equals(object? obj) => obj is GameplayTagConfigParseResult other && Equals(other);

        public static bool operator ==(GameplayTagConfigParseResult left, GameplayTagConfigParseResult right) => left.Equals(right);
        public static bool operator !=(GameplayTagConfigParseResult left, GameplayTagConfigParseResult right) => !left.Equals(right);

        public void Deconstruct(out int tagsRegistered, out int redirectsRegistered)
        {
            tagsRegistered = TagsRegistered; redirectsRegistered = RedirectsRegistered;
        }
    }

    public static class GameplayTagConfigParser
    {
        public static GameplayTagConfigParseResult ApplyToRegistry(
            string configText,
            GameplayTagRegistry registry,
            string source = "")
        {
            NkgThrow.IfNull(configText);
            NkgThrow.IfNull(registry);

            var tagsRegistered = 0;
            var redirectsRegistered = 0;

            foreach (var rawLine in configText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] is ';' or '#' or '[')
                {
                    continue;
                }

                var keyEnd = line.IndexOf('=');
                if (keyEnd <= 0)
                {
                    continue;
                }

                var key = line[..keyEnd].Trim().TrimStart('+');
                var value = line[(keyEnd + 1)..].Trim();
                var fields = ParseTupleFields(value);

                if (key is "GameplayTagList" or "RestrictedGameplayTagList")
                {
                    if (!fields.TryGetValue("Tag", out var tagName))
                    {
                        continue;
                    }

                    registry.Register(
                        tagName,
                        source,
                        fields.GetValueOrDefault("DevComment", string.Empty),
                        isRestricted: key == "RestrictedGameplayTagList",
                        allowNonRestrictedChildren: ParseBool(fields.GetValueOrDefault("bAllowNonRestrictedChildren")));
                    tagsRegistered++;
                }
                else if (key == "GameplayTagRedirects")
                {
                    if (!fields.TryGetValue("OldTagName", out var oldTagName)
                        || !fields.TryGetValue("NewTagName", out var newTagName))
                    {
                        continue;
                    }

                    registry.RegisterRedirect(oldTagName, newTagName);
                    redirectsRegistered++;
                }
            }

            return new GameplayTagConfigParseResult(tagsRegistered, redirectsRegistered);
        }

        internal static IReadOnlyDictionary<string, string> ParseTupleFields(string value)
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            var tuple = value.Trim();

            if (tuple.StartsWith('(') && tuple.EndsWith(')'))
            {
                tuple = tuple[1..^1];
            }

            foreach (var part in SplitTuple(tuple))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = part[..separator].Trim();
                var fieldValue = part[(separator + 1)..].Trim().Trim('"');
                fields[key] = fieldValue;
            }

            return fields;
        }

        private static IEnumerable<string> SplitTuple(string tuple)
        {
            var start = 0;
            var inQuotes = false;

            for (var i = 0; i < tuple.Length; i++)
            {
                var ch = tuple[i];
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    yield return tuple[start..i].Trim();
                    start = i + 1;
                }
            }

            if (start <= tuple.Length)
            {
                yield return tuple[start..].Trim();
            }
        }

        private static bool ParseBool(string? value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.Ordinal);
        }
    }
}
