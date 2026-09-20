using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Ecs
{

    // Named, ref-returning projection over a fixed set of components on one entity.
    // A value-type convenience layer over Entity.Get<T>; property access re-fetches
    // the store's ref so mutations via `aspect.First.X = ...` persist.
    public readonly struct EntityAspect<TFirst, TSecond>
        where TFirst : struct, IComponent
        where TSecond : struct, IComponent
    {
        private readonly Scene _scene;
        private readonly Entity _entity;

        internal EntityAspect(Scene scene, Entity entity)
        {
            _scene = scene;
            _entity = entity;
        }

        public Entity Entity => _entity;

        public ref TFirst First => ref _scene.GetComponent<TFirst>(_entity);

        public ref TSecond Second => ref _scene.GetComponent<TSecond>(_entity);
    }

    public readonly struct EntityAspect<TFirst, TSecond, TThird>
        where TFirst : struct, IComponent
        where TSecond : struct, IComponent
        where TThird : struct, IComponent
    {
        private readonly Scene _scene;
        private readonly Entity _entity;

        internal EntityAspect(Scene scene, Entity entity)
        {
            _scene = scene;
            _entity = entity;
        }

        public Entity Entity => _entity;

        public ref TFirst First => ref _scene.GetComponent<TFirst>(_entity);

        public ref TSecond Second => ref _scene.GetComponent<TSecond>(_entity);

        public ref TThird Third => ref _scene.GetComponent<TThird>(_entity);
    }
}
