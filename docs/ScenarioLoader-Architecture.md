# Scenario Loading Architecture Fix

## Problem

`Assets/Resources/TrafficSafe.asset` had `m_Script: {fileID: 0}`, meaning the asset lost its script reference. The YAML data was intact but Unity had no type to deserialize it into. On mobile IL2CPP builds this caused a fatal crash on Start:

```
FATAL ERROR: Bien 'data' (ScenarioData) dang bi NULL tren Prefab!
```

Adding `[Preserve]` attributes or retrying `Resources.Load<ScenarioData>()` could not fix this — it is a structural asset problem, not a runtime logic problem. See `IL2CPP.md` for the full root cause analysis.

---

## Solution

Replaced the single broken loading path with a **chain-of-responsibility loader** (`ScenarioLoader`) that guarantees `ScenarioController` always receives valid, non-null data regardless of build target or asset health.

---

## Files Changed

### New: `Assets/MobileARTemplateAssets/Scripts/ScenarioDataJson.cs`

Plain `[Serializable]` DTO classes that mirror the runtime data model (`StoryStep`, `VisualAction`, `Choice`) but replace Unity asset references (`AudioClip`, `Sprite`) with `string` resource paths.

```
ScenarioDataJson
  scenarioTitle: string
  steps: StoryStepJson[]
    stepName, dialogueText: string
    introVoicePath, outroVoicePath: string   ← Resources-relative path, no extension
    visualActions: VisualActionJson[]
      type: string                           ← "Move" | "Rotate" | "Animate" | "Wait"
      actorIndex: int
      parameter, targetPointName: string
      targetOffset: Vector3
      duration: float
      waitForFinish: bool
    choices: ChoiceJson[]
      choiceText, feedback: string
      isCorrect: bool
      nextStepIndex: int
```

`JsonUtility.FromJson` uses Unity's runtime serializer — no managed reflection — making it safe under IL2CPP with code stripping enabled. All types carry `[Preserve]` to prevent stripping.

---

### New: `Assets/MobileARTemplateAssets/Scripts/ScenarioLoader.cs`

Static loader that runs three strategies in order and returns the first valid result. **Never returns null.**

```
ScenarioLoader.Load(inspectorAssigned)
  │
  ├─ Strategy 1: Inspector-assigned ScriptableObject
  │    Used when: the .asset file is healthy (Editor builds, healthy mobile builds)
  │    Result: returned as-is, zero overhead
  │
  ├─ Strategy 2: JSON TextAsset from Resources
  │    File: Assets/Resources/TrafficSafeScenario.json
  │    Used when: Strategy 1 is null (broken asset on mobile)
  │    How: Resources.Load<TextAsset> → JsonUtility.FromJson<ScenarioDataJson>
  │         → ScriptableObject.CreateInstance<ScenarioData>() populated from DTO
  │         → audio resolved via Resources.Load<AudioClip>(path)
  │
  └─ Strategy 3: Hardcoded fallback
       Used when: JSON file is missing or malformed
       Content: verbatim transcription of the original TrafficSafe.asset YAML data
                (all targetPointNames, durations, choices, actor indices preserved)
       Audio: loaded via Resources.Load<AudioClip>; null = silent, not a crash
```

`ScenarioController.PlayVoice()` already null-checks `AudioClip` before playing, so silent fallback is safe.

---

### New: `Assets/Resources/TrafficSafeScenario.json`

The actual scenario data, transcribed exactly from the readable (but undeserializable) YAML in `TrafficSafe.asset`. Three steps:

| Step | Name | Description |
|------|------|-------------|
| 0 | Intro | Boy walks to crosswalk (`BoyMove1`), rotates 90°. Player chooses to cross or wait. |
| 1 | Incorrect | Car moves to `Car1Move1`, boy moves to `BoyMove2`, boy plays `falling-down` animation. |
| 2 | correct | Car moves to `Car1Move2`, boy crosses to `BoyMove3`. |

Audio paths reference `Assets/Resources/Audio/` (see audio section below).

To add or modify scenario content, edit this file. No code changes required.

---

### Modified: `Assets/MobileARTemplateAssets/Scripts/ARCitySpawner.cs`

**`Awake()`** — replaced ~15 lines of broken loading logic with a single call:

```csharp
// Before
if (scenarioDataToAssign == null && !string.IsNullOrEmpty(scenarioResourceName))
{
    scenarioDataToAssign = Resources.Load<ScenarioData>(scenarioResourceName);
    // always null on mobile → fatal crash downstream
}

// After
scenarioDataToAssign = ScenarioLoader.Load(scenarioDataToAssign);
```

**Removed field**: `public string scenarioResourceName` — dead code after this change.

No other methods in `ARCitySpawner` were changed.

---

### Moved: Audio files → `Assets/Resources/Audio/`

The four audio clips were in `Assets/MobileARTemplateAssets/Scripts/` (wrong location — not inside any `Resources` folder, so `Resources.Load` could not reach them at runtime).

| Old path | New path | Resources key |
|----------|----------|---------------|
| `Scripts/VoiceStepIntro.mp3` | `Resources/Audio/VoiceStepIntro.mp3` | `Audio/VoiceStepIntro` |
| `Scripts/VoiceOuttroStepIntro.mp3` | `Resources/Audio/VoiceOuttroStepIntro.mp3` | `Audio/VoiceOuttroStepIntro` |
| `Scripts/IncorrectVoice.mp3` | `Resources/Audio/IncorrectVoice.mp3` | `Audio/IncorrectVoice` |
| `Scripts/CorectVoice.mp3` | `Resources/Audio/CorectVoice.mp3` | `Audio/CorectVoice` |

Each `.meta` file was moved alongside its asset, preserving GUIDs. All existing Inspector references in prefabs and scenes remain valid after Unity reimports.

---

## Files Not Changed

`ScenarioController`, `SequenceController`, `StoryStep`, `ScenarioUIManager`, `BoyAnimationController`, `CarController` — none of these were modified.

---

## Adding New Scenarios

1. Edit `Assets/Resources/TrafficSafeScenario.json` following the existing structure.
2. Place audio files in `Assets/Resources/Audio/` and reference them by filename without extension.
3. Named waypoints (`targetPointName`) must exist as child transforms inside the city prefab — add them in the Unity Editor.
4. If the Inspector-assigned `ScenarioData` asset on `ARCitySpawner` is populated and valid, it takes priority over the JSON file.
