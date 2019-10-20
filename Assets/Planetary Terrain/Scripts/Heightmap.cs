using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PlanetaryTerrain
{
    /// <summary>
    /// Class used to store an uncompressed 8 or 16 bit heightmap.
    /// </summary>
    public class Heightmap
    {

        public byte[] bytes;
        public ushort[] ushorts;

        public int height;
        public int width;
        public bool is16bit;
        public bool disableInterpolation;
        public bool useBicubicInterpolation;

        public float scalex, scaley;
        public float mIndexX, mIndexY;
        const float TwoPI = Mathf.PI * 2;

        void ReadTextAsset(TextAsset textAsset)
        {
            bytes = textAsset.bytes; //Load Heightmap Data from Text Asset into memory

            TestHeightmapResolution(bytes.Length, width, height, is16bit);

            if (is16bit)
            {
                int length = height * width;

                ushorts = new ushort[length];

                for (int i = 0; i < length; i++)
                {
                    ushorts[i] = (ushort)((bytes[length + i] << 8) + bytes[i]); //two bytes make one ushort
                }
                bytes = null;
            }
        }

        public Heightmap(int width, int height, bool is16bit, bool useBicubicInterpolation, TextAsset textAsset)
        {

            this.width = width;
            this.height = height;
            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
            this.is16bit = is16bit;
            this.useBicubicInterpolation = useBicubicInterpolation;

            ReadTextAsset(textAsset);

            scaley = (height - 1) / Mathf.PI;
            scalex = (width - 1) / TwoPI;
        }

        public Heightmap(int width, int height, bool is16bit, bool useBicubicInterpolation)
        {

            this.width = width;
            this.height = height;
            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
            this.is16bit = is16bit;
            this.useBicubicInterpolation = useBicubicInterpolation;

            if (is16bit)
                ushorts = new ushort[width * height];
            else
                bytes = new byte[width * height];

            scaley = (height - 1) / Mathf.PI;
            scalex = (width - 1) / TwoPI;
        }

        public Heightmap(Texture2D texture, bool useBicubicInterpolation)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;

            ReadTexture2D(texture);

            this.mIndexX = width - 1;
            this.mIndexY = height - 1;

            scaley = (height - 1) / Mathf.PI;
            scalex = (width - 1) / TwoPI;

        }

        /// <summary>
        /// Returns interpolated height at pos. Heightmap must be equirectangular for this to work.
        /// </summary>
        public float GetPosInterpolated(Vector3 pos)
        {

            float lat = (Mathf.PI - Mathf.Acos(pos.y));
            float lon = (Mathf.Atan2(pos.z, pos.x) + 1.570796f);

            if (lon < 0f)
                lon += TwoPI;

            lat *= (float)scaley;
            lon *= (float)scalex;

            if (lat > mIndexY)
            {
                lat -= mIndexY;
            }
            if (lon > mIndexX)
            {
                lon -= mIndexX;
            }

            float result = 0f;

            if (useBicubicInterpolation && !disableInterpolation)
            {

                int x2 = Mathf.FloorToInt(lon);

                int x1 = x2 - 1;//Mathf.Max(Mathf.FloorToInt(lon) - 1, 0);
                if (x1 < 0) x1 += width;

                int x3 = Mathf.CeilToInt(lon);

                int x4 = x3 + 1;//Mathf.Min(Mathf.CeilToInt(lon) + 1, width - 1);
                if (x4 >= width) x4 -= width;


                int y2 = Mathf.FloorToInt(lat);

                int y1 = y2 - 1;//Mathf.Max(Mathf.FloorToInt(lat) - 1, 0);
                if (y1 < 0) y1 += height;

                int y3 = Mathf.CeilToInt(lat);

                int y4 = y3 + 1;//Mathf.Min(Mathf.CeilToInt(lat) + 1, height - 1);
                if (y4 >= height) y4 -= height;



                float[] pixels = new float[16];

                if (!is16bit)
                    pixels = new float[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y1 * width + x3], bytes[y1 * width + x4],
                                           bytes[y2 * width + x1], bytes[y2 * width + x2], bytes[y2 * width + x3], bytes[y2 * width + x4],
                                           bytes[y3 * width + x1], bytes[y3 * width + x2], bytes[y3 * width + x3], bytes[y3 * width + x4],
                                           bytes[y4 * width + x1], bytes[y4 * width + x2], bytes[y4 * width + x3], bytes[y4 * width + x4] }; //get sixteen pixels around point
                else
                    pixels = new float[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y1 * width + x3], ushorts[y1 * width + x4],
                                           ushorts[y2 * width + x1], ushorts[y2 * width + x2], ushorts[y2 * width + x3], ushorts[y2 * width + x4],
                                           ushorts[y3 * width + x1], ushorts[y3 * width + x2], ushorts[y3 * width + x3], ushorts[y3 * width + x4],
                                           ushorts[y4 * width + x1], ushorts[y4 * width + x2], ushorts[y4 * width + x3], ushorts[y4 * width + x4] }; //get sixteen pixels around point

                float xpos = (lon - x2);

                float val1 = Utils.CubicInterpolation(pixels[0], pixels[1], pixels[2], pixels[3], xpos); //cubic interpolation between lines
                float val2 = Utils.CubicInterpolation(pixels[4], pixels[5], pixels[6], pixels[7], xpos);
                float val3 = Utils.CubicInterpolation(pixels[8], pixels[9], pixels[10], pixels[11], xpos);
                float val4 = Utils.CubicInterpolation(pixels[12], pixels[13], pixels[14], pixels[15], xpos);

                result = Utils.CubicInterpolation(val1, val2, val3, val4, lat - y2); //interpolating between line values
            }
            else if (!disableInterpolation)
            {
                int x1 = Mathf.FloorToInt(lon);
                int x2 = Mathf.CeilToInt(lon);
                int y1 = Mathf.CeilToInt(lat);
                int y2 = Mathf.FloorToInt(lat);

                float[] pixels = new float[4];

                if (!is16bit)
                    pixels = new float[] { bytes[y2 * width + x1], bytes[y2 * width + x2], bytes[y1 * width + x1], bytes[y1 * width + x2] }; //get four pixels closest to point
                else
                    pixels = new float[] { ushorts[y2 * width + x1], ushorts[y2 * width + x2], ushorts[y1 * width + x1], ushorts[y1 * width + x2] }; //get four pixels closest to point

                float val1 = Mathf.Lerp(pixels[0], pixels[1], lon - x1);
                float val2 = Mathf.Lerp(pixels[2], pixels[3], lon - x1);
                result = Mathf.Lerp(val1, val2, lat - y2);
            }
            else
            {
                int x = Mathf.RoundToInt(lon);
                int y = Mathf.RoundToInt(lat);

                if (is16bit)
                    result = ushorts[y * width + x];
                else
                    result = bytes[y * width + x];
            }


            if (is16bit)
                result /= ushort.MaxValue;
            else
                result /= byte.MaxValue;

            return result;

        }
        
        public float GetPixel(int x, int y)
        {
            if (is16bit)
                return ushorts[y * width + x] / (float)ushort.MaxValue;
            return bytes[y * width + x] / (float)byte.MaxValue;
        }

        public void SetPixel(int x, int y, float value)
        {
            if (is16bit)
                ushorts[y * width + x] = (ushort)Mathf.RoundToInt(value * ushort.MaxValue);
            else
                bytes[y * width + x] = (byte)Mathf.RoundToInt(value * byte.MaxValue);
        }

        public void ReadTexture2D(Texture2D tex)
        {
            width = tex.width;
            height = tex.height;

            Color32[] pixels = tex.GetPixels32();
            ushorts = null;
            bytes = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                bytes[i] = pixels[i].r;
            }
        }

        /// <summary>
        /// Converts this heightmap to a Texture2D.
        /// </summary>
        public Texture2D GetTexture2D()
        {
            Color32[] colors = new Color32[width * height];

            for (int i = 0; i < colors.Length; i++)
            {
                byte grayscale = 0;

                if (is16bit)
                    grayscale = (byte)(ushorts[i] / 257);
                grayscale = bytes[i];

                colors[i] = new Color32(grayscale, grayscale, grayscale, byte.MaxValue);
            }

            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels32(colors);
            tex.Apply();
            return tex;
        }

        public static void TestHeightmapResolution(int length, int width, int height, bool is16bit)
        {
            if ((is16bit ? 0.5f * length : length) == height * width)
                return;

            throw new System.ArgumentOutOfRangeException("width, height", "Heightmap resolution incorrect! Cannot read heightmap!");
        }
    }
}
