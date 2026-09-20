using UnityEngine;

public class MemoryTarget : MonoBehaviour
{
    [Header("Object Identity")]
    public string objectID;
    public string room = "kitchen";

    [Header("Question Settings")]
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
    Count,
    MultipleChoice,
    Color,
    Detail
}