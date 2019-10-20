using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.Noise;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;


namespace PlanetaryTerrain
{

    public static class Utils
    {
        public const float sqrt2 = 1.4142135623731f;

        /// <summary>
        /// Gradient with float array instead of color
        /// </summary>
        public static float[] EvaluateTexture(float time, float[] textureHeights, byte[] textureIds)
        {
            time = Mathf.Clamp01(time);
            int index;

            for (index = 0; index < textureHeights.Length; index++)
                if (time < textureHeights[index])
                    break;


            int index1 = Mathf.Clamp(index - 1, 0, textureHeights.Length - 1);
            int index2 = Mathf.Clamp(index, 0, textureHeights.Length - 1);

            float[] result = new float[6];
            if (textureIds[index1] == textureIds[index2])
            {
                result[textureIds[index1]] = 1f;
                return result;
            }

            time = (time - textureHeights[index1]) / (textureHeights[index2] - textureHeights[index1]);

            result[textureIds[index1]] = 1f - time;
            result[textureIds[index2]] = time;
            return result;
        }
        /// <summary>
        /// Converts float array used for biomes to color array used for scaled space texture
        /// </summary>
        public static Color32 FloatArrayToColor(float[] floats, Color32[] colors)
        {

            Color32 result = new Color32(0, 0, 0, 0);
            for (int i = 0; i < colors.Length; i++)
            {
                result += floats[i] * (Color)colors[i];
            }
            return result;
        }

        public static int ColorDifference(Color32 a, Color32 b)
        {
            int result = 0;

            result += Mathf.Abs(a.r - b.r);
            result += Mathf.Abs(a.g - b.g);
            result += Mathf.Abs(a.b - b.b);
            result += Mathf.Abs(a.a - b.a);

            return result;
        }

        /// <summary>
        /// Converts from spherical to cartesian coordinates 
        /// </summary>
        /// <param name="lat">latitude
        /// </param>
        /// <param name="lon">longitude
        /// </param>
        /// <param name="radius">planet radius
        /// </param>
        public static Vector3 LatLonToXyz(double lat, double lon, float radius = 1f)
        {
            lat *= Mathf.Deg2Rad;
            lon *= Mathf.Deg2Rad;
            Vector3 xyz;
            xyz.x = (float)(radius * System.Math.Cos(lat) * System.Math.Sin(lon));
            xyz.y = (float)(radius * System.Math.Sin(lat));
            xyz.z = (float)(-radius * System.Math.Cos(lat) * System.Math.Cos(lon));
            return xyz;
        }

        /// <summary>
        /// Converts from spherical to cartesian coordinates 
        /// </summary>
        /// <param name="ll">spherical coordinates
        /// </param>
        public static Vector3 LatLonToXyz(Vector2 ll, float radius)
        {
            ll *= Mathf.Deg2Rad;
            Vector3 xyz;
            xyz.x = radius * Mathf.Cos(ll.x) * Mathf.Sin(ll.y);
            xyz.y = radius * Mathf.Sin(ll.x);
            xyz.z = -radius * Mathf.Cos(ll.x) * Mathf.Cos(ll.y);
            return xyz;
        }

        /// <summary>
        /// Converts from cartesian to spherical coordinates 
        /// </summary>
        /// <param name="pos">cartesian coordinates
        /// </param>
        public static Vector2 XyzToLatLon(Vector3 pos, float radius)
        {
            float lat = Mathf.Acos(pos.y / radius) - (Mathf.PI / 2);
            float lon = Mathf.Atan2(pos.z, pos.x) + (Mathf.PI / 2);
            Vector2 ll = new Vector2(lat, lon);

            ll *= Mathf.Rad2Deg;
            ll.x *= -1;

            return ll;
        }
        /// <summary>
        /// Interpolates between n0 and n4 with time a
        /// </summary>
        public static float CubicInterpolation(float n0, float n1, float n2, float n3, float a)
        {
            return n1 + .5f * a * (n2 - n0 + a * (2f * n0 - 5f * n1 + 4f * n2 - n3 + a * (3f * (n1 - n2) + n3 - n0)));
        }

        /// <summary>
        /// Rotates a point around a pivot
        /// </summary>
        public static Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            var dir = point - pivot;
            dir = rotation * dir;
            point = dir + pivot;
            return point;
        }

