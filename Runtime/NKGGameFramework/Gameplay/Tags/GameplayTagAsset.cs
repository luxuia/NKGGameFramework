using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Ecs;

namespace NKGGameFramework.Gameplay
{

    public interface IGameplayTagAsset
    {
        void GetOwnedGameplayTags(GameplayTagContainer tagContainer);
    }

    public static class GameplayTagAssetExtensions
    {
        public static GameplayTagContainer GetOwnedGameplayTags(this IGameplayTagAsset asset)
        {
            NkgThrow.IfNull(asset);

            var tags = new GameplayTagContainer();
            asset.GetOwnedGameplayTags(tags);
            return tags;
        }

        public static bool HasMatchingGameplayTag(this IGameplayTagAsset asset, GameplayTag tagToCheck)
        {
            return asset.GetOwnedGameplayTags().HasTag(tagToCheck);
        }

        public static bool HasAllMatchingGameplayTags(this IGameplayTagAsset asset, GameplayTagContainer tagContainer)
        {
            return asset.GetOwnedGameplayTags().HasAll(tagContainer);
        }

        public static bool HasAnyMatchingGameplayTags(this IGameplayTagAsset asset, GameplayTagContainer tagContainer)
        {
            return asset.GetOwnedGameplayTags().HasAny(tagContainer);
        }
    }

    public sealed class EntityGameplayTagAsset : IGameplayTagAsset
    {
        private readonly Entity _entity;

        public EntityGameplayTagAsset(Entity entity)
        {
            _entity = entity;
            Entity = entity;
        }

        public Entity Entity { get; }

        public void GetOwnedGameplayTags(GameplayTagContainer tagContainer)
        {
            NkgThrow.IfNull(tagContainer);
            GameplayTagUtility.AppendOwnedTags(Entity, tagContainer);
        }
    }
}
