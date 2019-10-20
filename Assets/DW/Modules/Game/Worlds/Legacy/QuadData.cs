using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using DW.Worlds;

namespace DW.WorldsLegacy
{
    public struct QuadData : IComponentData
    {
        public Entity world;
        public float3 transformPosition;
        public quaternion rotation;
        public QuadPlane plane;
        public WorldFace position;
    }
}