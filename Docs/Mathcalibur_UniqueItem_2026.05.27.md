# Mathcalibur UniqueItem Implementation Spec

> File name pattern: `Mathcalibur_UniqueItem_[date].md`  
> Document purpose: AI coding tool input / Unity UniqueItem implementation guide  
> Target base document role: `Mathcalibur_Main_[date].md`  
> Related normal item document role: `Mathcalibur_Item_[date].md`  
> Document status: self-contained UniqueItem implementation spec  
> Original spreadsheet requirement: do not require the Excel file during implementation  
> Scope: UniqueItem only. This document does not define normal item behavior.

---

## 0. Critical Implementation Rule

This document is designed to be used after the core game has been implemented from the Main Game Implementation Spec:

```text
Mathcalibur_Main_[date].md
```

The date part is for human version tracking only.

Implementation priority must be determined by document role, not by date.

The Main Game Implementation Spec remains the highest-priority source for:

- core board rules,
- expression validation,
- calculation rules,
- combat rules,
- stage flow,
- difficulty behavior,
- integrated shop structure,
- reroll structure,
- Unique Item Slot structure,
- item loading interface,
- config and balance-data rules.

This document defines only final UniqueItem data and UniqueItem effect behavior for the current MVP UniqueItem set.

If this document conflicts with `Mathcalibur_Main_[date].md` on core gameplay, shop structure, stage flow, difficulty behavior, retry/restart rules, normal item behavior, or config priority, follow `Mathcalibur_Main_[date].md`.

If this document defines UniqueItem effect behavior, use this document instead of dummy UniqueItem data in the Main document.

Do not ask for the original Excel file.

Do not depend on the original Excel file.

All UniqueItem data required for implementation is included in this document.

---

## 1. Self-Contained Data Rule

This file replaces the source spreadsheet for UniqueItem implementation.

Implementation must use the UniqueItem table and rules in this document directly.

The design team may later convert this data into:

- ScriptableObject assets,
- JSON files,
- CSV data,
- spreadsheet-derived data tables,
- or another editable data format.

Final UniqueItem balance must not be hidden inside gameplay logic.

---

## 2. Hard-Coding Ban

Do not hard-code UniqueItem balance values inside gameplay logic.

The following must be editable config data:

- UniqueItem prices,
- trigger count values,
- probability values,
- bonus damage values,
- shield values,
- gold reward modifiers,
- A/B category override ratios,
- trigger turn numbers,
- acquisition limits,
- UI display text if the implementation supports data-driven localization.

Every temporary value in this document uses the `TEMP_` prefix.

---

## 3. Item Category and Rarity

Use the existing MVP item category:

```text
itemCategory = UniqueItem
```

Use the existing MVP rarity grade:

```text
rarity = Legendary
```

Do not create additional categories such as `SpecialUniqueItem`, `UniquePassive`, or `UniqueBoardItem`.

All UniqueItems in this document are passive run-based effects.

---

## 4. Price and Acquisition Config

| Config Key | Temporary Value | Used By |
|---|---:|---|
| `TEMP_UNIQUE_ITEM_PRICE` | 150 | All shop-purchased UniqueItems |
| `TEMP_UNIQUE_ITEM_MAX_ACQUISITIONS_PER_RUN` | 1 | All UniqueItems |
| `TEMP_STARTING_UNIQUE_CANDIDATE_COUNT` | 3 | Starting UniqueItem candidate count from core config |

### 4.1 Starting Candidate Rule

At the start of a Normal or Hard run, the player chooses exactly 1 UniqueItem from starting candidates.

Starting UniqueItem selection is free.

Unselected starting candidates are not banned and may appear later in shop Unique Item Slots.

### 4.2 Shop Purchase Rule

Shop-purchased UniqueItems use:

```text
priceConfigKey = TEMP_UNIQUE_ITEM_PRICE
```

### 4.3 Acquisition Limit Rule

All UniqueItems use:

```text
maxAcquisitionsPerRun = 1
```

Owned UniqueItems do not appear again in the current run.

Displayed but unpurchased UniqueItems may appear again later.

When a new run starts, all owned UniqueItems reset.

---

## 5. Difficulty and Shop Availability

Follow the Main document difficulty rules.

### 5.1 Easy Mode

In Easy Mode:

- no starting UniqueItem candidate is offered,
- Unique Item Slots do not appear,
- UniqueItems are excluded from all item pools,
- UniqueItem effects cannot trigger.

