using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
namespace PlanetaryTerrain.EditorUtils
{
    public class HeightmapToRaw : EditorWindow
    {

        Texture2D heightmap;
        string filename = "heightmap";

        [MenuItem("Planetary Terrain/Utils/Texture Heightmap to RAW")]


        static void Init()
        {
#pragma warning disable 0219
            HeightmapToRaw window = (HeightmapToRaw)EditorWindow.GetWindow(typeof(HeightmapToRaw));
#pragma warning restore 0219
        }

        void OnGUI()
        {
            heightmap = (Texture2D)EditorGUILayout.ObjectField("Heightmap", heightmap, typeof(Texture2D), false);
            filename = EditorGUILayout.TextField("Filename", filename);


            if (GUILayout.Button("Convert"))
            {
                Color32[] pixels = heightmap.GetPixels32();

                Debug.Log("Width: " + heightmap.width + ", Height: " + heightmap.height);
                byte[] heightmapBytes = new byte[heightmap.width * heightmap.height];

                for (int i = 0; i < pixels.Length; i++)
                {

                    heightmapBytes[i] = pixels[i].r;

                }

                pixels = null;
                File.WriteAllBytes(Application.dataPath + "/" + filename + ".bytes", heightmapBytes);
                heightmapBytes = null;
            }
            if (GUILayout.Button("Convert to 16 bit"))
            {
                Color[] pixels = new Color[heightmap.width * heightmap.height];
                Debug.Log("Width: " + heightmap.width + ", Height: " + heightmap.height);
                for (int x = 0; x < heightmap.width; x++)
                {
                    for (int y = 0; y < heightmap.height; y++)
                    {
                        pixels[y * heightmap.width + x] = heightmap.GetPixel(x, y);
                    }
                }

                byte[] heightmapBytes = new byte[heightmap.width * heightmap.height * 2];

                for (int i = 0; i < pixels.Length; i++)
                {
                    ushort v = (ushort)(pixels[i].r * ushort.MaxValue);

                    heightmapBytes[i + pixels.Length] = (byte)(v >> 8); //Converting one ushort (16bit) to two bytes (each 8bit)
                    heightmapBytes[i] = (byte)(v & 255);
                }

                pixels = null;
                File.WriteAllBytes(Application.dataPath + "/" + filename + "_16bit.bytes", heightmapBytes);
                heightmapBytes = null;
                AssetDatabase.Refresh();
            }
        }

    }
}
