# STSL - AI for Games Final Project

Turn-based grid card game used as a testbed for evaluating game AI behavior, route selection, and map-structure design.

This project implements a Slay-the-Spire-like progression loop with tactical grid combat, card-based character deployment, rule-based character behavior, A* movement, and branching map generation. The project was developed for the Spring 2026 AI for Games final project.

## Project Overview

The game is organized around three connected decision layers:

1. **Combat layer**: player-side characters and enemies fight on a discrete grid battlefield.
2. **Character decision layer**: player-side units use role-specific behavior logic to decide targets, support actions, and attacks.
3. **Map progression layer**: the player selects Normal, Elite, Shop, Rest, Chest, and Boss nodes across a branching map.

The project evaluates how character behavior, map node selection, and map structure affect combat efficiency, survivability, resource accumulation, and boss readiness.

## AI Techniques

The implemented system integrates several game AI techniques:

- **Behavior Tree-style character logic** for player-side character roles.
- **A* pathfinding** for grid-based movement toward reachable targets.
- **Cell reservation** to prevent unit overlap during movement.
- **Procedural branching map generation** with room-type weights and route constraints.
- **Rule-based enemy behavior** used as a fixed combat baseline.
- **PPO-based Behavior Tree selection** discussed in the report as a high-level selector over predefined behavior policies.

## Software Requirements

- Unity **2022.3.62f3**
- Windows 10/11 recommended
- Unity packages are restored from `Packages/manifest.json`
- No additional package installation is required for the playable Unity project

Important packages used by the project include:

- `com.unity.ai.navigation` 1.1.6
- `com.unity.ugui` 1.0.0
- `com.unity.textmeshpro` 3.0.7
- `com.unity.test-framework` 1.1.33

## Installation

1. Clone or download the project source code.
2. Open **Unity Hub**.
3. Click **Add** and select the project root folder.
4. Open the project with Unity **2022.3.62f3**.
5. Wait for Unity to import assets and compile scripts.
6. Open `Assets/Scenes/MenuScene.unity` or `Assets/Scenes/MapGenerationScene.unity`.

Unity will regenerate local cache folders such as `Library/`, `Temp/`, and `Logs/` when the project is opened.

## Main Scenes

The enabled build scenes are:

| Scene | Purpose |
|---|---|
| `MapGenerationScene` | Main branching map and route-selection scene |
| `BattleScene_normal` | Normal combat encounter |
| `BattleScene_elite` | Elite combat encounter |
| `BattleScene_boss` | Boss combat encounter |
| `ShopScene` | Resource exchange and card upgrade scene |
| `RestScene` | Recovery scene |
| `ChestScene` | Reward scene |
| `FailScene` | Failure result scene |
| `FinalWinScene` | Final victory result scene |

## How to Run the Main Demo

Recommended demo path:

1. Open `Assets/Scenes/MenuScene.unity`.
2. Press **Play** in the Unity Editor.
3. Click the start button to enter `MapGenerationScene`.
4. Select an available map node.
5. If a battle scene loads, drag character cards from the hand area onto valid player grid cells.
6. Start or continue the combat flow using the scene UI.
7. After winning a battle, the run returns to the map and unlocks the next connected nodes.
8. Visit Shop, Rest, or Chest nodes to observe resource conversion and recovery behavior.
9. Continue until the Boss node is reached and resolved.

For a shorter inspection path, open `MapGenerationScene` directly and press **Play**. The map scene can generate or restore a run automatically.

## Controls

| Input | Function |
|---|---|
| Mouse click | Select map nodes and UI buttons |
| Mouse drag | Drag cards onto valid deployment cells in battle |
| Mouse drag in Shop | Drag merchandise onto cards to apply upgrades |
| Mouse wheel | Scroll the map view |
| `R` key in map scene | Regenerate a new map run |

Most interactions are UI-driven through Unity buttons and draggable objects.

## Important Settings and Parameters

### Map Generation

Default map settings are defined in `Assets/Scripts/MapGenerationScene/MapUIRenderer.cs` and `MapGenerator.cs`.

| Parameter | Default |
|---|---:|
| Map width | 7 |
| Map height | 15 |
| Path count | 6 |
| Starting node count | 4 |
| Shop weight | 20 |
| Rest weight | 25 |
| Elite weight, Act 1 | 15 |
| Elite weight after Act 1 | 20 |
| Minimum Shop count | 2 |
| Minimum Rest count | 2 |

### Run and Resource Settings

| System | Setting |
|---|---|
| Default gold | 0 |
| Normal battle reward | 10 gold in `BattleSceneController` |
| Elite battle reward | 20 gold in `BattleSceneController` |
| Rest recovery | 30% max HP for each surviving card |
| Shop upgrades | Sword, Shield, and Armor stat modifiers |
| Shop prices | 60 gold for Sword, Shield, or Armor |