### 5.2 Normal / Hard Mode

In Normal and Hard Mode:

- starting UniqueItem candidate selection is enabled,
- shops after clearing Stage 3 / 6 / 9 use the Unique Shop Layout,
- UniqueItems may appear only in the Unique Item Slot,
- UniqueItem effects may trigger only if the player owns that UniqueItem.

---

## 6. UniqueItem Timing Layers

UniqueItem effects are organized by trigger timing.

| Timing Layer | Purpose | UniqueItems |
|---|---|---|
| `BoardGenerationOverride` | Override board generation Layer 1 category roll | Unique 4 |
| `PreCalculationTransform` | Transform selected expression values before calculation | Unique 1 |
| `BoardStateTransformBeforeCalculation` | Transform actual board tile values before calculation | Unique 9 |
| `PostCalculationBonus` | Add damage or shield after base calculation | Unique 2, Unique 3, Unique 5, Unique 7 |
| `RewardModifier` | Modify stage clear gold | Unique 6, Unique 8 |

### 6.1 Full Valid Turn Resolution Order

When the player confirms a valid expression, process systems in this order:

```text
1. Validate selected expression path using current board state.
2. Increase validTurnCountThisStage by 1.
3. If Unique 9 is owned and validTurnCountThisStage is 9 or 18:
   apply Unique 9 board-wide transform before calculation.
4. If Unique 1 oneTransformReady is true:
   apply Unique 1 selected-expression calculation transform.
5. Calculate base expression result using standard core calculation rules.
6. Apply base Attack Mode or Defense Mode result.
7. Apply Attack Potion to base positive Attack Mode damage if pending.
8. Apply UniqueItem post-calculation bonus damage and shield effects.
9. Update Unique 1 usage counter if applicable.
10. Remove used expression tiles.
11. Apply gravity and refill.
12. Continue enemy countdown and stage flow according to the Main document.
```

Important:

- Unique 9 modifies the actual board before calculation.
- Unique 1 is calculation-only and does not permanently change board tiles.
- UniqueItem bonus damage is calculated separately from Attack Potion multiplication.

---

## 7. Attack Potion and UniqueItem Bonus Damage Rule

Attack Potion modifies only the base positive Attack Mode expression damage.

UniqueItem bonus damage is calculated separately from the original base damage and then added.

Do not multiply UniqueItem bonus damage by Attack Potion.

Recommended calculation:

```text
baseDamage = positive expression result
potionAdjustedBaseDamage = ceil(baseDamage * attackPotionMultiplierPercent / 100)
uniqueBonusDamage = calculated separately from baseDamage
finalDamage = potionAdjustedBaseDamage + uniqueBonusDamage
```

Example:

```text
baseDamage = 100
Attack Potion = 150%
Unique 7 bonus = 50%

potionAdjustedBaseDamage = ceil(100 * 150 / 100) = 150
uniqueBonusDamage = ceil(100 * 50 / 100) = 50
finalDamage = 200
```

If no Attack Potion is pending:

```text
potionAdjustedBaseDamage = baseDamage
```

---

## 8. UniqueItem Data Table

This table is the authoritative UniqueItem data for this implementation spec.

