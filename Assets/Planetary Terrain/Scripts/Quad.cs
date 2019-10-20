using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.Foliage;
using System.Text;


using UnityEngine.Rendering;

namespace PlanetaryTerrain
{
    public enum QuadPlane { XPlane, YPlane, ZPlane };
    public enum Position { Back, Front };

    public class Quad
    {
        public Planet planet;
        public Quad parent;
        public Quad[] children;
        public Quad[] neighbors;
        public bool4 configuration; //Configuration of quad edge fans in order right, left, down, up | 0000 : no quad edge fans | 1001 : quad edge fans on right and up edges
        public int level = 0;
        public string index;
        public QuadPlane plane = 0;
        public Position position = 0;
        public bool hasSplit = false;
        public bool isSplitting;
        public bool initialized;
        public Mesh mesh;
        public Vector3 trPosition;
        public Quaternion rotation;
        public GameObject renderedQuad;
        public Vector3 meshOffset;
        public bool disabled;
        public Coroutine coroutine;
        public float distance;
        public MeshCollider collider;
        public float msd; //mean squared deviation
        public string[] neighborIds;

        public FoliageRenderer foliageRenderer;
        bool visibleToCamera;
        bool4 configurationOld = bool4.True;
        float scale = 1f;
        Func<MeshData, MeshData> method;
        IAsyncResult cookie;
        internal byte uniformBiome; //255 = not uniform
        internal bool isComputingOnGPU;
        internal ComputeBuffer computeBuffer;
        internal AsyncGPUReadbackRequest gpuReadbackReq;



        Dictionary<int2, int[]> orderOfChildren = new Dictionary<int2, int[]>() {

                {new int2(0, 1), new int[] {2, 0, 1, 3}}, //{int2(plane, position), int[]{order of children}}
                {new int2(0, 0), new int[] {3, 1, 0, 2}},

                {new int2(1, 1), new int[] {1, 0, 2, 3}},
                {new int2(1, 0), new int[] {3, 2, 0, 1}},

                {new int2(2, 1), new int[] {3, 2, 0, 1}},
                {new int2(2, 0), new int[] {2, 3, 1, 0}},
            };

        /// <summary>
        /// Resets all variables. Used for pooling.
        /// </summary>
        public void Reset()
        {
            uniformBiome = 0;
            parent = null;
            children = null;
            neighbors = null;
            configuration = bool4.False;
            hasSplit = false;
            isSplitting = false;
            initialized = false;
            isComputingOnGPU = false;
            computeBuffer = null;
            configurationOld = bool4.True;
            collider = null;
            neighborIds = null;
            meshOffset = Vector3.zero;
            foliageRenderer = null;
            distance = Mathf.Infinity;
            coroutine = null;
            level = 1;
            msd = 0f;
            gpuReadbackReq = new AsyncGPUReadbackRequest();

        }



        #region MeshGeneration
        /// <summary>
        /// Creates a MeshData that is later applied to this Quad's mesh.
        /// </summary>
        MeshData SpherifyAndDisplace(MeshData md)
        {
            Vector3[] finalVerts = new Vector3[1089];
            float height = 0f;
            float[] texture;
            Vector3 down = Vector3.zero;
            Vector3 normalized = Vector3.zero;
            float slopeAngle = planet.slopeAngle * Mathf.Deg2Rad;

            float averageHeight = 0f;
            float[] heights = null;

            if (planet.calculateMsds)
                heights = new float[1089];

            double offsetX = 0, offsetY = 0;
            int levelConstant = 0;


            if (planet.uvType == UVType.Quad)
            {
                levelConstant = Utils.Pow(2, planet.detailDistances.Length - level);
            }
            else if (planet.uvType == UVType.Cube)
            {
                levelConstant = Utils.Pow(2, level);
            }

            if (planet.uvType == UVType.Cube)
            {
                char[] chars = index.Substring(2).ToCharArray();
                double p = 0.5 * planet.uvScale;
                for (int j = 0; j < chars.Length; j++)
                {
                    switch (chars[j])
                    {
                        case '0':
                            break;
                        case '1':
                            offsetX += p;
                            break;
                        case '2':
                            offsetY += p;
                            break;
                        case '3':
                            offsetX += p;
                            offsetY += p;
                            break;
                    }
                    p *= 0.5;

                }
            }

            if (planet.mode == Mode.ComputeShader)
            {
                meshOffset = md.vertices[0]; //Using first calculated vertex as mesh origin
                down = (Vector3.zero - meshOffset).normalized; //First vertex to calculate down vector

                for (int i = 0; i < 1089; i++)
                {

                    height = (md.vertices[i] + trPosition).magnitude / planet.radius;
                    height -= 1f;
                    height *= planet.heightInv;

                    if (planet.useBiomeMap)
                    {
                        normalized = md.vertices[i];
                        normalized /= (planet.heightInv + height) / planet.heightInv;
                        normalized += trPosition;
                        normalized /= planet.radius;
                        normalized /= 1.005f;
                    }

                    if (planet.calculateMsds)
                    {
                        averageHeight += height;
                        heights[i] = height;
                    }

                    md.vertices[i] -= meshOffset;

                    finalVerts[i] = md.vertices[i];

                    // Non-Legacy UV calculations
                    if (!planet.usingLegacyUVType)
                        switch (planet.uvType)
                        {
                            case UVType.Cube:

                                double x = (i / 33) / 32.0;
                                double y = (i % 33) / 32.0;

                                double scale = (double)planet.uvScale / levelConstant;

                                x *= scale;
                                y *= scale;
                                
                                x += (offsetX % 1);
                                y += (offsetY % 1);

                                y *= -1;
                                
                                md.uv[i] = new Vector2((float)x, (float)y);

                                break;

                            case UVType.Quad:
                                md.uv[i].x = (i / 33) / 32f;
                                md.uv[i].y = (i % 33) / 32f;

                                md.uv[i] *= levelConstant;

                                md.uv[i].y *= -1;
                                break;
                        }

                    if (planet.useBiomeMap)
                        texture = Utils.EvaluateTexture(planet.biomeMap.GetPosInterpolated(normalized), planet.textureHeights, planet.textureIds); //Getting biomes/textures at this elevation. Every float in the array is the intesity of one texture.
                    else
                        texture = Utils.EvaluateTexture(height, planet.textureHeights, planet.textureIds);

                    md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]); //Using color and uv4 channels to encode biome/texture data
                    md.uv2[i] = new Vector2(texture[4], texture[5]);

                    if (planet.generateDetails && !planet.generateFoliageInEveryBiome)
                        if (uniformBiome != 255) //Finding out if this Quad has one uniform biome, and if so, which one. 255 = not uniform.
                            for (byte j = 0; j < texture.Length; j++)
                                if (texture[j] >= .5f)
                                    if (i == 0)
                                        uniformBiome = j;
                                    else if (uniformBiome != j)
                                        uniformBiome = 255;

                }

                for (int i = 1089; i < md.vertices.Length; i++)
                {
                    md.vertices[i] -= meshOffset;
                }
            }

            else
            {
                for (int i = 0; i < 1089; i++)
                {
                    md.vertices[i] = GetPosition(md.vertices[i], out height, out normalized, out md.uv[i]);

                    if (planet.calculateMsds)
                    {
                        averageHeight += height;
                        heights[i] = height;
                    }

                    if (i == 0)
                    {
                        meshOffset = md.vertices[0]; //Using first calculated vertex as mesh origin
                        down = (Vector3.zero - meshOffset).normalized; //First vertex to calculate down vector
                    }

                    md.vertices[i] -= meshOffset;

                    finalVerts[i] = md.vertices[i];

                    // Non-Legacy UV calculations
                    if (!planet.usingLegacyUVType)
                        switch (planet.uvType)
                        {
                            case UVType.Cube:

                                //md.uv[i].x = (i / 33) / 32f;
                                //md.uv[i].y = (i % 33) / 32f;
                                //md.uv[i] *= planet.uvScale / levelConstant;
                                //md.uv[i] += offset;

                                //md.uv[i].y *= -1;

                                double x = (i / 33) / 32.0;
                                double y = (i % 33) / 32.0;

                                double scale = (double)planet.uvScale / levelConstant;

                                x *= scale;
                                y *= scale;

                                x += (offsetX % 1);
                                y += (offsetY % 1);

                                y *= -1;

                                md.uv[i] = new Vector2((float)x, (float)y);
                                break;

                            case UVType.Quad:
                                md.uv[i].x = (i / 33) / 32f;
                                md.uv[i].y = (i % 33) / 32f;

                                md.uv[i] *= levelConstant;

                                md.uv[i].y *= -1;
                                break;
                        }

                    if (planet.useBiomeMap)
                        texture = Utils.EvaluateTexture(planet.biomeMap.GetPosInterpolated(normalized), planet.textureHeights, planet.textureIds); //Getting biomes/textures at this elevation. Every float in the array is the intesity of one texture.
                    else
                        texture = Utils.EvaluateTexture(height, planet.textureHeights, planet.textureIds);

                    md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]); //Using color and uv4 channels to encode biome/texture data
                    md.uv2[i] = new Vector2(texture[4], texture[5]);

                    if (planet.generateDetails && !planet.generateFoliageInEveryBiome)
                        if (uniformBiome != 255) //Finding out if this Quad has one uniform biome, and if so, which one. 255 = not uniform.
                            for (byte j = 0; j < texture.Length; j++)
                                if (texture[j] >= .5f)
                                    if (i == 0)
                                        uniformBiome = j;
                                    else if (uniformBiome != j)
                                        uniformBiome = 255;


                }

                for (int i = 1089; i < md.vertices.Length; i++)
                {
                    md.vertices[i] = GetPosition(md.vertices[i]);
                    md.vertices[i] -= meshOffset;
                }
            }

            md.normals = CalculateNormals(ref md.vertices, ConstantTriArrays.trisExtendedPlane);
            md.vertices = finalVerts;

            if (planet.useSlopeTexture)
                for (int i = 0; i < 1089; i++)
                {
                    if (Mathf.Acos(Vector3.Dot(-down, md.normals[i])) > slopeAngle) //Using slope texture if slope is high enough
                    {
                        texture = new float[] { 0, 0, 0, 0, 0, 0 };
                        texture[planet.slopeTexture] = 1f;

                        md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]);
                        md.uv2[i] = new Vector2(texture[4], texture[5]);
                    }
                }


            if (planet.calculateMsds)
            {
                averageHeight /= 1089;

                for (int i = 0; i < 1089; i++)
                {
                    float deviation = (averageHeight - heights[i]);
                    msd += deviation * deviation;
                }
            }

            return md;
        }

        private Vector3[] CalculateNormals(ref Vector3[] vertices, int[] tris)
        {
            Vector3[] normals = new Vector3[vertices.Length];
            Vector3[] finalNormals = new Vector3[1089];

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 p1 = vertices[tris[i]];
                Vector3 p2 = vertices[tris[i + 1]];
                Vector3 p3 = vertices[tris[i + 2]];

                Vector3 l1 = p2 - p1;
                Vector3 l2 = p3 - p1;

                Vector3 normal = Vector3.Cross(l1, l2);

                normals[tris[i]] += normal;
                normals[tris[i + 1]] += normal;
                normals[tris[i + 2]] += normal;
            }

            for (int i = 0; i < 1089; i++)
            {
                finalNormals[i] = normals[i].normalized;
            }

            return finalNormals;
        }

        /// <summary>
        /// Position on unit cube to position on planet
        /// </summary>
        private Vector3 GetPosition(Vector3 vertex, out float height, out Vector3 normalized, out Vector2 uv)
        {
            vertex = vertex * scale; //Scaling down to subdivision level
            vertex = rotation * vertex; //Rotating so the vertices are on the unit cube. Planes that are fed into this function all face up.

            if (planet.usingLegacyUVType)
            {
                if (planet.uvType == UVType.LegacyContinuous)
                    vertex += trPosition; //Offsetting the plane. Now all vertices form a cube

                switch (plane)
                {
                    case QuadPlane.ZPlane:
                        uv = new Vector2(vertex.x, vertex.y);
                        break;
                    case QuadPlane.YPlane:
                        uv = new Vector2(vertex.x, vertex.z);
                        break;
                    case QuadPlane.XPlane:
                        uv = new Vector2(vertex.z, vertex.y);
                        break;
                    default:
                        uv = Vector2.zero;
                        break;
                }
                if (planet.uvType != UVType.LegacyContinuous)
                    vertex += trPosition;
            }
            else
            {
                vertex += trPosition;
                uv = Vector2.zero;
            }

            vertex.Normalize();//Normalizing the vertices. The cube now is a sphere.

            normalized = vertex;
            height = planet.HeightAtXYZ(vertex); //Getting height at vertex position
            vertex *= planet.radius; //Scaling up the sphere
            vertex -= trPosition; //Subtracting trPosition, center is now (0, 0, 0)
            vertex *= (planet.heightInv + height) / planet.heightInv; //Offsetting vertex from center based on height and inverse heightScale
            return vertex;
        }


        /// <summary>
        /// Position on unit cube to position on planet
        /// </summary>
        private Vector3 GetPosition(Vector3 vertex)
        {
            vertex = vertex * scale;
            vertex = rotation * vertex + trPosition;
            vertex.Normalize();
            float height = planet.HeightAtXYZ(vertex);
            vertex *= planet.radius;
            vertex -= trPosition;
            vertex *= (planet.heightInv + height) / planet.heightInv;
            return vertex;
        }

        private Vector3 GetNormalizedPosition(Vector3 vertex)
        {
            vertex = vertex * scale;
            vertex = rotation * vertex + trPosition;
            return vertex.normalized;
        }

        /// <summary>
        /// Applies MeshData to this Quad's mesh
        /// </summary>
        private void ApplyToMesh(MeshData md)
        {
            if (!mesh)
                mesh = new Mesh();
            else
                mesh.Clear();

            mesh.vertices = md.vertices;
            mesh.triangles = Utils.GetTriangles(configuration.ToString());
            mesh.colors32 = md.colors;
            mesh.uv = md.uv;
            mesh.uv4 = md.uv2;

            mesh.RecalculateBounds();
            mesh.normals = md.normals;
            //mesh.RecalculateNormals();
            initialized = true;

            if (renderedQuad)
                renderedQuad.GetComponent<MeshFilter>().mesh = mesh;
        }
        #endregion

        #region LOD

        /// <summary>
        /// Recalculates quad's distance to the camera. Can then split, combine or generate foliage based on new distance
        /// </summary>
        public void UpdateDistances()
        {

            if (mesh != null && initialized)
            {
                distance = mesh.bounds.SqrDistance(planet.worldToMeshVector - meshOffset); //Converting cameraPosition to local mesh position to use bounds.
                visibleToCamera = VisibleToCamera();
            }
            //LOD: Checking distances, deciding to split or combine
            if (level < planet.detailDistancesSqr.Length && initialized)
            {
                if (distance < planet.detailDistancesSqr[level] && visibleToCamera && (!planet.calculateMsds || msd >= planet.detailMsds[level]))
                    AddToQuadSplitQueue();

                if (distance > planet.detailDistancesSqr[level] || !visibleToCamera) //Combine as often as possible if invisible to camera
                {
                    RemoveFromQuadSplitQueue();
                    Combine();
                }
            }

            if (!renderedQuad && initialized && visibleToCamera && !hasSplit && !planet.inScaledSpace) //Create GameObject that will be visible in scene if visible
                renderedQuad = planet.RenderQuad(this);
            else if (renderedQuad && (!visibleToCamera || planet.inScaledSpace)) //Remove if the generated renderQuad is not visible
                planet.RemoveRenderQuad(this);

            if (renderedQuad && !renderedQuad.activeSelf && (level == 0 || parent.hasSplit))
                renderedQuad.SetActive(true);

        }


        /// <summary>
        /// Checks if mesh generation on other thread or on the GPU is finished. If so, it is applied to the mesh.
        /// </summary>
        public void Update()
        {
            if (cookie != null && cookie.IsCompleted)
            {
                MeshData result = method.EndInvoke(cookie);
                ApplyToMesh(result);
                UpdateDistances();
                cookie = null;
                method = null;
            }

            if (isComputingOnGPU && gpuReadbackReq.done)
            {
                isComputingOnGPU = false;

                if (gpuReadbackReq.hasError)
                {
                    computeBuffer.Dispose();
                    computeBuffer = null;
                    configurationOld = bool4.True;
                    GetNeighbors();
                }
                else
                {
                    var a = gpuReadbackReq.GetData<Vector3>().ToArray();
                    MeshData md = new MeshData(a, planet.quadMesh.normals, planet.quadMesh.uv);

                    //print(md.vertices.Length + ", [0]: " + md.vertices[0].ToString("F4") + ", [1089]: " + md.vertices[1089].ToString("F4"));
                    method = SpherifyAndDisplace;
                    cookie = method.BeginInvoke(md, null, null);
                    computeBuffer.Dispose();
                    computeBuffer = null;
                }
            }

            //Foliage Stuff:

            if (planet.generateDetails && (planet.generateFoliageInEveryBiome || planet.foliageBiomes.Contains(uniformBiome))) //Generating details if enabled and right biome.
            {
                if (level >= planet.grassLevel && foliageRenderer == null && renderedQuad && collider && distance < planet.dtDisSqr && planet.detailObjectsGenerating < planet.detailObjectsGeneratingSimultaneously)
                {
                    var down = planet.Vector3Down(renderedQuad.transform.position);
                    Ray ray = new Ray(collider.bounds.center - (down * 500), down);
                    RaycastHit hit;

                    if (collider.Raycast(ray, out hit, 5000f)) //Only start foliage generation if the collider is working, it needs a few frames to initialize
                    {
                        foliageRenderer = renderedQuad.AddComponent<FoliageRenderer>();
                        foliageRenderer.planet = planet;
                        foliageRenderer.quad = this;
                        planet.detailObjectsGenerating++;
                    }

                }
                if (foliageRenderer != null && distance > planet.dtDisSqr)
                {
                    MonoBehaviour.Destroy(foliageRenderer);
                    foliageRenderer = null;
                }
            }
        }


        /// <summary>
        /// Executes Update(), and UpdateDistances() if changedViewport == true
        /// </summary>
        public void Update(bool changedViewport)
        {
            Update();

            if (changedViewport)
                UpdateDistances();
        }

        private bool CanSplit()
        {
            for (int i = 0; i < neighbors.Length; i++)
                if (neighbors[i] != null && neighbors[i].level < level && level > 2)
                    return false;

            return true;
        }


        /// <summary>
        /// Finds this Quad's neighbors, then checks if edge fans are needed. If so, they are applied.
        /// </summary>
        public void GetNeighbors()
        {

            if (neighborIds == null) //Finding the IDs of all neigbors. This is only done once.
            {
                neighborIds = new string[4];
                for (int i = 0; i < 4; i++)
                {
                    neighborIds[i] = QuadNeighbor.GetNeighbor(index, i.ToString());
                }
            }
            neighbors = new Quad[4];
            for (int i = 0; i < neighbors.Length; i++) //Trying to find neighbors by id. If not there, neighbor has a lower subdivision level, last char of id is removed.
            {
                int j = 0;
                while (neighbors[i] == null && j < 4)
                {
                    neighbors[i] = planet.FindQuad(neighborIds[i].Substring(0, neighborIds[i].Length - j));
                    j++;
                }
                /*neighbors[i] = null;
                neighbors[i] = planet.FindQuad(neighborIds[i]);

                if (neighbors[i] == null)
                    neighbors[i] = planet.FindQuad(neighborIds[i].Substring(0, neighborIds[i].Length - 1));*/
            }

            configuration = bool4.False;

            for (int i = 0; i < neighbors.Length; i++) //Creating configuration based on neighbor levels.
            {
                if (neighbors[i] != null)
                {
                    if (neighbors[i].level == level - 1)
                        configuration[i] = true;
                    else
                        configuration[i] = false;
                }
                else
                    configuration[i] = false;
            }

            if (configuration != configurationOld) //Loading plane mesh and starting generation on another thread or GPU.
            {
                configurationOld = new bool4(configuration);

                if (!initialized)
                {
                    MeshData md = new MeshData(ConstantTriArrays.extendedPlane, planet.quadMesh.normals, planet.quadMesh.uv);

                    if (planet.mode == Mode.ComputeShader)
                    {
                        int kernelIndex = planet.computeShader.FindKernel("ComputePositions");

                        computeBuffer = new ComputeBuffer(1225, 12);
                        computeBuffer.SetData(ConstantTriArrays.extendedPlane);

                        planet.computeShader.SetFloat("scale", scale);
                        planet.computeShader.SetFloats("trPosition", new float[] { trPosition.x, trPosition.y, trPosition.z });
                        planet.computeShader.SetFloat("radius", planet.radius);
                        planet.computeShader.SetFloats("rotation", new float[] { rotation.x, rotation.y, rotation.z, rotation.w });
                        planet.computeShader.SetFloat("noiseDiv", 1f / planet.heightScale);

                        planet.computeShader.SetBuffer(kernelIndex, "dataBuffer", computeBuffer);

                        planet.computeShader.Dispatch(kernelIndex, 5, 1, 1);

                        gpuReadbackReq = AsyncGPUReadback.Request(computeBuffer);

                        isComputingOnGPU = true;
                    }
                    else
                    {
                        method = SpherifyAndDisplace;
                        cookie = method.BeginInvoke(md, null, null);
                    }
                }
                else
                {
                    if (mesh.vertices.Length > 0)
                        mesh.triangles = Utils.GetTriangles(configuration.ToString());
                    if (renderedQuad)
                        renderedQuad.GetComponent<MeshFilter>().mesh = mesh;
                }
            }
        }



        /// <summary>
        /// Splits the Quad, creates four smaller ones 
        /// </summary>
        public IEnumerator Split()
        {
            if (!hasSplit)
            {
                isSplitting = true;
                children = new Quad[4];

                int[] order = orderOfChildren[new int2((int)plane, (int)position)];

                //Creating children
                switch (plane)
                {
                    case QuadPlane.XPlane:

                        children[order[0]] = planet.NewQuad(new Vector3(trPosition.x, trPosition.y - 1f / 2f * scale, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[1]] = planet.NewQuad(new Vector3(trPosition.x, trPosition.y + 1f / 2f * scale, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[2]] = planet.NewQuad(new Vector3(trPosition.x, trPosition.y + 1f / 2f * scale, trPosition.z + 1f / 2f * scale), rotation);
                        children[order[3]] = planet.NewQuad(new Vector3(trPosition.x, trPosition.y - 1f / 2f * scale, trPosition.z + 1f / 2f * scale), rotation);
                        break;

                    case QuadPlane.YPlane:

                        children[order[0]] = planet.NewQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[1]] = planet.NewQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[2]] = planet.NewQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y, trPosition.z + 1f / 2f * scale), rotation);
                        children[order[3]] = planet.NewQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y, trPosition.z + 1f / 2f * scale), rotation);
                        break;

                    case QuadPlane.ZPlane:

                        children[order[0]] = planet.NewQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y - 1f / 2f * scale, trPosition.z), rotation);
                        children[order[1]] = planet.NewQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y - 1f / 2f * scale, trPosition.z), rotation);
                        children[order[2]] = planet.NewQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y + 1f / 2f * scale, trPosition.z), rotation);
                        children[order[3]] = planet.NewQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y + 1f / 2f * scale, trPosition.z), rotation);
                        break;
                }

                for (int i = 0; i < children.Length; i++)
                {
                    children[i].scale = scale / 2;
                    children[i].level = level + 1;
                    children[i].plane = plane;
                    children[i].parent = this;
                    children[i].planet = planet;
                    children[i].index = index + i;
                    children[i].position = position;
                    planet.quadIndices.Add(children[i].index, children[i]);
                }
                for (int i = 0; i < children.Length; i++)
                {
                    children[i].GetNeighbors();
                    planet.quads.Add(children[i]);
                    //children[i].Update(true);
                }


                for (int i = 0; i < children.Length; i++)
                {
                    //planet.quadIndices.Remove(children[i].index); 
                    yield return new WaitUntil(() => children[i].initialized);
                    children[i].Update(true);
                }

                for (int i = 0; i < children.Length; i++)
                {
                    //planet.quadIndices.Add(children[i].index, children[i]); 

                    if (children[i].renderedQuad)
                        children[i].renderedQuad.SetActive(true);
                }

                if (renderedQuad)
                    planet.RemoveRenderQuad(this);

                UpdateNeighbors();

                isSplitting = false;
                hasSplit = true;
                coroutine = null;
            }
        }

        private void AddToQuadSplitQueue()
        {

            if (!planet.quadSplitQueue.Contains(this) && !hasSplit)
                planet.quadSplitQueue.Add(this);

        }

        private void RemoveFromQuadSplitQueue()
        {

            if (planet.quadSplitQueue.Contains(this) && !isSplitting)
                planet.quadSplitQueue.Remove(this);

        }
        /// <summary>
        /// Update neighbors in all neighbors and all their children
        /// </summary>
        private void UpdateNeighbors()
        {
            if (neighbors != null)
                foreach (Quad q in neighbors)
                {
                    if (q != null)
                        q.GetNeighborsAll();
                }
        }
        /// <summary>
        /// Update neighbors in this Quad and all children
        /// </summary>
        public void GetNeighborsAll()
        {
            if (initialized)
            {
                if (children != null)
                    foreach (Quad q in children)
                        q.GetNeighborsAll();
                GetNeighbors();
                UpdateDistances();
            }
        }
        /// <summary>
        /// Removes all of this quad's children and reenables rendering
        /// </summary>
        private void Combine()
        {
            if (hasSplit && !isSplitting)
            {
                hasSplit = false;
                foreach (Quad q in children)
                {
                    if (q.hasSplit)
                        q.Combine();

                    planet.RemoveQuad(q);
                }
                children = null;
                UpdateNeighbors();
            }
        }
        /// <summary>
        /// Is this quad visible to the camera? Called when over Recompute Quad Threshold
        /// </summary>
        private bool VisibleToCamera()
        {
            if (distance <= planet.radiusVisSphere)
                if (planet.lodModeBehindCam == LODModeBehindCam.ComputeRender || Utils.TestPlanesAABB(planet.viewPlanes, planet.transform.TransformPoint(mesh.bounds.min + meshOffset), planet.transform.TransformPoint(mesh.bounds.max + meshOffset), true, planet.behindCameraExtraRange))
                    return true;
            return false;
        }
        #endregion

        /// <summary>
        /// Create a new Quad with trPosition and rotation
        /// </summary>
        public Quad(Vector3 position, Quaternion rotation)
        {
            this.trPosition = position;
            this.rotation = rotation;
        }

        private void print(object message)
        {
            Debug.Log(message);
        }
    }
}





