using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

namespace PlanetaryTerrain.Foliage
{
    /// <summary>
    /// Randomly generates points on the surface of a quad. The points are used for foliage.
    /// </summary>
    public class FoliageGenerator
    {

        public List<Vector3> positions;
        public List<Vector3> normals;
        public List<int> indicies;
        public Vector3 position;
        public Vector3 rquadPosition;
        MeshCollider collider;
        public Vector3 down;
        public Vector3 x, y;
        public Quaternion rotation;


        public void PointCloud(int grassNumber, float size)
        {

            for (int i = 0; i < grassNumber; ++i)
            {
                Vector3 origin = position;
                origin += x * size * Random.Range(-0.5f, 0.5f);
                origin += y * size * Random.Range(-0.5f, 0.5f);


                Ray ray = new Ray(origin, down);
                RaycastHit hit;

                if (collider.Raycast(ray, out hit, 5000f))
                {

                    /*if (genBiomes)
                    {
                        int index = mesh.triangles[hit.triangleIndex * 3];
                        float[] texture = new float[] { mesh.colors[index].r, mesh.colors[index].g, mesh.colors[index].b, mesh.colors[index].a, mesh.uv4[index].x, mesh.uv4[index].y };
                        byte biome = 0;
                        for (byte j = 0; j < texture.Length; j++)
                        {
                            if (texture[j] >= .5f)
                                biome = j;
                        }
                        generate = FoliageBiomes.Contains(biome);
                    }*/

                    origin = hit.point;

                    if (rotation != Quaternion.identity)
                        origin = Utils.RotateAroundPoint(origin, rquadPosition, rotation);

                    origin -= position;
                    origin -= rquadPosition;

                    indicies.Add(indicies.Count);
                    positions.Add(origin);
                    normals.Add(rotation * hit.normal);

                }

            }
        }

        public FoliageGenerator(Vector3 position, Quaternion rotation, Vector3 rquadPosition, MeshCollider collider, Vector3 down, Vector3 x, Vector3 y, int seed)
        {
            this.position = position;
            this.rquadPosition = rquadPosition;
            this.collider = collider;
            this.rotation = rotation;
            this.down = down;
            this.x = x;
            this.y = y;

            positions = new List<Vector3>();
            indicies = new List<int>();
            normals = new List<Vector3>();

            Random.InitState(seed);


        }

        public PosRot[] Positions(float fraction, DetailObject d)
        {

            int i = Mathf.RoundToInt((1f - fraction) * positions.Count);
            var posrots = new PosRot[(positions.Count - i)];

            Vector3 rdown = rotation * down;
            Quaternion rot = Quaternion.LookRotation(-rdown) * Quaternion.Euler(90f, 0f, 0f);
            Vector3 pos = position + (rdown * -d.meshOffsetUp);
            int j = 0;
            for (; i < positions.Count; i++)
            {
                posrots[j] = new PosRot(positions[i] + pos, rot);
                j++;
            }

            int x = Mathf.RoundToInt((1f - fraction) * positions.Count);
            int l = positions.Count - x;
            positions.RemoveRange(x, l);
            indicies.RemoveRange(x, l);
            normals.RemoveRange(x, l);
            return posrots;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.SetVertices(positions);
            mesh.SetIndices(indicies.ToArray(), MeshTopology.Points, 0);
            mesh.SetNormals(normals);

            positions.Clear();
            normals.Clear();
            indicies.Clear();

            return mesh;
        }

        public Mesh AddToMesh(Mesh mesh)
        {
            var mv = new List<Vector3>(mesh.vertices);
            var mn = new List<Vector3>(mesh.normals);
            var mi = new List<int>(mesh.GetIndices(0));

            mv.AddRange(positions);
            mn.AddRange(normals);


            int length = positions.Count - 1;
            for (int i = 0; i < length; i++)
                mi.Add(mi.Count);

            mesh.SetVertices(mv);
            mesh.SetNormals(mn);
            mesh.SetIndices(mi.ToArray(), MeshTopology.Points, 0);

            Clear();

            return mesh;
        }

        public void Clear()
        {
            positions = new List<Vector3>();
            indicies = new List<int>();
            normals = new List<Vector3>();
        }

    }
    [System.Serializable]
    public class DetailObject
    {
        public float meshFraction;
        public float meshOffsetUp;
        public PosRot[] posRots;

    }
    [System.Serializable]
    public class DetailMesh : DetailObject
    {
        public Vector3 meshScale;
        public Mesh mesh;
        public Material material;
        public bool useGPUInstancing;
        public bool isGrass;

        public DetailMesh(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;

            meshFraction = 0.001f;
            meshOffsetUp = 2.5f;
            meshScale = Vector3.one;
            useGPUInstancing = false;
            isGrass = false;
            posRots = new PosRot[0];
        }
        public DetailMesh(DetailMesh d)
        {
            mesh = d.mesh;
            material = d.material;
            meshFraction = d.meshFraction;
            meshOffsetUp = d.meshOffsetUp;
            meshScale = d.meshScale;
            useGPUInstancing = d.useGPUInstancing;
            isGrass = d.isGrass;
            posRots = new PosRot[0];
        }
        public DetailMesh()
        {
            mesh = null;
            material = null;
            meshFraction = 0.001f;
            meshOffsetUp = 2.5f;
            meshScale = Vector3.one;
            useGPUInstancing = false;
            isGrass = false;
            posRots = new PosRot[0];
        }
    }
    [System.Serializable]
    public class DetailPrefab : DetailObject
    {
        public GameObject prefab;

        public GameObject[] InstantiateObjects()
        {
            GameObject[] objects = new GameObject[posRots.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = MonoBehaviour.Instantiate(prefab, posRots[i].position, posRots[i].rotation);
            }

            return objects;
        }

        public DetailPrefab(GameObject prefab)
        {
            this.prefab = prefab;
        }


        public DetailPrefab(DetailPrefab d)
        {
            this.meshFraction = d.meshFraction;
            this.meshOffsetUp = d.meshOffsetUp;
            this.prefab = d.prefab;
        }

        public DetailPrefab() { }

    }


    [System.Serializable]
    public struct PosRot
    {
        public Vector3 position;
        public Quaternion rotation;


        public PosRot(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }


}