| uniqueItemId | displayName | targetNumber | itemCategory | rarity | priceConfigKey | maxAcquisitionsPerRun | effectType | triggerTiming | unlockStage | shortDescriptionKo |
|---|---|---:|---|---|---|---:|---|---|---:|---|
| `UNIQUE_1_AWAKENED_ONE` | 각성하는 하나 | 1 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `AccumulateOneThenTransformNextExpression` | `PreCalculationTransform` | 0 | 1을 10개 사용하면, 다음 유효 수식 1회 동안 1이 11로 계산됩니다. |
| `UNIQUE_2_PROBABILITY_STRIKE` | 두 번째 일격 | 2 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `ProbabilityBonusDamage` | `PostCalculationBonus` | 0 | 공격 시 확률적으로 추가 데미지를 줍니다. 수식 안의 2가 많을수록 확률이 올라갑니다. |
| `UNIQUE_3_TRINITY` | 성부, 성자, 성령 | 3 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `TrinityThreeSixNine` | `PostCalculationBonus` | 0 | 한 수식에 3, 6, 9가 모두 있으면 추가 효과가 발동합니다. |
| `UNIQUE_4_ORDER_OF_OPERATIONS` | 사칙연산의 질서 | 4 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `BoardGenerationOverride` | `BoardGenerationOverride` | 0 | 보드의 숫자/연산자 배치 리듬이 더 뚜렷해집니다. |
| `UNIQUE_5_SHIELD_NUMBER` | 다섯 번째 방패 | 5 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `ShieldPerFiveInExpression` | `PostCalculationBonus` | 0 | 수식 안의 5 하나당 보호막을 얻습니다. |
| `UNIQUE_6_FLAT_WEALTH` | 여섯 번째 재화 | 6 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `FlatStageClearGoldBonus` | `RewardModifier` | 0 | 스테이지 클리어 골드가 고정량 증가합니다. |
| `UNIQUE_7_DAVID` | 다비드 | 7 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `ExactLengthSevenBonusDamage` | `PostCalculationBonus` | 0 | 길이가 정확히 7인 공격 수식이 추가 데미지를 줍니다. |
| `UNIQUE_8_PERCENT_WEALTH` | 여덟 번째 부 | 8 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `PercentStageClearGoldBonus` | `RewardModifier` | 0 | 스테이지 클리어 골드가 비율로 증가합니다. |
| `UNIQUE_9_ODINS_NINE_TRIALS` | 오딘의 9가지 시험 | 9 | `UniqueItem` | `Legendary` | `TEMP_UNIQUE_ITEM_PRICE` | 1 | `OdinBoardWideNineTransform` | `BoardStateTransformBeforeCalculation` | 0 | 9번째와 18번째 유효 턴에 보드의 낮은 숫자들이 9로 변합니다. |

---

## 9. UniqueItem Balance Config

All values below are temporary and must remain editable.

| Config Key | Temporary Value | Purpose |
|---|---:|---|
| `TEMP_UNIQUE_1_REQUIRED_ONE_COUNT` | 10 | Number of 1 tiles used before Unique 1 prepares its transform. |
| `TEMP_UNIQUE_2_BASE_CHANCE_PERCENT` | 50 | Base trigger chance for Unique 2. |
| `TEMP_UNIQUE_2_CHANCE_PER_TWO_PERCENT` | 10 | Additional trigger chance per 2 in expression. |
| `TEMP_UNIQUE_2_MAX_CHANCE_PERCENT` | 90 | Max trigger chance for Unique 2. |
| `TEMP_UNIQUE_2_BONUS_DAMAGE_PERCENT` | 50 | Bonus damage percent for Unique 2. |
| `TEMP_UNIQUE_3_ATTACK_BONUS_DAMAGE` | 30 | Bonus damage for Unique 3 in Attack Mode. |
| `TEMP_UNIQUE_3_ATTACK_SHIELD_BONUS` | 30 | Shield bonus for Unique 3 in Attack Mode. |
| `TEMP_UNIQUE_3_DEFENSE_SHIELD_BONUS` | 60 | Shield bonus for Unique 3 in Defense Mode. |
| `TEMP_UNIQUE_4_A_CELL_NUMBER_RATIO` | 90 | A Cell Number ratio for Unique 4. |
| `TEMP_UNIQUE_4_A_CELL_OPERATOR_RATIO` | 10 | A Cell Operator ratio for Unique 4. |
| `TEMP_UNIQUE_4_B_CELL_NUMBER_RATIO` | 10 | B Cell Number ratio for Unique 4. |
| `TEMP_UNIQUE_4_B_CELL_OPERATOR_RATIO` | 90 | B Cell Operator ratio for Unique 4. |
| `TEMP_UNIQUE_5_SHIELD_PER_FIVE` | 5 | Shield per 5 in expression. |
| `TEMP_UNIQUE_6_FLAT_GOLD_BONUS` | 50 | Flat gold bonus on stage clear. |
| `TEMP_UNIQUE_7_BONUS_DAMAGE_PERCENT` | 50 | Bonus damage percent for exact length 7 attack. |
| `TEMP_UNIQUE_8_GOLD_MULTIPLIER_PERCENT` | 140 | Stage clear gold multiplier for Unique 8. |
| `TEMP_UNIQUE_9_TRIGGER_TURNS` | 9, 18 | Valid turn counts that trigger Unique 9. |
| `TEMP_UNIQUE_9_MAX_TRANSFORM_VALUE` | 6 | Number tiles with value <= this become 9. |
| `TEMP_UNIQUE_9_TARGET_VALUE` | 9 | Target value for Unique 9 board transform. |

---

## 10. Unique 1: Awakened One

### 10.1 Concept

Unique 1 is a high-risk / high-return effect.

The player uses many 1 tiles to prepare one powerful expression where 1 becomes 11.

### 10.2 Runtime State

Track per stage:

```text
usedOneCountThisStage
oneTransformReady
```

At stage start:

```text
usedOneCountThisStage = 0
oneTransformReady = false
```

### 10.3 Counting Rule

After a valid expression is resolved, count the number of 1 tiles used by the final expression value.

If Unique 9 transformed a 1 tile into 9 before calculation, that tile does not count as a 1 for Unique 1.

If Unique 1 transformed a 1 into 11 for calculation, that tile does not count toward the next Unique 1 cycle.

### 10.4 Preparation Rule

If:

```text
usedOneCountThisStage >= TEMP_UNIQUE_1_REQUIRED_ONE_COUNT
```

then:

```text
oneTransformReady = true
usedOneCountThisStage = 0
```

The expression that reaches the required count does not receive the 1 -> 11 transform.

The transform applies to the next valid expression only.

### 10.5 Overflow Rule

Count overflow does not carry over.

Example:

```text
usedOneCountThisStage = 8
valid expression contains four 1 tiles
8 + 4 = 12
oneTransformReady = true
usedOneCountThisStage = 0
extra 2 count is discarded
```

### 10.6 Transform Rule

If `oneTransformReady` is true:

- the next valid expression consumes `oneTransformReady`,
- all 1 values in that expression are calculated as 11,
- the board tiles themselves are not permanently changed,
- after that valid expression resolves, `oneTransformReady = false`.

If the next valid expression contains no 1 tiles, `oneTransformReady` is still consumed.

Invalid input does not consume `oneTransformReady`.

### 10.7 Repeated Trigger Rule

Unique 1 may trigger multiple times within the same stage if the player uses enough 1 tiles again.

At stage end, reset:

```text
usedOneCountThisStage = 0
oneTransformReady = false
```

---

## 11. Unique 2: Probability Strike

### 11.1 Trigger

Unique 2 triggers only when all conditions are true:

- Attack Mode,
- base expression result > 0.

It does not trigger in Defense Mode.

It does not trigger if the base expression result is 0 or negative.

### 11.2 Chance

```text
triggerChancePercent = TEMP_UNIQUE_2_BASE_CHANCE_PERCENT
                     + numberOf2InExpression * TEMP_UNIQUE_2_CHANCE_PER_TWO_PERCENT
```

Clamp:

```text
triggerChancePercent = min(triggerChancePercent, TEMP_UNIQUE_2_MAX_CHANCE_PERCENT)
```

Default table:

| Number of 2 in expression | Trigger Chance |
|---:|---:|
| 0 | 50% |
| 1 | 60% |
| 2 | 70% |
| 3 | 80% |
| 4 or more | 90% |

### 11.3 Effect

If triggered:

```text
bonusDamage = ceil(baseDamage * TEMP_UNIQUE_2_BONUS_DAMAGE_PERCENT / 100)
```

Unique 2 bonus damage is separate bonus damage and is not multiplied by Attack Potion.

---

## 12. Unique 3: Trinity

### 12.1 Trigger

Unique 3 triggers if the valid expression contains all of these number values at least once:

```text
3, 6, 9
```

This check uses the final values that enter calculation after board-state transforms.

If Unique 9 transformed a 3 or 6 into 9 before calculation, those original values no longer count as 3 or 6 for the Trinity condition.

### 12.2 Attack Mode Effect

In Attack Mode:

```text
bonusDamage = TEMP_UNIQUE_3_ATTACK_BONUS_DAMAGE
shieldBonus = TEMP_UNIQUE_3_ATTACK_SHIELD_BONUS
```

Default:

```text
bonusDamage = 30
shieldBonus = 30
```

### 12.3 Defense Mode Effect

In Defense Mode:

```text
bonusDamage = 0
shieldBonus = TEMP_UNIQUE_3_DEFENSE_SHIELD_BONUS
```

Default:

```text
shieldBonus = 60
```

### 12.4 Negative Result Rule

Unique 3 may still grant its shield bonus if the expression condition is satisfied.

The base negative expression result still follows the core rule and heals the enemy.

---

## 13. Unique 4: Order of Operations

### 13.1 Core Compatibility Rule

Unique 4 is not a normal `BoardDeckUpgrade`.

Unique 4 must be implemented only as a `UniqueItem` effect with:

```text
effectType = BoardGenerationOverride
triggerTiming = BoardGenerationOverride
```

Normal `BoardDeckUpgrade` items must not modify A/B checkerboard category ratios.

Unique 4 is a UniqueItem-only exception allowed by the revised Main document.

### 13.2 Duration

After Unique 4 is obtained, its effect stays active until the current run ends.

Restart From Stage 1 or a new run resets this effect.

### 13.3 Layer 1 Override

When Unique 4 is active, use these Layer 1 category ratios:

| Cell Type | Number Ratio | Operator Ratio |
|---|---:|---:|
| A Cell | 90 | 10 |
| B Cell | 10 | 90 |

### 13.4 Layer 2 Preservation

Unique 4 modifies only Layer 1 Number/Operator category selection.

It does not directly modify actual number or operator spawn weights.

After Layer 1 category selection:

- if Number is selected, choose the actual number using current number spawn weights,
- if Operator is selected, choose the actual operator using current operator spawn weights.

Normal board deck items continue to modify Layer 2 as usual.

### 13.5 Forbidden Behavior

Unique 4 must not modify:

- board width,
- board height,
- board coordinate rules,
- adjacency rules,
- expression validity rules,
- tile removal rules,
- gravity/refill rules.

---

## 14. Unique 5: Shield Number

### 14.1 Trigger

Unique 5 triggers when a valid expression contains one or more number 5 values.

This check uses the final values that enter calculation after board-state transforms.

If Unique 9 transformed a 5 into 9 before calculation, that tile no longer counts as 5 for Unique 5.

### 14.2 Effect

```text
shieldBonus = numberOf5InExpression * TEMP_UNIQUE_5_SHIELD_PER_FIVE
```

Default:

```text
shieldBonus = numberOf5InExpression * 5
```

### 14.3 Mode Rule

Unique 5 applies in both:

- Attack Mode,
- Defense Mode.

### 14.4 Negative Result Rule

Unique 5 may still grant shield if the expression contains 5.

The base negative expression result still follows the core rule and heals the enemy.

---

## 15. Unique 6: Flat Wealth

### 15.1 Trigger

Unique 6 triggers during stage clear reward calculation.

### 15.2 Effect

```text
finalGold += TEMP_UNIQUE_6_FLAT_GOLD_BONUS
```

Default:

```text
finalGold += 50
```

### 15.3 Duration

After Unique 6 is obtained, it stays active until the current run ends.

### 15.4 No Retroactive Reward

Gold already received before acquiring Unique 6 is not changed.

---

## 16. Unique 7: David

### 16.1 Trigger

Unique 7 triggers only when all conditions are true:

- Attack Mode,
- expression length is exactly 7,
- base expression result > 0.

It does not trigger in Defense Mode.

It does not trigger if the base expression result is 0 or negative.

### 16.2 Effect

```text
bonusDamage = ceil(baseDamage * TEMP_UNIQUE_7_BONUS_DAMAGE_PERCENT / 100)
```

Default:

```text
bonusDamage = ceil(baseDamage * 50 / 100)
```

Unique 7 bonus damage is separate bonus damage and is not multiplied by Attack Potion.

---

## 17. Unique 8: Percent Wealth

### 17.1 Trigger

Unique 8 triggers during stage clear reward calculation.

### 17.2 Effect

Unique 8 multiplies base stage clear gold.

```text
multipliedGold = ceil(baseStageClearGold * TEMP_UNIQUE_8_GOLD_MULTIPLIER_PERCENT / 100)
```

Default:

```text
multipliedGold = ceil(baseStageClearGold * 140 / 100)
```

### 17.3 Duration

After Unique 8 is obtained, it stays active until the current run ends.

### 17.4 No Retroactive Reward

Gold already received before acquiring Unique 8 is not changed.

---

## 18. Unique 6 and Unique 8 Combined Gold Rule

If both Unique 6 and Unique 8 are owned:

1. Start with base stage clear gold.
2. Apply Unique 8 multiplier first.
3. Apply Unique 6 flat bonus second.

Formula:

```text
finalGold = ceil(baseStageClearGold * TEMP_UNIQUE_8_GOLD_MULTIPLIER_PERCENT / 100)
          + TEMP_UNIQUE_6_FLAT_GOLD_BONUS
```

Default:

```text
finalGold = ceil(baseStageClearGold * 1.4) + 50
```

Examples:

