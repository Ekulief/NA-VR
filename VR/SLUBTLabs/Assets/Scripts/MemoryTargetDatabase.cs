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

[System.Serializable]
public class QuestionData
{
    public string questionText;
    public QuestionType questionType;
    public string correctAnswer;
    public string[] multipleChoiceOptions;
}

public enum QuestionType
{
    YesNo,
    MultipleChoice,
    Color,
    Detail,
    Count
}