using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using DW.Worlds;
using Unity.Mathematics;

namespace Assets.DW.Modules.Game.Worlds.V1
{
    public struct Quad : IComponentData
    {
        public float3 transformPosition;
        public quaternion rotation;
        public QuadPlane plane;
        public WorldFace face;
    }
}