| Base Stage Clear Gold | Unique 8 Result | Unique 6 Bonus | Final Gold |
|---:|---:|---:|---:|
| 100 | 140 | +50 | 190 |
| 150 | 210 | +50 | 260 |
| 200 | 280 | +50 | 330 |

Do not add +50 before multiplying.

---

## 19. Unique 9: Odin's Nine Trials

### 19.1 Display Name

```text
오딘의 9가지 시험
```

### 19.2 Trigger

Unique 9 triggers on these valid turn counts per stage:

```text
9, 18
```

The old 27th-turn trigger is deprecated for MVP and must not be implemented.

At stage start:

```text
validTurnCountThisStage = 0
```

Each valid expression increases:

```text
validTurnCountThisStage += 1
```

Invalid expressions do not increase the count.

### 19.3 Board-Wide Transform Effect

When Unique 9 triggers:

- scan the entire current board,
- find all Number tiles with value <= 6,
- convert those Number tiles to value 9,
- do not change Operator tiles,
- do not change Number tiles 7, 8, or 9.

Default transform:

```text
if tile.type == Number and tile.value <= TEMP_UNIQUE_9_MAX_TRANSFORM_VALUE:
    tile.value = TEMP_UNIQUE_9_TARGET_VALUE
```

Default values:

```text
TEMP_UNIQUE_9_MAX_TRANSFORM_VALUE = 6
TEMP_UNIQUE_9_TARGET_VALUE = 9
```

### 19.4 Permanent Board State Rule

Unique 9 is an actual board-state transform.

Converted 9 tiles do not revert to their previous values.

Converted 9 tiles remain 9 until they are removed through normal tile removal.

Newly refilled tiles follow normal board generation rules and are not automatically converted unless another Unique 9 trigger occurs later.

### 19.5 Selected Expression Rule

Unique 9 triggers before calculating the selected expression on the 9th or 18th valid turn.

Therefore, the currently selected expression uses the transformed board values.

If a selected tile was 1, 2, 3, 4, 5, or 6 before Unique 9 triggers, it becomes 9 before calculation.

### 19.6 Tile Removal Rule

Converted 9 tiles follow normal core tile removal rules.

If a converted 9 tile is part of the selected valid expression, it is removed after calculation.

If a converted 9 tile is not part of the selected expression, it remains on the board as 9.

Do not change core tile removal rules.

---

## 20. Unique 1 and Unique 9 Conflict Rule

Unique 9 has final priority over Unique 1 on the same valid turn.

If Unique 1 is ready and Unique 9 triggers on the same valid turn:

1. Unique 9 transforms the whole board first.
2. Any board tile with original value 1 becomes 9.
3. The selected expression uses the transformed value 9.
4. Unique 1 oneTransformReady is consumed because this was a valid expression.
5. Unique 1 does not turn those tiles into 11.

The player is responsible for avoiding bad timing.

---

## 21. 0 / Negative / Defense Mode Summary

| UniqueItem | Result = 0 | Result < 0 | Defense Mode |
|---|---|---|---|
| Unique 1 | Transform may apply. Core result handling follows base calculation. | Transform may apply. Negative result heals enemy by core rule. | Applies. |
| Unique 2 | Does not trigger. | Does not trigger. | Does not trigger. |
| Unique 3 | Can trigger if 3/6/9 condition is met. | Can trigger if condition is met. Base negative result still heals enemy. | Grants shield 60. |
| Unique 4 | Not applicable. | Not applicable. | Not applicable. |
| Unique 5 | Can grant shield if expression contains 5. | Can grant shield if expression contains 5. Base negative result still heals enemy. | Applies. |
| Unique 6 | Not applicable. | Not applicable. | Not applicable. |
| Unique 7 | Does not trigger. | Does not trigger. | Does not trigger. |
| Unique 8 | Not applicable. | Not applicable. | Not applicable. |
| Unique 9 | Transform may apply. Core result handling follows transformed calculation. | Transform may apply. Negative result heals enemy by core rule. | Applies. |

---

## 22. Runtime State Requirements

The following UniqueItem-related state should be included in run state and retry snapshots when relevant:

- owned UniqueItem IDs,
- Unique 1 `usedOneCountThisStage`,
- Unique 1 `oneTransformReady`,
- Unique 4 active override state,
- current board tile values after Unique 9 transforms,
- `validTurnCountThisStage`,
- UniqueItem acquisition counts,
- generated shop Unique Item Slot state if retry can return to a generated shop.

