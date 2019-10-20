using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;
public class ScaledSpace : MonoBehaviour
{
    public float scaleFactor = 100000;
    public Transform mainCamera;
    public Transform scaledSpaceCamera;
    FloatingOrigin fo;



    void Start()
    {
        fo = GetComponent<FloatingOrigin>();
    }

    void Update()
    {
        if (fo)
            scaledSpaceCamera.position = (mainCamera.position + fo.distanceFromOriginalOrigin) / scaleFactor;
		else
			scaledSpaceCamera.position = mainCamera.position / scaleFactor;
        scaledSpaceCamera.rotation = mainCamera.rotation;
    }

}
