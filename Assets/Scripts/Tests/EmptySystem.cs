using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Tests
{
	
#if NET_DOTS
    public class EmptySystem : ComponentSystem
    {
        protected override void OnUpdate()
        {

        }
        public new EntityQuery GetEntityQuery(params EntityQueryDesc[] queriesDesc)
        {
            return base.GetEntityQuery(queriesDesc);
        }

        public new EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
        {
            return base.GetEntityQuery(componentTypes);
        }
        public new EntityQuery GetEntityQuery(NativeArray<ComponentType> componentTypes)
        {
            return base.GetEntityQuery(componentTypes);
        }
        public BufferFromEntity<T> GetBufferFromEntity<T>(bool isReadOnly = false) where T : struct, IBufferElementData
        {
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return EntityManager.GetBufferFromEntity<T>(isReadOnly);
        }
    }
#else
	public class EmptySystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle dep) { return dep; }


		new public EntityQuery GetEntityQuery(params EntityQueryDesc[] queriesDesc)
		{
			return base.GetEntityQuery(queriesDesc);
		}

		new public EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
		{
			return base.GetEntityQuery(componentTypes);
		}
		new public EntityQuery GetEntityQuery(NativeArray<ComponentType> componentTypes)
		{
			return base.GetEntityQuery(componentTypes);
		}
	}
#endif
}