Restart From Stage 1 resets all UniqueItem state.

Retry Current Stage should restore UniqueItem state from the stage-start snapshot according to the core retry rule.

---

## 23. Unknown Data Handling Rule

In development builds, the implementation must log a clear config error and disable the affected UniqueItem if any of the following are unknown or invalid:

- `uniqueItemId`,
- `targetNumber`,
- `itemCategory`,
- `rarity`,
- `priceConfigKey`,
- `maxAcquisitionsPerRun`,
- `effectType`,
- `triggerTiming`,
- `effectValueConfigKey`,
- transform config values,
- trigger turn values.

In release builds, the affected UniqueItem should fail safely:

- do not crash the run,
- do not apply partial or broken effects,
- remove or disable the affected UniqueItem from eligible pools,
- log a warning if release logging is available.

Do not silently ignore invalid UniqueItem data.

---

## 24. Implementation Checklist

Use this checklist before considering UniqueItem implementation complete.

- [ ] All 9 UniqueItem definitions are loaded from editable data.
- [ ] No final UniqueItem values are hard-coded in gameplay logic.
- [ ] Easy Mode excludes all UniqueItem systems.
- [ ] Normal/Hard start with starting UniqueItem candidate selection.
- [ ] Starting UniqueItem selection is free.
- [ ] Unselected starting candidates may appear later.
- [ ] Stage 3 / 6 / 9 shops use the Unique Item Slot from the Main document.
- [ ] Owned UniqueItems do not appear again in the current run.
- [ ] Displayed but unpurchased UniqueItems may appear again.
- [ ] Unique 1 counts used 1 values correctly.
- [ ] Unique 1 does not carry overflow count.
- [ ] Unique 1 consumes oneTransformReady on the next valid expression even if no 1 exists.
- [ ] Invalid input does not consume Unique 1 oneTransformReady.
- [ ] Unique 2 triggers only in positive Attack Mode results.
- [ ] Unique 2 probability is clamped to 90%.
- [ ] Unique 3 checks 3/6/9 condition using final calculation values.
- [ ] Unique 3 Attack Mode gives damage 30 and shield 30.
- [ ] Unique 3 Defense Mode gives shield 60 only.
- [ ] Unique 4 is implemented as UniqueItem-only BoardGenerationOverride.
- [ ] Unique 4 modifies Layer 1 category ratios only.
- [ ] Unique 4 preserves Layer 2 number/operator spawn weights.
- [ ] Unique 5 grants shield in both Attack and Defense Mode.
- [ ] Unique 6 adds flat +50 gold after other multiplier effects.
- [ ] Unique 7 triggers only in positive Attack Mode with exact expression length 7.
- [ ] Unique 8 multiplies base stage clear gold by 140%.
- [ ] Unique 6 and 8 combined order is multiplier first, flat bonus second.
- [ ] Unique 9 triggers only on valid turns 9 and 18.
- [ ] Unique 9 does not implement the old 27th-turn trigger.
- [ ] Unique 9 transforms the entire board before current expression calculation.
- [ ] Unique 9 converted 9 tiles remain 9 until removed.
- [ ] Unique 9 selected converted tiles are removed normally after use.
- [ ] Unique 9 has priority over Unique 1 on the same valid turn.
- [ ] Attack Potion multiplies only base damage, not UniqueItem bonus damage.
- [ ] UniqueItem-related state is included in stage retry snapshots.
- [ ] Unknown UniqueItem data logs clear config errors and disables affected items.

---

## 25. MVP Test Cases

### 25.1 Unique 1 Tests

| Situation | Expected Result |
|---|---|
| Stage starts | `usedOneCountThisStage = 0`, `oneTransformReady = false`. |
| Valid expression contains two 1 values | Count increases by 2. |
| Count reaches 10 | `oneTransformReady = true`, count resets to 0. |
| Count goes from 8 to 12 | `oneTransformReady = true`, count resets to 0, overflow 2 is discarded. |
| Trigger expression reaches 10 | That same expression does not use 1 -> 11. |
| Next valid expression contains 1 | 1 is calculated as 11, then ready state is consumed. |
| Next valid expression contains no 1 | Ready state is still consumed. |
| Invalid expression while ready | Ready state is not consumed. |

### 25.2 Unique 2 Tests

