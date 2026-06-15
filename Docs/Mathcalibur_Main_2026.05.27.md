# Mathcalibur Main Game AI Coding Spec

> Game Title: Mathcalibur  
> Former Planning Name: Apple Boom  
> Document Purpose: AI coding tool input / Unity prototype implementation guide  
> Target Platform: Mobile-first  
> Current Goal: Exhibition / demo-friendly prototype  
> Document Scope: Core game + shop/item integration shell only  
> Item Data Status: External item specs will be provided later  
> Ambiguity Target After External Item Specs: 0.5–0.8 / 10  
> Revision Note: v0.7 clarified document-role naming, UniqueItem timing layers, Unique Item Slot indexing, invalid selection handling, Unique Item Slot empty-pool behavior, Next Stage requirement, Stage 10 clear flow, and Stage 1~9 enemy rotation rules.  

---

## 0. Source Priority, Scope, and Document Rules

This document is the highest-priority document for the Mathcalibur MVP core game implementation.

For coding implementation, use this document as the primary source for:

- core gameplay loop,
- board rules,
- expression rules,
- calculation rules,
- combat rules,
- stage flow,
- difficulty behavior,
- shop layout,
- item slot generation structure,
- item acquisition flow,
- UniqueItem integration structure,
- config and balance-data requirements.

Older presentation files, meeting notes, PDFs, previous planning documents, and earlier MD files are reference-only.

If older rules conflict with this document, ignore the older rules and follow this document.

### 0.0 Document Role and File Naming Rule

This document's role is the Main Game Implementation Spec.

Use the following file naming convention for Mathcalibur implementation specs:

```text
Main game implementation spec: Mathcalibur_Main_[date].md
Normal item implementation spec: Mathcalibur_Item_[date].md
UniqueItem implementation spec: Mathcalibur_UniqueItem_[date].md
```

The date part is for human version tracking only.

Implementation priority must be determined by document role, not by date.

If multiple implementation specs are provided at the same time, use the following role-based priority:

```text
1. Mathcalibur_Main_[date].md
2. Mathcalibur_Item_[date].md, only for detailed normal item data
3. Mathcalibur_UniqueItem_[date].md, only for detailed UniqueItem effects
```

Do not treat a newer date in an Item or UniqueItem document as permission to override Main game rules unless this Main document explicitly allows that override.

### 0.1 Current Rule Priority

```text
1. Current core gameplay rules in this document
2. Core shop / item slot / UniqueItem integration rules in this document
3. External Item Implementation Spec, only for detailed normal item data
4. External UniqueItem Implementation Spec, only for detailed UniqueItem effects
5. Temporary dummy test data in this document
6. Older documents and reference notes
```

### 0.2 External Spec Delegation Rule

This core document remains the highest-priority document for core gameplay and system structure.

This document does not define final normal item effects.

This document does not define final UniqueItem effects.

Detailed normal item implementation must be provided later by a separate Item Implementation Spec.

Recommended file name pattern:

```text
Mathcalibur_Item_[date].md
```

Detailed UniqueItem implementation must be provided later by a separate UniqueItem Implementation Spec.

Recommended file name pattern:

```text
Mathcalibur_UniqueItem_[date].md
```

External item specs may define:

- item IDs,
- display names,
- item categories,
- rarity,
- prices,
- maxAcquisitionsPerRun,
- effect types,
- effect values,
- unlock stages,
- item behavior,
- UniqueItem trigger conditions,
- UniqueItem effect logic,
- UniqueItem presentation notes.

External specs must not override the core gameplay, shop layout, difficulty behavior, or item integration rules in this document unless this document explicitly allows it.


### 0.2.1 UniqueItem Board Generation Override Exception

Normal items, including `BoardDeckUpgrade`, `PassiveItem`, `ActiveItem`, and `ConnectionLimitUpgrade`, must not override core board generation structure.

However, an external UniqueItem Implementation Spec may define a number-specific UniqueItem effect that modifies board generation behavior only when all of the following are true:

- the item category is `UniqueItem`,
- the effect is explicitly marked as `BoardGenerationOverride`,
- the effect is defined in the external UniqueItem Implementation Spec,
- the effect does not change the core valid expression rules,
- the effect does not change board width or board height,
- the effect remains editable config data,
- the effect resets when the current Run ends.

This exception exists only for final UniqueItem effects.

Normal board deck growth must still use the normal spawn weight modifier system and must not use this exception.

### 0.2.2 UniqueItem Timing Layer Rule

UniqueItem effects may define their own timing layer in the external UniqueItem Implementation Spec.

Do not assume that all UniqueItem effects are post-calculation effects.

If a UniqueItem effect is marked as one of the following timing types, apply it at the timing specified by the external UniqueItem Implementation Spec:

```text
BoardGenerationOverride
PreCalculationTransform
BoardStateTransformBeforeCalculation
PostCalculationBonus
RewardModifier
```

Timing layer meanings:

| Timing Layer | Meaning | Example Use |
|---|---|---|
| `BoardGenerationOverride` | Modifies board generation before tiles are created. | A UniqueItem-only effect that changes A/B Number/Operator category ratios. |
| `BoardStateTransformBeforeCalculation` | Modifies the current board or selected tiles after a valid expression is confirmed but before expression calculation. | A UniqueItem effect that converts board numbers before calculating the selected expression. |
| `PreCalculationTransform` | Modifies how selected expression tiles are interpreted before the expression result is calculated. | A UniqueItem effect that treats selected `1` tiles as `11` under its trigger condition. |
| `PostCalculationBonus` | Applies extra damage, shield, or other combat effects after the base expression result is calculated. | A UniqueItem effect that adds bonus damage after calculation. |
| `RewardModifier` | Modifies stage-clear reward, gold, or other post-stage rewards. | A UniqueItem effect that increases stage-clear gold. |

Core expression validity rules, board size, adjacency rules, tile removal rules, shop layout rules, and difficulty rules must still follow this Main document unless this Main document explicitly allows the external UniqueItem Implementation Spec to override them.

If two or more UniqueItem effects attempt to modify the same timing layer or the same value, follow the conflict-resolution rule defined in the external UniqueItem Implementation Spec.

If the external UniqueItem Implementation Spec does not define a conflict-resolution rule for that case, log a clear config error and use the safest deterministic order by `uniqueItemId` ascending for prototype testing.

### 0.3 Temporary Dummy Data Rule

Temporary item data in this document is dummy data only.

It exists only to test:

- shop slot generation,
- item selection,
- paid purchase flow,
- reroll locking,
- acquisition count handling,
- placeholder item effect dispatch,
- Unique Item Slot behavior.

Do not treat dummy item data as final item content.

Do not build final item balance from dummy data.

When the external Item Implementation Spec is provided, use that document for normal item data.

When the external UniqueItem Implementation Spec is provided, use that document for UniqueItem effects.

### 0.4 Data and Balance Rule

All balance values must be stored as editable config data.

Do not hard-code final balance values inside gameplay logic.

Recommended Unity implementation methods:

- ScriptableObject config assets,
- JSON config,
- CSV tables,
- spreadsheet-derived data tables,
- or another editable data table format.

### 0.5 Balance Data Hard-Coding Ban

Do not hard-code these values inside gameplay logic:

- item prices,
- rarity weights,
- item effect values,
- gold rewards,
- reroll costs,
- shop slot counts,
- UniqueItem candidate counts,
- item acquisition limits,
- stage balance,
- enemy HP,
- enemy attack damage,
- enemy attack cycle,
- turn limits,
- player HP,
- shield conversion rate,
- board size,
- tile spawn weights.

All of these values must be editable by the design team without code changes.

### 0.6 Temporary Balance Rule

Some values in this document are temporary prototype values.

Any temporary value must:

- use the prefix `TEMP_`,
- be stored in editable config data,
- appear in a clear balance table,
- be easy for the design team to find,
- be easy to edit or delete later,
- never be hidden inside gameplay logic.

---

## 1. Terminology Lock

Use `Stage` as the MVP gameplay unit.

```text
Stage = one enemy fight
Run = exactly 10 stages for MVP
Shop Screen = reward/shop screen after each cleared Stage 1~9
```

Do not create a separate `Battle` or `Round` system for the MVP.

If older terms such as `Battle`, `Round`, or `BattleConfig` appear in previous documents, treat them as `Stage`, `Stage`, or `StageConfig`.

Use `StageConfig` as the config asset for resolved enemy data, stage turn limit, and allowed operators. Use editable enemy config data for base enemy types, enemy rotation blocks, and enemy stat multipliers.

---

## 2. Implementation Milestone Lock

The AI coding tool must implement the prototype in this order.

Do not implement later milestone systems before the required previous milestone is working.

### Milestone 1: Core Board and Expression

Implement:

- 5 x 5 board generation,
- checkerboard category weight map,
- number/operator tile generation,
- touch input and mouse debug input for tile selection,
- orthogonal adjacency validation,
- expression validation,
- expression preview,
- standard precedence expression calculation,
- Attack Mode damage application,
- tile removal,
- gravity refill,
- valid path check after refill,
- board regeneration safety rule.

Do not implement yet:

- Defense Mode,
- shop,
- item rarity,
- reroll,
- paid items,
- retry snapshot,
- final item effects,
- final UniqueItem effects.

### Milestone 2: Stage Combat Pressure

Implement:

- player HP,
- enemy HP,
- enemy attack countdown,
- enemy attack cycle,
- stage turn limit,
- victory condition,
- defeat condition.

### Milestone 3: Defense Mode

Implement:

- Attack / Defense mode toggle,
- shield conversion,
- shield duration until next enemy attack,
- negative result healing in Defense Mode.

### Milestone 4: Integrated Shop and Item Integration Shell

Implement:

- gold reward,
- integrated shop screen,
- 3 free item slots,
- 3 paid item slots,
- Unique Item Slot replacement rule,
- free item equal-probability selection,
- paid item rarity-weight selection,
- item eligibility checks,
- item acquisition counts,
- `maxAcquisitionsPerRun`,
- paid item price handling,
- reroll button,
- reroll cost increase,
- Exit To Title button,
- Next Stage button,
- item data loading interface,
- placeholder item effect dispatch interface,
- UniqueItem data loading interface,
- UniqueItem slot purchase flow.

Do not implement final normal item effects from this document.

Do not implement final UniqueItem effects from this document.

Use dummy item data only when no external Item Implementation Spec has been provided.

### Milestone 5: Retry, Restart, and MVP Polish

Implement:

