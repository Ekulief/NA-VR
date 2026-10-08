#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RevealPersistentData
{
    [MenuItem("SLUBT Labs/Open Save Folder")]
    static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif