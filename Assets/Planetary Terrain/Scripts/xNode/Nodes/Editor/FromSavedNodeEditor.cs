using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain;
using XNodeEditor;
using UnityEditor;

[CustomNodeEditor(typeof(FromSavedNode))]
public class FromSavedNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        base.OnBodyGUILight();

        var node = (FromSavedNode)target;

        node.path = EditorGUILayout.TextField("Path", node.path);
        if (GUILayout.Button("Select"))
            node.path = EditorUtility.OpenFilePanel("Noise Module", Application.dataPath, "txt");

        if (node.previewChanged)
        {
            if (node.previewHeightmap == null) return;
            node.preview = node.previewHeightmap.GetTexture2D();
            node.previewChanged = false;
            NodeEditorWindow.current.Repaint();
        }

        if (node.preview == null)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                node.UpdatePreview();
            }, null);
        }

        var centered = new GUIStyle();
        centered.alignment = TextAnchor.UpperCenter;

        GUILayout.Label(node.preview, centered);
    }
}