- stage-start snapshot,
- Retry Current Stage,
- Restart From Stage 1,
- Quit Game,
- lightweight visual feedback,
- damage popup,
- shield popup,
- invalid selection feedback,
- simple tile animation.

---

## 3. Technical Environment Lock

### 3.1 Unity Version

```text
Unity Engine Version: 6.3.9f1
```

### 3.2 Runtime Libraries

Use the following libraries:

- R3
- UniTask
- LitMotion

These libraries are allowed and recommended for runtime implementation.

### 3.3 UI Rules

- Use Unity UGUI for runtime gameplay UI.
- Use Canvas-based UI.
- Use TextMeshPro for all runtime text.
- Do NOT use UI Toolkit for runtime gameplay UI.
- Mobile-first layout is required.

### 3.4 Target Resolution

Base target resolution:

```text
1080 x 1920
Portrait
```

The UI must be designed for portrait mobile gameplay first.

Implementation should support responsive scaling for different mobile screen sizes, but the base layout reference is `1080 x 1920`.

### 3.5 Screen Orientation

MVP supports portrait mode only.

Landscape mode is not required for MVP.

---

## 4. Project Overview

Mathcalibur is a mobile-first puzzle combat game where the player connects number tiles and operator tiles to create mathematical expressions.

The expression result becomes a combat action.

The current goal is a short, clear, exhibition-friendly prototype that allows players to quickly experience:

- connecting tiles,
- creating expressions,
- seeing calculation results,
- converting results into combat feedback,
- choosing upgrades and shop items between stages.

### Core One-Line Summary

```text
Mathcalibur is a stage-based puzzle combat game where players connect numbers and operators to create expressions, use the results to attack or defend, and grow their board through short item and upgrade choices between stages.
```

---

## 5. Target and Positioning

### Main Target

- Late teens to early twenties casual puzzle players.

### Secondary Target

- Players who enjoy number-combination puzzles.
- Players who enjoy link-based puzzle games.
- Players who enjoy light strategy combat games.

### Positioning

Mathcalibur is not a children-only educational math game.

Children are not excluded, but the game should not be designed only as an educational game for children.

The main experience is:

- not solving math problems,
- but using numbers and operators as combat materials.

### Key Phrases

- This is not a game that teaches math.
- This is a game that uses numbers and operators as weapons.
- The player is not finding the correct answer.
- The player is creating an attack or defense through calculation.

---

## 6. MVP Core Loop

1. Stage starts.
2. One enemy appears.
3. A board is generated.
4. Board-generation effects, including UniqueItem-only `BoardGenerationOverride`, are applied only if explicitly defined and currently active.
5. The player chooses Attack Mode or Defense Mode.
6. The player connects adjacent number/operator tiles.
7. The expression is previewed while dragging.
8. The player releases touch input.
9. The selected path is validated.
10. If valid, any applicable `BoardStateTransformBeforeCalculation` effects may be applied.
11. Any applicable `PreCalculationTransform` effects may be applied to the confirmed expression interpretation.
12. The expression is calculated.
13. The base result becomes damage or shield.
14. Placeholder item hooks and `PostCalculationBonus` UniqueItem effects may be checked after base calculation.
15. Used valid expression tiles disappear.
16. Tiles fall downward.
17. Empty top cells are refilled.
18. Valid path check runs after refill.
19. If enemy HP reaches 0, the current stage is cleared immediately.
20. If the enemy is still alive, enemy countdown decreases after the valid player action is fully resolved.
21. If enemy countdown reaches 0, the enemy attacks.
22. If Stage 1~9 is cleared, stage-clear rewards and any applicable `RewardModifier` effects are resolved, then the shop screen opens.
23. The player chooses/shop items.
24. The next stage starts.
25. A full MVP run contains exactly 10 stages. Stage 10 uses a separate final boss rule.

---

## 7. Scene Structure

MVP uses a simple scene structure.

Required scenes:

- `TitleScene`
- `GameScene`

### 7.1 TitleScene

Used for:

- game title,
- start button,
- difficulty selection entry point,
- return point from Quit Game.

### 7.2 GameScene

Used for:

- stage gameplay,
- shop screen,
- stage transition,
- player death options,
- retry current stage,
- restart from Stage 1.

For MVP, stage gameplay, shop, retry, restart, and death UI may all be handled inside `GameScene` using UI panels.

Optional later scenes:

- `ResultScene`
- `SettingsScene`
- `TutorialScene`

These optional scenes are not required for MVP.

---

## 8. Save Rule

MVP does not require persistent save data.

Run state exists only during the current play session.

The game does not need to save progress after the app is closed.

Restart From Stage 1 starts a new run.

Quit Game returns to the title screen.

Persistent save data may be added later if needed.

---

## 9. Difficulty Rule

The MVP supports three difficulty modes:

| Difficulty | Direction | UniqueItem System |
|---|---|---|
| Easy | Low Risk / Low Return | Disabled |
| Normal | Balance / Balance | Enabled |
| Hard | High Risk / High Return | Enabled |

Difficulty may influence spawn weights, stage balance, or reward tuning later.

For the first MVP implementation, difficulty must at minimum control whether UniqueItem systems are enabled.

### 9.1 UniqueItem Difficulty Rule

UniqueItem systems are disabled in Easy Mode.

In Easy Mode:

- no starting UniqueItem candidate is offered,
- Stage 3 / 6 / 9 shops do not use a Unique Item Slot,
- all shops use the normal layout: 3 free item slots + 3 paid item slots,
- UniqueItem effects cannot trigger,
- UniqueItem items must not appear in any shop pool.

In Normal and Hard Mode:

- starting UniqueItem candidate selection is enabled,
- shops after clearing Stage 3 / 6 / 9 use the Unique Shop Layout,
- Unique Shop Layout = 3 free item slots + 2 paid item slots + 1 Unique Item Slot,
- UniqueItem effects may trigger if the player owns the required UniqueItem.

### 9.2 Starting UniqueItem Candidate Rule

At the start of a run, if the selected difficulty is Normal or Hard, present several UniqueItem candidates and let the player choose exactly 1.

Easy Mode does not provide starting UniqueItem candidates.

The starting UniqueItem candidate count must be editable config data.

Recommended config value:

```text
startingUniqueCandidateCount = TEMP_STARTING_UNIQUE_CANDIDATE_COUNT
```

Default temporary value:

```text
TEMP_STARTING_UNIQUE_CANDIDATE_COUNT = 3
```

If no external UniqueItem data has been provided yet, the implementation may show dummy UniqueItem candidates for shop-flow testing only.

---

## 10. Board Rules

### 10.1 MVP Board Size

MVP default board size:

```text
Width: 5
Height: 5
Total Cells: 25
```

Board width and height must be editable config values.

However, stage-based board expansion is not required for MVP.

The implementation should allow later board size changes through `BoardConfig`, but actual MVP gameplay uses `5 x 5` by default.

### 10.2 Board Coordinate Rule

- Board origin is top-left.
- Top-left cell is `(0, 0)`.
- `x` increases from left to right.
- `y` increases from top to bottom.
- `(x + y) % 2 == 0` uses A Cell weight.
- `(x + y) % 2 == 1` uses B Cell weight.

### 10.3 Tile Types

#### Number Tiles

MVP number range:

```text
1, 2, 3, 4, 5, 6, 7, 8, 9
```

#### Operator Tiles

MVP operators:

```text
+, -, x, ÷
```

### 10.4 Operator Unlock Rule

Operators are unlocked by stage number.

```text
Stage 1–2: + and - only
Stage 3 and later: +, -, x, and ÷
```

Before Stage 3:

- `x` final spawn weight is treated as 0,
- `÷` final spawn weight is treated as 0,
- shop items or upgrades that modify `x` or `÷` should not appear in the normal eligible item pool.

Stage 3 and later:

- all MVP operators may appear,
- stronger or riskier operators can still use lower spawn weights.

Operator unlock state has higher priority than upgrade modifiers.

If an operator is locked, its final spawn weight is always 0, even if upgrade modifiers would increase it.

Unlock check is applied after upgrade modifiers and before final weight caching.

---

## 11. Board Generation

The board must not use pure uniform random generation.

The board uses a checkerboard category weight map.

Board generation has two layers.

### Layer 1: Category Weight Map

The checkerboard weight map decides whether each cell prefers:

- Number,
- or Operator.

This layer is position-based.

### Layer 2: Actual Tile Value Weights

After the category is selected, the actual tile value is selected.

Examples:

- If the category is Number, choose between 1 to 9.
- If the category is Operator, choose between currently unlocked operators.

Board deck upgrades affect Layer 2 only.

They do not affect Layer 1.

### 11.1 Checkerboard Category Weight Map

Top-left cell starts as A.

```text
Row 0: A / B / A / B / A
Row 1: B / A / B / A / B
Row 2: A / B / A / B / A
Row 3: B / A / B / A / B
Row 4: A / B / A / B / A
```

#### A Cell

```text
Number: 60%
Operator: 40%
```

#### B Cell

```text
Number: 40%
Operator: 60%
```

#### Coordinate Rule

```text
If (x + y) % 2 == 0:
    use A Cell weight
Else:
    use B Cell weight
```

Board deck upgrades must not modify these A/B category weights.

A/B category weights may be overridden only by an external UniqueItem effect explicitly marked as `BoardGenerationOverride`.

This is not a normal `BoardDeckUpgrade` behavior.

If a `BoardGenerationOverride` UniqueItem is active, it overrides the Layer 1 Number/Operator category roll only.

Layer 2 actual number/operator value selection must still use the current cached number/operator spawn weight tables.

### 11.2 Default Number Spawn Weights

These weights are applied after a cell has already selected the Number category.

| Number | Spawn Weight |
|---:|---:|
| 1 | 20 |
| 2 | 20 |
| 3 | 20 |
| 4 | 20 |
| 5 | 9 |
| 6 | 5 |
| 7 | 3 |
| 8 | 2 |
| 9 | 1 |

Use weighted random values.

Do not force manual 100% normalization every time.

### 11.3 Default Operator Spawn Weights

These values are initial prototype values and can be adjusted during testing.

| Operator | Spawn Weight |
|---|---:|
| `+` | 45 |
| `-` | 25 |
| `x` | 20 |
| `÷` | 10 |

Before Stage 3, `x` and `÷` final weights are overridden to 0.

### 11.4 Board Generation Flow

Use this logical flow for board generation.

```text
For each cell in the board:
    1. Determine whether the cell is A or B using (x + y) % 2.
    2. Roll Number or Operator using that cell's category weights.
    3. If Number is selected:
        choose actual number using current number spawn weights.
    4. If Operator is selected:
        choose actual operator using current operator spawn weights.
    5. Place the tile on the board.

After the board is generated:
    6. Check whether at least one valid expression path exists.
    7. If no valid path exists, regenerate the entire board.
```

### 11.5 Valid Path Guarantee

After generating the board, check whether at least one valid expression path exists.

A valid expression path must satisfy all of the following:

- starts with a Number tile,
- alternates between Number and Operator,
- ends with a Number tile,
- uses at least 3 tiles,
- does not exceed the current max connection limit,
- uses orthogonal adjacency only,
- does not use diagonal movement.

If no valid expression path exists:

- regenerate the entire board.

For MVP, full regeneration is preferred over partial shuffle because it is simpler and more stable.

### 11.6 Board Regeneration Safety Rule

Max regeneration attempts:

```text
20
```

If no valid board is generated after 20 attempts, force-insert one guaranteed valid fallback path into the board.

MVP fallback path:

```text
1 + 2
```

Fallback insertion rules:

- Insert the fallback path into three orthogonally adjacent cells.
- The fallback path may overwrite existing generated tiles.
- The fallback rule is a safety mechanism only.
- It should not be used as the normal board generation method.

Random fallback path generation is not required for MVP.

---

## 12. Board Deck System

### 12.1 Core Definition

```text
Board Deck System = Spawn Weight Modifier System
```

The system must not be implemented as a hand-card combat system.

The player does not draw cards into a hand.

The player modifies future board generation tendencies.

Board deck upgrades apply only during the current run.

When a new run starts, all board deck modifiers reset to default.

Permanent progression is not included in MVP.

### 12.2 What the Player Changes

The player can change:

- actual number spawn weights,
- actual operator spawn weights,
- passive effects related to board or combat,
- connection limit growth.

The player does not change:

- A/B checkerboard category weights,
- board coordinate pattern,
- core valid expression rules.

### 12.3 Allowed Modifier Targets

Board deck upgrades may modify:

- number `1` spawn weight,
- number `2` spawn weight,
- number `3` spawn weight,
- number `4` spawn weight,
- number `5` spawn weight,
- number `6` spawn weight,
- number `7` spawn weight,
- number `8` spawn weight,
- number `9` spawn weight,
- `+` operator spawn weight,
- `-` operator spawn weight,
- `x` operator spawn weight,
- `÷` operator spawn weight.

### 12.4 Forbidden Modifier Targets

Board deck upgrades must not directly modify:

- board width,
- board height,
- A/B checkerboard pattern,
- A Cell 60/40 category ratio,
- B Cell 40/60 category ratio,
- valid expression rules.

These may be changed only by explicit config values, special stage rules, later expansion systems, or a UniqueItem-only `BoardGenerationOverride` explicitly defined by the external UniqueItem Implementation Spec.

Normal board deck growth and normal item effects must not modify these forbidden targets.

A `BoardGenerationOverride` UniqueItem may override A/B category ratios, but it must not modify board size, coordinate rules, adjacency rules, expression validity rules, or tile removal rules.

### 12.5 Upgrade Stacking Rule

Upgrade effects can stack across the current run.

Default spawn weight increase value:

```text
TEMP_DEFAULT_WEIGHT_INCREASE = +2
```

Default spawn weight decrease value:

```text
TEMP_DEFAULT_WEIGHT_DECREASE = -2
```

The default values are starting prototype values only.

All upgrade effect values must be editable in `UpgradeDefinition` or item config data.

Do not hard-code upgrade values inside random selection logic.

The same upgrade may be obtained multiple times across the run and its effects may stack, unless the external Item Implementation Spec sets a lower `maxAcquisitionsPerRun`.

### 12.6 Final Weight Clamp Rule

Final spawn weights may reach 0.

If a final spawn weight is 0, that tile value cannot spawn.

Final spawn weights must never be negative.

If a coding error, stacking result, or data issue produces a negative final weight, clamp it to 0.

```text
finalWeight = max(0, calculatedWeight)
```

### 12.7 Cached Final Weight Table Rule

When upgrades change, rebuild the final spawn weight table once.

During board generation, use the cached final spawn weight table.

Do not recalculate upgrade effects for every tile.

Recommended flow:

```text
When stage starts or upgrades change:
    1. Load base number/operator spawn weights.
    2. Apply active board deck modifiers.
    3. Apply locked operator rules.
    4. Clamp negative final weights to 0.
    5. Precompute total weights.
    6. Cache the final weight table.

During board generation:
    1. Use A/B category weight map.
    2. Select Number or Operator category.
    3. Select actual tile value using cached final weight table.
```

---

## 13. Input Rules

### 13.1 Primary Input

- Touch input is the primary input method.
- Mouse input may be supported only for Unity Editor / debug testing.
- Runtime gameplay must be designed around mobile touch input first.

### 13.2 Tile Drag Input Rules

- The player starts selection by touching a number tile.
- Operator tiles cannot be the first selected tile.
- The player may extend the path only to orthogonally adjacent tiles.
- Diagonal movement is not allowed.
- Non-adjacent tiles are never appended to the selected path.
- Already selected tiles are not appended again, except for the immediate backtracking rule.
- If the player drags back to the previously selected tile, the last selected tile is removed from the current path.
- Adjacent tiles that break expression order, such as Number → Number or Operator → Operator, may be appended visually as an invalid path state.
- The path is not auto-corrected.
- The final selected path is validated when touch is released.
- The expression is confirmed when the player releases touch input.
- Invalid selections are cancelled with light visual feedback.

### 13.3 Invalid Selection Handling Rule

Non-adjacent tiles are never appended to the selected path.

Diagonal tiles are never appended to the selected path.

Already selected tiles are not appended again, except for the immediate backtracking rule.

Adjacent tiles that break expression order, such as Number → Number or Operator → Operator, may be appended visually as an invalid path state.

The path is not auto-corrected.

Final validation occurs when the player releases touch input.

If the final selected path is invalid:

- no turn is consumed,
- no tiles are removed,
- no combat effect is applied,
- enemy attack countdown does not decrease,
- remaining stage turn count does not decrease.

### 13.4 Important Input Interpretation

```text
Already selected tile: cannot be selected again.
Immediate backtracking to the previous tile: cancels the last selected tile.
Non-adjacent tile: never appended to the path.
Diagonal tile: never appended to the path.
Adjacent tile that breaks expression order: may remain as an invalid path state.
Touch release: confirms the current selected path and runs final validation.
```

---

## 14. Expression Rules

### 14.1 Valid Expression

A valid expression must:

- start with a number tile,
- alternate between number and operator tiles,
- end with a number tile,
- use at least 3 tiles,
- use no more than the current max connection limit,
- use only orthogonal adjacent tiles,
- not use diagonal movement,
- not use the same tile twice.

The shortest valid expression is:

```text
Number → Operator → Number
```

A single number tile is not a valid expression in MVP.

Invalid examples:

```text
3
3 +
3 4 + 5
3 + x 5
```

### 14.2 Connection Limit Rule

Initial max connection limit:

```text
TEMP_INITIAL_MAX_CONNECTION_LENGTH = 5
```

Connection limit upgrades are obtained through shop / item rewards.

Each connection limit upgrade increases the max connection limit by the editable config value:

```text
TEMP_CONNECTION_LIMIT_INCREASE_VALUE = 2
```

Progression example:

```text
5 → 7 → 9 → ...
```

UI label should avoid hard-coding the number if possible.

Recommended UI label:

```text
Connection Limit Up
```

### 14.3 Operator Ending Rule

If the selected path ends with an operator and the path can be cut into a valid expression, only the valid expression tiles are consumed.

The ignored final operator tile is treated as not selected.

The ignored final operator tile:

- is not included in calculation,
- is not removed from the board,
- does not count as a used tile,
- returns to normal visual state after input release.

Valid cut example:

```text
Selected path: 3 + 4 x
Valid expression: 3 + 4
Ignored tile: x
```

Invalid cut example:

```text
Selected path: 3 +
Valid expression: none
Result: invalid selection
```

### 14.4 Expression Preview Rule

During dragging, show the current selected expression and preview result.

The preview result updates when the selected path changes.

The preview may calculate the current selected path while dragging, but it must not apply combat effects.

The preview does not apply:

- damage,
- shield,
- turn consumption,
- tile removal,
- enemy countdown changes,
- item effects,
- UniqueItem effects.

The final combat result is applied only when the player releases input and confirms a valid expression.

---

## 15. Calculation Rules

### 15.1 MVP Calculation Rule

MVP uses standard arithmetic precedence.

```text
x and ÷ are calculated before + and -.
```

Connected-order calculation is not the MVP rule.

Connected-order calculation is Prototype Optional only.

### 15.2 Division Rule

Do not use float.

Division must use mathematical ceiling division.

This rule must work correctly even if negative values are involved.

Implementation requirement:

```text
result = ceil(n / m)
```

Do not implement ceiling division using a simple `n / m + 1` remainder rule unless the implementation is proven to handle negative values correctly.

Division by zero must never be allowed.

If a division-by-zero expression is somehow selected, treat the expression as invalid.

Division rounds up immediately at the division calculation step.

#### Required Calculation Tests

| Expression | Expected Result | Notes |
|---|---:|---|
| `3 + 4 x 2` | `11` | Standard precedence: multiplication first. |
| `8 - 10` | `-2` | Negative result. |
| `5 ÷ 2` | `3` | Mathematical ceiling division. |
| `-5 ÷ 2` | `-2` | Mathematical ceiling division must handle negative values correctly. |
| `5 ÷ 0` | Invalid | Division by zero is not allowed. |

### 15.3 Negative Result Rule

If the final expression result is negative:

- the enemy is healed by the absolute value of the result.

Example:

```text
Expression result: -12
Enemy HP: 50 -> 62
```

Negative results always heal the enemy, regardless of Attack Mode or Defense Mode.

---

## 16. Tile Removal, Gravity, and Refill

After valid expression tiles are removed:

1. Tiles fall downward within the same column.
2. Empty cells are created at the top of each column.
3. New tiles spawn into the top empty cells.
4. New tiles use the current board generation and board deck rules.
5. Tiles do not move horizontally during gravity refill.
6. After refill, check whether at least one valid expression path exists.
7. If no valid path exists, apply the board regeneration safety rule.

Invalid expressions do not remove tiles.

Invalid expressions do not trigger gravity or refill.

---

## 17. Combat System

### 17.1 Stage and Enemy Structure

- One enemy appears per stage.
- One enemy defeat equals one stage clear.
- A full MVP run contains exactly 10 stages.
- Stage 1~9 use the enemy rotation rule below.
- Stage 10 is excluded from the Stage 1~9 enemy rotation rule.
- Stage 10 will use a separate final boss / Demon King rule.
- Between cleared Stage 1~9 fights, the player enters the integrated shop screen.
- After clearing Stage 10, the run ends and the shop screen does not open.

### 17.1.1 Base Enemy Types

There are three base enemy types for Stage 1~9.

These values are prototype balance data.

Do not hard-code them inside gameplay logic.

Store them as editable enemy config data.

| enemyId | displayName | baseHp | baseAttackDamage | attackCycle | Notes |
|---|---|---:|---:|---:|---|
| `Orc` | 오크 | 100 | 20 | 3 | Standard enemy. |
| `StoneGolem` | 스톤골렘 | 250 | 50 | 5 | High HP / high damage / slow attack enemy. |
| `Kobold` | 코볼트 | 80 | 20 | 2 | Low HP / fast attack enemy. |

`attackCycle` means the enemy attacks every N valid player turns.

Invalid expressions do not advance `attackCycle` countdown.

### 17.1.2 Enemy Rotation Blocks for Stage 1~9

Stage 1~9 are divided into three 3-stage enemy blocks.

| Enemy Block | Stage Range | Enemy Pool |
|---:|---|---|
| 1 | Stage 1~3 | `Orc`, `StoneGolem`, `Kobold` |
| 2 | Stage 4~6 | `Orc`, `StoneGolem`, `Kobold` |
| 3 | Stage 7~9 | `Orc`, `StoneGolem`, `Kobold` |

At the start of each enemy block, generate a randomized order of the three enemy types.

Each enemy type must appear exactly once within the same 3-stage enemy block.

Duplicate enemy types are not allowed within the same enemy block.

The enemy order is randomized independently for each enemy block.

The previous block's order does not constrain the next block's order.

Therefore, the same enemy type may appear consecutively across block boundaries, such as Stage 3 and Stage 4, unless a later config explicitly forbids it.

### 17.1.3 Stage 1 StoneGolem Exception

Stage 1 must not be `StoneGolem`.

For Enemy Block 1 only:

1. Choose the Stage 1 enemy randomly from `Orc` and `Kobold`.
2. Assign the remaining two enemy types randomly to Stage 2 and Stage 3.
3. `StoneGolem` must still appear exactly once somewhere in Stage 2~3.

This exception applies only to Stage 1.

Stage 4 and Stage 7 may be `StoneGolem`.

### 17.1.4 Enemy Stat Multiplier by Stage Range

Only enemy HP and enemy attack damage are multiplied.

Enemy attack cycle is never multiplied.

| Stage Range | Condition | hpMultiplier | attackDamageMultiplier |
|---|---|---:|---:|
| Stage 1~3 | Always | 1 | 1 |
| Stage 4~6 | Always | 2 | 2 |
| Stage 7~9 | Owned `BoardDeckUpgrade` count is 0~5 | 3 | 3 |
| Stage 7~9 | Owned `BoardDeckUpgrade` count is 6 or more | 4 | 4 |

For this multiplier rule, `BoardDeckUpgrade` count means the number of acquired shop items whose `itemCategory` is exactly `BoardDeckUpgrade` in the current run.

Free shop selections count if the selected item is a `BoardDeckUpgrade`.

Paid purchases count if the purchased item is a `BoardDeckUpgrade`.

UniqueItems do not count as `BoardDeckUpgrade`, even if a UniqueItem modifies board generation.

Final enemy stats are calculated as:

```text
finalHp = baseHp x hpMultiplier
finalAttackDamage = baseAttackDamage x attackDamageMultiplier
finalAttackCycle = attackCycle
```

### 17.1.5 Stage 10 Final Boss Exclusion

Stage 10 is not generated by the Stage 1~9 enemy rotation rule.

Stage 10 will use a separate final boss / Demon King config.

Until the final boss config is provided, Stage 10 enemy data may remain as a temporary placeholder.

The Stage 1~9 enemy rotation rule must not accidentally assign `Orc`, `StoneGolem`, or `Kobold` to Stage 10.

### 17.2 Turn Definition

One valid player expression action consumes one valid turn.

Invalid expressions do not consume a turn.

Invalid expressions do not:

- remove tiles,
- apply damage,
- apply shield,
- advance the enemy attack countdown,
- reduce remaining stage turn count.

Invalid expressions should show light visual feedback, such as a small tile shake, then cancel or reset the selection.

### 17.3 Enemy Attack Countdown Rule

Enemy countdown decreases only after a valid player expression action is fully resolved.

Resolution order:

```text
1. Player confirms a valid expression.
2. Apply any applicable `BoardStateTransformBeforeCalculation` UniqueItem effects.
3. Apply any applicable `PreCalculationTransform` UniqueItem effects to the confirmed expression interpretation.
4. Expression result is calculated.
5. Base attack or shield effect is applied.
6. Placeholder item hooks and `PostCalculationBonus` UniqueItem effects may run if implemented.
7. Used tiles are removed.
8. Board falls and refills.
9. Valid path check runs after refill.
10. Check enemy defeat.
11. If enemy HP is 0 or less, clear the stage immediately.
12. If the enemy is defeated, do not decrease enemy attack countdown and do not perform an enemy attack.
13. If the enemy is still alive, enemy attack countdown decreases by 1.
14. If countdown reaches 0, the enemy attacks.
15. After the enemy attacks, countdown resets to enemyAttackCycle.
```

Invalid expressions do not decrease the enemy countdown.

### 17.4 Attack Mode

If Attack Mode is active:

```text
Damage = expression result
```

If the expression result is positive, the enemy takes damage.

If the expression result is zero, enemy takes 0 damage and the valid turn is still consumed.

If the expression result is negative, the enemy is healed by the absolute value.

### 17.5 Defense Mode

If Defense Mode is active and the expression result is positive or zero:

```text
Shield = ceil(expressionResult x shieldConversionRate)
```

Default MVP value:

```text
TEMP_SHIELD_CONVERSION_RATE = 1.0
```

This means the full expression result becomes shield value by default.

The shield conversion rate must be editable in `CombatConfig`.

Do not hard-code the shield conversion rate inside combat logic.

If Defense Mode result is negative:

```text
Shield = 0
Enemy heals by abs(expression result)
```

### 17.6 Shield Duration Rule

Shield is temporary.

When the player gains shield in Defense Mode, the shield remains until the next enemy attack.

During the next enemy attack:

- incoming damage is reduced by current shield,
- if shield is greater than incoming damage, remaining shield is discarded,
- after the enemy attack is resolved, shield becomes 0.

Shield does not persist permanently.

Shield does not stack across multiple enemy attacks in MVP.

### 17.7 Victory Condition

The player wins the stage if the enemy HP reaches 0 before the stage fails.

Victory is checked after the valid player expression, base combat effect, item hooks, UniqueItem hooks, tile removal, board refill, and valid path check are resolved.

If the enemy is defeated by the player's valid action, the stage is cleared immediately and the enemy does not attack on that same action.

### 17.8 Defeat Condition

The player loses the stage if:

- player HP reaches 0,
- or the stage turn limit reaches 0 before the enemy is defeated.

### 17.9 Stage Turn Limit

Each stage has a turn limit.

Default MVP turn limit:

```text
TEMP_STAGE_TURN_LIMIT = 20 valid player turns
```

Only valid expressions consume turn count.

Invalid expressions do not consume turn count.

If remaining turns reach 0 and the enemy is still alive, the player loses.

Turn limit must be editable in `StageConfig`.

Do not hard-code the turn limit inside stage logic.

---

## 18. Player Death Flow

If the player dies, show 3 options:

1. Retry Current Stage
2. Restart From Stage 1
3. Quit Game

### 18.1 Retry Current Stage

Retry Current Stage restores the game to the start state of the failed stage.

Before each stage begins, save a stage-start snapshot.

The stage-start snapshot should include:

- current stage number,
- selected difficulty,
- player HP,
- gold,
- board deck modifiers,
- connection limit,
- passive effects,
- temporary item effects,
- owned UniqueItems,
- reroll used count for the current run,
- item acquisition counts,
- any other run-based state required to restore the stage start.

When the player selects Retry Current Stage, restore this snapshot and restart the failed stage.

### 18.2 Restart From Stage 1

Restart From Stage 1 starts a new run from Stage 1.

Reset all run-based states:

- stage number,
- gold,
- board deck modifiers,
- connection limit upgrades,
- passive effects,
- temporary item effects,
- owned UniqueItems,
- shop state,
- reroll count,
- item acquisition counts.

### 18.3 Quit Game

Quit Game returns the player to the title screen.

Do not close the application directly in MVP.

---

## 19. Integrated Shop System

After each cleared stage, except for the final Stage 10 clear flow, the player enters the shop screen.

For MVP, Stage 10 clear ends the run and does not open the shop screen.

The shop screen uses a 3 / 3 / 1 display structure:

```text
Top row:     3 free item slots
Middle row:  3 purchasable item slots
Lower row:   1 reroll button
```

The shop screen also has:

- 1 Exit To Title button,
- 1 Next Stage button.

This integrated shop screen replaces older separate reward/shop flow rules.

Do not implement an older flow where the player first chooses a free upgrade and then enters a separate 3-entry shop.

Do not implement the older 2-paid-slot structure.

Do not implement fixed `Paid Passive Slot` and `Paid Active Slot` categories for the MVP shop.

### 19.1 Normal Shop Layout

For most shop visits, the top and middle rows are:

```text
[ Free Slot 1 ] [ Free Slot 2 ] [ Free Slot 3 ]
[ Paid Slot 1 ] [ Paid Slot 2 ] [ Paid Slot 3 ]
[ Reroll Button ]
```

### 19.2 Unique Shop Layout

For Normal and Hard Mode only, the shop opened immediately after Stage 3, Stage 6, and Stage 9 are cleared uses the Unique Shop Layout.

The top and middle rows become:

```text
[ Free Slot 1 ] [ Free Slot 2 ] [ Free Slot 3 ]
[ Paid Slot 1 ] [ Paid Slot 2 ] [ Unique Item Slot ]
[ Reroll Button ]
```

The rightmost middle-row slot is the Unique Item Slot.

The Unique Item Slot replaces `Paid Slot 3` only for these shop visits.

In Easy Mode, the Unique Shop Layout is never used.

### 19.2.1 Unique Item Slot Position and Index Rule

The Unique Item Slot replaces the 3rd visible slot in the middle row.

For UI display, use one-based visible slot numbering:

```text
Middle Row Slot 1 = Paid Slot 1
Middle Row Slot 2 = Paid Slot 2
Middle Row Slot 3 = Unique Item Slot
```

For implementation arrays, use zero-based indexing unless a specific UI framework requires otherwise:

```text
Middle Row Slot 1 = index 0
Middle Row Slot 2 = index 1
Unique Item Slot = index 2
```

Required editable config values:

```text
TEMP_UNIQUE_SLOT_NUMBER_IN_MIDDLE_ROW = 3
TEMP_UNIQUE_SLOT_ZERO_BASED_INDEX_IN_MIDDLE_ROW = 2
```

Do not use `index 3` for the Unique Item Slot unless the implementation explicitly uses one-based indexing.

### 19.3 Shop Visit Index Rule

The special unique shop timing is based on the cleared stage number.

```text
Unique Shop = shop opened after clearing Stage 3, Stage 6, or Stage 9
```

Do not count rerolls as new shop visits.

Retrying a stage and returning to the same shop should still use the shop state restored from the stage-start snapshot or the current run state, depending on the retry implementation.

The unique shop condition should be checked using:

- selected difficulty,
- cleared stage number,
- uniqueShopStageList.

Recommended check:

```text
if difficulty is Normal or Hard
and clearedStageNumber is in uniqueShopStageList:
    use Unique Shop Layout
else:
    use Normal Shop Layout
```

`uniqueShopStageList` must be editable config data.

Default MVP value:

```text
TEMP_UNIQUE_SHOP_STAGE_LIST = [3, 6, 9]
```

### 19.4 Free Item Slots

The 3 top-row slots are free item slots.

Each free item slot randomly selects 1 item from the currently eligible free item pool.

Currently eligible free item means:

- the item is unlocked for the current stage,
- the item is not blocked by operator unlock rules,
- the item has not reached its own `maxAcquisitionsPerRun`,
- the item is valid for the current shop context,
- the item is not a UniqueItem.

Free item slots do not use rarity weights.

All eligible free items have equal item-level selection probability.

Example:

```text
If there are 30 eligible free items:
    each item has 1 / 30 selection chance for a free slot.
```

The 3 free item slots must not contain duplicate items within the same shop visit.

The player may choose exactly 1 of the 3 free items.

After the player chooses 1 free item:

- the chosen item is obtained,
- the other 2 free item slots become locked,
- the reroll button becomes locked for this shop visit.

### 19.5 Paid Item Slots

The middle row contains 3 purchasable slots in Normal Shop Layout.

Paid item slots do not have fixed category labels such as `Paid Passive Slot` or `Paid Active Slot`.

Each normal paid item slot may contain any currently eligible paid item, including:

- ActiveItem,
- PassiveItem,
- BoardDeckUpgrade,
- ConnectionLimitUpgrade.

UniqueItems are excluded from normal paid slots.

UniqueItems may appear only in the Unique Item Slot for Normal/Hard Mode after clearing Stage 3, Stage 6, or Stage 9.

The player may buy paid items using gold.

The player may buy multiple paid items if they have enough gold.

Each visible paid item slot can be purchased once per shop visit by default.

### 19.6 Item Category Rule

Use the following item categories for MVP:

```text
ActiveItem
PassiveItem
BoardDeckUpgrade
ConnectionLimitUpgrade
UniqueItem
```

Do not create a separate `ConsumableItem` category for MVP.

`ConsumableItem` is deprecated.

If older documents use `ConsumableItem`, treat it as `ActiveItem`.

Healing potions, attack potions, board refresh items, and other usable or consumable effects are all `ActiveItem`.

### 19.7 Rarity Grade Rule

Use only 3 rarity grades for MVP:

```text
Common
Rare
Legendary
```

Do not use `Epic`.

If older documents or dummy data contain `Epic`, convert it to either `Rare` or `Legendary` depending on design intent.

Rarity is used for item classification, presentation, and paid-slot selection.

Rarity must not be hard-coded into shop logic.

### 19.8 Paid Item Rarity Selection Rule

Paid item slots use rarity-weight selection.

Paid item slot generation flow:

```text
For each normal paid item slot:
    1. Build the currently eligible paid item pool.
    2. Exclude UniqueItems.
    3. Roll an item rarity using paidItemRarityWeights.
    4. Build the eligible item pool for the selected rarity.
    5. If that rarity has at least one eligible item:
        select 1 item from that rarity pool with equal probability.
    6. If that rarity has no eligible item:
        reroll rarity.
    7. If repeated rarity rerolls fail because no valid rarity pool exists:
        select 1 item from the full eligible paid item pool with equal probability.
```

The rarity weight table must be editable config data.

Do not hard-code paid item rarity weights inside shop logic.

Recommended config name:

```text
paidItemRarityWeights
```

Temporary dummy rarity weights:

| Rarity | TEMP Weight |
|---|---:|
| Common | 84 |
| Rare | 15 |
| Legendary | 1 |

These values are dummy balance values only.

The design team must be able to rebalance them without code changes.

### 19.9 Unique Item Slot Rule

The Unique Item Slot layout appears only when all of the following are true:

- difficulty is Normal or Hard,
- the shop opens after clearing Stage 3, Stage 6, or Stage 9.

If the current shop is a Unique Shop, `Paid Slot 3` is always replaced by the Unique Item Slot.

UniqueItems must not appear in free item slots.

UniqueItems must not appear in normal paid item slots.

UniqueItems are selected only from the eligible UniqueItem pool.

The Unique Item Slot is still a paid middle-row slot.

The player must pay the UniqueItem's `price` to obtain it.

Purchasing the Unique Item Slot counts as buying a paid item and locks the reroll button for that shop visit.

Recommended item category:

```text
itemCategory = UniqueItem
```

The Unique Item Slot selection flow:

```text
If current shop is a Unique Shop:
    1. Replace Paid Slot 3 with Unique Item Slot.
    2. Build the eligible UniqueItem pool.
    3. If at least one eligible UniqueItem exists:
        select 1 UniqueItem from the eligible UniqueItem pool.
        display it in the rightmost middle-row slot.
    4. If no eligible UniqueItem exists:
        display a locked placeholder slot.
        log a clear config warning.
```

### 19.9.1 Unique Item Slot Empty Pool Rule

If the current shop is a Unique Shop, `Paid Slot 3` is always replaced by the Unique Item Slot.

If at least one eligible UniqueItem exists, display one selectable UniqueItem in the Unique Item Slot.

If no eligible UniqueItem exists, display a locked placeholder slot and log a clear config warning.

Do not replace the Unique Item Slot with a normal paid item.

Do not silently hide the Unique Item Slot in a Unique Shop.

### 19.10 Duplicate Rule

Free item slots cannot duplicate each other within the same shop visit.

Paid item slots cannot duplicate each other within the same shop visit.

In a Unique Shop, `Paid Slot 1`, `Paid Slot 2`, and `Unique Item Slot` must not duplicate each other.

However, free item slots and paid item slots may contain the same item.

This is allowed because free selection and paid purchase are separate opportunity types.

### 19.11 Reroll Rule

The reroll button refreshes all 6 item slots:

- 3 free item slots,
- 3 middle-row item slots.

For a Unique Shop, reroll still refreshes all 6 item slots, but the rightmost middle-row slot remains the Unique Item Slot.

Unique Shop reroll behavior:

```text
Before reroll:
[ Paid Slot 1 ] [ Paid Slot 2 ] [ Unique Item Slot ]

After reroll:
[ New Paid Slot 1 ] [ New Paid Slot 2 ] [ New Unique Item Slot ]
```

The Unique Item Slot may show a different UniqueItem after reroll.

The Unique Item Slot must not become a normal paid item slot after reroll.

Reroll is available only before the player obtains or purchases any displayed item.

If the player selects a free item or buys a paid item, the reroll button becomes locked for that shop visit.

### 19.12 Reroll Cost Rule

Reroll cost increases during the current run.

Default formula:

```text
rerollCost = baseRerollCost + rerollUsedCountThisRun * rerollCostIncrease
```

Example:

```text
1st reroll: n
2nd reroll: n + m
3rd reroll: n + 2m
4th reroll: n + 3m
```

`baseRerollCost` and `rerollCostIncrease` must be editable in `ShopConfig`.

When a new run starts, reroll count resets to 0.

### 19.13 Exit To Title Button

The shop screen has an Exit To Title button in the bottom row.

When selected, it returns the player to `TitleScene`.

Do not close the application directly in MVP.

The button label may be:

```text
Exit
```

or

```text
Title
```

Final UI text may be adjusted later.

### 19.14 Next Stage Button

The shop screen has a Next Stage button in the bottom row.

When selected, it closes the shop screen and starts the next stage.

For MVP, the player must select exactly 1 free item before proceeding to the next stage.

Paid item purchase is optional.

The player may buy 0 or more paid items if they have enough gold.

Paid item purchase does not satisfy the free item selection requirement.

The Next Stage button is disabled until the player selects exactly 1 free item.

After the free item requirement is satisfied, the player may proceed to the next stage even if no paid items were purchased.

The free item requirement must be controlled by editable shop config.

Recommended config value:

```text
TEMP_REQUIRE_FREE_ITEM_BEFORE_NEXT = true
```

---

## 20. Gold Reward Rule

After each cleared stage from Stage 1 through Stage 9, the player receives a fixed stage-clear gold reward.

Do not use remaining-turn gold bonus for MVP.

Temporary MVP values:

| Cleared Stage Range | Gold Reward |
|---|---:|
| Stage 1–3 | 100 |
| Stage 4–6 | 150 |
| Stage 7–9 | 200 |

For MVP, Stage 10 is the final stage of the run.

After clearing Stage 10:

- do not open the shop screen,
- do not generate free item slots,
- do not generate paid item slots,
- do not generate a Unique Item Slot,
- do not require item selection,
- proceed to the temporary run-clear result flow or return-to-title flow.

Stage 10 clear reward may be omitted for MVP because no further shop visit occurs after Stage 10.

If a result screen is implemented later, Stage 10 clear reward may be handled separately through `RewardConfig`.

Gold reward values must be editable in `RewardConfig`.

