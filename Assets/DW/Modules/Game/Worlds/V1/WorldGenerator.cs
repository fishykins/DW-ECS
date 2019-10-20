using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using DW.Worlds;

namespace Assets.DW.Modules.Game.Worlds.V1
{
    class WorldGenerator : MonoBehaviour
    {
        [SerializeField]
        private float radius = 100f;
        [SerializeField]
        private Mesh quadMesh;
        [SerializeField]
        private Material material;

        public struct QuadInitData
        {
            public float3 transformPosition;
            public float3 rotation;
            public QuadPlane plane;
            public WorldFace face;

            public QuadInitData(float3 transformPosition, float3 rotation, QuadPlane plane, WorldFace face)
            {
                this.transformPosition = transformPosition;
                this.rotation = rotation;
                this.plane = plane;
                this.face = face;
            }
        }

        private void Awake()
        {
            EntityManager entityManager = World.Active.EntityManager;

            var dworld = entityManager.CreateEntity();
            entityManager.AddComponentData(dworld, new DWorld { radius = this.radius });

            GenerateQuads(entityManager);
        }

        private void GenerateQuads(EntityManager entityManager)
        {
            List<QuadInitData> initData = new List<QuadInitData>();

            initData.Add(new QuadInitData(new float3(0f, 180f, 0f), new float3(0f, 180f, 0f), QuadPlane.YPlane, WorldFace.Front));
            initData.Add(new QuadInitData(new float3(0f, -180f, 0f), new float3(180f, 180f, 0f), QuadPlane.YPlane, WorldFace.Back));
            initData.Add(new QuadInitData(new float3(0f, 0f, 180f), new float3(270f, 270f, 270f), QuadPlane.ZPlane, WorldFace.Front));
            initData.Add(new QuadInitData(new float3(0f, 0f, -180f), new float3(270f, 0f, 0f), QuadPlane.ZPlane, WorldFace.Back));
            initData.Add(new QuadInitData(new float3(180f, 0f, 0f), new float3(270f, 0f, 270f), QuadPlane.XPlane, WorldFace.Front));
            initData.Add(new QuadInitData(new float3(-180f, 0f, 0f), new float3(270f, 0f, 90f), QuadPlane.XPlane, WorldFace.Back));

            EntityArchetype quadArch = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(Rotation),
                typeof(RotationEulerXYZ),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(Quad)
            );

            foreach (var item in initData)
            {
                var quad = entityManager.CreateEntity(quadArch);
                entityManager.SetComponentData(quad, new Quad { face = item.face, plane = item.plane, rotation = Quaternion.Euler(item.rotation), transformPosition = item.transformPosition });
                entityManager.SetComponentData(quad, new Translation { Value = item.transformPosition / 10f });

                float3 radRot = new float3(math.radians(item.rotation.x), math.radians(item.rotation.y), math.radians(item.rotation.z));
                entityManager.SetComponentData(quad, new RotationEulerXYZ { Value = radRot });

                entityManager.SetSharedComponentData(quad, new RenderMesh { mesh = quadMesh, material = material });

                InitializeQuad(quad);
            }
        }

        private void InitializeQuad(Entity quad)
        {

        }
    }
}