        /// <summary>
        /// Tests if bounds are in viewPlanes
        /// </summary>
        public static bool TestPlanesAABB(Plane[] planes, Vector3 boundsMin, Vector3 boundsMax, bool testIntersection = true, float extraRange = 0f)
        {
            if (planes == null)
                return false;

            Vector3 vmin, vmax;
            int testResult = 2;

            for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
            {
                var normal = planes[planeIndex].normal;
                var planeDistance = planes[planeIndex].distance;

                // X axis
                if (normal.x < 0)
                {
                    vmin.x = boundsMin.x;
                    vmax.x = boundsMax.x;
                }
                else
                {
                    vmin.x = boundsMax.x;
                    vmax.x = boundsMin.x;
                }

                // Y axis
                if (normal.y < 0)
                {
                    vmin.y = boundsMin.y;
                    vmax.y = boundsMax.y;
                }
                else
                {
                    vmin.y = boundsMax.y;
                    vmax.y = boundsMin.y;
                }

                // Z axis
                if (normal.z < 0)
                {
                    vmin.z = boundsMin.z;
                    vmax.z = boundsMax.z;
                }
                else
                {
                    vmin.z = boundsMax.z;
                    vmax.z = boundsMin.z;
                }

                var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
                if (dot1 + planeDistance < 0 - extraRange)
                    return false;

                if (testIntersection)
                {
                    var dot2 = normal.x * vmax.x + normal.y * vmax.y + normal.z * vmax.z;
                    if (dot2 + planeDistance <= 0 + extraRange)
                        testResult = 1;
                }
            }
            return testResult > 0;
        }

        public static int Pow(int num, int exponent)
        {
            int result = 1;
            for (int i = 0; i < exponent; i++)
                result *= num;

            return result;
        }

