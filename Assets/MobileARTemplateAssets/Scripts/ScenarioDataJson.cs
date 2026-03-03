using System;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// Plain-data DTOs used exclusively for JSON deserialization via JsonUtility.
///
/// These mirror the runtime data classes (StoryStep, VisualAction, Choice) but
/// replace Unity asset references (AudioClip, Sprite) with Resources-relative
/// path strings that are resolved at load time by ScenarioLoader.
///
/// All types are [Serializable] and [Preserve] so JsonUtility can deserialize
/// them correctly under IL2CPP with code stripping enabled.
/// </summary>

[Preserve]
[Serializable]
public class VisualActionJson
{
    public string type;             // "Move" | "Rotate" | "Animate" | "Wait"
    public int    actorIndex;
    public string parameter;        // animation state name for Animate actions
    public string targetPointName;  // named child transform for Move (absolute)
    public Vector3 targetOffset;    // Euler angles for Rotate; local offset for Move (relative)
    public float  duration;
    public bool   waitForFinish;
}

[Preserve]
[Serializable]
public class ChoiceJson
{
    public string choiceText;
    public bool   isCorrect;
    public string feedback;
    public int    nextStepIndex;    // 0 = restart scenario from beginning
}

[Preserve]
[Serializable]
public class StoryStepJson
{
    public string stepName;
    public string dialogueText;
    public string introVoicePath;   // Resources-relative path (no extension), empty = no audio
    public string outroVoicePath;   // Resources-relative path (no extension), empty = no audio
    public VisualActionJson[] visualActions;
    public ChoiceJson[]       choices;
}

[Preserve]
[Serializable]
public class ScenarioDataJson
{
    public string        scenarioTitle;
    public StoryStepJson[] steps;
}
