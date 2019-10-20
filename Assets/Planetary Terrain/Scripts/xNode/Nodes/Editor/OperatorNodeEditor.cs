using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;
using PlanetaryTerrain.Noise;
using UnityEditor;
using System.Threading;

[CustomNodeEditor(typeof(OperatorNode))]
public class OperatorNodeEditor : NodeEditor
{
    public static readonly string[] noPort = { "output" };
    public static readonly string[] onePort = { "input0", "output" };
    public static readonly string[] twoPorts = { "input0", "input1", "output" };
    public static readonly string[] threePorts = { "input0", "input1", "input2", "output" };

    private int numberOfInputs;

    bool hasChanged;

    public override void OnBodyGUI()
    {

        var node = (OperatorNode)target;

        node.opType = (OperatorType)UnityEditor.EditorGUILayout.EnumPopup("OperatorType", node.opType);

        if (node.parameters == null)
            node.parameters = new float[6];
        if (node.opType == OperatorType.Curve && node.curve == null)
            node.curve = new AnimationCurve();

        switch (node.opType)
        {
            case OperatorType.Select:
                numberOfInputs = 3;
                node.parameters[0] = EditorGUILayout.FloatField("Fall Off", node.parameters[0]);
                node.parameters[1] = EditorGUILayout.FloatField("Min", node.parameters[1]);
                node.parameters[2] = EditorGUILayout.FloatField("Max", node.parameters[2]);
                break;

            case OperatorType.Curve:
                numberOfInputs = 1;
                node.curve = EditorGUILayout.CurveField("Curve", node.curve);
                break;

            case OperatorType.Blend:
                numberOfInputs = 2;
                node.parameters[0] = EditorGUILayout.FloatField("Bias", node.parameters[0]);
                break;

            case OperatorType.Remap:
                numberOfInputs = 1;
                Vector3 scale = EditorGUILayout.Vector3Field("Scale", new Vector3(node.parameters[0], node.parameters[1], node.parameters[2]));
                Vector3 offset = EditorGUILayout.Vector3Field("Offset", new Vector3(node.parameters[3], node.parameters[4], node.parameters[5]));
                node.parameters = new float[] { scale.x, scale.y, scale.z, offset.x, offset.y, offset.z };
                break;

            case OperatorType.Add:
                numberOfInputs = 2;
                break;

            case OperatorType.Subtract:
                numberOfInputs = 2;
                break;

            case OperatorType.Multiply:
                numberOfInputs = 2;
                break;

            case OperatorType.Min:
                numberOfInputs = 2;
                break;

            case OperatorType.Max:
                numberOfInputs = 2;
                break;

            case OperatorType.Scale:
                numberOfInputs = 1;
                node.parameters[0] = EditorGUILayout.FloatField("Scale", node.parameters[0]);
                break;

            case OperatorType.ScaleBias:
                numberOfInputs = 1;
                node.parameters[0] = EditorGUILayout.FloatField("Scale", node.parameters[0]);
                node.parameters[1] = EditorGUILayout.FloatField("Bias", node.parameters[1]);
                break;

            case OperatorType.Abs:
                numberOfInputs = 1;
                break;

            case OperatorType.Clamp:
                numberOfInputs = 1;
                node.parameters[0] = EditorGUILayout.FloatField("Min", node.parameters[0]);
                node.parameters[1] = EditorGUILayout.FloatField("Max", node.parameters[1]);
                break;

            case OperatorType.Invert:
                numberOfInputs = 1;
                break;

            case OperatorType.Const:
                numberOfInputs = 0;
                node.parameters[0] = EditorGUILayout.FloatField("Constant", node.parameters[0]);
                break;
        }

        switch (numberOfInputs)
        {
            case 0:
                base.OnBodyGUISelected(noPort);
                break;
            case 1:
                base.OnBodyGUISelected(onePort);
                break;
            case 2:
                base.OnBodyGUISelected(twoPorts);
                break;
            case 3:
                base.OnBodyGUISelected(threePorts);
                break;
        }
        if (node.preview == null)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                node.UpdatePreview();
            }, null);
        }
        if (node.previewChanged)
        {
            if (node.previewHeightmap == null) return;
            node.preview = node.previewHeightmap.GetTexture2D();
            node.previewChanged = false;
            NodeEditorWindow.current.Repaint();
        }

        var centered = new GUIStyle();
        centered.alignment = TextAnchor.UpperCenter;

        GUILayout.Label(node.preview, centered);

    }


}
