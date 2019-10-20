using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain.Foliage;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{

    public enum LODModeBehindCam { NotComputed, ComputeRender } //How are quads behind the camera handled?
    public enum Mode { Heightmap, Noise, Hybrid, Const, ComputeShader }
    public enum UVType { Cube, Quad, Legacy, LegacyContinuous }
    public class Planet : MonoBehaviour
    {

        //General
        public float radius = 10000;
        public QuaternionD rotation;
        public float[] detailDistances = { 50000, 25000, 12500, 6250, 3125 };
        public bool calculateMsds;
        public float[] detailMsds = { 0f, 0f, 0f, 0f, 0f };
        public LODModeBehindCam lodModeBehindCam = LODModeBehindCam.ComputeRender;
        public float behindCameraExtraRange;
        public Material planetMaterial;
        public UVType uvType = UVType.Legacy;
        public float uvScale = 1f;
        public bool[] generateColliders = { false, false, false, false, false, true };
        public float visSphereRadiusMod = 1f;
        public bool updateAllQuads = false;
        public int maxQuadsToUpdate = 250;
        public float rotationUpdateThreshold = 1f;
        public float recomputeQuadDistancesThreshold = 10f;
        public int quadsSplittingSimultaneously = 2;

        //Scaled Space
        public bool useScaledSpace;
        public bool createScaledSpaceCopy;
        public float scaledSpaceDistance = 1500f;
        public float scaledSpaceFactor = 100000f;
        public Material scaledSpaceMaterial;
        public GameObject scaledSpaceCopy;

        //Biomes
        public bool useBiomeMap;
        public Texture2D biomeMapTexture;
        public float[] textureHeights = { 0f, 0.01f, 0.02f, 0.75f, 1f };
        public byte[] textureIds = { 0, 1, 2, 3, 4, 5 };
        public bool useSlopeTexture;
        public float slopeAngle = 60;
        public byte slopeTexture = 5;

        //Terrain generation
        public Mode mode = Mode.Const;
        public float heightScale = 0.02f;
        public int heightmapSizeX = 8192;
        public int heightmapSizeY = 4096;
        public bool heightmap16bit;
        public TextAsset heightmapTextAsset;
        public bool useBicubicInterpolation = true;
        public ComputeShader computeShader;
        public TextAsset noiseSerialized;
        public float hybridModeNoiseDiv = 50f;
        public float constantHeight = 0f;

        //Detail/Grass generation
        public bool generateDetails;
        public bool generateGrass;
        public bool planetIsRotating;
        public Material grassMaterial;
        public int grassPerQuad = 10000;
        public int grassLevel = 5;
        public float detailDistance;
        public List<DetailMesh> detailMeshes = new List<DetailMesh>();
        public List<DetailPrefab> detailPrefabs = new List<DetailPrefab>();
        public bool generateFoliageInEveryBiome;
        public List<byte> foliageBiomes = new List<byte> { 1 };
        public int detailObjectsGeneratingSimultaneously = 3;

        //Misc
        public FloatingOrigin floatingOrigin;
        public bool hideQuads = true;
        public int numQuads;


        internal List<Quad> quads = new List<Quad>();
        internal Hashtable quadIndices = new Hashtable();
        internal UnityEngine.Plane[] viewPlanes;
        internal float radiusVisSphere;
        internal List<Quad> quadSplitQueue = new List<Quad>();
        internal bool initialized;
        internal float[] detailDistancesSqr;
        internal bool inScaledSpace;
        internal GameObject quadGO;
        internal float dtDisSqr;
        internal bool originMoved;
        internal bool overRotationThreshold;
        internal Vector3 worldToMeshVector;
        internal Mesh quadMesh;
        internal Vector3[] quadMeshVertices;
        internal int detailObjectsGenerating;
        internal bool usingLegacyUVType;
        internal float heightInv;

        Camera mainCamera;
        Transform mainCameraTr;
        List<GameObject> renderedQuadsPool = new List<GameObject>();
        List<Quad> quadsPool = new List<Quad>();
        Quad[] l0Quads;
        float radiusSqr;
        float radiusMaxSqr;
        Heightmap heightmap;
        internal Heightmap biomeMap;
        Quaternion oldCamRotation;
        Vector3 oldCamPosition;
        Vector3 oldPlanetPosition;
        QuaternionD oldPlanetRotation;
        Module noise;
        float scaledSpaceDisSqr;
        float rqdtSqr;
        Coroutine quadUpdateCV;
        SortingClass comparer;

        const int quadPoolMaxSize = 30;
        const int renderedQuadPoolMaxSize = 30;


        public void Start()
        {

            if (detailDistances.Length > generateColliders.Length - 1)
                throw new ArgumentOutOfRangeException("detailDistances, generateColliders", "Generate Colliders needs to be one longer than Detail Distances!");

            if (calculateMsds && detailDistances.Length != detailMsds.Length)
                throw new ArgumentOutOfRangeException("detailDistances, detailMsds", "Detail Distances and Detail Msds need to be the same size!");

            Initialize();

            if (useScaledSpace && createScaledSpaceCopy)
                CreateScaledSpaceCopy();

            StartCoroutine(InstantiatePlanet());

        }

        public void Initialize()
        {

            usingLegacyUVType = uvType == UVType.Legacy || uvType == UVType.LegacyContinuous;
            mainCamera = Camera.main;
            mainCameraTr = mainCamera.transform;
            comparer = new SortingClass();

            quadMesh = ((GameObject)Resources.Load("planeMesh")).GetComponent<MeshFilter>().sharedMesh;
            quadMeshVertices = quadMesh.vertices;

            if (!floatingOrigin)
            {
                floatingOrigin = GetComponent<FloatingOrigin>();
                if (floatingOrigin)
                {
                    Debug.LogError("Floating Origin found but not defined on planet! Make sure to define floating origin on each planet.");
                }
            }
            heightInv = 1f / heightScale;
            rotation = transform.rotation.ToQuaterniond();
            oldPlanetPosition = transform.position;
            oldPlanetRotation = rotation;

            if (useBiomeMap)
                biomeMap = new Heightmap(biomeMapTexture, true);

            if (mode == Mode.Noise || mode == Mode.Hybrid) //Deserializing Noise Module
            {
                MemoryStream stream = new MemoryStream(noiseSerialized.bytes);
                noise = Utils.DeserializeModule(stream);
            }

            quadGO = ((GameObject)Resources.Load("plane")); //Original renderQuad

            if (generateDetails && grassMaterial)
                ((GameObject)Resources.Load("Grass")).GetComponent<MeshRenderer>().material = grassMaterial;

            if (mode == Mode.Heightmap || mode == Mode.Hybrid)
            {
                heightmap = new Heightmap(heightmapSizeX, heightmapSizeY, heightmap16bit, useBicubicInterpolation, heightmapTextAsset);
            }

            radiusMaxSqr = radius * (heightInv + 1) / heightInv; //Squared values so sqrt is not required
            radiusMaxSqr *= radiusMaxSqr;
            radiusSqr = radius * radius;
            rqdtSqr = recomputeQuadDistancesThreshold * recomputeQuadDistancesThreshold;
            scaledSpaceDisSqr = scaledSpaceDistance * scaledSpaceDistance;
            dtDisSqr = detailDistance * detailDistance;
        }

        public void Reset()
        {
            noise = null;
            heightmap = null;
        }

        /// <summary>
        /// Creates the six base quads (one for each side of the spherified cube).
        /// </summary>
        IEnumerator InstantiatePlanet() //Instantiate quads and assign values
        {
            detailDistancesSqr = new float[detailDistances.Length];

            for (int i = 0; i < detailDistances.Length; i++)
                detailDistancesSqr[i] = detailDistances[i] * detailDistances[i];

            quads.Add(new Quad(Vector3.up, Quaternion.Euler(0, 180, 0))); 
            quads[0].plane = QuadPlane.YPlane;
            quads[0].position = Position.Front;
            quads[0].index = "01"; //Check QuadNeighbor.cs for an explaination of indices.

            quads.Add(new Quad(Vector3.down, Quaternion.Euler(180, 180, 0)));
            quads[1].plane = QuadPlane.YPlane;
            quads[1].position = Position.Back;
            quads[1].index = "21";

            quads.Add(new Quad(Vector3.forward, Quaternion.Euler(270, 270, 270)));
            quads[2].plane = QuadPlane.ZPlane;
            quads[2].position = Position.Front;
            quads[2].index = "03";

            quads.Add(new Quad(Vector3.back, Quaternion.Euler(270, 0, 0)));
            quads[3].plane = QuadPlane.ZPlane;
            quads[3].position = Position.Back;
            quads[3].index = "13";

            quads.Add(new Quad(Vector3.right, Quaternion.Euler(270, 0, 270)));
            quads[4].plane = QuadPlane.XPlane;
            quads[4].position = Position.Front;
            quads[4].index = "02";

            quads.Add(new Quad(Vector3.left, Quaternion.Euler(270, 0, 90)));
            quads[5].plane = QuadPlane.XPlane;
            quads[5].position = Position.Back;
            quads[5].index = "12";

            l0Quads = new Quad[6];

            for (int i = 0; i < quads.Count; i++)
            {
                l0Quads[i] = quads[i];
                quads[i].planet = this;
                quads[i].disabled = false;
                quadIndices.Add(quads[i].index, quads[i]);
            }

            foreach (Quad q in l0Quads)
            {
                q.GetNeighbors();
                q.Update(true);
                yield return new WaitUntil(() => q.initialized); //Waiting until Quad is initialized
                q.Update(true);

            }

            initialized = true;
        }

        void Update()
        {
            //Quad Split Queue
            if (quadSplitQueue.Count > 0) //Check if quads are in the queue
            {
                //Sorting quadSplitQueue based on quads level and distance to the camera. Quads of lowest distance to the camera and level are split first.
                if (quadSplitQueue.Count > quadsSplittingSimultaneously)
                    quadSplitQueue.Sort(quadsSplittingSimultaneously - 1, quadSplitQueue.Count - quadsSplittingSimultaneously, comparer);

                for (int i = 0; i < quadsSplittingSimultaneously; i++)
                {
                    if (quadSplitQueue.Count >= i + 1)
                    {
                        if (quadSplitQueue[i] == null)
                            quadSplitQueue.Remove(quadSplitQueue[i]);

                        if (!quadSplitQueue[i].isSplitting && quadSplitQueue[i].coroutine == null && quadSplitQueue[i].planet)
                            quadSplitQueue[i].coroutine = StartCoroutine(quadSplitQueue[i].Split());

                        if (quadSplitQueue[i].hasSplit) //Wait until quad has split, then spot is freed
                            quadSplitQueue.Remove(quadSplitQueue[i]);
                    }
                    else break;
                }
            }


            transform.rotation = (Quaternion)rotation;

            Vector3 camPos = mainCameraTr.position;
            Quaternion camRot = mainCameraTr.rotation;

            Vector3 trPos = transform.position;
            Quaternion trRot = transform.rotation;

            Vector3 relCamPos = camPos - trPos;

            //Vector used by Quads when computing distance
            worldToMeshVector = Quaternion.Inverse(trRot) * (mainCameraTr.position - trPos);

            numQuads = quads.Count;

            //radiusVisSphere is used by Quads to check if they are visible
            float camHeight = (camPos - trPos).sqrMagnitude;
            radiusVisSphere = (camHeight + (radiusMaxSqr - 2 * radiusSqr)) * visSphereRadiusMod;

            //if (Input.GetKeyDown(KeyCode.G))
            //{
            //    print("Update Quads");
            //    UpdateQuads(true);
            //}

            //Recompute quad positions if the planet rotation has changed
            overRotationThreshold = !QuaternionD.Equals(rotation, oldPlanetRotation);
            if (overRotationThreshold)
            {
                oldPlanetRotation = rotation;
                UpdatePosition();
            }

            //Quad positions are also recalculated when the camera has moved farther than the threshold
            bool changedViewport = (relCamPos - oldCamPosition).sqrMagnitude > rqdtSqr || overRotationThreshold || (camRot != oldCamRotation && lodModeBehindCam == LODModeBehindCam.NotComputed);

            
            if (changedViewport)
            {
                oldCamPosition = relCamPos;
                oldCamRotation = camRot;
            }

            if (scaledSpaceCopy)
            {
                scaledSpaceCopy.transform.rotation = trRot * Quaternion.Euler(0f, 90f, 0f); //Set scaledSpaceCopy rotation to planet's actual rotation
                if (floatingOrigin == null)
                    scaledSpaceCopy.transform.position = transform.position / scaledSpaceFactor;
                else
                    scaledSpaceCopy.transform.position = floatingOrigin.WorldSpaceToScaledSpace(transform.position, scaledSpaceFactor);
            }

            if (camHeight < scaledSpaceDisSqr * 1.5f || !useScaledSpace) //Update all quads when close to planet or not using scaled space
            {
                UpdateQuads(changedViewport); //If changedViewport = false, quads only check if any of their tasks on the CPU or GPU are done and apply them if so.
                                              //otherwise, they recalculate their distance to the camera and check if they are close enough to split or far enough to combine.
            }
            
            //If cameraViewPlanes are needed to check quad visibilty they are computed here.
            if (lodModeBehindCam == LODModeBehindCam.NotComputed)
                viewPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            //Move all quads if planet center has moved.
            if (trPos != oldPlanetPosition)
            {
                UpdatePosition();
                originMoved = true; //Used by foliage system
                oldPlanetPosition = trPos;
            }
            else
                originMoved = false;

            if (useScaledSpace && changedViewport)
            {
                if (camHeight > scaledSpaceDisSqr)
                    inScaledSpace = true;
                else
                    inScaledSpace = false;
            }

        }

        /// <summary>
        /// Recalculates the positions of all quads. Is called when the planet has been moved or rotated.
        /// </summary>
        internal void UpdatePosition()
        {
            for (int i = 0; i < quads.Count; i++)
            {
                Vector3d plPos = transform.position.ToVector3d(); //We need to do the math with doubles for increase accuracy. The final result is converted back to floats.
                if (quads[i].renderedQuad)
                {
                    quads[i].renderedQuad.transform.position = (Vector3)((rotation * quads[i].trPosition.ToVector3d() + rotation * quads[i].meshOffset.ToVector3d()) + plPos);
                    quads[i].renderedQuad.transform.rotation = rotation;
                }
            }
        }

        /// <summary>
        /// Updates all quads. When changedViewport = true, the Update process is done over multiple frames via a coroutine. The quads recompute their distances to the camera and can then split or combine.
        /// When changedViewport = false, the Process is done in one frame. Distances aren't recomputed, the quads just check if results from async processes are ready.
        /// </summary>
        void UpdateQuads(bool changedViewport)
        {
            for (int i = 0; i < quads.Count; i++)
            {
                if (!quads[i].disabled)
                    quads[i].Update();
                else
                    quads.Remove(quads[i]);
            }

            if (changedViewport && quadUpdateCV == null && !updateAllQuads)
            {
                //Coroutine for Updating viewport over multiple frames
                quadUpdateCV = StartCoroutine(UpdateChangedViewport());
            }
            else if (changedViewport)
            {
                //Fallback if UpdateChangedViewport Coroutine is too slow. This should optimally never be run. Increase maxQuadsToUpdate or recomputeQuadDistancesThreshold if this is run.
                if (quadUpdateCV != null)
                    StopCoroutine(quadUpdateCV);

                quadUpdateCV = null;

                for (int i = 0; i < quads.Count; i++)
                {
                    if (!quads[i].disabled)
                        quads[i].UpdateDistances();
                    else
                        quads.Remove(quads[i]);
                }
            }
        }

        /// <summary>
        /// Coroutine for Updating viewport over multiple frames.
        /// </summary>
        IEnumerator UpdateChangedViewport()
        {
            Quad[] quadsArray = quads.ToArray();

            for (int i = 0; i < quadsArray.Length; i++)
            {
                if (!quadsArray[i].disabled)
                    quadsArray[i].UpdateDistances();
                else
                    quads.Remove(quadsArray[i]);
                //Wait until next frame if maxQuadsToUpdate(perFrame) has been reached
                if (i % maxQuadsToUpdate == 0 && i != 0)
                    yield return null;
            }

            quadUpdateCV = null;
        }
        /// <summary>
        /// Returns a new Quad, either from the pool if possible or instantiated
        /// </summary>
        public Quad NewQuad(Vector3 trPosition, Quaternion rotation)
        {
            if (quadsPool.Count == 0)
                return new Quad(trPosition, rotation);
            else
            {
                Quad q = quadsPool[0];
                quadsPool.Remove(q);
                q.trPosition = trPosition;
                q.rotation = rotation;
                q.disabled = false;
                return q;
            }
        }
        /// <summary>
        /// Removes Quad, either moves it to the pool or destroys it
        /// </summary>
        public void RemoveQuad(Quad quad)
        {
            quad.disabled = true;

            if (quad.coroutine != null)
            {
                if (!quad.isSplitting)
                    detailObjectsGenerating--;
                StopCoroutine(quad.coroutine);
            }
            if (quad.isComputingOnGPU)
            {
                quad.computeBuffer.Dispose();
                quad.computeBuffer = null;
            }

            if (quad.children != null)
                for (int i = 0; i < quad.children.Length; i++)
                    RemoveQuad(quad.children[i]);


            if (quad.renderedQuad)
                RemoveRenderQuad(quad);

            Destroy(quad.mesh);
            quad.Reset();
            quads.Remove(quad);
            quadSplitQueue.Remove(quad);

            if (quad.index != null)
                quadIndices.Remove(quad.index);

            if (quadsPool.Count < quadPoolMaxSize)
                quadsPool.Add(quad);
        }

        /// <summary>
        /// Adds renderedQuad to a Quad, either from pool or instantiated.
        /// </summary>
        public GameObject RenderQuad(Quad quad)
        {
            GameObject rquad;

            if (renderedQuadsPool.Count == 0)
            {
                rquad = (GameObject)Instantiate(quadGO, transform.rotation * (quad.trPosition + quad.meshOffset) + transform.position, transform.rotation);
                rquad.GetComponent<MeshRenderer>().material = planetMaterial;
                if (hideQuads)
                    rquad.hideFlags = HideFlags.HideInHierarchy;
            }
            else
            {
                rquad = renderedQuadsPool[renderedQuadsPool.Count - 1];
                renderedQuadsPool.RemoveAt(renderedQuadsPool.Count - 1);

                if (rquad)
                {
                    rquad.transform.position = transform.rotation * (quad.trPosition + quad.meshOffset) + transform.position;
                    rquad.transform.rotation = transform.rotation;
                }
                else
                {
                    rquad = (GameObject)Instantiate(quadGO, transform.rotation * (quad.trPosition + quad.meshOffset) + transform.position, transform.rotation);
                    rquad.GetComponent<MeshRenderer>().material = planetMaterial;
                    if (hideQuads)
                        rquad.hideFlags = HideFlags.HideInHierarchy;
                }

            }
            rquad.GetComponent<MeshFilter>().mesh = quad.mesh;
            rquad.name = "Quad " + quad.index;

            if (generateColliders[quad.level])
                quad.collider = rquad.AddComponent<MeshCollider>();

            return rquad;
        }

        /// <summary>
        /// Removes renderedQuad, either moves it to pool or destroys it
        /// </summary>
        public void RemoveRenderQuad(Quad quad)
        {
            if (renderedQuadsPool.Count < renderedQuadPoolMaxSize)
            {
                Destroy(quad.renderedQuad.GetComponent<FoliageRenderer>());
                Destroy(quad.renderedQuad.GetComponent<MeshCollider>());
                renderedQuadsPool.Add(quad.renderedQuad);
                quad.renderedQuad.SetActive(false);
                quad.renderedQuad = null;
            }
            else
            {
                Destroy(quad.renderedQuad);
            }
        }
        /// <summary>
        /// Finds quad by index
        /// </summary>
        public Quad FindQuad(string index)
        {
            return (Quad)quadIndices[index];
        }

        /// <summary>
        /// Instantiates object on this planet.
        /// </summary>
        /// <param name="objToInst">the object to instantiate
        /// </param>
        /// <param name="LatLon">position
        /// </param>
        public GameObject InstantiateOnPlanet(GameObject objToInst, Vector2 LatLon, float offsetUp = 0f)
        {
            Vector3 xyz = transform.rotation * Utils.LatLonToXyz(LatLon, radius);
            return Instantiate(objToInst, xyz * ((heightInv + HeightAtXYZ(Quaternion.Inverse(transform.rotation) * (xyz / radius))) / heightInv) + (Vector3Down(xyz, true) * -offsetUp) + transform.position, RotationAtPosition(xyz, true));
        }

        /// <summary>
        /// Instantiates object on this planet.
        /// </summary>
        /// <param name="objToInst">the object to instantiate
        /// </param>
        /// <param name="pos">position, cartesian coordinates, x, y and z, ranging from -1 to 1, relative to planet
        /// </param>
        public GameObject InstantiateOnPlanet(GameObject objToInst, Vector3 pos, float offsetUp = 0f)
        {
            pos = pos * radius;
            return Instantiate(objToInst, pos * ((heightInv + HeightAtXYZ(Quaternion.Inverse(transform.rotation) * (pos / radius))) / heightInv) + (Vector3Down(pos, true) * -offsetUp) + transform.position, RotationAtPosition(pos, true));
        }

        /// <summary>
        /// Returns up vector at specific position on the planet.
        /// </summary>
        /// <param name="pos">position
        /// </param>
        public Quaternion RotationAtPosition(Vector3 pos, bool posRelativeToPlanet = false)
        {
            return Quaternion.LookRotation(-Vector3Down(pos, posRelativeToPlanet)) * Quaternion.Euler(90f, 0f, 0f);
        }
        /// <summary>
        /// Returns up vector at specific position on the planet. 
        /// </summary>
        /// <param name="LatLon">position
        /// </param>
        public Quaternion RotationAtPosition(Vector2 LatLon)
        {
            Vector3 pos = Utils.LatLonToXyz(LatLon, radius);

            return Quaternion.LookRotation(Vector3Down(pos, true)) * Quaternion.Euler(90f, 0f, 0f);
        }
        /// <summary>
        /// Returns a normalized Vector towards the planet center at position
        /// </summary>
        public Vector3 Vector3Down(Vector3 position, bool posRelativeToPlanet = false)
        {
            return posRelativeToPlanet ? -position.normalized : (transform.position - position).normalized;
        }


        /// <summary>
        /// Creates a copy of this planet in scaled space
        /// </summary>
        public void CreateScaledSpaceCopy()
        {
            if (!scaledSpaceCopy)
            {

                GameObject sphere = (GameObject)Resources.Load("scaledSpacePlanet");

                sphere = Instantiate(sphere, transform.position / scaledSpaceFactor, transform.rotation * Quaternion.Euler(0f, 90f, 0f));

                Mesh mesh;
                if (Application.isPlaying)
                    mesh = sphere.GetComponent<MeshFilter>().mesh;
                else
                    mesh = Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh);


                Vector3[] vertices = mesh.vertices;
                Color32[] colors = new Color32[vertices.Length];

                float sphereRadius = radius / scaledSpaceFactor;
                

                for (int i = 0; i < vertices.Length; i++)
                {
                    float height = HeightAtXYZ(Quaternion.Euler(0f, 90f, 0f) * vertices[i]);
                    vertices[i] *= (heightInv + height) / heightInv;
                    vertices[i] *= sphereRadius;
                }

                mesh.vertices = vertices;
                mesh.colors32 = colors;
                mesh.RecalculateBounds();
                NormalSolver.RecalculateNormals(mesh, 60);
                if (!Application.isPlaying)
                    sphere.GetComponent<MeshFilter>().mesh = mesh;
                sphere.GetComponent<Renderer>().material = scaledSpaceMaterial;
                sphere.name = transform.name + "_ScaledSpace";

                scaledSpaceCopy = sphere;
            }
        }
        /// <summary>
        /// Returns height at a point pos on the planet
        /// </summary>
        public float HeightAtXYZ(Vector3 pos)
        {
            if (mode == Mode.Heightmap || mode == Mode.Hybrid)
            {
                var result = heightmap.GetPosInterpolated(pos);

                if (mode != Mode.Hybrid)
                    return result;
                else
                    //return result * (hybridModeNoiseDiv - NoiseAtXYZ(pos)) / hybridModeNoiseDiv; //Blend
                    return 1 + noise.GetNoise(-pos.x, pos.y, -pos.z); //Add

            }
            else if (mode == Mode.Noise)
                return NoiseAtXYZ(pos);
            return constantHeight;

        }

        public float NoiseAtXYZ(Vector3 pos)
        {
            return ((noise.GetNoise(-pos.x, pos.y, -pos.z) + 1f) / 2f);
        }
    }
}

