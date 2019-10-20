using Unity.Entities;

namespace DW.WorldsLegacy
{
    public struct WorldSpawnerData : IComponentData
    {
        public Entity worldPrefab;
        public Entity quadPrefab;
        public float radius;
    }
}