using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using PlanetaryTerrain.Noise;
using System.Text;

namespace PlanetaryTerrain.EditorUtils
{
    public class HeightmapGenerator : EditorWindow
    {
        public enum DataSource { Noise, ComputeShader, RawHeightmap }
        string filename = "heightmapGenerated";

        int resolutionX = 8192;
        int resolutionY = 4096;

        public float[] textureHeights = { 0f, 0.01f, 0.4f, 0.8f, 1f };
        public Color32[] colors = { new Color32(166, 130, 90, 255), new Color32(72, 80, 28, 255), new Color32(60, 53, 37, 255), new Color32(81, 81, 81, 255), new Color32(255, 255, 255, 255) };
        public byte[] textureIds = new byte[] { 0, 1, 2, 3, 4, 5 };
        bool ocean;
        float oceanLevel = 0f;
        Color32 oceanColor = new Color32(48, 57, 56, 255);
        Texture2D heightmap;
        Texture2D texture;
        Texture2D gradient;
        DataSource dataSource = DataSource.Noise;
        bool heightmap16bit = false;
        int width, height;
        byte[] heightmapBytes;
        float progress;
        IAsyncResult cookie;
        bool preview;
        Module module;
        byte[] lastBytes;

        TextAsset textAsset;
        ComputeShader computeShader;

        bool generateHeightmapTex = true, generateTexture = true, generateHeightmap = true;
        bool debugLayout;



        [MenuItem("Planetary Terrain/Heightmap Generator")]

        static void Init()
        {
#pragma warning disable 0219
            HeightmapGenerator window = (HeightmapGenerator)EditorWindow.GetWindow(typeof(HeightmapGenerator));
#pragma warning restore 0219
        }

        void OnGUI()
        {
            DrawGUI();

            if (GUILayout.Button("Generate Gradient"))
            {
                GenerateGradient();
            }

            GUILayout.Label(gradient);
            GUILayout.Space(15);

            if (dataSource != DataSource.RawHeightmap && GUILayout.Button("Generate Preview") && cookie == null)
            {
                progress = 0f;
                preview = true;
                width = 512;
                height = 256;

                if (dataSource == DataSource.Noise)
                {
                    LoadSerializedNoise();
                    Action method = GenerateBytes;
                    cookie = method.BeginInvoke(null, null);
                }
                else if (dataSource == DataSource.ComputeShader)
                {
                    GenerateBytesGPU();
                    GenerateTextures();
                }
            }

            if (GUILayout.Button("Generate") && cookie == null)
            {
                progress = 0f;
                preview = false;
                width = resolutionX;
                height = resolutionY;

                if (dataSource == DataSource.Noise)
                {
                    LoadSerializedNoise();
                    Action method = GenerateBytes;
                    cookie = method.BeginInvoke(null, null);
                }
                else if (dataSource == DataSource.ComputeShader)
                {
                    GenerateBytesGPU();
                    GenerateTextures();
                    SaveAssets();
                }
                else if (dataSource == DataSource.RawHeightmap)
                {
                    ReadHeightmap();
                    GenerateTextures();
                    if (generateTexture)
                        File.WriteAllBytes(Application.dataPath + "/" + filename + "_texture.png", texture.EncodeToPNG());
                    if (generateHeightmapTex)
                        File.WriteAllBytes(Application.dataPath + "/" + filename + "_heightmap.png", heightmap.EncodeToPNG());
                    AssetDatabase.Refresh();
                }
            }

            if (cookie != null)
            {
                EditorUtility.DisplayProgressBar("Progress", "Generating heightmap from noise...", progress);
                if (cookie.IsCompleted)
                {
                    EditorUtility.ClearProgressBar();
                    GenerateTextures();
                    SaveAssets();
                    cookie = null;

                }
            }
            GUILayout.Label(heightmap);
            GUILayout.Label(texture);

            GUILayout.FlexibleSpace();
            debugLayout = GUILayout.Toggle(debugLayout, new GUIContent("Safe Layout", "Toggle this if arrays are invisible. Circumvents a bug with Horizontal Layouts in recent versions of Unity by disabling them."));
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void SaveAssets()
        {
            if (!preview)
            {
                if (generateTexture)
                    File.WriteAllBytes(Application.dataPath + "/" + filename + "_texture.png", texture.EncodeToPNG());
                if (generateHeightmapTex)
                    File.WriteAllBytes(Application.dataPath + "/" + filename + "_heightmap.png", heightmap.EncodeToPNG());
                if (generateHeightmap)
                    File.WriteAllBytes(Application.dataPath + "/" + filename + ".bytes", heightmapBytes);
            }
            AssetDatabase.Refresh();
        }
        void ReadHeightmap()
        {
            Heightmap.TestHeightmapResolution(textAsset.bytes.Length, width, height, heightmap16bit);

            if (heightmap16bit)
            {
                byte[] bytes = textAsset.bytes; //Load Heightmap Data from Text Asset into memory
                int length = width * height;
                heightmapBytes = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    heightmapBytes[i] = (byte)(((bytes[length + i] << 8) + bytes[i]) / 257);
                }
            }
            else
            {
                heightmapBytes = textAsset.bytes;

            }
        }

        void LoadSerializedNoise()
        {
            if (textAsset != null && textAsset.bytes.Length != 0 && textAsset.bytes != lastBytes)
            {
                try
                {
                    lastBytes = textAsset.bytes;
                    MemoryStream stream = new MemoryStream(textAsset.bytes);
                    module = Utils.DeserializeModule(stream);

                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    module = null;
                }
            }
        }

        void GenerateBytes()
        {
            System.DateTime startTime = System.DateTime.UtcNow;

            int length = width * height;
            if (!heightmap16bit)
                heightmapBytes = new byte[length];
            else
                heightmapBytes = new byte[length * 2];

            int resXh = width / 2;
            int resYh = height / 2;

            double divisor = height / 180.0;

            double lat, lon;
            Vector3 xyz;
            float value;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    lat = (y - resYh) / divisor;
                    lon = (x - resXh) / divisor;

                    xyz = Utils.LatLonToXyz(lat, lon);

                    value = Mathf.Clamp01((module.GetNoise(xyz.x, xyz.y, xyz.z) + 1f) / 2f); //Noise ranges from -1 to 1; texture needs value from 0 to 1

                    if (!heightmap16bit)
                        heightmapBytes[y * width + x] = (byte)(value * 255f);
                    else
                    {
                        ushort v = (ushort)(value * ushort.MaxValue);
                        int i = (y * width + x);

                        heightmapBytes[i + length] = (byte)(v >> 8); //Converting one ushort (16bit) to two bytes (each 8bit)
                        heightmapBytes[i] = (byte)(v & 255);
                    }
                }
                progress = (float)x / width;
            }
            Debug.Log("Generation Time: " + (System.DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        void GenerateBytesGPU()
        {
            System.DateTime startTime = System.DateTime.UtcNow;

            int length = width * height;
            if (!heightmap16bit)
                heightmapBytes = new byte[length];
            else
                heightmapBytes = new byte[length * 2];

            int resXh = width / 2;
            int resYh = height / 2;

            double divisor = height / 180.0;

            double lat, lon;
            Vector3[] xyz = new Vector3[length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    lat = (y - resYh) / divisor;
                    lon = (x - resXh) / divisor;

                    xyz[y * width + x] = Utils.LatLonToXyz(lat, lon);

                }
            }

            int kernelIndex = computeShader.FindKernel("ComputeHeightmap");

            ComputeBuffer computeBuffer = new ComputeBuffer(xyz.Length, 12);
            computeBuffer.SetData(xyz);

            computeShader.SetBuffer(kernelIndex, "dataBuffer", computeBuffer);
            computeShader.Dispatch(kernelIndex, Mathf.CeilToInt(length / 256f), 1, 1);
            computeBuffer.GetData(xyz);
            computeBuffer.Dispose();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = Mathf.Clamp01(xyz[y * width + x].x);

                    if (!heightmap16bit)
                        heightmapBytes[y * width + x] = (byte)(value * byte.MaxValue);
                    else
                    {
                        ushort v = (ushort)(value * ushort.MaxValue);
                        int i = (y * width + x);

                        heightmapBytes[i + length] = (byte)(v >> 8); //Converting one ushort (16bit) to two bytes (each 8bit)
                        heightmapBytes[i] = (byte)(v & 255);
                    }
                }
                progress = (float)x / width;
            }
            Debug.Log("Generation Time: " + (System.DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        void GenerateTextures()
        {
            if (generateHeightmapTex)
                heightmap = new Texture2D(width, height);
            if (generateTexture)
                texture = new Texture2D(width, height);


            float value = 0f;
            int length = width * height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!heightmap16bit || dataSource == DataSource.RawHeightmap)
                        value = heightmapBytes[y * width + x] / 255f;
                    else
                    {
                        int i = (y * width + x);
                        value = ((ushort)((heightmapBytes[length + i] << 8) + heightmapBytes[i])) / (float)ushort.MaxValue;
                    }
                    if (generateHeightmapTex)
                        heightmap.SetPixel(x, y, new Color(value, value, value));

                    if (generateTexture)
                        if (!ocean)
                            texture.SetPixel(x, y, Utils.FloatArrayToColor(Utils.EvaluateTexture(value, textureHeights, textureIds), colors));
                        else
                            texture.SetPixel(x, y, value >= oceanLevel ? Utils.FloatArrayToColor(Utils.EvaluateTexture(value, textureHeights, textureIds), colors) : oceanColor);

                }
            }
            if (generateHeightmapTex)
                heightmap.Apply();
            if (generateTexture)
                texture.Apply();
        }

        void GenerateGradient()
        {
            gradient = new Texture2D(512, 32);
            gradient.alphaIsTransparency = false;

            var colorsOpaque = new Color32[colors.Length];

            for (int i = 0; i < colorsOpaque.Length; i++)
            {
                colorsOpaque[i] = colors[i];
                colorsOpaque[i].a = 255;
            }


            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    gradient.SetPixel(x, y, Utils.FloatArrayToColor(Utils.EvaluateTexture(x / 511f, textureHeights, textureIds), colorsOpaque));
                }
            }
            gradient.Apply();
        }

        void DrawGUI()
        {
            GUILayout.Label("Heightmap/Texture Generator", EditorStyles.boldLabel);
            filename = EditorGUILayout.TextField("Filename", filename);


            GUILayout.Space(15);
            GUILayout.Label("Textures size: ", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            resolutionX = EditorGUILayout.IntField("Width", resolutionX);
            resolutionY = EditorGUILayout.IntField("Height", resolutionY);
            EditorGUILayout.EndHorizontal();
            heightmap16bit = EditorGUILayout.Toggle("16bit Mode", heightmap16bit);

            GUILayout.Space(15);
            GUILayout.Label("Data source: ", EditorStyles.boldLabel);
            dataSource = (DataSource)EditorGUILayout.EnumPopup(new GUIContent("Data source", "Source for heightmap/texture generation. You can also use heightmaps imported with Texture Heightmap to RAW. Don't forget setting the bit depth."), dataSource);


            switch (dataSource)
            {
                case DataSource.Noise:
                    textAsset = (TextAsset)EditorGUILayout.ObjectField("Noise", textAsset, typeof(TextAsset), true);
                    break;
                case DataSource.ComputeShader:
                    computeShader = (ComputeShader)EditorGUILayout.ObjectField("ComputeShader", computeShader, typeof(ComputeShader), false);
                    break;
                case DataSource.RawHeightmap:
                    textAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", textAsset, typeof(TextAsset), true);
                    break;
            }
            GUILayout.Space(15);

            GUILayout.Label("Color gradient: ", EditorStyles.boldLabel);

            ocean = EditorGUILayout.Toggle("Generate Ocean", ocean);
            if (ocean)
            {
                oceanLevel = EditorGUILayout.Slider("Water Level", oceanLevel, 0f, 1f);
                oceanColor = EditorGUILayout.ColorField("Ocean Color", oceanColor);
            }

            SerializedObject serialObj = new SerializedObject(this);
            SerializedProperty textureHeightsArray = serialObj.FindProperty("textureHeights");
            SerializedProperty colorsArray = serialObj.FindProperty("colors");
            SerializedProperty textureIdsArray = serialObj.FindProperty("textureIds");

            if (!debugLayout)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(textureHeightsArray, true);
            EditorGUILayout.PropertyField(textureIdsArray, true);
            if (debugLayout)
                EditorGUILayout.PropertyField(colorsArray, true);

            if (!debugLayout)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(colorsArray, true);
            }
            if (GUILayout.Button("Set Alpha(Smoothness) to 0"))
                for (int i = 0; i < colors.Length; i++)
                    colors[i].a = 0;
            if (GUILayout.Button("Set Alpha(Smoothness) to 255"))
                for (int i = 0; i < colors.Length; i++)
                    colors[i].a = 255;
            if (!debugLayout)
                EditorGUILayout.EndVertical();

            if (!debugLayout)
                EditorGUILayout.EndHorizontal();

            serialObj.ApplyModifiedProperties();


            GUILayout.Space(15);

            GUILayout.Label("Files to generate: ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            generateHeightmapTex = EditorGUILayout.Toggle("Heightmap Texture", generateHeightmapTex);
            generateTexture = EditorGUILayout.Toggle("Texture", generateTexture);
            generateHeightmap = EditorGUILayout.Toggle("Heightmap", generateHeightmap);
            EditorGUILayout.EndHorizontal();

        }
    }
}




