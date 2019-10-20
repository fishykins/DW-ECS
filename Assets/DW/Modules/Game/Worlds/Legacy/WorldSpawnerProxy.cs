using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace DW.WorldsLegacy
{
    [RequiresEntityConversion]
    public class WorldSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject worldPrefab;
        public GameObject quadPrefab;
        public float radius;

        

        public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
        {
            gameObjects.Add(worldPrefab);
            gameObjects.Add(quadPrefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var spawnerData = new WorldSpawnerData
            {
                worldPrefab = conversionSystem.GetPrimaryEntity(worldPrefab),
                quadPrefab = conversionSystem.GetPrimaryEntity(quadPrefab),
                radius = radius
            };
            dstManager.AddComponentData(entity, spawnerData);
        }
    }
}