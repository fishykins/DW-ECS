using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

using PlanetaryTerrain;
using PlanetaryTerrain.DoubleMath;


namespace PlanetaryTerrain.Noise
{
    public static class ComputeShaderGenerator
    {
        const string xyz = "(x, y, z);";

        public static string GenerateComputeShader(Module m)
        {
            List<List<Module>> tree = new List<List<Module>>();

            StringBuilder code = new StringBuilder();



            code.AppendLine("float GetNoise(float x, float y, float z) {");
            code.AppendLine();

            BuildTree(ref tree, m, 0);

            string[] inputIds = new string[3];
            for (int i = tree.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < tree[i].Count; j++)
                {

                    if (tree[i][j].inputs != null)
                        for (int k = 0; k < tree[i][j].inputs.Length; k++)
                        {
                            for (int l = i; l < tree.Count; l++)
                            {
                                int index = tree[l].FindIndex(tree[i][j].inputs[k].Equals);
                                if (index != -1)
                                {
                                    inputIds[k] = Id(l, index);
                                    break;
                                }

                            }

                        }

                    if (tree[i][j].isNoise || tree[i][j] is FastNoise)
                    {
                        FastNoise n = (FastNoise)tree[i][j];

                        code.AppendLine("   SetParameters(" + n.GetSeed().ToString() + ", " + n.m_frequency.ToString() + ", " + n.m_octaves.ToString() + ", " + n.m_lacunarity.ToString() + ", " + ((int)n.m_fractalType).ToString() + ");");
                        code.Append("   float ");
                        code.Append(Id(i, j));

                        switch (n.m_noiseType)
                        {
                            case NoiseType.Value:
                                code.AppendLine(" = GetValue" + xyz);
                                break;
                            case NoiseType.ValueFractal:
                                code.AppendLine(" = GetValueFractal" + xyz);
                                break;
                            case NoiseType.Perlin:
                                code.AppendLine(" = GetPerlin" + xyz);
                                break;
                            case NoiseType.PerlinFractal:
                                code.AppendLine(" = GetPerlinFractal" + xyz);
                                break;
                            case NoiseType.Simplex:
                                code.AppendLine(" = GetSimplex" + xyz);
                                break;
                            case NoiseType.SimplexFractal:
                                code.AppendLine(" = GetSimplexFractal" + xyz);
                                break;
                            case NoiseType.Cellular:
                                code.AppendLine(" = GetCellular" + xyz);
                                break;
                            case NoiseType.WhiteNoise:
                                code.AppendLine(" = GetWhiteNoise" + xyz);
                                break;
                            case NoiseType.Cubic:
                                code.AppendLine(" = GetCubic" + xyz);
                                break;
                            case NoiseType.CubicFractal:
                                code.AppendLine(" = GetCubicFractal" + xyz);
                                break;
                        }
                    }
                    else
                    {
                        Module n = tree[i][j];

                        if (n.opType != OperatorType.Curve)
                        {
                            code.Append("   float f");
                            code.Append(i.ToString() + "_" + j.ToString());
                            code.Append(" = ");
                        }

                        switch (n.opType)
                        {
                            case OperatorType.Select:

                                float f = 1f / (2f * n.parameters[0]);

                                code.Append("lerp(");
                                code.Append(inputIds[1]);
                                code.Append(", ");
                                code.Append(inputIds[0]);
                                code.Append(", ");

                                code.Append("Select(");
                                code.Append(inputIds[2]);
                                code.Append(", ");
                                code.Append(n.parameters[2].ToString("F6"));
                                code.Append(", ");
                                code.Append(f.ToString("F6"));

                                code.AppendLine("));");

                                break;

                            case OperatorType.Curve:
                                code.Append("   static const float curve_times");
                                code.Append(i.ToString() + "_" + j.ToString());
                                code.Append("[] = ");
                                code.Append(n.curve.times.ValuesToString());
                                code.AppendLine(";");

                                code.Append("   static const float curve_values");
                                code.Append(i.ToString() + "_" + j.ToString());
                                code.Append("[] = ");
                                code.Append(n.curve.values.ValuesToString());
                                code.AppendLine(";");

                                code.Append("   float f");
                                code.Append(i.ToString() + "_" + j.ToString());

                                code.Append(" = Curve(");
                                code.Append(inputIds[0]);
                                code.Append(", curve_times");
                                code.Append(i.ToString() + "_" + j.ToString());
                                code.Append(", curve_values");
                                code.Append(i.ToString() + "_" + j.ToString());
                                code.AppendLine(");");
                                break;

                            case OperatorType.Blend:
                                code.Append("lerp(");
                                code.Append(inputIds[0]);
                                code.Append(", ");
                                code.Append(inputIds[1]);
                                code.Append(", ");
                                code.Append(n.parameters[0].ToString("F6"));
                                code.AppendLine(");");
                                break;
                            case OperatorType.Remap:

                                code.Append(inputIds[0]);
                                code.AppendLine(";");
                                EditorUtility.DisplayDialog("Not supported", "The remap operator is not supported on the GPU. For a lower-level access you can edit GetNoise() in the compute shader directly.", "Ok");
                                break;
                            case OperatorType.Add:
                                code.Append(inputIds[0]);
                                code.Append(" + ");
                                code.Append(inputIds[1]);
                                code.AppendLine(";");
                                break;

                            case OperatorType.Subtract:
                                code.Append(inputIds[0]);
                                code.Append(" - ");
                                code.Append(inputIds[1]);
                                code.AppendLine(";");
                                break;

                            case OperatorType.Multiply:
                                code.Append(inputIds[0]);
                                code.Append(" * ");
                                code.Append(inputIds[1]);
                                code.AppendLine(";");
                                break;

                            case OperatorType.Min:
                                code.Append("min(");
                                code.Append(inputIds[0]);
                                code.Append(", ");
                                code.Append(inputIds[1]);
                                code.AppendLine(");");
                                break;

                            case OperatorType.Max:
                                code.Append("max(");
                                code.Append(inputIds[0]);
                                code.Append(", ");
                                code.Append(inputIds[1]);
                                code.AppendLine(");");
                                break;

                            case OperatorType.Scale:
                                code.Append(inputIds[0]);
                                code.Append(" * ");
                                code.Append(n.parameters[0].ToString("F6"));
                                code.AppendLine(";");
                                break;

                            case OperatorType.ScaleBias:
                                code.Append("mad(");
                                code.Append(inputIds[0]);
                                code.Append(", ");
                                code.Append(n.parameters[0].ToString("F6"));
                                code.Append(", ");
                                code.Append(n.parameters[1].ToString("F6"));
                                code.AppendLine(");");
                                break;

                            case OperatorType.Abs:
                                code.Append("abs(");
                                code.Append(inputIds[0]);
                                code.AppendLine(");");
                                break;

                            case OperatorType.Invert:
                                code.Append("-");
                                code.Append(inputIds[0]);
                                code.AppendLine(";");
                                break;

                            case OperatorType.Clamp:
                                code.Append("clamp(");
                                code.Append(inputIds[0]);
                                code.Append(", ");
                                code.Append(n.parameters[0].ToString("F6"));
                                code.Append(", ");
                                code.Append(n.parameters[1].ToString("F6"));
                                code.AppendLine(");");
                                break;

                            case OperatorType.Const:
                                code.Append(n.parameters[0].ToString("F6"));
                                code.AppendLine(";");
                                break;
                            default:
                                Debug.LogError("Operator is not implemented for GPU!");
                                break;
                        }
                    }
                    code.AppendLine();
                }

                if (i == 0)
                    code.AppendLine("   return (f0_0 + 1) / 2;");
            }

