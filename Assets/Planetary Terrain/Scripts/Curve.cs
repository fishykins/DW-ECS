using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PlanetaryTerrain.Noise
{
    [System.Serializable]


    /// <summary>
    /// Very simple implementation of a cubic-interpolated curve. Needed because Unitys built in Animation Curve is neither thread-save nor very fast.
    /// </summary>
    public class FloatCurve
    {
        public List<float> times = new List<float>() { 0f, 0.25f, 0.75f, 1f };
        public List<float> values = new List<float>() { 0f, 0.0625f, 0.5625f, 1f };

        public float Evaluate(float time)
        {
            if (times.Count == values.Count && times.Count > 0)
            {

                time = (time + 1f) / 2f; //Scaling to 0-1 scale
                int index;
                for (index = 0; index < times.Count; index++)
                {
                    if (time < times[index])
                        break;

                }
                int length = times.Count - 1;

                int index0 = Mathf.Clamp(index - 2, 0, length);
                int index1 = Mathf.Clamp(index - 1, 0, length);
                int index2 = Mathf.Clamp(index, 0, length);
                int index3 = Mathf.Clamp(index + 1, 0, length);

                if (index1 == index2)
                    return values[index1];

                float alpha = (time - times[index1]) / (times[index2] - times[index1]);

                return (Utils.CubicInterpolation(values[index0], values[index1], values[index2], values[index3], alpha) * 2f) - 1f; //Scaling back to -1 to 1.
            }
            else
            {
                return 0f;
            }
        }

        public void AddKey(float time, float value)
        {
            int index;
            for (index = 0; index < times.Count; index++)
            {
                if (time < times[index])
                    break;
            }
            times.Insert(index, time);
            values.Insert(index, value);
        }

        public FloatCurve(AnimationCurve animCurve, int accuracy = 25)
        {
            Clear();
            for (int i = 0; i < accuracy; i++)
            {
                times.Add(i / (accuracy - 1f));
                values.Add(Mathf.Clamp01(animCurve.Evaluate(i / (accuracy - 1f))));
            }
        }
        public FloatCurve() { }
        public void Clear()
        {
            times.Clear();
            values.Clear();
        }


    }
}
