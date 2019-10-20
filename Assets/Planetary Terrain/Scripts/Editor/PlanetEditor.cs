using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlanetaryTerrain.Foliage;
namespace PlanetaryTerrain.EditorUtils
{
    [CustomEditor(typeof(Planet))]
    public class PlanetEditor : Editor
    {
        public enum Tab
        {
            General, Generation, Visual, Foliage, Debug
        }
        Tab tab = Tab.General;
        Planet planet;
        SerializedProperty detailDistances, generateColliders, detailMSDs;
        SerializedProperty scaledSpaceMaterial, planetMaterial;
        SerializedProperty heightmap;
        SerializedProperty heights, textureIds;
        SerializedProperty detailObjects;
        SerializedProperty foliageBiomes;


        string[] tabNames = { "General", "Generation", "Visual", "Foliage", "Debug" };
        public void OnEnable()
        {
            planet = (Planet)target;

            detailDistances = serializedObject.FindProperty("detailDistances");
            generateColliders = serializedObject.FindProperty("generateColliders");
            detailMSDs = serializedObject.FindProperty("detailMsds");
            scaledSpaceMaterial = serializedObject.FindProperty("scaledSpaceMaterial");
            planetMaterial = serializedObject.FindProperty("planetMaterial");
            heights = serializedObject.FindProperty("textureHeights");
            textureIds = serializedObject.FindProperty("textureIds");
            foliageBiomes = serializedObject.FindProperty("foliageBiomes");

        }
        public override void OnInspectorGUI()
        {

            EditorGUILayout.Space();
            tab = (Tab)GUILayout.Toolbar((int)tab, tabNames, EditorStyles.toolbarButton);
            EditorGUILayout.Space();

            switch (tab)
            {
                case Tab.General:

                    planet.radius = EditorGUILayout.FloatField("Radius", planet.radius);
                    EditorGUILayout.PropertyField(detailDistances, true);
                    planet.calculateMsds = EditorGUILayout.Toggle(new GUIContent("Calculate MSDs", "The MSD is the bumpiness of the quad. When calculated, bumpyness thresholds can be set for splitting quads."), planet.calculateMsds);
                    if (planet.calculateMsds)
                        EditorGUILayout.PropertyField(detailMSDs, true);
                    EditorGUILayout.PropertyField(generateColliders, true);
                    GUILayout.Space(5f);
                    planet.lodModeBehindCam = (LODModeBehindCam)EditorGUILayout.EnumPopup(new GUIContent("LOD Mode behind Camera", "How are quads behind the camera handled?"), planet.lodModeBehindCam);
                    if (planet.lodModeBehindCam == LODModeBehindCam.NotComputed)
                        planet.behindCameraExtraRange = EditorGUILayout.FloatField(new GUIContent("LOD Extra Range", "Extra Range for quads behind the Camera. Increase for large planets."), planet.behindCameraExtraRange);
                    GUILayout.Space(5f);
                    planet.recomputeQuadDistancesThreshold = EditorGUILayout.FloatField(new GUIContent("Recompute Quad Threshold", "Threshold for recomputing all quad distances. Increase for better performance while moving with many quads."), planet.recomputeQuadDistancesThreshold);
                    planet.rotationUpdateThreshold = EditorGUILayout.FloatField(new GUIContent("Rotation Update Threshold", "Degrees of rotation after which Quads are updated."), planet.rotationUpdateThreshold);
                    planet.updateAllQuads = EditorGUILayout.Toggle(new GUIContent("Update all Quads simultaneously", "Update all Quads in one frame or over multiple frames? Only turn on when player is very fast and planet has few quads."), planet.updateAllQuads);
                    if (!planet.updateAllQuads)
                        planet.maxQuadsToUpdate = EditorGUILayout.IntField(new GUIContent("Max Quads to update per frame", "Max Quads to update in one frame. Lower value means process of updating all Quads takes longer, fewer spikes of lower framerates. If it takes too long, the next update tries to start while the last one is still running, warning and suggestion to increase maxQuadsToUpdate will be logged."), planet.maxQuadsToUpdate);
                    planet.floatingOrigin = (FloatingOrigin)EditorGUILayout.ObjectField("Floating Origin (if used)", planet.floatingOrigin, typeof(FloatingOrigin), true);
                    planet.hideQuads = EditorGUILayout.Toggle("Hide Quads in Hierarchy", planet.hideQuads);
                    break;

                case Tab.Generation:
                    planet.mode = (Mode)EditorGUILayout.EnumPopup("Generation Mode", planet.mode);
                    GUILayout.Space(5f);

                    switch (planet.mode)
                    {
                        case Mode.Heightmap:
                            Heightmap();
                            break;

                        case Mode.Noise:
                            planet.noiseSerialized = (TextAsset)EditorGUILayout.ObjectField("Noise", planet.noiseSerialized, typeof(TextAsset), false);
                            break;

                        case Mode.Hybrid:
                            Heightmap();
                            GUILayout.Space(5f);
                            planet.noiseSerialized = (TextAsset)EditorGUILayout.ObjectField("Noise", planet.noiseSerialized, typeof(TextAsset), false);

                            GUILayout.Space(5f);
                            planet.hybridModeNoiseDiv = EditorGUILayout.FloatField(new GUIContent("Noise Divisor", "Increase for noise to be less pronounced."), planet.hybridModeNoiseDiv);
                            break;
                        case Mode.Const:
                            planet.constantHeight = EditorGUILayout.FloatField("Constant Height", planet.constantHeight);
                            break;

                        case Mode.ComputeShader:
                            planet.computeShader = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader", planet.computeShader, typeof(ComputeShader), false);
                            break;
                    }

                    GUILayout.Space(10f);
                    planet.heightScale = EditorGUILayout.FloatField("Height Scale", planet.heightScale);
                    planet.useScaledSpace = EditorGUILayout.Toggle("Use Scaled Space", planet.useScaledSpace);
                    if (planet.useScaledSpace)
                    {
                        planet.createScaledSpaceCopy = EditorGUILayout.Toggle("Create Scaled Space Copy", planet.createScaledSpaceCopy);
                        planet.scaledSpaceFactor = EditorGUILayout.FloatField("Scaled Space Factor", planet.scaledSpaceFactor);
                        if (GUILayout.Button(new GUIContent("Create Scaled Space Copy", "Will be done at runtime if enabled and not yet generated.")))
                        {
                            planet.Initialize();
                            planet.CreateScaledSpaceCopy();
                            planet.Reset();
                        }

                    }
                    planet.quadsSplittingSimultaneously = EditorGUILayout.IntField(new GUIContent("Quads Splitting Simultaneously", "Number of quads that can split at the same time. Higher means shorter loading time but more CPU usage."), planet.quadsSplittingSimultaneously);
                    break;

                case Tab.Visual:
                    EditorGUILayout.PropertyField(planetMaterial);
                    planet.uvType = (UVType)EditorGUILayout.EnumPopup("UV Type", (System.Enum)planet.uvType);
                    if (planet.uvType == UVType.Cube)
                        planet.uvScale = EditorGUILayout.FloatField("UV Scale", planet.uvScale);
                    if (planet.useScaledSpace)
                    {
                        planet.scaledSpaceDistance = EditorGUILayout.FloatField(new GUIContent("Scaled Space Distance", "Distance at which the planet disappears and the Scaled Space copy of the planet is shown if enabled."), planet.scaledSpaceDistance);
                        if (planet.createScaledSpaceCopy)
                            EditorGUILayout.PropertyField(scaledSpaceMaterial);
                    }
                    GUILayout.Space(5f);

                    planet.useBiomeMap = EditorGUILayout.Toggle(new GUIContent("Use biome map", "Override height map used for texture selection."), planet.useBiomeMap);
                    if (planet.useBiomeMap)
                        planet.biomeMapTexture = (Texture2D)EditorGUILayout.ObjectField("Biome Map Texture", planet.biomeMapTexture, typeof(Texture2D), false);

                    GUILayout.Space(5f);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(textureIds, true);
                    EditorGUILayout.PropertyField(heights, true);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5f);
                    planet.useSlopeTexture = EditorGUILayout.Toggle("Use Slope Texture", planet.useSlopeTexture);
                    if (planet.useSlopeTexture)
                    {
                        planet.slopeAngle = EditorGUILayout.Slider("Slope Angle", planet.slopeAngle, 0f, 90f);
                        planet.slopeTexture = (byte)EditorGUILayout.IntField(new GUIContent("Slope Texture", "Texture ID (0-5) used for slope."), planet.slopeTexture);
                    }
                    GUILayout.Space(5f);
                    planet.visSphereRadiusMod = EditorGUILayout.FloatField("Visibilty Sphere Radius Mod", planet.visSphereRadiusMod);

                    break;
                case Tab.Foliage:

                    planet.generateDetails = EditorGUILayout.Toggle("Generate Details", planet.generateDetails);

                    if (planet.generateDetails)
                    {
                        planet.generateFoliageInEveryBiome = EditorGUILayout.Toggle("Generate Foliage in every biome", planet.generateFoliageInEveryBiome);
                        if (!planet.generateFoliageInEveryBiome)
                            EditorGUILayout.PropertyField(foliageBiomes, true);

                        planet.planetIsRotating = EditorGUILayout.Toggle(new GUIContent("Planet is Rotating", "If the planet is rotating, all points for a quad have to generated in one frame."), planet.planetIsRotating);
                        planet.grassPerQuad = EditorGUILayout.IntSlider(new GUIContent("Points per Quad", "How many random positions on each quad? Points are used for both grass and meshes."), planet.grassPerQuad, 0, 60000);
                        GUILayout.Space(5f);
                        planet.generateGrass = EditorGUILayout.Toggle("Generate Grass", planet.generateGrass);
                        if (planet.generateGrass)
                            planet.grassMaterial = (Material)EditorGUILayout.ObjectField("Grass Material", planet.grassMaterial, typeof(Material), false);

                        planet.grassLevel = EditorGUILayout.IntField(new GUIContent("Detail Level", "Level at and after which details are generated"), planet.grassLevel);
                        planet.detailDistance = EditorGUILayout.FloatField(new GUIContent("Detail Distance", "Distance at which grass and meshes are generated"), planet.detailDistance);
                        planet.detailObjectsGeneratingSimultaneously = EditorGUILayout.IntField(new GUIContent("Details generating simultaneously", "How many quads can generate details at the same time."), planet.detailObjectsGeneratingSimultaneously);
                        GUILayout.Space(5f);

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Add Mesh"))
                            planet.detailMeshes.Add(new DetailMesh());
                        if (GUILayout.Button("Add Prefab"))
                            planet.detailPrefabs.Add(new DetailPrefab());
                        EditorGUILayout.EndHorizontal();

                        for (int i = 0; i < planet.detailMeshes.Count; i++)
                        {
                            var dM = planet.detailMeshes[i];

                            if (dM.isGrass)
                                continue;

                            GUILayout.Label("Detail Mesh:", EditorStyles.boldLabel);

                            dM.meshFraction = EditorGUILayout.Slider(new GUIContent("Fraction", "Fraction of generated points used for meshes instead of grass"), dM.meshFraction, 0f, 1f);
                            dM.meshOffsetUp = EditorGUILayout.FloatField("Offset Up", dM.meshOffsetUp);
                            dM.meshScale = EditorGUILayout.Vector3Field("Scale", dM.meshScale);
                            dM.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", dM.mesh, typeof(Mesh), false);
                            dM.material = (Material)EditorGUILayout.ObjectField("Material", dM.material, typeof(Material), false);
                            dM.useGPUInstancing = EditorGUILayout.Toggle("Use GPU Instancing", dM.useGPUInstancing);

                            planet.detailMeshes[i] = dM;

                            if (GUILayout.Button("Remove"))
                                planet.detailMeshes.RemoveAt(i);


                            GUILayout.Space(10f);
                        }

                        for (int i = 0; i < planet.detailPrefabs.Count; i++)
                        {

                            var dP = (DetailPrefab)planet.detailPrefabs[i];

                            GUILayout.Label("Detail Prefab:", EditorStyles.boldLabel);

                            dP.meshFraction = EditorGUILayout.Slider(new GUIContent("Fraction", "Fraction of generated points used for prefab instead of grass"), dP.meshFraction, 0f, 1f);
                            dP.meshOffsetUp = EditorGUILayout.FloatField("Offset Up", dP.meshOffsetUp);
                            dP.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", dP.prefab, typeof(GameObject), true);

                            if (GUILayout.Button("Remove"))
                                planet.detailPrefabs.RemoveAt(i);

                            GUILayout.Space(10f);
                        }

                    }


                    break;

                case Tab.Debug:
                    DrawDefaultInspector();
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }

        void Heightmap()
        {
            planet.heightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", planet.heightmapTextAsset, typeof(TextAsset), false);
            EditorGUILayout.BeginHorizontal();
            planet.heightmapSizeX = EditorGUILayout.IntField("Width", planet.heightmapSizeX);
            planet.heightmapSizeY = EditorGUILayout.IntField("Height", planet.heightmapSizeY);
            EditorGUILayout.EndHorizontal();
            planet.heightmap16bit = EditorGUILayout.Toggle(new GUIContent("16bit", "16bit mode for more elevation levels. Enable if you generated a 16bit heightmap in the generator."), planet.heightmap16bit);
            planet.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic interpolation", planet.useBicubicInterpolation);
        }
        [MenuItem ("GameObject/3D Object/Planet")]
        public static void CreatePlanet()
        {
            var go = new GameObject();
            go.AddComponent<Planet>().planetMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            go.name = "Planet";
        }
    }
}