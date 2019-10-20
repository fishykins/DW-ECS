using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using DW.Worlds;

namespace Assets.DW.Modules.Game.Worlds.V1
{
    public struct Quad : IComponentData
    {
        public QuadPlane plane;
    }
}

