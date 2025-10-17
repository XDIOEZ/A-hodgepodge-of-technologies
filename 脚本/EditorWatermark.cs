using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorWatermark
{
    static EditorWatermark()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal = { textColor = new Color(1f, 1f, 1f, 1f) },
            alignment = TextAnchor.LowerRight
        };

        GUILayout.BeginArea(new Rect(0, 0, sceneView.position.width - 10, sceneView.position.height - 10));
        GUILayout.Label("此展馆软件由 罗非鱼 开发  QQ: 202332793", style);
        GUILayout.EndArea();

        Handles.EndGUI();
    }
}