| Situation | Expected Result |
|---|---|
| Attack Mode result 100, no 2 in expression | 50% chance, bonus damage 50 if triggered. |
| Attack Mode result 100, two 2 values | 70% chance, bonus damage 50 if triggered. |
| Attack Mode result 0 | Does not trigger. |
| Attack Mode result -10 | Does not trigger. |
| Defense Mode result 100 | Does not trigger. |

### 25.3 Unique 3 Tests

| Situation | Expected Result |
|---|---|
| Expression contains 3, 6, 9 in Attack Mode | Bonus damage 30, shield 30. |
| Expression contains 3, 6, 9 in Defense Mode | Shield 60, no bonus damage. |
| Expression contains only 3 and 6 | Does not trigger. |
| Unique 9 transformed 3 and 6 into 9 before calculation | Original 3/6 no longer count for Trinity. |

### 25.4 Unique 4 Tests

| Situation | Expected Result |
|---|---|
| Unique 4 not owned | Use normal A Cell 60/40 and B Cell 40/60 category ratios. |
| Unique 4 owned | Use A Cell 90/10 and B Cell 10/90 category ratios. |
| Number category selected while Unique 4 owned | Actual number uses current number spawn weights. |
| Operator category selected while Unique 4 owned | Actual operator uses current operator spawn weights. |
| Normal BoardDeckUpgrade obtained while Unique 4 owned | It modifies Layer 2 only. |

### 25.5 Unique 5 Tests

| Situation | Expected Result |
|---|---|
| Attack expression contains one 5 | Gain shield 5. |
| Attack expression contains three 5 values | Gain shield 15. |
| Defense expression contains two 5 values | Gain shield 10 in addition to normal defense shield. |
| Unique 9 transformed 5 into 9 before calculation | That tile does not count as 5. |

### 25.6 Unique 6 and 8 Tests

| Situation | Expected Result |
|---|---|
| Only Unique 6 owned, base gold 100 | Final gold 150. |
| Only Unique 8 owned, base gold 100 | Final gold 140. |
| Both Unique 6 and 8 owned, base gold 100 | Final gold 190. |
| Both Unique 6 and 8 owned, base gold 150 | Final gold 260. |
| Gold was received before obtaining Unique 6 or 8 | Old gold is not modified. |

### 25.7 Unique 7 Tests

| Situation | Expected Result |
|---|---|
| Attack Mode, length exactly 7, base result 100 | Bonus damage 50. |
| Attack Mode, length 5, base result 100 | Does not trigger. |
| Attack Mode, length 7, base result 0 | Does not trigger. |
| Attack Mode, length 7, base result -10 | Does not trigger. |
| Defense Mode, length 7, base result 100 | Does not trigger. |

### 25.8 Unique 9 Tests

| Situation | Expected Result |
|---|---|
| Valid turn count becomes 9 | Transform all board number tiles <= 6 into 9 before calculation. |
| Valid turn count becomes 18 | Transform all board number tiles <= 6 into 9 before calculation. |
| Valid turn count becomes 27 | No MVP trigger. |
| Invalid expression | Does not increase valid turn count and does not trigger Unique 9. |
| Board tile value 1 | Becomes 9. |
| Board tile value 6 | Becomes 9. |
| Board tile value 7 | Stays 7. |
| Operator tile | Does not change. |
| Converted 9 is selected in current expression | Used as 9 and removed after calculation. |
| Converted 9 is not selected | Remains on board as 9. |
| New tile refilled later | Uses normal board generation rules. |

### 25.9 Unique 1 and 9 Conflict Tests

| Situation | Expected Result |
|---|---|
| Unique 1 ready and valid turn count becomes 9 | Unique 9 transforms board first. |
| Selected expression contains original 1 during Unique 9 trigger | Original 1 becomes 9, not 11. |
| Unique 1 ready consumed on that valid expression | `oneTransformReady = false` after resolution. |
| Player times Unique 1 on a non-9/18 valid turn | 1 becomes 11 normally for that valid expression. |

---

## 26. Remaining Intended Tuning Areas

The following values are intentionally editable and may be changed later without code changes:

- UniqueItem display names,
- UniqueItem short descriptions,
- UniqueItem prices,
- Unique 1 required count,
- Unique 2 probability and damage values,
- Unique 3 damage and shield values,
- Unique 4 A/B ratio values,
- Unique 5 shield value,
- Unique 6 gold bonus,
- Unique 7 bonus damage percent,
- Unique 8 gold multiplier,
- Unique 9 trigger turns,
- Unique 9 transform threshold and target value,
- UniqueItem VFX/SFX intensity.

End of file.
