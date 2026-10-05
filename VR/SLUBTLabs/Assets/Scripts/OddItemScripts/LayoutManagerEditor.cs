#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for LayoutManager.
/// Adds a Refresh button and a session dropdown so instructors can
/// pick which saved layout to replay without typing file paths.
/// </summary>
[CustomEditor(typeof(LayoutManager))]
public class LayoutManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LayoutManager mgr = (LayoutManager)target;

        // Draw all default fields except the ones we're customising
        DrawPropertiesExcluding(serializedObject,
            "selectedSessionIndex",
            "savedSessionNames",
            "savedSessionPaths");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Saved Sessions", EditorStyles.boldLabel);

        // Refresh button
        if (GUILayout.Button("Refresh Session List"))
        {
            mgr.RefreshSessionList();
            EditorUtility.SetDirty(target);
        }

        if (mgr.savedSessionNames == null || mgr.savedSessionNames.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No saved sessions found. Run the experiment at least once to generate a layout.",
                MessageType.Info);
        }
        else
        {
            // Dropdown to pick a session
            string[] options = mgr.savedSessionNames.ToArray();
            int newIndex = EditorGUILayout.Popup(
                "Replay Session",
                Mathf.Clamp(mgr.selectedSessionIndex, 0, options.Length - 1),
                options
            );

            if (newIndex != mgr.selectedSessionIndex)
            {
                mgr.selectedSessionIndex = newIndex;
                EditorUtility.SetDirty(target);
            }

            // Show selected session info
            if (mgr.selectedSessionIndex < mgr.savedSessionNames.Count)
            {
                EditorGUILayout.HelpBox(
                    $"Selected: {mgr.savedSessionNames[mgr.selectedSessionIndex]}\n" +
                    $"Path: {(mgr.savedSessionPaths.Count > mgr.selectedSessionIndex ? mgr.savedSessionPaths[mgr.selectedSessionIndex] : "unknown")}",
                    MessageType.None);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif