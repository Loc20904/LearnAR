# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**LearnAR** is an AR educational game built with Unity 6000.0.65f1, targeting Android (ARCore) and iOS (ARKit). It teaches traffic safety to children through interactive AR scenarios where a 3D city spawns on real surfaces.

## Build & Development

This is a Unity project — there are no CLI build/test commands. Development happens inside **Unity Editor 6000.0.65f1**.

- **Main scene**: `Assets/Scenes/SampleScene.unity`
- **Play in Editor**: Press Play button (uses AR simulation or device stream)
- **Mobile build**: File → Build Settings → Android/iOS → Build

### Unity Package Manager
All dependencies are in `Packages/manifest.json`. Key packages:
- `com.unity.xr.arfoundation` 6.0.6 — AR Foundation cross-platform layer
- `com.unity.xr.arcore` 6.3.1 / `com.unity.xr.arkit` 6.3.1 — platform backends
- `com.unity.xr.interaction.toolkit` 3.3.0 — XR input and interaction
- `com.unity.render-pipelines.universal` 17.0.4 — URP rendering

## Architecture

### Game Flow

```
App Launch
  → ARCitySpawner waits for user tap on detected AR plane
  → SpawnAtPlaneCenter() instantiates city prefab, fixes vertical offset
  → SetupAfterSpawn() (2-frame delay) injects ScenarioData + UIManager into ScenarioController
  → ShowStartButton() → user taps Start
  → ScenarioController.StartScenario() → ShowStep(0)
  → ExecuteStepRoutine(): intro voice → visual actions → outro voice → display choices
  → OnChoiceSelected() jumps to nextStepIndex (0 = restart)
```

### Key Scripts (`Assets/MobileARTemplateAssets/Scripts/`)

| Script | Role |
|--------|------|
| `ARCitySpawner.cs` | AR raycast, city prefab spawning, injects dependencies post-spawn |
| `ScenarioController.cs` | Orchestrates step playback (audio + visuals + choices) via coroutines |
| `SequenceController.cs` | Executes `VisualAction[]` — Move/Rotate/Animate/Wait on named actor GameObjects |
| `ScenarioUIManager.cs` | Creates choice buttons, Start button; bridges UI events to `ScenarioController` |
| `StoryStep.cs` | Data classes: `ScenarioData`, `StoryStep`, `VisualAction`, `Choice` |
| `BoyAnimationController.cs` | Boy character FSM — idle/walk/fall triggered by `CarController` |
| `CarController.cs` | Moves car forward, sphere-detects boy, stops and triggers fall |
| `GoalManager.cs` | Onboarding state machine guiding user through AR surface detection |

### Data Model (`StoryStep.cs`)

```
ScenarioData (ScriptableObject)
  scenarioTitle: string
  steps: StoryStep[]
    stepName, dialogueText
    introVoice, outroVoice: AudioClip
    visualActions: VisualAction[]
      type: Move | Rotate | Animate | Wait
      actorIndex: int
      targetPointName: string   // named child transform for absolute position
      targetOffset: Vector3     // relative offset alternative
      parameter: string         // animation state name
      duration: float
      waitForFinish: bool
    choices: Choice[]
      choiceText, isCorrect, feedback: string
      nextStepIndex: int        // 0 = restart scenario
```

### Dependency Injection Pattern
`ARCitySpawner` spawns the city prefab and then (after 2 frames for Awake/Start) manually injects `ScenarioData` and `ScenarioUIManager` into the prefab's `ScenarioController`. Do not rely on Inspector-assigned references inside the city prefab itself.

### IL2CPP / `[Preserve]` Attributes
All runtime-critical classes use `[Preserve]` (from `UnityEngine.Scripting`) to prevent IL2CPP code stripping. `link.xml` at project root preserves all of `Assembly-CSharp`. **Do not remove these.**

## Scenario Loading Architecture

`Assets/Resources/TrafficSafe.asset` has `m_Script: {fileID: 0}` and cannot be deserialized on mobile IL2CPP. The fix is a **chain-of-responsibility loader** in `ScenarioLoader.cs`:

```
Strategy 1 → Inspector-assigned ScriptableObject  (works in Editor when asset is healthy)
Strategy 2 → JSON TextAsset (Assets/Resources/TrafficSafeScenario.json)  ← primary mobile path
Strategy 3 → Hardcoded fallback mirroring exact asset data               ← safety net
```

`ScenarioLoader.Load()` always returns a valid, non-null `ScenarioData`. `ARCitySpawner.Awake()` calls it and `ScenarioController` receives valid data in all build configurations.

**Audio files** were moved from `Scripts/` to `Assets/Resources/Audio/` so `Resources.Load<AudioClip>(path)` works at runtime. Their `.meta` files moved with them, preserving GUIDs and all existing project references.

**Adding/editing scenario content**: edit `Assets/Resources/TrafficSafeScenario.json`. Audio clips go in `Assets/Resources/Audio/` and are referenced by filename without extension. Do not re-introduce `Resources.Load<ScenarioData>()` — the `.asset` file is structurally broken.

## Code Conventions

- Coroutine-based sequencing throughout — always use `StartCoroutine()` for timed/async operations
- `SequenceController.PlayActions()` accepts an `Action onComplete` callback
- All step execution has a 60-second timeout for visual actions and 30-second timeout for audio
- Comments and log messages are mixed Vietnamese/English (in-progress localization)
- C# 9.0, .NET Standard 2.1, scripting backend IL2CPP for mobile
