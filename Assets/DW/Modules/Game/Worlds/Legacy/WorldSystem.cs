using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/*
namespace DW.Worlds
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class WorldSystem : JobComponentSystem
    {
        protected override void OnCreate()
        {

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new WorldJob
            {
                thing = 1
            }.ScheduleSingle(this, inputDeps);

            return job;
        }
    }

    #region Jobs
    struct WorldJob : IJobForEachWithEntity<WorldData, LocalToWorld>
    {
        public int thing;
        public void Execute(Entity entity, int index, [ReadOnly] ref WorldData world, [ReadOnly] ref LocalToWorld location)
        {
            
        }
    }
    #endregion
} */