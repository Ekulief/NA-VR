using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "KitchenDatabase", menuName = "SLUBT/Memory Target Database")]
public class MemoryTargetDatabase : ScriptableObject
{
    public List<MemoryTargetEntry> entries;
}

[System.Serializable]
public class MemoryTargetEntry
{
    public string gameID;
    public string objectName;
    public string room;
    public QuestionData[] questions;
}