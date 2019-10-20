using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


namespace PlanetaryTerrain.EditorUtils
{
    public class pRAWConverter : EditorWindow
    {

        string path = "";
        string filename = "heightmapPhotoshop";
        int width = 8192, height = 4096;
        bool is16bit;

        [MenuItem("Planetary Terrain/Utils/PhotoshopRAWConverter")]
        static void Init()
        {
#pragma warning disable 414
#pragma warning disable 0219
            pRAWConverter window = (pRAWConverter)EditorWindow.GetWindow(typeof(pRAWConverter));
#pragma warning restore 0219
#pragma warning restore 414
        }


        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField("Path", path);
            if (GUILayout.Button("Select"))
                path = EditorUtility.OpenFilePanel("Photoshop RAW File", "", "raw");
            EditorGUILayout.EndHorizontal();
            filename = EditorGUILayout.TextField("Filename", filename);
            EditorGUILayout.BeginHorizontal();
            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
            EditorGUILayout.EndHorizontal();
            is16bit = EditorGUILayout.Toggle("16bit", is16bit);

            if (GUILayout.Button("Convert"))
                ConvertToHeightmap();

            EditorGUILayout.LabelField("Converts grayscale Photoshop .raw files to heightmaps. Export with Macintosh byte order and 0 header.");
        }

        void ConvertToHeightmap()
        {
            var fs = File.OpenRead(path);
            byte[] bytes;

            if (is16bit)
            {
                int halfLength = width * height;
                int length = halfLength * 2;
                int hh = height - 1;
                bytes = new byte[length];

                int i = 0;

                if (fs.Length != bytes.Length)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    return;
                }


                try
                {
                    while (i < halfLength)
                    {
                        int index = (hh - (int)(i / (float)width)) * width + (i % width);

                        var us = new byte[2];
                        fs.Read(us, 0, 2);

                        bytes[index] = us[1];
                        bytes[(index + halfLength)] = us[0];//

                        i++;
                        //Debug.Log("i: " + i);

                    }
                }
                catch (System.IndexOutOfRangeException e)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    Debug.LogError(e);
                    return;
                }
            }
            else
            {
                int length = width * height;
                int hh = height - 1;

                bytes = new byte[length];

                if (fs.Length != bytes.Length)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    return;
                }

                int i = 0;//, index = 0;

                try
                {
                    while (i < length)
                    {
                        int index = (hh - (int)(i / (float)width)) * width + (i % width);
                        bytes[index] = (byte) fs.ReadByte();
                        i++;
                    }
                }
                catch (System.IndexOutOfRangeException e)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    Debug.LogError(e);
                    return;
                }
                
            }


            Debug.Log("Successfully converted!");
            File.WriteAllBytes(Application.dataPath + "/" + "heightmapPhotoshop" + ".bytes", bytes);
            AssetDatabase.Refresh();
        }
    }
}
