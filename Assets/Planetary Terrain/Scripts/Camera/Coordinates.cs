using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

namespace PlanetaryTerrain.Extra
{
    public class Coordinates : MonoBehaviour
    {

        public Vector2 latlon = new Vector2(90f, 0f);
        public Planet planet;
        public bool teleport;
        public float height = 1.0006f;

        void Start()
        {
            if (teleport)
                planet.transform.position = -Utils.LatLonToXyz(latlon, planet.radius) * height;
        }
        void Update()
        {
            latlon = Utils.XyzToLatLon(Quaternion.Inverse(planet.transform.rotation) * (transform.position - planet.transform.position), planet.radius);
        }
    }
}