### Combat and Movement

- Units occupy discrete grid cells.
- Movement uses four-direction A* pathfinding.
- `GridReservationManager` prevents units from selecting the same destination cell.
- Enemy behavior is fixed around reachable target selection, movement, and attack range.
- Player-side character behavior is role-specific:
  - StreamMage emphasizes ranged damage and control.
  - TideTitan emphasizes tanking and shielding.
  - StormWarrior emphasizes engagement and finishing damage.

### Simulation Speed

`Assets/Scripts/MenuScene/ControlSpeed.cs` exposes a speed value in the Unity Inspector:

- Range: `1.0` to `3.0`
- Default value: `2.0`

This changes `Time.timeScale` during play mode.

## Reproducing the Evaluation

The final report evaluates three experiment levels. The following steps describe how to reproduce the main observations from the playable project.

### Experiment 1: Combat-Level Behavior

Goal: compare combat behavior under fixed enemy conditions.

Suggested procedure:

1. Open `BattleScene_normal`, `BattleScene_elite`, or `BattleScene_boss`.
2. Keep enemy setup fixed.
3. Deploy the same player team composition.
4. Run repeated battles while recording:
   - clear result,
   - battle duration,
   - final team HP,
   - damage taken,
   - timeout or wipe cases.
5. Compare role behavior by adjusting player-side behavior parameters in the character prefabs or Inspector.

### Experiment 2: Run-Level Route Selection

Goal: compare combat-oriented, balanced, and avoidance-oriented route tendencies.

Suggested procedure:

1. Start a new run from `MapGenerationScene`.
2. For each run, record the number of selected Normal, Elite, Shop, and Rest nodes.
3. Record whether the boss room is reached.
4. Record whether the boss is defeated.
5. Record remaining team HP after the boss battle and gold spent.
6. Classify the run route as:
   - combat-oriented,
   - balanced,
   - avoidance-oriented.

The report uses 50 manually collected complete run records for this experiment.

### Experiment 3: Structural Map Evaluation

Goal: evaluate whether map structures provide distinguishable route choices.

Suggested procedure:

1. In `MapUIRenderer` or `MapGenerator`, adjust map settings such as:
   - `pathCount`,
   - `startNodeCount`,
   - Shop weight,
   - Rest weight,
   - Elite weight.
2. Disable random seeds or set fixed seeds when comparing variants.
3. Generate multiple maps for each variant.
4. Record structural indicators:
   - average branching factor,
   - reachable Shop/Rest/Chest ratio,
   - reachable Elite count,
   - recognizable route tendencies.

The final report compares baseline, linear, high-branching, high-risk, and supply-rich map variants using fixed-seed structural measurements.

## Project Structure

| Path | Description |
|---|---|
| `Assets/Scenes/` | Unity scenes for map, combat, shop, rest, chest, and result screens |
| `Assets/Scripts/All_Scene/` | Persistent game, hand, and player-state managers |
| `Assets/Scripts/Battle/` | Combat units, cards, grid slots, battle setup, and movement reservation |
| `Assets/Scripts/Enemy/` | Enemy behavior, A* pathfinding, and Voodoo enemy variants |
| `Assets/Scripts/Player/` | StreamMage, TideTitan, StormWarrior, and skill effects |
| `Assets/Scripts/MapGenerationScene/` | Branching map generation, map UI, run state, scene controllers |
| `Assets/Scripts/ShopScene/` | Shop merchandise and drag-to-upgrade logic |
| `Assets/Scripts/RestScene/` | Rest scene behavior |
| `Packages/` | Unity package manifest |
| `ProjectSettings/` | Unity project settings and build scene configuration |

## Source-Code Submission Scope

For source-code reproduction, the important Unity project files are:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `README.md`

Unity-generated local folders such as `Library/`, `Temp/`, `Logs/`, and `UserSettings/` are not required for grading because Unity can regenerate them.

## Known Limitations

- Experiment 2 is based on manually collected run records rather than a large external player study.
- Experiment 3 measures map structure and decision-space availability, not real player preference.
- Some PPO trainer hyperparameters were not preserved as a trainer configuration file in the source archive.
- The playable Unity project focuses on demonstrating and evaluating the implemented system behavior; final report, presentation materials, and demo video are separate required submission materials.
- The game is balanced for project evaluation rather than for a production-ready release.

## Team Contribution Statement

The official final submission should include a separate team contribution statement. Suggested categories:

- AI implementation
- System design
- Game environment and scene setup
- Evaluation and experiments
- Report writing
- Presentation preparation
- Demonstration video preparation