Do not hard-code gold reward values inside stage logic.

### 20.1 Stage 10 Clear Flow Rule

For MVP, Stage 10 is the final stage of the run.

After clearing Stage 10, the run ends and the shop screen does not open.

Stage 10 clear flow should proceed to one of the following MVP-safe flows:

- temporary run-clear result flow,
- return-to-title flow.

Do not require any shop, free item, paid item, UniqueItem, or reroll behavior after Stage 10 is cleared.

---

## 21. Item Integration Shell

This document defines item integration structure only.

It does not define final item effects.

### 21.1 Item Price Category Rule

Use these temporary category-based prices unless an external Item Implementation Spec provides different editable values:

| Item Category | Temporary Default Price |
|---|---:|
| ActiveItem | 30 |
| BoardDeckUpgrade | 50 |
| PassiveItem | 50 |
| ConnectionLimitUpgrade | 50 |
| UniqueItem | 150 |

These are temporary default values.

All prices must be editable in item config data.

Do not hard-code item prices inside shop logic.

### 21.2 Per-Item Acquisition Limit Rule

Each item may define its own `maxAcquisitionsPerRun` value.

Acquisition limits are applied per individual item, not per rarity grade.

If an item reaches its `maxAcquisitionsPerRun`, that specific item is removed from the eligible item pool for the rest of the current run.

Other items with the same rarity may still appear if they have not reached their own acquisition limit.

The acquisition count increases only when the player actually obtains the item.

If an item is displayed but not obtained, the acquisition count does not increase.

When a new run starts, item acquisition counts reset.

### 21.3 Placeholder Effect Dispatch Rule

Implement a data-driven item effect dispatch path.

The item system should be able to call an effect by `effectType`.

However, final effect behavior should come from the external Item Implementation Spec.

If the effect type is unknown, log a clear config error.

Do not silently ignore unknown effects in development builds.

For dummy items, it is acceptable to use a safe placeholder effect such as `NoGameplayEffect`.

### 21.4 Board Refresh Rule

Use `Board Refresh` as the implementation-safe name for the old `Board Shuffle` concept.

For MVP, Board Refresh does not shuffle existing tiles.

It regenerates the entire current board immediately using current board generation rules.

This means:

- current board is discarded,
- a new board is generated using current board deck weights,
- valid path guarantee runs after regeneration,
- fallback path insertion may occur if regeneration fails after max attempts.

Recommended effect type:

```text
RegenerateCurrentBoardImmediately
```

Do not implement this as `RegenerateBoardNextStage`.

Do not delay the effect until the next stage.

The UI display name may later be changed to `Board Shuffle` if the design team prefers that wording.

---

## 22. Temporary Dummy Test Item Data

This section is dummy data only.

It exists only so the shop and item integration shell can be tested before external item specs are provided.

Do not treat this table as final item content.

Do not use this table for final balance.

When the external Item Implementation Spec is provided, replace this table.

When the external UniqueItem Implementation Spec is provided, replace dummy UniqueItem behavior.

| itemId | displayName | itemCategory | rarity | priceConfigKey | maxAcquisitionsPerRun | effectType | effectValueConfigKey | unlockStage | notes |
|---|---|---|---|---|---:|---|---|---:|---|
| `DUMMY_ACTIVE_HEAL` | Dummy Heal | ActiveItem | Common | `TEMP_ACTIVE_ITEM_PRICE` | 999 | `HealPlayer` | `TEMP_DUMMY_HEAL_AMOUNT` | 1 | Dummy ActiveItem test only. |
| `DUMMY_BOARD_REFRESH` | Board Refresh | ActiveItem | Common | `TEMP_ACTIVE_ITEM_PRICE` | 999 | `RegenerateCurrentBoardImmediately` | `1` | 1 | Dummy board refresh test only. |
| `DUMMY_CONNECTION_LIMIT_UP` | Dummy Connection Limit Up | ConnectionLimitUpgrade | Rare | `TEMP_PASSIVE_ITEM_PRICE` | 3 | `IncreaseConnectionLimit` | `TEMP_CONNECTION_LIMIT_INCREASE_VALUE` | 1 | Dummy connection limit test only. |
| `DUMMY_WEIGHT_UP` | Dummy High Number Up | BoardDeckUpgrade | Rare | `TEMP_PASSIVE_ITEM_PRICE` | 999 | `ModifySpawnWeights` | `TEMP_DEFAULT_WEIGHT_INCREASE` | 1 | Dummy board deck test only. Target numbers: 7, 8, 9. |
| `DUMMY_UNIQUE_PLACEHOLDER` | Dummy Unique Placeholder | UniqueItem | Legendary | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `NoGameplayEffect` | `0` | 0 | Dummy Unique Item Slot test only. Replace with external UniqueItem spec. |

---

## 23. Config and Balance Data Lock

### 23.1 Recommended Config Assets

| Config Asset | Responsibility |
|---|---|
| `BoardConfig` | Board size, connection limit, regeneration attempts, fallback expression. |
| `SpawnWeightConfig` | Number/operator spawn weights and A/B category weights. |
| `CombatConfig` | Player HP, shield conversion rate, negative result rule. |
| `DifficultyConfig` | Difficulty mode, UniqueItem availability, optional difficulty modifiers. |
| `EnemyConfig` | Base enemy type data, enemy rotation blocks, Stage 1 exception, enemy stat multipliers. |
| `StageConfig` | Resolved enemy data, turn limit, allowed operators, Stage 10 placeholder/final boss reference. |
| `RewardConfig` | Fixed stage-clear gold rewards. |
| `ShopConfig` | Free slot count, paid slot count, reroll cost, purchase rules, Next button rule, Exit button rule. |
| `ShopRarityConfig` | Paid item rarity weights and rarity reroll safety settings. |
| `UniqueShopConfig` | Unique shop stage list, unique slot visible number, unique slot zero-based index, starting UniqueItem candidate count. |
| `UniqueItemDefinition` | Individual UniqueItem ID, display name, target number, price, trigger timing layer, effect type, effect values, presentation notes. |
| `UniqueBoardGenerationOverrideConfig` | A/B category ratio override values for UniqueItem-only `BoardGenerationOverride` effects. |
| `ShopItemDefinition` | Individual item ID, display name, category, rarity, price, effect type, effect value. |
| `UpgradeDefinition` | Upgrade ID, display name, target value, modifier value, unlock stage, stack rule. |

### 23.2 Required Editable Values

The following values must be editable:

- board width,
- board height,
- initial max connection length,
- connection limit increase value,
- max regeneration attempts,
- fallback expression,
- number spawn weights,
- operator spawn weights,
- A/B category weights,
- UniqueItem-only board generation override ratios,
- base enemy type IDs and display names,
- base enemy HP,
- base enemy attack damage,
- base enemy attack cycle,
- enemy rotation block size,
- enemy rotation stage range,
- Stage 1 forbidden enemy rule,
- enemy stat multipliers by stage range,
- Stage 7~9 BoardDeckUpgrade count threshold,
- Stage 10 final boss placeholder/reference,
- player max HP,
- player starting HP,
- shield conversion rate,
- turn limit,
- fixed stage clear gold rewards,
- free item slot count,
- paid item slot count,
- paid item rarity weight table,
- paid rarity reroll safety count,
- unique shop stage list,
- unique slot visible number,
- unique slot zero-based index,
- starting UniqueItem candidate count,
- require free item before next rule,
- reroll base cost,
- reroll cost increase,
- shop item prices,
- upgrade modifier values,
- item rarity,
- item `maxAcquisitionsPerRun`.

### 23.3 Temporary Balance Table

These values are temporary prototype values.

They are not final balance.

The design team may edit or delete them later.

| Variable Name | Temporary Value | Purpose |
|---|---:|---|
| `TEMP_BOARD_WIDTH` | 5 | Default MVP board width. |
| `TEMP_BOARD_HEIGHT` | 5 | Default MVP board height. |
| `TEMP_INITIAL_MAX_CONNECTION_LENGTH` | 5 | Initial max connectable tile count. |
| `TEMP_CONNECTION_LIMIT_INCREASE_VALUE` | 2 | Increase value for Connection Limit Up. |
| `TEMP_MAX_REGENERATION_ATTEMPTS` | 20 | Max board regeneration attempts before fallback. |
| `TEMP_PLAYER_MAX_HP` | 100 | Player max HP prototype value. |
| `TEMP_PLAYER_STARTING_HP` | 100 | Player starting HP prototype value. |
| `TEMP_SHIELD_CONVERSION_RATE` | 1.0 | Default expression-to-shield conversion rate. |
| `TEMP_STAGE_TURN_LIMIT` | 20 | Default valid turn limit per stage. |
| `TEMP_ENEMY_ROTATION_STAGE_RANGE` | 1~9 | Stages that use the 3-enemy rotation rule. |
| `TEMP_ENEMY_ROTATION_BLOCK_SIZE` | 3 | Number of stages per enemy rotation block. |
| `TEMP_STAGE_1_FORBIDDEN_ENEMY` | `StoneGolem` | Stage 1 must not generate this enemy. |
| `TEMP_ORC_BASE_HP` | 100 | Orc base HP. |
| `TEMP_ORC_BASE_ATTACK_DAMAGE` | 20 | Orc base attack damage. |
| `TEMP_ORC_ATTACK_CYCLE` | 3 | Orc attacks every 3 valid player turns. |
| `TEMP_STONE_GOLEM_BASE_HP` | 250 | StoneGolem base HP. |
| `TEMP_STONE_GOLEM_BASE_ATTACK_DAMAGE` | 50 | StoneGolem base attack damage. |
| `TEMP_STONE_GOLEM_ATTACK_CYCLE` | 5 | StoneGolem attacks every 5 valid player turns. |
| `TEMP_KOBOLD_BASE_HP` | 80 | Kobold base HP. |
| `TEMP_KOBOLD_BASE_ATTACK_DAMAGE` | 20 | Kobold base attack damage. |
| `TEMP_KOBOLD_ATTACK_CYCLE` | 2 | Kobold attacks every 2 valid player turns. |
| `TEMP_ENEMY_STAT_MULTIPLIER_STAGE_1_TO_3` | 1 | HP and attack damage multiplier for Stage 1~3. |
| `TEMP_ENEMY_STAT_MULTIPLIER_STAGE_4_TO_6` | 2 | HP and attack damage multiplier for Stage 4~6. |
| `TEMP_STAGE_7_TO_9_BOARD_DECK_UPGRADE_THRESHOLD` | 6 | Stage 7~9 uses x4 multiplier when owned BoardDeckUpgrade count is 6 or more. |
| `TEMP_ENEMY_STAT_MULTIPLIER_STAGE_7_TO_9_LOW_UPGRADE_COUNT` | 3 | HP and attack damage multiplier for Stage 7~9 when BoardDeckUpgrade count is 0~5. |
| `TEMP_ENEMY_STAT_MULTIPLIER_STAGE_7_TO_9_HIGH_UPGRADE_COUNT` | 4 | HP and attack damage multiplier for Stage 7~9 when BoardDeckUpgrade count is 6 or more. |
| `TEMP_STAGE_CLEAR_GOLD_1_TO_3` | 100 | Stage 1–3 clear gold. |
| `TEMP_STAGE_CLEAR_GOLD_4_TO_6` | 150 | Stage 4–6 clear gold. |
| `TEMP_STAGE_CLEAR_GOLD_7_TO_9` | 200 | Stage 7–9 clear gold. |
| `TEMP_ACTIVE_ITEM_PRICE` | 30 | Temporary default ActiveItem price. |
| `TEMP_PASSIVE_ITEM_PRICE` | 50 | Temporary default passive / board deck / connection limit price. |
| `TEMP_UNIQUE_ITEM_PRICE` | 150 | Temporary default UniqueItem price. |
| `TEMP_FREE_ITEM_SLOT_COUNT` | 3 | Free item slots in shop. |
| `TEMP_PAID_ITEM_SLOT_COUNT` | 3 | Middle-row purchasable slots in normal shop. |
| `TEMP_PAID_RARITY_COMMON_WEIGHT` | 84 | Temporary Common rarity weight for paid item slots. |
| `TEMP_PAID_RARITY_RARE_WEIGHT` | 15 | Temporary Rare rarity weight for paid item slots. |
| `TEMP_PAID_RARITY_LEGENDARY_WEIGHT` | 1 | Temporary Legendary rarity weight for paid item slots. |
| `TEMP_UNIQUE_SHOP_STAGE_LIST` | 3, 6, 9 | Stages after which the rightmost paid slot becomes a Unique Item Slot in Normal/Hard. |
| `TEMP_UNIQUE_SLOT_NUMBER_IN_MIDDLE_ROW` | 3 | 3rd visible slot in the middle row. |
| `TEMP_UNIQUE_SLOT_ZERO_BASED_INDEX_IN_MIDDLE_ROW` | 2 | Zero-based array index for the rightmost middle-row slot. |
| `TEMP_STARTING_UNIQUE_CANDIDATE_COUNT` | 3 | Starting UniqueItem candidate count for Normal/Hard. |
| `TEMP_UNIQUE_4_A_CELL_NUMBER_RATIO` | 90 | Temporary A Cell Number ratio for UniqueItem-only BoardGenerationOverride testing. |
| `TEMP_UNIQUE_4_A_CELL_OPERATOR_RATIO` | 10 | Temporary A Cell Operator ratio for UniqueItem-only BoardGenerationOverride testing. |
| `TEMP_UNIQUE_4_B_CELL_NUMBER_RATIO` | 10 | Temporary B Cell Number ratio for UniqueItem-only BoardGenerationOverride testing. |
| `TEMP_UNIQUE_4_B_CELL_OPERATOR_RATIO` | 90 | Temporary B Cell Operator ratio for UniqueItem-only BoardGenerationOverride testing. |
| `TEMP_REQUIRE_FREE_ITEM_BEFORE_NEXT` | true | Player must select 1 free item before moving to the next stage. |
| `TEMP_BASE_REROLL_COST` | 5 | First reroll cost. |
| `TEMP_REROLL_COST_INCREASE` | 3 | Additional cost per reroll used in current run. |
| `TEMP_DUMMY_HEAL_AMOUNT` | 20 | Dummy Heal test value. |
| `TEMP_DEFAULT_WEIGHT_INCREASE` | 2 | Default dummy spawn weight increase value. |
| `TEMP_DEFAULT_WEIGHT_DECREASE` | -2 | Default dummy spawn weight decrease value. |

### 23.4 Temporary Enemy Rotation and Stage Balance Tables

The old fixed per-stage enemy HP / attack damage / attack cycle table is deprecated.

Do not implement Stage 1~9 enemies from a fixed per-stage enemy stat table.

Stage 1~9 enemies must be generated from the enemy rotation rule below.

The design team may freely edit or replace these values later.

#### 23.4.1 Base Enemy Table

| enemyId | displayName | baseHp | baseAttackDamage | attackCycle |
|---|---|---:|---:|---:|
| `Orc` | 오크 | 100 | 20 | 3 |
| `StoneGolem` | 스톤골렘 | 250 | 50 | 5 |
| `Kobold` | 코볼트 | 80 | 20 | 2 |

#### 23.4.2 Enemy Rotation Block Table

| Enemy Block | Stage Range | Rule |
|---:|---|---|
| 1 | Stage 1~3 | Generate all three enemy types exactly once. Stage 1 must not be `StoneGolem`. |
| 2 | Stage 4~6 | Generate all three enemy types exactly once. Full random order allowed. |
| 3 | Stage 7~9 | Generate all three enemy types exactly once. Full random order allowed. |

#### 23.4.3 Enemy Stat Multiplier Table

| Stage Range | Condition | HP Multiplier | Attack Damage Multiplier |
|---|---|---:|---:|
| Stage 1~3 | Always | 1 | 1 |
| Stage 4~6 | Always | 2 | 2 |
| Stage 7~9 | Owned `BoardDeckUpgrade` count is 0~5 | 3 | 3 |
| Stage 7~9 | Owned `BoardDeckUpgrade` count is 6 or more | 4 | 4 |

Only HP and attack damage are multiplied.

Enemy attack cycle is never multiplied.

#### 23.4.4 Final Enemy Stat Formula

```text
finalHp = baseHp x hpMultiplier
finalAttackDamage = baseAttackDamage x attackDamageMultiplier
finalAttackCycle = attackCycle
```

#### 23.4.5 Stage 10 Placeholder Rule

Stage 10 is excluded from the Stage 1~9 enemy rotation rule.

Stage 10 will use a separate final boss / Demon King rule.

Until the final boss data is provided, Stage 10 may use a temporary placeholder enemy config, but it must be clearly separated from the Stage 1~9 enemy rotation system.

#### 23.4.6 Temporary Allowed Operator Table

Allowed operators remain stage-based.

| Stage | Allowed Operators |
|---:|---|
| 1 | `+`, `-` |
| 2 | `+`, `-` |
| 3 | `+`, `-`, `x`, `÷` |
| 4 | `+`, `-`, `x`, `÷` |
| 5 | `+`, `-`, `x`, `÷` |
| 6 | `+`, `-`, `x`, `÷` |
| 7 | `+`, `-`, `x`, `÷` |
| 8 | `+`, `-`, `x`, `÷` |
| 9 | `+`, `-`, `x`, `÷` |
| 10 | `+`, `-`, `x`, `÷` |

---

## 24. MVP Visual Feedback Scope

Implement lightweight visual feedback first.

Required MVP feedback:

- selected tile highlight,
- connection line,
- expression preview text,
- valid release feedback,
- invalid tile shake,
- damage number popup,
- shield gain popup,
- enemy attack feedback,
- simple tile removal animation,
- simple tile fall animation.

Avoid heavy particle effects for MVP.

More advanced effects may be added later after the core gameplay is stable.

---

## 25. Mobile Optimization Requirement

This project must be developed with mobile optimization as a core requirement from the beginning.

Optimization is not a later polishing step.

It is a primary development constraint.

### 25.1 General Optimization Rules

The implementation should avoid:

- unnecessary per-frame calculations,
- excessive object creation,
- repeated board-wide searches,
- heavy runtime allocations,
- expensive visual effects,
- unnecessary physics simulation,
- unnecessary UI rebuilds.

### 25.2 Board Optimization Rules

- Avoid scanning every possible path every frame.
- Validate paths only when the player changes selection or releases input.
- Cache board cell references.
- Use simple coordinate-based adjacency checks.
- Use object pooling for tiles if tiles are frequently destroyed and regenerated.
- Keep the board size small for MVP.

### 25.3 Weighted Random Optimization

- Precompute total weights when tables change.
- Do not recalculate total weights every tile if the table has not changed.
- Recalculate weights only when an upgrade modifies the board deck state.

### 25.4 UI Optimization Rules

- Update combat UI only when values change.
- Avoid rebuilding the entire board UI every turn.
- Separate visual animation from logical board state.
- Use lightweight effects for mobile.

### 25.5 Calculation Optimization Rules

- Expression calculation only happens when the player confirms a path.
- Do not calculate expression results every frame while dragging unless needed for preview.
- If live preview is used, update only when the selected path changes.

---

## 26. Implementation Module Suggestions

Primary target: Unity / C#.

Rules should remain as engine-agnostic as possible, but the suggested module structure assumes Unity development.

| Module | Responsibility |
|---|---|
| `RunManager` | Controls run state, difficulty, stage count, restart, retry, shop/stage transitions. |
| `StageManager` | Loads stage config and stage-start snapshots. |
| `BoardManager` | Owns board state, tile placement, removal, falling, refill, regeneration. |
| `Tile` | Represents one board tile. |
| `TileType` | Defines Number / Operator category. |
| `TileValue` | Stores actual number or operator value. |
| `BoardWeightMap` | Provides A/B checkerboard category weights. |
| `SpawnWeightTable` | Stores number/operator spawn weights. |
| `BoardDeckState` | Stores player upgrade modifiers for current run. |
| `BoardGenerator` | Generates board using weight map and spawn tables. |
| `ValidPathChecker` | Checks whether at least one valid expression path exists. |
| `ExpressionValidator` | Checks whether selected path is valid. |
| `ExpressionCalculator` | Calculates expression result using standard precedence. |
| `CombatManager` | Applies attack, defense, enemy turns, victory, defeat. |
| `EnemyController` | Stores enemy HP, attack cycle, damage. |
| `ShopManager` | Presents free items, paid items, reroll, purchases, and Unique Item Slot. |
| `ItemDatabase` | Loads item definitions from config or external item spec data. |
| `UniqueItemDatabase` | Loads UniqueItem definitions from config or external UniqueItem spec data. |
| `UpgradeApplier` | Applies upgrade and item effects. |
| `WeightedRandomSelector` | Selects values based on weighted random logic. |
| `UIManager` | Controls UGUI panels and TextMeshPro text updates. |
| `OptimizationGuard` | Optional helper for pooling, allocation checks, and debug profiling. |

