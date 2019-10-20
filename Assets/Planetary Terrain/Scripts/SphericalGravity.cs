using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

[RequireComponent(typeof(Planet))]
public class SphericalGravity : MonoBehaviour {
    public Transform[] objects;
    public float acceleration = -9.81f;
    float radius;

	void Start () {
        radius = GetComponent<Planet>().radius;
    }
	
	void FixedUpdate () {
        foreach(Transform go in objects)
        {
            Transform tr = go.transform;
            go.GetComponent<Rigidbody>().AddForce((transform.position - tr.position).normalized * -acceleration * go.GetComponent<Rigidbody>().mass * radius * radius / (transform.position - tr.position).sqrMagnitude);
        }
	}
}
