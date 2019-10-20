using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using DW.Worlds;

namespace DW.WorldsLegacy
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class WorldSpawnerSystem : JobComponentSystem
    {
        #region Variables
        BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;
        #endregion


        #region Component Methds
        protected override void OnCreate()
        {
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SpawnWorldJob
            {
                CommandBuffer = entityCommandBufferSystem.CreateCommandBuffer()
            }.ScheduleSingle(this, inputDeps);

            entityCommandBufferSystem.AddJobHandleForProducer(job);

            return job;
        }
        #endregion

        #region Jobs
        struct SpawnWorldJob : IJobForEachWithEntity<WorldSpawnerData, LocalToWorld>
        {
            //Set from where the job is called. CommandBuffer is basically where we can safely create and remove ents
            public EntityCommandBuffer CommandBuffer;


            public void Execute(Entity entity, int index, [ReadOnly] ref WorldSpawnerData spawner, [ReadOnly] ref LocalToWorld location)
            {
                var world = CommandBuffer.Instantiate(spawner.worldPrefab);
                CommandBuffer.SetComponent<WorldData>(world, new WorldData
                {
                    radius = spawner.radius
                });

                #region Quads
                var quad = CommandBuffer.Instantiate(spawner.quadPrefab);
                CommandBuffer.AddComponent<QuadData>(quad, new QuadData
                {
                    world = world,
                    transformPosition = new float3(0f, 180f, 0f),
                    rotation = quaternion.Euler(0f, 180f, 0f),
                    plane = QuadPlane.YPlane,
                    position = DW.Worlds.WorldFace.Front
                });

                quad = CommandBuffer.Instantiate(spawner.quadPrefab);
                CommandBuffer.AddComponent<QuadData>(quad, new QuadData
                {
                    world = world,
                    transformPosition = new float3(0f, -180f, 0f),
                    rotation = quaternion.Euler(180f, 180f, 0f),
                    plane = QuadPlane.YPlane,
                    position = DW.Worlds.WorldFace.Back
                });

                quad = CommandBuffer.Instantiate(spawner.quadPrefab);
                CommandBuffer.AddComponent<QuadData>(quad, new QuadData
                {
                    world = world,
                    transformPosition = new float3(0f, 0f, 180f),
                    rotation = quaternion.Euler(270f, 270f, 270f),
                    plane = QuadPlane.ZPlane,
                    position = DW.Worlds.WorldFace.Front
                });

                quad = CommandBuffer.Instantiate(spawner.quadPrefab);
                CommandBuffer.AddComponent<QuadData>(quad, new QuadData
                {
                    world = world,
                    transformPosition = new float3(0f, 0f, -180f),
                    rotation = quaternion.Euler(270f, 0f, 0f),
                    plane = QuadPlane.ZPlane,
                    position = DW.Worlds.WorldFace.Back
                });

                quad = CommandBuffer.Instantiate(spawner.quadPrefab);
                CommandBuffer.AddComponent<QuadData>(quad, new QuadData
                {
                    world = world,
                    transformPosition = new float3(180f, 0f, 0f),
                    rotation = quaternion.Euler(270f, 0f, 270f),
                    plane = QuadPlane.XPlane,
                    position = DW.Worlds.WorldFace.Front
                });

                quad = CommandBuffer.Instantiate(spawner.quadPrefab);
                CommandBuffer.AddComponent<QuadData>(quad, new QuadData
                {
                    world = world,
                    transformPosition = new float3(-180f, 0f, 0f),
                    rotation = quaternion.Euler(270f, 0f, 90f),
                    plane = QuadPlane.XPlane,
                    position = DW.Worlds.WorldFace.Back
                });
                #endregion

                CommandBuffer.DestroyEntity(entity);
            }
        }
        #endregion
    }
}