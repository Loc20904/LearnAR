using System;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// Loads ScenarioData using a chain-of-responsibility pattern that is safe
/// under IL2CPP code stripping and does not depend on ScriptableObject
/// deserialization (which fails when the .asset has m_Script: {fileID: 0}).
///
/// Loading order:
///   1. Inspector-assigned ScriptableObject — preserves the Editor drag-and-drop
///      workflow. Used when the asset is healthy (Editor builds).
///   2. JSON TextAsset from Resources — the primary mobile path. TextAsset is a
///      built-in Unity type that deserializes reliably on all platforms.
///      JsonUtility.FromJson is IL2CPP-safe because it uses Unity's runtime
///      serialization, not managed reflection.
///   3. Hardcoded fallback — mirrors the exact scenario data from TrafficSafe.asset
///      and guarantees StartScenario() never receives null regardless of asset state.
/// </summary>
[Preserve]
public static class ScenarioLoader
{
    // Resources-relative path (no extension) of the JSON scenario file.
    // File must be at: Assets/Resources/TrafficSafeScenario.json
    private const string JsonResourcePath = "TrafficSafeScenario";

    /// <summary>
    /// Returns a valid ScenarioData. Never returns null.
    /// </summary>
    /// <param name="inspectorAssigned">
    /// The value of ARCitySpawner.scenarioDataToAssign. Passed through as
    /// Strategy 1; pass null if the field is already null on mobile.
    /// </param>
    public static ScenarioData Load(ScenarioData inspectorAssigned = null)
    {
        // ── Strategy 1: Inspector-assigned ScriptableObject ─────────────────
        if (inspectorAssigned != null)
        {
            Debug.Log("[ScenarioLoader] Using Inspector-assigned ScenarioData.");
            return inspectorAssigned;
        }

        // ── Strategy 2: JSON TextAsset ───────────────────────────────────────
        TextAsset jsonAsset = Resources.Load<TextAsset>(JsonResourcePath);
        if (jsonAsset != null)
        {
            ScenarioData fromJson = ParseJson(jsonAsset.text);
            if (fromJson != null)
            {
                Debug.Log($"[ScenarioLoader] Loaded '{fromJson.scenarioTitle}' from JSON " +
                          $"({fromJson.steps?.Length ?? 0} steps).");
                return fromJson;
            }
        }
        else
        {
            Debug.LogWarning($"[ScenarioLoader] JSON not found at Resources/{JsonResourcePath}. " +
                              "Falling back to hardcoded data.");
        }

        // ── Strategy 3: Hardcoded fallback ───────────────────────────────────
        Debug.LogWarning("[ScenarioLoader] Using hardcoded fallback scenario.");
        return BuildHardcodedFallback();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static ScenarioData ParseJson(string json)
    {
        try
        {
            ScenarioDataJson dto = JsonUtility.FromJson<ScenarioDataJson>(json);

            if (dto == null || dto.steps == null || dto.steps.Length == 0)
            {
                Debug.LogError("[ScenarioLoader] JSON parsed but scenario data is empty.");
                return null;
            }

            ScenarioData data = ScriptableObject.CreateInstance<ScenarioData>();
            data.scenarioTitle = dto.scenarioTitle;
            data.steps = new StoryStep[dto.steps.Length];

            for (int i = 0; i < dto.steps.Length; i++)
            {
                StoryStepJson src = dto.steps[i];
                StoryStep step = new StoryStep
                {
                    stepName      = src.stepName,
                    dialogueText  = src.dialogueText,
                    visualActions = ConvertVisualActions(src.visualActions),
                    choices       = ConvertChoices(src.choices),
                };

                // Resolve audio clips by Resources path; null = silent (not a crash).
                if (!string.IsNullOrEmpty(src.introVoicePath))
                    step.introVoice = Resources.Load<AudioClip>(src.introVoicePath);
                if (!string.IsNullOrEmpty(src.outroVoicePath))
                    step.outroVoice = Resources.Load<AudioClip>(src.outroVoicePath);

                data.steps[i] = step;
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScenarioLoader] JSON parse failed: {ex.Message}");
            return null;
        }
    }

    private static VisualAction[] ConvertVisualActions(VisualActionJson[] src)
    {
        if (src == null) return Array.Empty<VisualAction>();

        var result = new VisualAction[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            result[i] = new VisualAction
            {
                type            = ParseActionType(src[i].type),
                actorIndex      = src[i].actorIndex,
                parameter       = src[i].parameter,
                targetPointName = src[i].targetPointName,
                targetOffset    = src[i].targetOffset,
                duration        = src[i].duration,
                waitForFinish   = src[i].waitForFinish,
            };
        }
        return result;
    }

    private static ActionType ParseActionType(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out ActionType result))
            return result;

