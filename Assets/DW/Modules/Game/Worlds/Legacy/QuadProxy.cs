using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace DW.WorldsLegacy
{
    [RequiresEntityConversion]
    public class QuadProxy : MonoBehaviour, IConvertGameObjectToEntity
    {

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new QuadData
            {

            };
            dstManager.AddComponentData(entity, data);
        }
    }
}