---

## 27. MVP Test Cases

### 27.1 Expression Validation Tests

| Selected Path | Expected Result | Turn Consumed | Notes |
|---|---|---:|---|
| `3` | Invalid | No | A single number is not valid. |
| `3 +` | Invalid | No | After cutting `+`, only `3` remains. |
| `3 + 4` | Valid | Yes | Shortest valid expression. |
| `3 + 4 x` | Valid as `3 + 4` | Yes | Final operator is ignored and not consumed. |
| `3 + x 4` | Invalid | No | Operators cannot appear consecutively. |
| `3 4 + 5` | Invalid | No | Numbers cannot appear consecutively. |
| `3 + 4 - 2` | Valid | Yes | Alternating path within initial 5-tile limit. |
| `3 + 4 - 2 x` | Valid as `3 + 4 - 2` | Yes | Final operator ignored if remaining path is valid. |

### 27.2 Combat Tests

| Mode | Expression Result | Expected Behavior |
|---|---:|---|
| Attack | `10` | Enemy takes 10 damage. |
| Attack | `0` | Enemy takes 0 damage and valid turn is consumed. |
| Attack | `-10` | Enemy heals 10 HP. |
| Defense | `10` | Player gains 10 shield by default. |
| Defense | `0` | Player gains 0 shield and valid turn is consumed. |
| Defense | `-10` | Player gains 0 shield, enemy heals 10 HP. |

### 27.3 Turn and Invalid Feedback Tests

| Situation | Expected Behavior |
|---|---|
| Valid expression confirmed | Consumes 1 valid turn. |
| Invalid expression confirmed | Consumes 0 turns. |
| Invalid expression confirmed | Removes 0 tiles. |
| Invalid expression confirmed | Enemy attack countdown does not advance. |
| Invalid expression confirmed | Selected tiles show small invalid feedback, then selection is cancelled/reset. |

### 27.4 Enemy Rotation Tests

| Situation | Expected Result |
|---|---|
| Stage 1 enemy is generated | Enemy is either `Orc` or `Kobold`. |
| Stage 1 enemy generation attempts to select `StoneGolem` | Reject and regenerate / use Stage 1 exception logic. |
| Stage 1~3 enemy block is generated | `Orc`, `StoneGolem`, and `Kobold` each appear exactly once. |
| Stage 4~6 enemy block is generated | `Orc`, `StoneGolem`, and `Kobold` each appear exactly once. |
| Stage 7~9 enemy block is generated | `Orc`, `StoneGolem`, and `Kobold` each appear exactly once. |
| Same enemy appears in Stage 3 and Stage 4 | Allowed because they belong to different enemy blocks. |
| Stage 4~6 enemy is generated | Final HP and attack damage use x2 multiplier. |
| Stage 7~9 enemy is generated with owned BoardDeckUpgrade count 0~5 | Final HP and attack damage use x3 multiplier. |
| Stage 7~9 enemy is generated with owned BoardDeckUpgrade count 6 or more | Final HP and attack damage use x4 multiplier. |
| Any Stage 1~9 enemy is generated | Enemy attack cycle remains the base attack cycle and is not multiplied. |
| Stage 10 enemy is generated | Do not use Stage 1~9 enemy rotation; use separate final boss / Demon King config. |

### 27.5 Operator Unlock Tests

| Stage | Allowed Operators | `x` / `÷` Shop Items |
|---:|---|---|
| 1 | `+`, `-` | Hidden from eligible pool. |
| 2 | `+`, `-` | Hidden from eligible pool. |
| 3 | `+`, `-`, `x`, `÷` | May appear. |
| 4+ | `+`, `-`, `x`, `÷` | May appear. |

### 27.6 Board Generation Safety Tests

| Situation | Expected Behavior |
|---|---|
| Generated board has at least one valid path | Use the board. |
| Generated board has no valid path | Regenerate the entire board. |
| Regeneration fails 20 times | Force-insert fallback path `1 + 2`. |
| Fallback path inserted | The path occupies three orthogonally adjacent cells. |

### 27.7 Difficulty and UniqueItem Tests

| Situation | Expected Result |
|---|---|
| Easy Mode run starts | No starting UniqueItem candidate is shown. |
| Normal Mode run starts | Starting UniqueItem candidates are shown. |
| Hard Mode run starts | Starting UniqueItem candidates are shown. |
| Easy Mode Stage 3 / 6 / 9 shop opens | Normal shop layout remains 3 free + 3 paid. |
| Normal/Hard Stage 3 / 6 / 9 shop opens | Middle-right paid slot is replaced by Unique Item Slot. |
| Easy Mode item pool is built | UniqueItems are excluded. |
| Normal/Hard Unique Shop is built | Eligible UniqueItem pool is used for Unique Item Slot only. |
| A UniqueItem with `BoardGenerationOverride` is owned | Only that UniqueItem effect may override A/B category ratios; normal items still modify Layer 2 spawn weights only. |

### 27.8 Shop Tests

| Situation | Expected Result |
|---|---|
| Stage 1–9 is cleared | Player receives fixed stage-clear gold. |
| Stage 1–9 is cleared | Shop screen opens. |
| Stage 10 is cleared | Shop screen does not open and the run-clear flow begins. |
| Normal shop opens | 3 free slots, 3 paid slots, 1 reroll button, 1 Exit button, and 1 Next button are displayed. |
| Stage 3 / 6 / 9 clear shop opens in Normal/Hard | Rightmost middle-row paid slot is replaced by a Unique Item Slot. |
| Free slots are generated | Free slots do not duplicate each other. |
| Free slots are generated | UniqueItems do not appear in free slots. |
| Paid slots are generated | Paid slots use paid item rarity weights. |
| Selected paid rarity has no eligible item | Rarity is rerolled. |
| Paid slots are generated | Paid slots do not duplicate each other. |
| Free and paid slots contain same item | Allowed. |
| Unique Shop is rerolled | Unique Item Slot remains a Unique Item Slot and may show a different UniqueItem. |
| Player selects a free item | Other free items lock and reroll locks. |
| Player buys a paid item | That slot locks and reroll locks. |
| Reroll used before any item obtained | All 6 item slots refresh. |
| Reroll used | Reroll cost increases for the current run. |
| New run starts | Reroll count resets. |
| Exit button selected | Return to `TitleScene`. |
| Next button selected before selecting a free item | Next button remains disabled or does not proceed. |
| Next button selected after exactly 1 free item is selected | Shop closes and next stage starts. |
| Next button selected after selecting 1 free item and buying 0 paid items | Shop closes and next stage starts. |

### 27.9 Item Acquisition Limit Tests

| Situation | Expected Result |
|---|---|
| Item A maxAcquisitionsPerRun = 1 and is obtained once | Item A is removed from eligible pool for the rest of the run. |
| Item A and Item B are both Legendary | Obtaining Item A does not block Item B. |
| Item is displayed but not obtained | Acquisition count does not increase. |
| New run starts | Item acquisition counts reset. |

### 27.10 Board Refresh Tests

| Situation | Expected Result |
|---|---|
| Board Refresh is used | Current board is immediately regenerated. |
| Board Refresh is used | Current board deck weights are applied. |
| Board Refresh generates no valid path | Regeneration safety rule runs. |
| Board Refresh fails regeneration 20 times | Fallback path `1 + 2` is inserted. |
| Board Refresh is used | It does not wait until the next stage. |

### 27.11 Death Flow Tests

| Situation | Expected Result |
|---|---|
| Player dies | Show Retry Current Stage / Restart From Stage 1 / Quit Game. |
| Retry Current Stage selected | Restore stage-start snapshot. |
| Restart From Stage 1 selected | Start new run and reset run-based states. |
| Quit Game selected | Return to TitleScene. |

---

## 28. Prototype Optional

These features can be implemented after the MVP core is stable.

- Connected-order calculation test.
- More elaborate enemy conditions.
- More advanced passive item builds.
- Special tiles.
- Stage-specific board size changes.
- Stage-specific weight maps.
- Partial shuffle instead of full regeneration.
- More elaborate visual effects.
- Result scene.
- Settings scene.
- Tutorial scene.
- Persistent save data.

---

## 29. Later Expansion

The following should not block the first playable prototype.

- Boss-specific gimmicks.
- Negative tile generation after player is hit.
- Advanced roguelike reward structure.
- Permanent progression.
- More board sizes.
- More stage-specific weight maps.
- Long-term content volume.
- More elaborate UI/UX onboarding.
- Full final item set.
- Full final UniqueItem set.
- Final VFX/SFX implementation for UniqueItem triggers.

---

## 30. Remaining Real TBD

Only the following are intentionally left for later design tuning or external specs.

### 30.1 Final Item Data TBD

Defined by external Item Implementation Spec:

- final item list,
- final item rarity,
- final item `maxAcquisitionsPerRun`,
- final item prices,
- final item effect values,
- final item effect behavior.

### 30.2 Final UniqueItem Data TBD

Defined by external UniqueItem Implementation Spec:

- final UniqueItem list,
- number-specific trigger conditions,
- effect values,
- trigger timing,
- VFX/SFX requirements,
- exact calculation order if special calculation modifications exist.

### 30.3 Final Balance TBD

- final player HP,
- final enemy HP by stage,
- final enemy attack damage,
- final enemy attack cycle,
- final stage turn limits,
- final gold rewards,
- final reroll base cost,
- final reroll cost increase,
- final rarity weights.

Temporary balance values are allowed for prototype implementation but must remain editable.

### 30.4 Final Visual Direction TBD

- final art style,
- final VFX intensity,
- final character/enemy animations,
- final shop UI polish.

MVP should use lightweight feedback first.

---

## 31. Final Implementation Reminder

Build the prototype around the core loop first.

Do not overbuild long-term systems before the following are fun:

- tile connection,
- expression validation,
- expression preview,
- expression result calculation,
- attack / defense conversion,
- enemy turn pressure,
- tile removal and refill,
- board regeneration,
- integrated shop flow,
- short item selection,
- retry / restart flow.

The board deck system should feel like a light upgrade system, not a complicated card game.

The player should feel:

```text
I chose this item, so future boards are becoming more like the style I want.
```

The player should not feel:

```text
I need to calculate exact deck counts and probability math every turn.
```

Keep the system simple, readable, mobile-friendly, data-driven, and easy to tune.