        Debug.LogWarning($"[ScenarioLoader] Unknown ActionType '{value}', defaulting to Wait.");
        return ActionType.Wait;
    }

    private static Choice[] ConvertChoices(ChoiceJson[] src)
    {
        if (src == null) return Array.Empty<Choice>();

        var result = new Choice[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            result[i] = new Choice
            {
                choiceText    = src[i].choiceText,
                isCorrect     = src[i].isCorrect,
                feedback      = src[i].feedback,
                nextStepIndex = src[i].nextStepIndex,
            };
        }
        return result;
    }

    /// <summary>
    /// Exact scenario data recovered from the broken TrafficSafe.asset YAML.
    /// Audio clips are intentionally omitted (null = silent); voices play when
    /// the audio files are reachable via Resources but the game never crashes
    /// without them. ScenarioController.PlayVoice() null-checks before playing.
    /// </summary>
    private static ScenarioData BuildHardcodedFallback()
    {
        ScenarioData data = ScriptableObject.CreateInstance<ScenarioData>();
        data.scenarioTitle = "Traffic safe";
        data.steps = new[]
        {
            // Step 0 – Intro: boy walks to crosswalk, player chooses action
            new StoryStep
            {
                stepName     = "Intro",
                dialogueText = "",
                introVoice   = Resources.Load<AudioClip>("Audio/VoiceStepIntro"),
                outroVoice   = Resources.Load<AudioClip>("Audio/VoiceOuttroStepIntro"),
                visualActions = new[]
                {
                    new VisualAction { type = ActionType.Move,   actorIndex = 0, targetPointName = "BoyMove1", duration = 8f,   waitForFinish = true  },
                    new VisualAction { type = ActionType.Rotate, actorIndex = 0, targetOffset = new Vector3(0f, 90f, 0f), duration = 0.5f, waitForFinish = true  },
                },
                choices = new[]
                {
                    new Choice { choiceText = "Băng qua đường", isCorrect = false, feedback = "", nextStepIndex = 1 },
                    new Choice { choiceText = "Chờ xe qua hết", isCorrect = true,  feedback = "", nextStepIndex = 2 },
                },
            },

            // Step 1 – Incorrect: car hits boy, falling animation
            new StoryStep
            {
                stepName     = "Incorrect",
                dialogueText = "",
                outroVoice   = Resources.Load<AudioClip>("Audio/IncorrectVoice"),
                visualActions = new[]
                {
                    new VisualAction { type = ActionType.Move,    actorIndex = 1, targetPointName = "Car1Move1",    duration = 2f,  waitForFinish = false },
                    new VisualAction { type = ActionType.Move,    actorIndex = 0, targetPointName = "BoyMove2",     duration = 2f,  waitForFinish = true  },
                    new VisualAction { type = ActionType.Animate, actorIndex = 0, parameter = "falling-down",       duration = 10f, waitForFinish = true  },
                },
                choices = new[]
                {
                    new Choice { choiceText = "Restart", isCorrect = false, feedback = "", nextStepIndex = 0 },
                },
            },

            // Step 2 – Correct: car passes, boy crosses safely
            new StoryStep
            {
                stepName     = "correct",
                dialogueText = "",
                introVoice   = Resources.Load<AudioClip>("Audio/CorectVoice"),
                visualActions = new[]
                {
                    new VisualAction { type = ActionType.Move, actorIndex = 1, targetPointName = "Car1Move2", duration = 5f, waitForFinish = true },
                    new VisualAction { type = ActionType.Move, actorIndex = 0, targetPointName = "BoyMove3",  duration = 5f, waitForFinish = true },
                },
                choices = new[]
                {
                    new Choice { choiceText = "Restart", isCorrect = false, feedback = "", nextStepIndex = 0 },
                },
            },
        };

        return data;
    }
}
