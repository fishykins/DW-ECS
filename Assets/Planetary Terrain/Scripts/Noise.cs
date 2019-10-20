using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

namespace PlanetaryTerrain.Noise
{
    public enum OperatorType { Select, Curve, Blend, Remap, Add, Subtract, Multiply, Min, Max, Scale, ScaleBias, Abs, Invert, Clamp, Const }

    [System.Serializable]
    public class Select : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float cv = inputs[2].GetNoise(x, y, z);

            if (parameters[0] > 0f)
            {
                float a;
                if (cv < (parameters[1] - parameters[0]))
                {
                    return inputs[0].GetNoise(x, y, z);
                }

                if (cv < (parameters[1] + parameters[0]))
                {
                    float lc = (parameters[1] - parameters[0]);
                    float uc = (parameters[1] + parameters[0]);
                    a = MapCubicSCurve((cv - lc) / (uc - lc));
                    return InterpolateLinear(inputs[0].GetNoise(x, y, z), inputs[1].GetNoise(x, y, z), a);
                }

                if (cv < (parameters[2] - parameters[0]))
                {
                    return inputs[1].GetNoise(x, y, z);
                }

                if (cv < (parameters[2] + parameters[0]))
                {
                    float lc = (parameters[2] - parameters[0]);
                    float uc = (parameters[2] + parameters[0]);
                    a = MapCubicSCurve((cv - lc) / (uc - lc));
                    return InterpolateLinear(inputs[1].GetNoise(x, y, z), inputs[0].GetNoise(x, y, z), a);
                }
                return inputs[0].GetNoise(x, y, z);
            }

