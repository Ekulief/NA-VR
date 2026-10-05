#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for ExperimentAnchor.
/// Shows a dropdown of all scenes in Build Settings so you don't have to type scene names manually.
/// Put this file in Assets/Editor/
/// </summary>
[CustomEditor(typeof(ExperimentAnchor))]
public class ExperimentAnchorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ExperimentAnchor anchor = (ExperimentAnchor)target;

        EditorGUILayout.LabelField("Dynamic Target", EditorStyles.boldLabel);

        // Build list of scene names from Build Settings
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No scenes in Build Settings. Add scenes via File → Build Settings.",
                MessageType.Warning);
            DrawDefaultInspector();
            return;
        }

        // Collect scene names
        string[] sceneNames = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(scenes[i].path);

        // Find current index
        int currentIndex = System.Array.IndexOf(sceneNames, anchor.sceneToLoad);
        if (currentIndex < 0) currentIndex = 0;

        // Draw dropdown
        int newIndex = EditorGUILayout.Popup("Scene To Load", currentIndex, sceneNames);

        if (newIndex != currentIndex || anchor.sceneToLoad != sceneNames[newIndex])
        {
            Undo.RecordObject(anchor, "Change Scene To Load");
            anchor.sceneToLoad = sceneNames[newIndex];
            EditorUtility.SetDirty(anchor);
        }

        EditorGUILayout.HelpBox($"Will load: \"{anchor.sceneToLoad}\"", MessageType.None);

        // Draw remaining fields normally
        EditorGUILayout.Space(4);
        DrawPropertiesExcluding(serializedObject, "m_Script", "sceneToLoad");
        serializedObject.ApplyModifiedProperties();
    }
}
#endif