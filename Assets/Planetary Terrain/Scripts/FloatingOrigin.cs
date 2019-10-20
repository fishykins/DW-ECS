using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PlanetaryTerrain
{
    [RequireComponent(typeof(Planet))]
    public class FloatingOrigin : MonoBehaviour
    {
        public float threshold = 6000f;
        public Transform player;
        public Transform[] objects;


        [HideInInspector]
        public Vector3 distanceFromOriginalOrigin = Vector3.zero;
        public List<Planet> planets = new List<Planet>();

        void Start()
        {
            if (!planets.Contains(GetComponent<Planet>()))
                planets.Add(GetComponent<Planet>());
        }
        void Update()
        {
            //Shifts origin if cameras distance from it is larger than the threshold.
            if ((player.position.magnitude > threshold) && (planets[0].initialized || planets[0].inScaledSpace))
            {
                print("moving origin");
                distanceFromOriginalOrigin += player.position;
                transform.position -= player.position;

                for (int i = 0; i < objects.Length; i++)
                    objects[i].position -= player.position;

                foreach (Planet p in planets)
                {
                    for (int i = 0; i < p.quads.Count; i++)
                        if (p.quads[i].renderedQuad)
                            p.quads[i].renderedQuad.transform.position -= player.position;
                }

                player.position = Vector3.zero;

            }

            if (Input.GetKeyDown(KeyCode.H))
                foreach (Planet p in planets)
                    p.UpdatePosition();

        }
        public Vector3 WorldSpaceToScaledSpace(Vector3 worldPos, float scaleFactor)
        {
            return (worldPos + distanceFromOriginalOrigin) / scaleFactor;
        }
    }
}
