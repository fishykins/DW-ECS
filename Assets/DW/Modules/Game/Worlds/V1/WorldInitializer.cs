using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using DW.Worlds;

namespace Assets.DW.Modules.Game.Worlds.V1
{
    class WorldInitializer : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        private float radius = 10;
        [SerializeField]
        private Mesh quadMesh;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            foreach (GameObject child in transform)
            {
                Destroy(child);
            }

            var quad = dstManager.CreateEntity();
            dstManager.AddComponentObject(quad, new Quad { plane = QuadPlane.XPlane });

            //var quad = new GameObject("quad");
            //quad.transform.SetParent(transform);

        }
    }
}
