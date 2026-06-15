# Mathcalibur Item Implementation Spec

> File name pattern: `Mathcalibur_Item_[date].md`  
> Document purpose: AI coding tool input / Unity item implementation guide  
> Target base document role: `Mathcalibur_Main_[date].md`  
> Document status: self-contained normal item implementation spec  
> Original spreadsheet requirement: do not require the Excel file during implementation  
> Scope: normal items only. This document does not define final UniqueItem behavior.

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
- UniqueItem slot structure,
- item loading interface,
- placeholder item effect dispatch interface,
- config and balance-data rules.

This document defines only the final normal item data and normal item effects for the current MVP item set.

If this document conflicts with `Mathcalibur_Main_[date].md` on core gameplay, shop structure, stage flow, difficulty behavior, UniqueItem slot behavior, retry/restart structure, or config priority, follow `Mathcalibur_Main_[date].md`.

If this document defines normal item data or normal item effect behavior, use this document instead of the dummy item data in the Main document.

Do not ask for the original Excel file.

Do not depend on the original Excel file.

All item data required for implementation is included in this document.

---

## 1. Expected Implementation State

Before implementing this item spec, the project should already have, or should first implement from the Main document:

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
- reroll locking and reroll cost increase,
- item data loading interface,
- item effect dispatch interface,
- board spawn weight modifier system,
- final weight clamp rule,
- cached final weight table rebuild.

If any of these systems are missing, implement the required item integration shell from the Main document before implementing this final item data.

---

## 2. Self-Contained Data Rule

This file replaces the source spreadsheet for implementation.

Implementation must use the item table in this document directly.

The design team may later convert this table into:

- ScriptableObject assets,
- JSON files,
- CSV data,
- spreadsheet-derived data tables,
- another editable data format.

However, final item balance must not be hidden inside gameplay logic.

---

## 3. Hard-Coding Ban

Do not hard-code item balance values inside gameplay logic.

The following must be editable config data:

- item prices,
- effect values,
- spawn weight modifier values,
- acquisition limits,
- ActiveItem stack limits,
- heal amount,
- attack multiplier percent,
- max HP increase,
- connection limit increase,
- item rarity,
- item unlock stage.

Every temporary value in this document uses the `TEMP_` prefix.

---

## 4. Item Category Mapping

Use only these item categories:

```text
ActiveItem
PassiveItem
BoardDeckUpgrade
ConnectionLimitUpgrade
UniqueItem
```

This document defines no `UniqueItem` effects.

The source spreadsheet used `소비형` for potion-like items. In implementation, those items must be `ActiveItem`.

Do not create a separate `ConsumableItem` category.

### 4.1 Category Conversion

| Source Type | Implementation Category |
|---|---|
| 수치형 | `BoardDeckUpgrade` |
| 소비형 | `ActiveItem` |
| 연결 확장형 | `ConnectionLimitUpgrade` |
| 패시브형 | `PassiveItem` |
| 고유형 | `UniqueItem` |

---

## 5. Rarity Mapping

Use only these rarity grades:

```text
Common
Rare
Legendary
```

Do not use `Epic`.

### 5.1 Grade Conversion

| Source Grade | Implementation Rarity |
|---|---|
| 3.일반 | `Common` |
| 2.희귀 | `Rare` |
| 1.전설 | `Legendary` |

---

## 6. Price Config

All prices must be editable.

| Config Key | Temporary Value | Used By |
|---|---:|---|
| `TEMP_ACTIVE_ITEM_PRICE` | 30 | ActiveItem |
| `TEMP_BOARD_DECK_UPGRADE_PRICE` | 50 | BoardDeckUpgrade |
| `TEMP_PASSIVE_ITEM_PRICE` | 50 | PassiveItem |
| `TEMP_CONNECTION_LIMIT_UP_PRICE` | 50 | ConnectionLimitUpgrade |

---

## 7. Spawn Weight Modifier Config

All modifier values below are temporary and must be editable.

The source expression `아주 조금` is normalized to `아주 미세하게` in this document.

### 7.1 Increase Modifiers

| Natural Language Grade | Config Key | Temporary Value |
|---|---|---:|
| 아주 미세하게 증가 | `TEMP_WEIGHT_MOD_TINY_UP` | 1 |
| 조금 증가 | `TEMP_WEIGHT_MOD_SMALL_UP` | 1 |
| 증가 | `TEMP_WEIGHT_MOD_NORMAL_UP` | 2 |
| 많이 증가 | `TEMP_WEIGHT_MOD_MANY_UP` | 4 |
| 크게 증가 | `TEMP_WEIGHT_MOD_LARGE_UP` | 6 |
| 극적으로 증가 | `TEMP_WEIGHT_MOD_EXTREME_UP` | 10 |

### 7.2 Decrease Modifiers

| Natural Language Grade | Config Key | Temporary Value |
|---|---|---:|
| 조금 내려감 | `TEMP_WEIGHT_MOD_SMALL_DOWN` | -2 |
| 내려감 | `TEMP_WEIGHT_MOD_NORMAL_DOWN` | -3 |
| 크게 감소 | `TEMP_WEIGHT_MOD_LARGE_DOWN` | -6 |
| 극적으로 내려감 | `TEMP_WEIGHT_MOD_EXTREME_DOWN` | -10 |

### 7.3 Ambiguous Phrase Mapping

Use these mapping rules when converting display/descriptive text into implementation data:

| Phrase | Implementation Mapping |
|---|---|
| 아주 조금 올라갑니다 | `TEMP_WEIGHT_MOD_TINY_UP` |
| 아주 미세하게 올라갑니다 | `TEMP_WEIGHT_MOD_TINY_UP` |
| 조금 올라갑니다 | `TEMP_WEIGHT_MOD_SMALL_UP` |
| 올라갑니다 | `TEMP_WEIGHT_MOD_NORMAL_UP` |
| 증가합니다 | `TEMP_WEIGHT_MOD_NORMAL_UP` |
| 더 증가합니다 | `TEMP_WEIGHT_MOD_MANY_UP` |
| 많이 증가합니다 | `TEMP_WEIGHT_MOD_MANY_UP` |
| 크게 증가합니다 | `TEMP_WEIGHT_MOD_LARGE_UP` |
| 극적으로 올라갑니다 | `TEMP_WEIGHT_MOD_EXTREME_UP` |
| 조금 낮아집니다 | `TEMP_WEIGHT_MOD_SMALL_DOWN` |
| 조금 내려갑니다 | `TEMP_WEIGHT_MOD_SMALL_DOWN` |
| 낮아집니다 | `TEMP_WEIGHT_MOD_NORMAL_DOWN` |
| 내려갑니다 | `TEMP_WEIGHT_MOD_NORMAL_DOWN` |
| 크게 내려갑니다 | `TEMP_WEIGHT_MOD_LARGE_DOWN` |
| 크게 감소합니다 | `TEMP_WEIGHT_MOD_LARGE_DOWN` |
| 극적으로 내려갑니다 | `TEMP_WEIGHT_MOD_EXTREME_DOWN` |

If an item's text and the table data disagree, follow the item table data in Section 14.

---

## 8. Additional Balance Config

| Config Key | Temporary Value | Purpose |
|---|---:|---|
| `TEMP_SMALL_HEAL_AMOUNT` | 20 | Healing Potion heal amount |
| `TEMP_NEXT_ATTACK_BOOST_MULTIPLIER_PERCENT` | 150 | Attack Potion damage multiplier percent. Display as 1.5x in UI text. |
| `TEMP_CONNECTION_LIMIT_INCREASE_VALUE` | 2 | Connection Limit Up increase value |
| `TEMP_MAX_HP_INCREASE` | 10 | Max HP Up increase value |
| `TEMP_DEFAULT_MAX_ACQUISITIONS_PER_RUN` | 999 | Default acquisition limit for repeatable items |
| `TEMP_DEFAULT_ACTIVE_ITEM_MAX_STACK` | 999 | Default inventory stack limit for ActiveItems |

---

## 9. Shop Pool Rules

Follow the shop rules from the Main document.

This item spec confirms the following item-pool rules for this item set.

### 9.1 Free Item Slots

Free item slots may contain any eligible non-Unique item:

```text
ActiveItem
PassiveItem
BoardDeckUpgrade
ConnectionLimitUpgrade
```

Free item slots must exclude:

```text
UniqueItem
```

Free item slots ignore rarity weights.

Free item slots use equal item-level probability among all eligible non-Unique items.

Free item slots must not duplicate each other within the same shop visit.

### 9.2 Paid Item Slots

Paid item slots may contain any eligible non-Unique item:

```text
ActiveItem
PassiveItem
BoardDeckUpgrade
ConnectionLimitUpgrade
```

Paid item slots must exclude:

```text
UniqueItem
```

Paid item slots use rarity-weight selection as defined by the Main document.

Paid item slots must not duplicate each other within the same shop visit.

### 9.3 Free and Paid Duplicate Rule

The same item may appear once in a free slot and once in a paid slot during the same shop visit.

This is allowed because free selection and paid purchase are separate opportunity types.

---

## 10. Unlock Stage Rule

Items that modify `x` or `÷` must unlock at Stage 3.

Before Stage 3:

- `x` final spawn weight is treated as 0,
- `÷` final spawn weight is treated as 0,
- items modifying `x` or `÷` must not appear in the eligible normal item pool.

Items that modify only numbers, `+`, `-`, HP, ActiveItem behavior, or connection limit may unlock at Stage 1.

### 10.1 Normal Item Unlock Stage Check

For normal item unlock checks in the shop, use the upcoming stage number, not the cleared stage number.

```text
upcomingStageNumber = clearedStageNumber + 1
```

Example:

- After clearing Stage 1, the shop prepares Stage 2.
- After clearing Stage 2, the shop prepares Stage 3.
- Therefore, items with `unlockStage = 3` may appear in the Stage 2 clear shop.

Unique Shop timing is different and must still follow the Main document:

```text
Unique Shop = shop opened after clearing Stage 3, Stage 6, or Stage 9
```

Do not use upcoming stage number to trigger Unique Shop timing.

---

## 11. BoardDeckUpgrade Effect Rule

`BoardDeckUpgrade` items modify actual number/operator spawn weights only.

They must not modify:

- board width,
- board height,
- A/B checkerboard category weights,
- A/B coordinate pattern,
- core expression validity rules.

### 11.1 Effect Type

Use this effect type for board deck modifiers:

```text
ModifySpawnWeights
```

Recommended payload shape:

```json
{
  "modifiers": [
    { "targetType": "Number", "targetValue": "1", "modifierConfigKey": "TEMP_WEIGHT_MOD_SMALL_DOWN" },
    { "targetType": "Operator", "targetValue": "+", "modifierConfigKey": "TEMP_WEIGHT_MOD_SMALL_UP" }
  ]
}
```

### 11.2 Stacking

Board deck effects stack across the current run unless the item's `maxAcquisitionsPerRun` prevents further acquisition.

When a new run starts, all board deck modifiers reset.

### 11.3 Final Weight Clamp

Final spawn weights must never be negative.

If stacking or data produces a negative final weight, clamp it to 0:

```text
finalWeight = max(0, calculatedWeight)
```

### 11.4 Cached Weight Rebuild

When an item changes spawn weights:

1. Apply the item modifier to the current run's board deck state.
2. Rebuild cached final spawn weight tables.
3. Apply operator lock rules.
4. Clamp negative final weights.
5. Use the cached final weights during board generation.

Do not recalculate all modifiers for every tile spawn.

---

## 12. ActiveItem Rule

`ActiveItem` means a usable item stored in the player's current-run inventory.

### 12.1 Inventory Rule

When the player obtains an ActiveItem:

```text
activeItemInventory[itemId] += 1
```

ActiveItems may stack.

Default stack limit:

```text
TEMP_DEFAULT_ACTIVE_ITEM_MAX_STACK = 999
```

The design team may later set a different per-item stack limit.

### 12.2 Use Timing

ActiveItems are used manually by the player during stage combat.

ActiveItem use is not a valid expression action.

ActiveItem use does not:

- consume a turn,
- decrease enemy attack countdown,
- remove tiles,
- trigger gravity,
- refill the board,
- consume the stage turn limit.

### 12.2.1 ActiveItem Use Window

ActiveItems may be used only during the player's stage-combat input-waiting state.

ActiveItems must not be usable during:

- tile drag selection,
- expression preview update,
- expression validation,
- combat result resolution,
- tile removal,
- gravity and refill,
- valid path check,
- enemy attack resolution,
- shop screen,
- retry / restart / title / result UI.

If the player is not in the input-waiting state, the ActiveItem button must be disabled or reject input without consuming the item.

### 12.3 Healing Potion

Effect type:

```text
HealPlayer
```

When used:

```text
currentHP = min(maxHP, currentHP + TEMP_SMALL_HEAL_AMOUNT)
activeItemInventory[ITEM_HEALING_POTION] -= 1
```

The Healing Potion can be used even if current HP is already full.

If current HP is already full:

```text
actualHealAmount = 0
item is still consumed
```

### 12.4 Attack Potion

Effect type:

```text
SetNextAttackMultiplier
```

Internal config uses integer percent, not floating-point multiplier:

```text
TEMP_NEXT_ATTACK_BOOST_MULTIPLIER_PERCENT = 150
```

The UI may display this as:

```text
1.5x
```

When used while no pending attack multiplier is active:

```text
pendingNextAttackMultiplierPercent = TEMP_NEXT_ATTACK_BOOST_MULTIPLIER_PERCENT
activeItemInventory[ITEM_ATTACK_POTION] -= 1
```

The multiplier applies only to the next valid Attack Mode expression.

It does not apply to Defense Mode.

### 12.4.1 Attack Potion Duplicate Use Rule

If `pendingNextAttackMultiplierPercent` is already active, Attack Potion cannot be used again.

When a pending attack multiplier already exists:

- disable the Attack Potion button, or
- show clear feedback and reject the input,
- do not consume another Attack Potion,
- do not overwrite the existing pending multiplier,
- do not stack multipliers.

Attack Potion effects do not stack.

### 12.4.2 Attack Potion Resolution Rule

If the next valid Attack Mode expression result is positive:

```text
damage = ceil(expressionResult * pendingNextAttackMultiplierPercent / 100)
consume pendingNextAttackMultiplierPercent
```

If the next valid Attack Mode expression result is 0:

```text
damage = 0
consume pendingNextAttackMultiplierPercent
```

If the next valid Attack Mode expression result is negative:

```text
enemy heals by abs(expressionResult)
do not multiply the enemy healing amount
consume pendingNextAttackMultiplierPercent
```

The Attack Potion inventory item is consumed when the potion is used, not when the boosted expression resolves.

The pending multiplier is consumed even if the boosted expression result is 0 or negative.


---

## 13. Passive and Connection Effects

### 13.1 Connection Limit Up

Effect type:

```text
IncreaseConnectionLimit
```

When obtained:

```text
currentMaxConnectionLength += TEMP_CONNECTION_LIMIT_INCREASE_VALUE
```

Default acquisition limit:

```text
maxAcquisitionsPerRun = 2
```

Expected default flow:

```text
5 -> 7 -> 9
```

### 13.2 Max HP Up

Effect type:

```text
IncreaseMaxHpAndCurrentHp
```

When obtained:

```text
playerMaxHP += TEMP_MAX_HP_INCREASE
currentHP = min(playerMaxHP, currentHP + TEMP_MAX_HP_INCREASE)
```

Default acquisition limit:

```text
maxAcquisitionsPerRun = 3
```

---

## 14. Final Item Data Table

This table is the authoritative item data for this normal item implementation spec.

Expanded data note:

- The source spreadsheet had one row named `n의 보강`.
- This implementation expands it into three independent item data rows:
  - `3의 보강`
  - `4의 보강`
  - `5의 보강`
- These three items share the same implementation logic.
- The item data count in this table is therefore 33 items.

### 14.1 effectPayload Authoring Shorthand Rule

The compact `effectPayload` text in this table is authoring shorthand.

Implementation must convert it into the structured modifier payload shape defined in Section 11.1 before gameplay logic uses it.

Do not parse `effectPayload` as UI text.

Do not depend on `uiDescriptionKo` for gameplay logic.

If `effectPayload`, `uiDescriptionKo`, and `implementationNote` disagree, follow the implementation data meaning of `effectPayload`, after converting it into structured data.

Examples:

```text
number 1: TEMP_WEIGHT_MOD_SMALL_DOWN
```

means:

```json
{
  "modifiers": [
    { "targetType": "Number", "targetValue": "1", "modifierConfigKey": "TEMP_WEIGHT_MOD_SMALL_DOWN" }
  ]
}
```

```text
operators x,÷: TEMP_WEIGHT_MOD_NORMAL_UP; operator +: TEMP_WEIGHT_MOD_NORMAL_DOWN
```

means:

```json
{
  "modifiers": [
    { "targetType": "Operator", "targetValue": "x", "modifierConfigKey": "TEMP_WEIGHT_MOD_NORMAL_UP" },
    { "targetType": "Operator", "targetValue": "÷", "modifierConfigKey": "TEMP_WEIGHT_MOD_NORMAL_UP" },
    { "targetType": "Operator", "targetValue": "+", "modifierConfigKey": "TEMP_WEIGHT_MOD_NORMAL_DOWN" }
  ]
}
```

| itemId | displayName | itemCategory | rarity | priceConfigKey | maxAcquisitionsPerRun | effectType | effectPayload | unlockStage | uiDescriptionKo | implementationNote |
|---|---|---|---|---|---:|---|---|---:|---|---|
| `ITEM_SMALL_CLEANUP` | 작은 정리 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 1: TEMP_WEIGHT_MOD_SMALL_DOWN | 1 | 1의 등장 확률이 조금 낮아집니다. | Slightly raises the board's average number value by reducing 1. |
| `ITEM_LOW_TWO_RESTRAINT` | 낮은 수 절제 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 2: TEMP_WEIGHT_MOD_SMALL_DOWN | 1 | 2의 등장 확률이 조금 낮아집니다. | Reduces 2 slightly. May reduce some low-number stability. |
| `ITEM_MID_NUMBER_PREFERENCE` | 중간 수 선호 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 4,5,6: TEMP_WEIGHT_MOD_NORMAL_UP | 1 | 4, 5, 6의 등장 확률이 올라갑니다. | Stable mid-number reinforcement. |
| `ITEM_BASIC_REINFORCEMENT` | 기초 보강 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 2,3,4: TEMP_WEIGHT_MOD_SMALL_UP | 1 | 2, 3, 4의 등장 확률이 조금 올라갑니다. | Early board becomes more stable. |
| `ITEM_ADDITION_MASTERY` | 더하기 숙련 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operator +: TEMP_WEIGHT_MOD_SMALL_UP | 1 | +의 등장 확률이 조금 올라갑니다. | More stable expressions, but lower damage ceiling. |
| `ITEM_SUBTRACTION_RESTRAINT` | 빼기 절제 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operator -: TEMP_WEIGHT_MOD_SMALL_DOWN | 1 | -의 등장 확률이 조금 낮아집니다. | Reduces negative-result risk. |
| `ITEM_STABLE_OPERATIONS` | 안정된 연산 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operators +,-: TEMP_WEIGHT_MOD_MANY_UP | 1 | +와 -의 등장 비율이 더 증가합니다. | The phrase 더 증가 is mapped to TEMP_WEIGHT_MOD_MANY_UP. |
| `ITEM_EVEN_REINFORCEMENT` | 짝수 보강 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 2,4,6,8: TEMP_WEIGHT_MOD_TINY_UP | 1 | 짝수 숫자의 등장 확률이 아주 미세하게 올라갑니다. | Original phrase 아주 조금 is normalized to 아주 미세하게. |
| `ITEM_ODD_REINFORCEMENT` | 홀수 보강 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 1,3,5,7: TEMP_WEIGHT_MOD_TINY_UP | 1 | 홀수 숫자의 등장 확률이 아주 미세하게 올라갑니다. | Original phrase 아주 조금 is normalized to 아주 미세하게. |
| `ITEM_STABLE_MULTIPLICATION` | 안정된 곱셈 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operator x: TEMP_WEIGHT_MOD_TINY_UP; numbers 2,3: TEMP_WEIGHT_MOD_TINY_UP | 3 | x와 낮은 곱셈용 숫자의 등장 확률이 아주 미세하게 올라갑니다. | Locked until Stage 3 because it modifies x. |
| `ITEM_REINFORCE_3` | 3의 보강 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 3: TEMP_WEIGHT_MOD_SMALL_UP | 1 | 3의 등장 확률이 조금 올라갑니다. | One of the three expanded n reinforcement items. Same implementation logic as 4 and 5. |
| `ITEM_REINFORCE_4` | 4의 보강 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 4: TEMP_WEIGHT_MOD_SMALL_UP | 1 | 4의 등장 확률이 조금 올라갑니다. | One of the three expanded n reinforcement items. Same implementation logic as 3 and 5. |
| `ITEM_REINFORCE_5` | 5의 보강 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 5: TEMP_WEIGHT_MOD_SMALL_UP | 1 | 5의 등장 확률이 조금 올라갑니다. | One of the three expanded n reinforcement items. Same implementation logic as 3 and 4. |
| `ITEM_SMALL_LUCK` | 작은 행운 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operator x: TEMP_WEIGHT_MOD_TINY_UP | 3 | x의 등장 확률이 아주 미세하게 올라갑니다. | Locked until Stage 3 because it modifies x. |
| `ITEM_BEGINNER_PURIFICATION` | 초급 정화 | `BoardDeckUpgrade` | `Common` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 1: TEMP_WEIGHT_MOD_SMALL_DOWN; operators -,÷: TEMP_WEIGHT_MOD_SMALL_DOWN | 3 | 1, -, ÷의 등장 확률이 조금 내려갑니다. | Locked until Stage 3 because it modifies ÷. If too strong, design may later move this to Rare. |
| `ITEM_HEALING_POTION` | 힐링 포션 | `ActiveItem` | `Common` | `TEMP_ACTIVE_ITEM_PRICE` | 999 | `HealPlayer` | healAmount: TEMP_SMALL_HEAL_AMOUNT | 1 | 전투 중 사용 시 현재 체력을 20 회복합니다. 현재 체력은 최대 체력을 초과할 수 없습니다. | Stored in inventory. Manual use. Consumed even if current HP is already full. |
| `ITEM_SEVEN_AURA` | 7의 기운 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 7: TEMP_WEIGHT_MOD_NORMAL_UP | 1 | 7의 등장 확률이 올라갑니다. | Simple high-number reinforcement. |
| `ITEM_GREEDY_MULTIPLICATION` | 탐욕의 곱셈 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operator x: TEMP_WEIGHT_MOD_NORMAL_UP; number 1: TEMP_WEIGHT_MOD_NORMAL_UP | 3 | x의 등장 확률이 올라가지만, 1의 등장 확률도 올라갑니다. | High-risk high-return. Locked until Stage 3 because it modifies x. |
| `ITEM_INCOMPLETE_CROWN` | 미완의 왕관 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 9: TEMP_WEIGHT_MOD_NORMAL_UP; operator ÷: TEMP_WEIGHT_MOD_NORMAL_UP | 3 | 9의 등장 확률이 올라가지만, ÷의 등장 확률도 올라갑니다. | High number and division risk increase together. Locked until Stage 3. |
| `ITEM_HIGH_NUMBER_LURE` | 높은 수 유도 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 7,8: TEMP_WEIGHT_MOD_NORMAL_UP; number 1: TEMP_WEIGHT_MOD_SMALL_UP | 1 | 7, 8의 등장 확률이 올라가지만, 1의 등장 확률도 조금 올라갑니다. | Mid-high number reinforcement with small low-number risk. |
| `ITEM_UNSTABLE_HIGH_NUMBERS` | 불안정한 고수 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 8,9: TEMP_WEIGHT_MOD_NORMAL_UP; operator ÷: TEMP_WEIGHT_MOD_NORMAL_UP | 3 | 8, 9의 등장 확률이 올라가지만, ÷의 등장 확률도 올라갑니다. | High damage potential and calculation instability. Locked until Stage 3. |
| `ITEM_STRONG_RUNE` | 강한 룬 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operator x: TEMP_WEIGHT_MOD_NORMAL_UP; operator -: TEMP_WEIGHT_MOD_SMALL_UP | 3 | x의 등장 확률이 올라가지만, -의 등장 확률도 조금 올라갑니다. | Multiplication boost with small negative-result risk. Locked until Stage 3. |
| `ITEM_DANGEROUS_CALCULATION` | 위험한 계산 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operators x,÷: TEMP_WEIGHT_MOD_NORMAL_UP; operator +: TEMP_WEIGHT_MOD_NORMAL_DOWN | 3 | x와 ÷의 등장 확률이 올라가지만, +의 등장 확률이 낮아집니다. | Higher ceiling, lower stability. Locked until Stage 3. |
| `ITEM_LOW_NUMBER_DELETE` | 낮은 수 삭제 | `BoardDeckUpgrade` | `Rare` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 1,2,3: TEMP_WEIGHT_MOD_NORMAL_DOWN | 1 | 1, 2, 3의 등장 확률이 낮아집니다. | Raises board average but can reduce early connection stability. |
| `ITEM_CONNECTION_LIMIT_UP` | 연결 수 증가 | `ConnectionLimitUpgrade` | `Rare` | `TEMP_CONNECTION_LIMIT_UP_PRICE` | 2 | `IncreaseConnectionLimit` | increaseValue: TEMP_CONNECTION_LIMIT_INCREASE_VALUE | 1 | 현재 Run의 최대 연결 가능 타일 수가 2 증가합니다. | Default flow is 5 to 7 to 9. Maximum 2 acquisitions per run. |
| `ITEM_ATTACK_POTION` | 공격 포션 | `ActiveItem` | `Rare` | `TEMP_ACTIVE_ITEM_PRICE` | 999 | `SetNextAttackMultiplier` | multiplierPercent: TEMP_NEXT_ATTACK_BOOST_MULTIPLIER_PERCENT | 1 | 다음 Attack Mode 유효 수식 1회 데미지가 1.5배가 됩니다. 결과가 0 이하라도 pending 효과는 소모됩니다. | Stored in inventory. Manual use. Disabled while another Attack Potion effect is pending. Does not apply to Defense Mode. Does not multiply enemy healing from negative results. |
| `ITEM_MAX_HP_UP` | 최대 체력 증가 | `PassiveItem` | `Rare` | `TEMP_PASSIVE_ITEM_PRICE` | 3 | `IncreaseMaxHpAndCurrentHp` | increaseValue: TEMP_MAX_HP_INCREASE | 1 | 최대 체력이 10 증가합니다. 획득 시 현재 체력도 10 증가합니다. | Immediate survivability increase. Maximum 3 acquisitions per run. |
| `ITEM_NINE_BLESSING` | 9의 축복 | `BoardDeckUpgrade` | `Legendary` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 9: TEMP_WEIGHT_MOD_NORMAL_UP | 1 | 9의 등장 확률이 올라갑니다. | Legendary presentation, simple high-ceiling reinforcement. Value remains config-driven. |
| `ITEM_KINGS_NUMBERS` | 왕의 숫자 | `BoardDeckUpgrade` | `Legendary` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | numbers 7,8,9: TEMP_WEIGHT_MOD_NORMAL_UP | 1 | 7, 8, 9의 등장 확률이 모두 올라갑니다. | High-number build reinforcement. |
| `ITEM_RUNE_PURIFICATION` | 룬 정화 | `BoardDeckUpgrade` | `Legendary` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operators -,÷: TEMP_WEIGHT_MOD_NORMAL_DOWN | 3 | -와 ÷의 등장 확률이 낮아집니다. | Reduces risky operators. Locked until Stage 3 because it modifies ÷. |
| `ITEM_COMPLETE_SWORD_FORM` | 완성된 검식 | `BoardDeckUpgrade` | `Legendary` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | operators +,x: TEMP_WEIGHT_MOD_NORMAL_UP | 3 | +와 x의 등장 확률이 올라갑니다. | Stability and damage ceiling together. Locked until Stage 3 because it modifies x. |
| `ITEM_ADVANCED_PURIFICATION` | 고급 정화 | `BoardDeckUpgrade` | `Legendary` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 1: TEMP_WEIGHT_MOD_LARGE_DOWN; operators -,÷: TEMP_WEIGHT_MOD_LARGE_DOWN | 3 | 1, -, ÷의 등장 확률이 크게 내려갑니다. | Strongly removes weak/risky elements. Locked until Stage 3 because it modifies ÷. |
| `ITEM_UNKNOWN_ONE_SURGE` | '???' | `BoardDeckUpgrade` | `Legendary` | `TEMP_BOARD_DECK_UPGRADE_PRICE` | 999 | `ModifySpawnWeights` | number 1: TEMP_WEIGHT_MOD_EXTREME_UP | 1 | 1의 등장 확률이 극적으로 올라갑니다. | The hidden display name is intentional. Strong synergy with the future 1 UniqueItem system. |

---

## 15. EffectType Reference

| effectType | Required Behavior |
|---|---|
| `ModifySpawnWeights` | Apply one or more number/operator spawn weight modifiers to the current run's board deck state. |
| `HealPlayer` | Restore player HP by a config value, capped by max HP, then consume the ActiveItem. |
| `SetNextAttackMultiplier` | Store a pending multiplier for the next valid Attack Mode expression, then consume the ActiveItem. |
| `IncreaseConnectionLimit` | Increase current run max connection length by config value. |
| `IncreaseMaxHpAndCurrentHp` | Increase player max HP and current HP by config value. |

If an unknown `effectType` is encountered in development builds, log a clear config error.

Do not silently ignore unknown effect types.

---

## 16. Item Eligibility Rules

An item is eligible for a shop slot only if all conditions are true:

1. `shopTargetStageNumber`, calculated as `clearedStageNumber + 1`, is greater than or equal to `unlockStage`.
2. The item has not reached `maxAcquisitionsPerRun`.
3. The item's operator targets are not locked.
4. The item category is valid for the target slot type.
5. The item is not a UniqueItem for normal free/paid slots.

### 16.1 Operator Target Lock Check

If an item targets `x` or `÷`, it must not appear before Stage 3.

This applies even if its `unlockStage` is incorrectly configured.

Operator unlock state has higher priority than item data mistakes.

---

## 17. Acquisition Count Rule

Track acquisition counts per item ID.

```text
acquisitionCounts[itemId] += 1
```

The count increases only when the player actually obtains the item.

Displayed but unselected free items do not increase the count.

Displayed but unpurchased paid items do not increase the count.

When a new run starts, reset all item acquisition counts.

If an item reaches `maxAcquisitionsPerRun`, remove only that item from the eligible pool.

Do not block other items of the same rarity.

---

## 18. Retry and Restart State

The following item-related state must be included in the stage-start snapshot:

- active item inventory counts,
- board deck modifiers,
- connection limit,
- passive effects,
- player max HP,
- current HP,
- pending next attack multiplier,
- acquisition counts,
- current shop state if retry can return to a generated shop.

Restart From Stage 1 resets all run-based item state.

---

## 19. Display Text Rule

`displayName` may use Korean names.

`itemId` must remain stable English uppercase snake case.

The hidden item named `'???'` intentionally keeps its hidden display name.

Do not rename `'???'` unless the design team explicitly changes the item name later.

---

## 20. Implementation Checklist

Use this checklist before considering item implementation complete.

- [ ] All 33 item definitions are loaded from editable data.
- [ ] No final item values are hard-coded in gameplay logic.
- [ ] ActiveItems are stored in inventory.
- [ ] ActiveItem use does not consume a valid turn.
- [ ] Healing Potion can be used at full HP and is consumed.
- [ ] Attack Potion affects only the next valid Attack Mode expression.
- [ ] Attack Potion is consumed on 0 or negative result.
- [ ] Negative result healing is not multiplied by Attack Potion.
- [ ] BoardDeckUpgrade effects modify only Layer 2 number/operator weights.
- [ ] Items that modify `x` or `÷` do not appear before Stage 3.
- [ ] `n의 보강` has been implemented as three data items with one shared effect logic.
- [ ] Connection Limit Up can be acquired at most 2 times per run.
- [ ] Max HP Up can be acquired at most 3 times per run.
- [ ] Free slots can show all eligible non-Unique items and ignore rarity weights.
- [ ] Paid slots can show all eligible non-Unique items and use rarity weights.
- [ ] Acquisition limits are tracked per item, not per rarity.
- [ ] New run resets acquisition counts and run-based item state.
- [ ] Normal item unlock checks use upcoming stage number.
- [ ] `effectPayload` shorthand is converted into structured runtime data.
- [ ] ActiveItems can be used only during player input-waiting state.
- [ ] Attack Potion uses percent config internally.
- [ ] Attack Potion button is disabled while a pending attack multiplier exists.
- [ ] Visible duplicate item slots are re-checked after acquisition.
- [ ] ActiveItems at stack limit are removed from eligible pools.
- [ ] Operator symbols are canonicalized to `+`, `-`, `x`, `÷`.
- [ ] Unknown item data logs clear config errors and disables affected items.
- [ ] Unknown effect types log clear config errors.

---

## 21. Implementation Edge Case Rules

These rules exist to reduce implementation ambiguity for normal item behavior.

If these rules conflict with the Main document on core shop structure, stage flow, retry structure, or UniqueItem slot behavior, follow the Main document.

### 21.1 Insufficient Shop Pool Rule

If the Main document already defines empty-pool or insufficient-pool behavior for a shop slot, follow the Main document.

For any normal item slot case not explicitly covered by the Main document:

1. Do not duplicate items within the same slot group.
2. Fill available slots with valid eligible items first.
3. Display a locked placeholder for remaining unfillable slots.
4. Log a clear config warning in development builds.

Do not silently create duplicate items to fill empty space.

Do not reduce the configured slot count at runtime unless the Main document explicitly allows it.

### 21.2 Free/Paid Duplicate After Acquisition Rule

The same item may appear once in a free slot and once in a paid slot during the same shop visit.

After the player obtains one visible copy of an item, immediately re-check that item against:

- `maxAcquisitionsPerRun`,
- ActiveItem stack limit,
- unlock state,
- operator lock state,
- any other eligibility condition.

If another visible copy of the same item is no longer eligible, disable that remaining visible copy.

The disabled copy must not be selectable or purchasable.

If the item is still eligible after acquisition, the remaining visible copy may stay selectable or purchasable according to the normal shop rules.

### 21.3 Immediate Item Effect Application Rule

Item acquisition and item effect application happen immediately when the player selects or purchases the item.

Recommended acquisition flow:

```text
1. Validate slot and item eligibility.
2. If paid item, validate and subtract gold.
3. Grant the item.
4. Increase acquisitionCounts[itemId].
5. Apply the item's immediate effect or inventory change.
6. Re-check visible duplicate eligibility.
7. Lock reroll for this shop visit.
8. Update shop UI.
```

By category:

| itemCategory | Immediate Result |
|---|---|
| `ActiveItem` | Add to current-run ActiveItem inventory. |
| `BoardDeckUpgrade` | Apply modifier to current-run board deck state and rebuild cached final weight tables before the next board generation. |
| `ConnectionLimitUpgrade` | Increase current-run max connection length immediately. |
| `PassiveItem` | Apply passive effect to current-run state immediately. |
| `UniqueItem` | Not defined by this document. Follow the Main document and external UniqueItem spec. |

Do not delay normal item effects until the next stage unless a specific item explicitly says so.

### 21.4 Retry Snapshot Interpretation Rule

Retry Current Stage restores the failed stage to the saved stage-start snapshot.

The player does not return to the previous shop selection state unless the Main document's retry implementation explicitly stores and restores that shop state.

Normal interpretation:

```text
Shop choices are finalized before the next stage starts.
Retry restores the stage-start state after those finalized choices have already been applied.
```

### 21.5 ActiveItem Stack Limit Eligibility Rule

If an ActiveItem has reached its current stack limit, that item is not eligible for new free or paid shop slots.

Eligibility check:

```text
activeItemInventory[itemId] < activeItemMaxStack[itemId]
```

If a visible ActiveItem reaches its stack limit during the same shop visit, re-check and disable any remaining visible copies of that same item.

### 21.6 Operator Canonicalization Rule

For implementation, operator values must use the following canonical values:

| Meaning | Canonical Value |
|---|---|
| Addition | `+` |
| Subtraction | `-` |
| Multiplication | `x` |
| Division | `÷` |

Authoring text may contain these alternate symbols:

| Authoring Symbol | Convert To |
|---|---|
| `*` | `x` |
| `×` | `x` |
| `/` | `÷` |

Before loading item data into gameplay logic:

- `*` must be converted to `x`,
- `×` must be converted to `x`,
- `/` must be converted to `÷`.

Gameplay logic, item effect payloads, spawn weight tables, and operator lock checks must use only the canonical values:

```text
+, -, x, ÷
```

### 21.7 Unknown Data Handling Rule

In development builds, the implementation must log a clear config error and disable the affected item if any of the following are unknown or invalid:

- `itemId`,
- `itemCategory`,
- `rarity`,
- `priceConfigKey`,
- `effectType`,
- `effectPayload`,
- `modifierConfigKey`,
- `targetType`,
- `targetValue`,
- `unlockStage`.

In release builds, the affected item should fail safely:

- do not crash the run,
- do not apply a partial or broken effect,
- do not silently treat the item as valid,
- remove or disable the affected item from eligible pools,
- log a warning if release logging is available.

Unknown data must not be silently ignored.

### 21.8 Attack Potion Percent Calculation Rule

Attack Potion multiplier config is stored as integer percent:

```text
TEMP_NEXT_ATTACK_BOOST_MULTIPLIER_PERCENT = 150
```

Use integer or decimal-safe calculation:

```text
damage = ceil(expressionResult * TEMP_NEXT_ATTACK_BOOST_MULTIPLIER_PERCENT / 100)
```

Do not rely on binary floating-point multiplication for final damage if avoidable.

The UI may still display this as `1.5x`.

### 21.9 Attack Potion Button Disable Rule

If `pendingNextAttackMultiplierPercent` is active, the Attack Potion button must be disabled or must reject input without consumption.

Required behavior:

- no additional Attack Potion is consumed,
- existing pending multiplier remains unchanged,
- no multiplier stacking occurs,
- no multiplier overwriting occurs.

### 21.10 effectPayload Conversion Priority Rule

The compact `effectPayload` values in Section 14 are easier for the design team to edit.

They are not the final runtime data model.

Runtime data must use structured effect data.

For `ModifySpawnWeights`, runtime data must be equivalent to:

```json
{
  "modifiers": [
    { "targetType": "Number", "targetValue": "1", "modifierConfigKey": "TEMP_WEIGHT_MOD_SMALL_DOWN" }
  ]
}
```

If the implementation uses ScriptableObject, JSON, CSV, or another data format, preserve this structured meaning.

---

## 22. Remaining Intended Tuning Areas

The following are intentionally editable and may be changed later without code changes:

- all spawn weight modifier config values,
- all item prices,
- all item rarities,
- all `maxAcquisitionsPerRun` values,
- ActiveItem max stack limits,
- heal amount,
- attack multiplier percent,
- connection limit increase value,
- max HP increase value,
- item unlock stages,
- UI text.

End of file.
