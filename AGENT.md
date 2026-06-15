# AGENT.md

## Purpose

This repository already contains authoritative Mathcalibur design documents.

This file does not define gameplay rules.

This file only defines how AI coding agents must follow the existing documents.

The objective is document compliance, not reinterpretation.

---

## Required Reading Order

Before any implementation, modification, refactor, review, or audit:

1. Read AGENT.md
2. Read Mathcalibur_Main_2026.05.27.md
3. Read Mathcalibur_Item_2026.05.27.md
4. Read Mathcalibur_UniqueItem_2026.05.27.md

Implementation without reading these documents is not allowed.

---

## Authoritative Documents

The following documents are authoritative:

* Mathcalibur_Main_2026.05.27.md
* Mathcalibur_Item_2026.05.27.md
* Mathcalibur_UniqueItem_2026.05.27.md



If newer versions of these documents are created, use the newest version that replaces the older document.

Document priority is determined by role, not by date.

Priority order:

1. Main
2. Item
3. UniqueItem

---

## Supporting Resource Files

The repository may contain supporting resource files.

Example:

* Mathcalibur_UniqueItem_Text.csv

These files are not authoritative implementation documents.

They exist only to support implementation of UI-facing content.

Supporting resource files may be used for:

* UI display text
* localization text
* tooltip text
* description text
* presentation content

Supporting resource files must not define or override:

* gameplay rules
* item behavior
* effect logic
* trigger conditions
* balance values
* rarity
* prices
* acquisition limits
* shop rules
* stage flow
* combat rules
* board generation rules

If a supporting resource file conflicts with an authoritative document:

1. Main
2. Item
3. UniqueItem

always take priority.

Supporting resource files never override authoritative documents.

AI agents may read supporting resource files at any time when relevant.

Supporting resource files are primarily intended for:

* UI text
* localization
* tooltip display
* description display
* presentation-layer content

Do not treat supporting resource files as gameplay specifications.

---


## Conflict Resolution

If documents conflict:

* Follow the higher-priority document.
* Do not invent a compromise solution.
* Do not merge conflicting rules.
* Report the conflict.

---

## Core Rule

Implement only what is explicitly defined in the authoritative documents.

Do not create gameplay mechanics that are not documented.

Do not add convenience systems.

Do not add inferred systems.

Do not reinterpret design intent.

Do not replace documented systems with alternative systems.

If behavior is not specified:

* Stop implementation.
* Report the missing specification.
* Request clarification.

Do not make assumptions.

---

## Specification Compliance Rule

The authoritative Mathcalibur documents are intentionally detailed.

If a documented rule appears unusual, restrictive, overly specific, or different from common game-development patterns:

* Follow the documented rule exactly.
* Do not simplify the rule.
* Do not replace it with a more common solution.
* Do not redesign it.
* Do not reinterpret the intended behavior.

Document compliance has higher priority than:

* architecture preference
* optimization preference
* framework preference
* personal opinion
* common industry patterns

If a rule is explicitly documented, implement it as written.

---

## Naming Rule

Use terminology defined by the authoritative documents.

Do not rename documented systems unless explicitly requested.

Do not replace documented terminology with personal preferences.

---

## Prohibited Behavior

Do not introduce new gameplay systems that are not documented.

Examples include:

* Match-3 mechanics
* Automatic line-clear mechanics
* Auto-combo systems
* Alternative item categories
* Alternative progression systems
* Alternative combat systems
* Alternative shop systems

If a feature is not documented, do not implement it.

Report it as a suggestion instead.

---

## Modification Policy

When implementing requested changes:

* Change only the requested scope.
* Do not refactor unrelated systems.
* Do not rename unrelated systems.
* Do not restructure architecture without explicit instruction.
* Preserve existing behavior unless the documents require otherwise.

---

## Audit Policy

When reviewing code:

* Do not automatically modify code.
* Do not automatically refactor code.
* Do not automatically rewrite systems.

Compare implementation against the authoritative documents and report:

* Missing requirements
* Rule violations
* Inconsistencies
* Areas requiring confirmation

If certainty is low, mark the result as:

Needs Confirmation

Do not guess.

---

## Success Condition

The implementation is considered correct only when it matches the authoritative Mathcalibur documents.

Correctness is determined by document compliance.

Not optimization.

Not architecture preference.

Not personal preference.

Not creative interpretation.