            if (cv < parameters[1] || cv > parameters[2])
            {
                return inputs[0].GetNoise(x, y, z);
            }
            return inputs[1].GetNoise(x, y, z);
        }
        static float MapCubicSCurve(float value)
        {
            return (value * value * (3f - 2f * value));
        }

        static float InterpolateLinear(float a, float b, float position)
        {
            return ((1f - position) * a) + (position * b);
        }

        public Select(Module terrainType, Module noise1, Module noise2, float fallOff = 0.175f, float min = -1f, float max = 0f)
        {
            opType = OperatorType.Select;

            inputs = new Module[3];
            parameters = new float[3];

            inputs[0] = noise1;
            inputs[1] = noise2;
            inputs[2] = terrainType;

            parameters[0] = fallOff;
            parameters[1] = min;
            parameters[2] = max;
        }
    }
    [System.Serializable]
    public class Const : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return parameters[0];
        }
        public Const(float constant)
        {
            opType = OperatorType.Const;

            parameters = new float[1];
            this.parameters[0] = constant;
        }
    }
    [System.Serializable]
    public class Add : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) + inputs[1].GetNoise(x, y, z);
        }
        public Add(Module module1, Module module2)
        {
            opType = OperatorType.Add;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }
    [System.Serializable]
    public class Multiply : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) * inputs[1].GetNoise(x, y, z);
        }
        public Multiply(Module module1, Module module2)
        {
            opType = OperatorType.Multiply;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }
    [System.Serializable]
    public class Scale : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) * parameters[0];
        }
        public Scale(Module module1, float scale)
        {
            opType = OperatorType.Scale;

            inputs = new Module[1];
            parameters = new float[1];

            inputs[0] = module1;
            parameters[0] = scale;

        }
    }
    [System.Serializable]
    public class ScaleBias : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) * parameters[0] + parameters[1];
        }
        public ScaleBias(Module module1, float scale, float bias)
        {
            opType = OperatorType.ScaleBias;

            inputs = new Module[1];
            parameters = new float[2];

            inputs[0] = module1;
            parameters[0] = scale;
            parameters[1] = bias;

        }
    }
    [System.Serializable]
    public class Abs : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return Mathf.Abs(inputs[0].GetNoise(x, y, z));
        }
        public Abs(Module module1)
        {
            opType = OperatorType.Abs;

            inputs = new Module[1];

            inputs[0] = module1;

        }
    }
    [System.Serializable]
    public class Clamp : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return Mathf.Clamp(inputs[0].GetNoise(x, y, z), parameters[0], parameters[1]);
        }
        public Clamp(Module module1, float min, float max)
        {
            opType = OperatorType.Clamp;

            inputs = new Module[1];
            parameters = new float[2];

            inputs[0] = module1;
            parameters[0] = min;
            parameters[1] = max;

        }
    }
    [System.Serializable]
    public class Curve : Module
    {

        public override float GetNoise(float x, float y, float z)
        {
            return curve.Evaluate(inputs[0].GetNoise(x, y, z));
        }
        public Curve(Module module1, AnimationCurve curve)
        {
            opType = OperatorType.Curve;

            inputs = new Module[1];

            inputs[0] = module1;
            this.curve = new FloatCurve(curve);

        }
    }

    [System.Serializable]
    public class Subtract : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) - inputs[1].GetNoise(x, y, z);
        }
        public Subtract(Module module1, Module module2)
        {
            opType = OperatorType.Subtract;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }

    [System.Serializable]
    public class Blend : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float a = inputs[0].GetNoise(x, y, z);
            float b = inputs[1].GetNoise(x, y, z);

            return a + parameters[0] * (b - a);
        }
        public Blend(Module module1, Module module2, float bias = 0.5f)
        {
            opType = OperatorType.Blend;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;

            parameters = new float[1];
            this.parameters[0] = bias;
        }
    }

    [System.Serializable]
    public class Remap : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            //Scale and offset coordiantes
            return inputs[0].GetNoise(
                (x * parameters[0]) + parameters[3],
                (y * parameters[1]) + parameters[4],
                (z * parameters[2]) + parameters[5]
                  );

        }
        public Remap(Module module1, float scaleX, float scaleY, float scaleZ, float offsetX, float offsetY, float offsetZ)
        {
            opType = OperatorType.Remap;

            inputs = new Module[1];

            inputs[0] = module1;

            parameters = new float[6];

            this.parameters[0] = scaleX;
            this.parameters[1] = scaleY;
            this.parameters[2] = scaleZ;

            this.parameters[3] = offsetX;
            this.parameters[4] = offsetY;
            this.parameters[5] = offsetZ;

        }

        public Remap(Module module1, float[] parameters)
        {
            opType = OperatorType.Remap;

            inputs = new Module[1];

            inputs[0] = module1;

            if (parameters.Length != 6)
                throw new System.ArgumentOutOfRangeException("parameters[]", "Size of parameters for Remap needs to be 6.");
            this.parameters = parameters;
        }

    }

    [System.Serializable]
    public class Min : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float a = inputs[0].GetNoise(x, y, z);
            float b = inputs[1].GetNoise(x, y, z);

            if (b < a)
                return b;
            return a;

        }
        public Min(Module module1, Module module2)
        {
            opType = OperatorType.Min;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }


    [System.Serializable]
    public class Max : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float a = inputs[0].GetNoise(x, y, z);
            float b = inputs[1].GetNoise(x, y, z);

            if (b > a)
                return b;
            return a;

        }
        public Max(Module module1, Module module2)
        {
            opType = OperatorType.Max;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }

    [System.Serializable]
    public class Invert : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return -inputs[0].GetNoise(x, y, z);
        }
        public Invert(Module module1)
        {
            opType = OperatorType.Invert;

            inputs = new Module[1];

            inputs[0] = module1;
        }
    }



    [System.Serializable]
    public abstract class Module
    {
        public Module[] inputs;
        public float[] parameters;
        public OperatorType opType;
        public FloatCurve curve;
        public bool isNoise = false;

        public abstract float GetNoise(float x, float y, float z);

        public void Serialize(FileStream fs)
        {
            BinaryFormatter bf = new BinaryFormatter();

            try
            {
                bf.Serialize(fs, this);
            }
            catch (SerializationException e)
            {
                Debug.Log("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }

        }


        public override bool Equals(object obj)
        {
            if (!(obj is Module)) return false;

            var m = obj as Module;

            if (obj == null || GetType() != obj.GetType())
                return false;

            if (!(m is FastNoise))
            {
                if(this is FastNoise)
                    return false;

                if (m.parameters != parameters || m.opType != opType || m.curve != curve || m.isNoise != isNoise)
                    return false;

                if (m.inputs != null && inputs != null)
                {
                    if (m.inputs.Length != inputs.Length) return false;

                    for (int i = 0; i < inputs.Length; i++)
                        if (!inputs[i].Equals(m.inputs[i])) return false;
                }
                else if (!(inputs == null && m.inputs == null)) return false;
            }
            else
            {
                if(!(this is FastNoise))
                    return false;
                
                var f1 = this as FastNoise;
                var f2 = m as FastNoise;

                if(f1.m_fractalType != f2.m_fractalType || f1.m_frequency != f2.m_frequency || f1.m_lacunarity != f2.m_lacunarity || f1.m_noiseType != f2.m_noiseType || f1.m_octaves != f2.m_octaves)
                    return false;
            }
            return true;
        }


        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }
    }
}