        public static int[] GetTriangles(string configuration)
        {
            switch (configuration)
            {
                default:
                    return ConstantTriArrays.tris0000;
                case "0001":
                    return ConstantTriArrays.tris0001;
                case "0010":
                    return ConstantTriArrays.tris0010;
                case "0100":
                    return ConstantTriArrays.tris0100;
                case "0101":
                    return ConstantTriArrays.tris0101;
                case "0110":
                    return ConstantTriArrays.tris0110;
                case "1000":
                    return ConstantTriArrays.tris1000;
                case "1001":
                    return ConstantTriArrays.tris1001;
                case "1010":
                    return ConstantTriArrays.tris1010;
            }
        }
        /// <summary>
        /// Generates grayscale preview of noise module
        /// </summary>
        public static Texture2D GeneratePreview(Module module, int resX = 256, int resY = 256)
        {
            Texture2D preview = new Texture2D(resX, resY);

            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    float v = (module.GetNoise(x / (float)resX, y / (float)resY, 0f) + 1f) / 2f;
                    preview.SetPixel(x, y, new Color(v, v, v));
                }
            }
            preview.Apply();
            return preview;
        }

        /// <summary>
        /// Generates grayscale previw of noise module
        /// </summary>
        public static Heightmap GeneratePreviewHeightmap(Module module, int resX = 256, int resY = 256)
        {
            Heightmap preview = new Heightmap(resX, resY, false, false);
            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    float v = (module.GetNoise(x / (float)resX, y / (float)resY, 0f) + 1f) / 2f;
                    v = Mathf.Clamp01(v);
                    preview.SetPixel(x, y, v);
                }
            }
            
            return preview;
        }

        /// <summary>
        /// Deserializes noise module to module tree
        /// </summary>
        public static Module DeserializeModule(Stream fs)
        {
            BinaryFormatter bf = new BinaryFormatter();
            return (Module)bf.Deserialize(fs);
        }


        /// <summary>
        /// Returns position of vertex of the base plane (used for quad generation) based on index. Only works with indices up to 1088, for higher indices use ConstantTriArrays.extendedPlane.
        /// </summary>
        public static Vector3 VertFromIndex(int i)
        {
            return new Vector3(Mathf.FloorToInt(i / 33f) * 0.0625f - 1f, 0f, -((i % 33) * 0.0625f - 1f));
        }



        /// <summary>
        /// Randomly sets the seeds of a noise module
        /// </summary>
        public static void RandomizeNoise(ref Module m)
        {
            if (m.isNoise)
            {
                ((FastNoise)m).SetSeed(Random.Range(int.MinValue, int.MaxValue));

                //Randomize Frequency:
                //var frequency = ((FastNoise)m).m_frequency;
                //frequency += Random.Range(frequency/-100f, frequency/100f);
                //((FastNoise)m).SetFrequency(frequency); 
            }

            if (m.inputs != null)
                for (int i = 0; i < m.inputs.Length; i++)
                    RandomizeNoise(ref m.inputs[i]);
        }


        /// <summary>
        /// Saves a Vector3 array as a binary file.
        /// </summary>
        public static void SaveAsBinary(Vector3[] array)
        {

            float[] floats = new float[array.Length * 3];

            for (int i = 0; i < array.Length; i++)
            {
                int index = i * 3;

                floats[index] = array[i].x;
                floats[index + 1] = array[i].y;
                floats[index + 2] = array[i].z;
            }

            var bf = new BinaryFormatter();
            bf.Serialize(new FileStream(Application.dataPath + "/" + "filename" + ".txt", FileMode.Create), floats);
            Debug.Break();
        }

        /// <summary>
        /// Saves a Vector3 array as a text file.
        /// </summary>
        public static void SaveAsText(Vector3[] array)
        {
            StringBuilder sb = new StringBuilder("{");
            sb.AppendLine();

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append("new Vector3(");
                sb.Append(array[i].x);
                sb.Append("f, ");
                sb.Append(array[i].y);
                sb.Append("f, ");
                sb.Append(array[i].z);
                sb.Append("f), ");

                if (i % 33 == 0 && i != 0)
                {
                    sb.AppendLine();
                }

            }

            File.WriteAllText(Application.dataPath + "/" + "filename" + ".txt", sb.ToString());
            Debug.Break();
        }

        /// <summary>
        /// Saves an int array as a text file.
        /// </summary>
        public static void SaveAsText(int[] array)
        {
            StringBuilder sb = new StringBuilder("{");
            sb.AppendLine();

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                sb.Append(", ");

                if (i % 66 == 0 && i != 0)
                {
                    sb.AppendLine();
                }

            }

            File.WriteAllText(Application.dataPath + "/" + "filename" + ".txt", sb.ToString());
            Debug.Break();
        }

    }

    struct MeshData
    {
        public Vector3[] vertices;
        public Color32[] colors;
        public Vector3[] normals;
        public Vector2[] uv;
        public Vector2[] uv2;


        public MeshData(Vector3[] vertices, Color32[] colors, Vector3[] normals, Vector2[] uv, Vector2[] uv2)
        {
            this.vertices = vertices;
            this.colors = colors;
            this.normals = normals;
            this.uv = uv;
            this.uv2 = uv2;
        }

        public MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uv)
        {
            this.vertices = vertices;
            this.normals = normals;
            this.uv = uv;
            this.colors = new Color32[1089];
            this.uv2 = new Vector2[1089];

        }
    }

    public class SortingClass : IComparer<Quad>
    {
        public int Compare(Quad x, Quad y)
        {
            if (x.level > y.level)
                return 1;
            if (x.distance > y.distance && x.level == y.level)
                return 1;

            return -1;
        }
    }

    public struct int2
    {
        public int x;
        public int y;

        public int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int this[int index]
        {
            get
            {
                if (index == 0)
                    return x;

                if (index > 1)
                    throw new System.IndexOutOfRangeException("int2 index out of range");

                return y;
            }
            set
            {

                if (index == 0)
                {
                    x = value;
                    return;
                }
                if (index > 1)
                    throw new System.IndexOutOfRangeException("int2 index out of range");

                y = value;

            }
        }


    }

    public struct bool4
    {
        public bool x, y, z, w;

        public static bool4 True
        {
            get
            {
                return new bool4(true, true, true, true);
            }
        }
        public static bool4 False
        {
            get
            {
                return new bool4(false, false, false, false);
            }
        }

        public override string ToString()
        {
            char[] chars = new char[4];

            for (int i = 0; i < 4; i++)
            {
                if (this[i])
                    chars[i] = '1';
                else
                    chars[i] = '0';
            }
            return new string(chars);
        }

        public bool4(bool x, bool y, bool z, bool w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public bool4(bool[] bools)
        {
            this.x = bools[0];
            this.y = bools[1];
            this.z = bools[2];
            this.w = bools[3];
        }
        public bool4(bool4 bools) {
            this.x = bools.x;
            this.y = bools.y;
            this.z = bools.z;
            this.w = bools.w;
        }

        public bool this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    case 3:
                        return w;
                    default:
                        throw new System.IndexOutOfRangeException("bool4 index out of range");

                }

            }
            set
            {

                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    case 3:
                        w = value;
                        break;
                    default:
                        throw new System.IndexOutOfRangeException("bool4 index out of range");
                }
                return;

            }
        }

        public static bool operator ==(bool4 x, bool4 y)
        {
            return x.x == y.x && x.y == y.y && x.z == y.z && x.w == y.w;
        }

        public static bool operator !=(bool4 x, bool4 y)
        {
            return x.x != y.x || x.y != y.y || x.z != y.z || x.w != y.w;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is bool4 && this == (bool4)obj;
        }

    }

}

