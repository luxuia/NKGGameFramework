using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Ecs;
using OdinSerializer;

namespace NKGGameFramework.Gameplay
{

    public struct GameplayTagComponent : IComponent
    {
        [OdinSerialize]
        private GameplayTagContainer? _tags;

        public GameplayTagComponent(GameplayTagContainer tags)
        {
            _tags = tags;
        }

        public GameplayTagContainer Tags => _tags ??= new GameplayTagContainer();
    }
}
