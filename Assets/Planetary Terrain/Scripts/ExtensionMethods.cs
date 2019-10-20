using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetaryTerrain.DoubleMath
{
    public static class ExtensionMethods
    {
        public static Vector3d ToVector3d(this Vector3 v3)
        {
            return new Vector3d(v3.x, v3.y, v3.z);
        }

        public static QuaternionD ToQuaterniond(this Quaternion q)
        {
            return new QuaternionD(q.x, q.y, q.z, q.w);
        }

        public static string ValuesToString(this List<float> list)
        {

            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i].ToString("F6"));
                if (i < list.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