            code.AppendLine("}");

            StringBuilder template = new StringBuilder(((TextAsset)Resources.Load("computeShaderTemplate")).text);
            template.Replace("~", code.ToString());
            return template.ToString();
        }

        static void BuildTree(ref List<List<Module>> tree, Module m, int l)
        {
            if (tree.Count <= l)
                tree.Add(new List<Module>());
            tree[l].Add(m);
            if (m.inputs != null)
                for (int i = 0; i < m.inputs.Length; i++)
                {
                    if (!Search(ref tree, l, m.inputs[i]))
                        BuildTree(ref tree, m.inputs[i], l + 1);
                }
        }

        static void FindGenerators(Module module, ref List<string> ids, ref List<List<Module>> tree, int l)
        {
            if (module.inputs != null)
                for (int i = 0; i < module.inputs.Length; i++)
                {
                    if (module.inputs[i].isNoise)
                    {
                        ids.Add(Id(l, tree[l].FindIndex(module.inputs[i].Equals)));
                    }
                    else
                        FindGenerators(module.inputs[i], ref ids, ref tree, l + 1);
                }
        }

        //Searches for duplicates to prevent calculating the same path multiple times because its output is used multiple times.
        static bool Search(ref List<List<Module>> tree, int c, Module m)
        {
            if (c < tree.Count)
            {
                for (int i = c; i < tree.Count; i++) //Does a duplicate exists in following layers
                    if (tree[i].Exists(m.Equals))
                    {
                        return true;
                    }

                for (int i = 0; i < c; i++) //If not, does one exist in previous layers? If so, delete the one in the previous layer (so that all modules that need it can access it) and use this one.
                {
                    int index = tree[i].IndexOf(m);

                    if (index != -1)
                    {
                        tree[i].RemoveAt(index);
                    }
                }

            }
            return false;
        }

        static string Id(int i, int j)
        {
            return "f" + i.ToString() + "_" + j.ToString();
        }
        static string IdN(int i, int j)
        {
            return i.ToString() + "_" + j.ToString();
        }

        static bool Equal(Module a, Module b)
        {
            return a.parameters == b.parameters && a.opType == b.opType && a.GetType() == b.GetType() && a.isNoise == b.isNoise;
        }

    }
}
