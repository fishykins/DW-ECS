using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace DW.WorldsLegacy
{
    [RequiresEntityConversion]
    public class WorldProxy : MonoBehaviour, IConvertGameObjectToEntity
    {

        public float radius;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new WorldData
            {
                radius = radius
            };
            dstManager.AddComponentData(entity, data);
        }
    }
}