using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain.Foliage
{
    /// <summary>
    /// Generates foliage when added to a quad and then renders it.
    /// </summary>
    public class FoliageRenderer : MonoBehaviour
    {
        internal List<DetailMesh> detailObjects = new List<DetailMesh>();
        internal List<GameObject> spawnedPrefabs = new List<GameObject>();
        public Matrix4x4[][] matrices;
        internal Planet planet;
        internal Quad quad;
        Vector3 oldPosition, position;
        Quaternion oldRotation, rotation;
        bool initialized;
        bool generating;


        public void Initialize()
        {
            position = transform.position;
            rotation = transform.rotation;

            matrices = new Matrix4x4[detailObjects.Count][];
            RecalculateMatrices();

            oldPosition = position;
            oldRotation = rotation;

            initialized = true;
        }

        void Update()
        {
            if (initialized)
            {

                position = transform.position;
                rotation = transform.rotation;

                if (position != oldPosition || rotation != oldRotation)
                {

                    RecalculateMatrices();

                    oldPosition = position;
                    oldRotation = rotation;
                }

                for (int i = 0; i < matrices.Length; i++)
                {
                    if (!detailObjects[i].useGPUInstancing)
                        for (int j = 0; j < matrices[i].Length; j++)
                        {
                            Graphics.DrawMesh(detailObjects[i].mesh, matrices[i][j], detailObjects[i].material, 0);
                        }
                    else Graphics.DrawMeshInstanced(detailObjects[i].mesh, 0, detailObjects[i].material, matrices[i]);
                }
            }
        }

        /// <summary>
        /// Matrices are needed to render meshes or grass with DrawMesh()/DrawMeshInstanced(). They need to be recomputed when the quad has rotated or moved.
        /// </summary>
        void RecalculateMatrices()
        {
            for (int i = 0; i < matrices.Length; i++)
            {
                matrices[i] = ToMatrix4x4Array(detailObjects[i].posRots, detailObjects[i].meshScale);
            }
        }

        Matrix4x4[] ToMatrix4x4Array(PosRot[] posRots, Vector3 meshScale)
        {

            var matrices = new Matrix4x4[posRots.Length];

            if (rotation != Quaternion.identity)
            {
                for (int i = 0; i < matrices.Length; i++)
                {
                    matrices[i].SetTRS(Utils.RotateAroundPoint((posRots[i].position + position), position, rotation), posRots[i].rotation * rotation, meshScale);
                }

            } // Computation is simpler when quad is not rotated.
            else for (int i = 0; i < matrices.Length; i++)
                {
                    matrices[i].SetTRS(posRots[i].position + transform.position, posRots[i].rotation, meshScale);
                }

            return matrices;
        }

        public void Start()
        {
            generating = true;
            StartCoroutine(GenerateGrass());
        }

        public void OnDestroy()
        {
            if (generating)
                planet.detailObjectsGenerating--;

            while (spawnedPrefabs.Count != 0)
            {
                Destroy(spawnedPrefabs[0]);
                spawnedPrefabs.RemoveAt(0);
            }
        }

        public IEnumerator GenerateGrass()
        {

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

            MeshCollider collider = GetComponent<MeshCollider>();

            float size = meshRenderer.bounds.size.magnitude / Utils.sqrt2 * 1.2f;

            Vector3 down = planet.Vector3Down(meshRenderer.bounds.center);
            Vector3 x = Vector3.Cross(down, Vector3.right).normalized;

            Vector3 y = Vector3.Cross(x, down);
            Vector3 origin = meshRenderer.bounds.center + (down * -2500f);

            FoliageGenerator fm = new FoliageGenerator(origin, QuaternionD.Inverse(planet.rotation), transform.position, collider, down, x, y, int.Parse(quad.index.Substring(quad.index.Length - 7, 6)));

            int numberOfPoints = planet.grassPerQuad;
            const int pointsPerFrame = 1000;

            int length = (numberOfPoints / pointsPerFrame);

            if (length <= 1)
            {
                fm.PointCloud(numberOfPoints, size); //generate if points per quad is less than points per frame
                length = 0;
            }

            for (int i = 0; i < length; i++)
            {
                if (planet.originMoved)
                {
                    down = planet.Vector3Down(meshRenderer.bounds.center);
                    x = Vector3.Cross(down, Vector3.right).normalized;
                    y = Vector3.Cross(x, down);

                    origin = meshRenderer.bounds.center + (down * -500f);
                    //int.Parse(quad.index.Substring(Mathf.Max(0, quad.index.Length - 9)))
                    fm = new FoliageGenerator(origin, QuaternionD.Inverse(planet.rotation), transform.position, collider, down, x, y, System.DateTime.Now.Millisecond);
                    i = 0;
                    numberOfPoints = planet.grassPerQuad;
                }

                if (numberOfPoints - pointsPerFrame > 0)
                {
                    numberOfPoints -= pointsPerFrame;
                    fm.PointCloud(pointsPerFrame, size);

                    if (!planet.planetIsRotating)
                    {
                        yield return null;
                    }
                }
                else fm.PointCloud(numberOfPoints, size);
            }

            List<DetailPrefab> detailPrefabs = new List<DetailPrefab>();

            for (int i = 0; i < planet.detailMeshes.Count; i++)
            {
                if (planet.detailMeshes[i].meshFraction != 0f)
                {
                    DetailMesh dO;
                    dO = new DetailMesh(planet.detailMeshes[i]);
                    detailObjects.Add(dO);
                    dO.posRots = fm.Positions(planet.detailMeshes[i].meshFraction, planet.detailMeshes[i]);
                }
            }

            for (int i = 0; i < planet.detailPrefabs.Count; i++)
            {
                if (planet.detailPrefabs[i].meshFraction != 0f)
                {
                    DetailPrefab dO;// = new DetailObject(planet.detailObjects[i]);
                    dO = new DetailPrefab(planet.detailPrefabs[i]);
                    detailPrefabs.Add(dO);
                    dO.posRots = fm.Positions(planet.detailPrefabs[i].meshFraction, planet.detailPrefabs[i]);
                }
            }

            //All points that remain after mesh and prefab spawning are used for grass.
            if (planet.generateGrass)
            {
                DetailMesh grass = new DetailMesh(fm.CreateMesh(), planet.grassMaterial);
                grass.posRots = new PosRot[] { new PosRot(fm.position, Quaternion.Euler(0f, 0f, 0f)) };
                grass.meshFraction = 0f;
                grass.isGrass = true;
                detailObjects.Add(grass);
            }

            for (int i = 0; i < detailPrefabs.Count; i++)
            {
                for (int j = 0; j < detailPrefabs[i].posRots.Length; j++)
                {
                    spawnedPrefabs.Add(Instantiate(detailPrefabs[i].prefab, detailPrefabs[i].posRots[j].position + transform.position, detailPrefabs[i].posRots[j].rotation * transform.rotation, transform));
                }
            }

            planet.detailObjectsGenerating--;
            generating = false;
            Initialize();
        }
    }
}
