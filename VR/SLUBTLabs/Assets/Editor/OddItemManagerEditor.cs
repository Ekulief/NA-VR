#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for OddItemManager.
/// Shows a dropdown of all odd item prefabs so the instructor can
/// pick which one to inject without typing names.
/// Put this file in Assets/Editor/
/// </summary>
[CustomEditor(typeof(OddItemManager))]
public class OddItemManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        OddItemManager mgr = (OddItemManager)target;

        // Draw all fields except the ones we customise
        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "selectedOddItemIndex");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Odd Item Selection", EditorStyles.boldLabel);

        if (mgr.oddItemPrefabs == null || mgr.oddItemPrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Add prefabs to the Odd Item Prefabs list above to enable the dropdown.",
                MessageType.Info);
        }
        else
        {
            // Build dropdown options from prefab names
            string[] options = new string[mgr.oddItemPrefabs.Count];
            for (int i = 0; i < mgr.oddItemPrefabs.Count; i++)
            {
                options[i] = mgr.oddItemPrefabs[i] != null
                    ? mgr.oddItemPrefabs[i].name
                    : $"(empty slot {i})";
            }

            int clampedIndex = Mathf.Clamp(mgr.selectedOddItemIndex, 0, options.Length - 1);
            int newIndex = EditorGUILayout.Popup("Selected Odd Item", clampedIndex, options);

            if (newIndex != mgr.selectedOddItemIndex)
            {
                Undo.RecordObject(mgr, "Change Selected Odd Item");
                mgr.selectedOddItemIndex = newIndex;
                EditorUtility.SetDirty(mgr);
            }

            EditorGUILayout.HelpBox(
                $"This trial will inject: \"{options[clampedIndex]}\"",
                MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif