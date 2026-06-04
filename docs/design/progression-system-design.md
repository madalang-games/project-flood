# Progression System Design

## 1. Overview
The Progression System defines how a player moves through the game, measures success via Stars, and achieves long-term goals through Chapter milestones.

---

## 2. Star System (Performance Metrics)
Every stage has 3 performance tiers (Stars).

### Star Criteria
| Tier | Condition | Definition |
|:---:|---|---|
| **1★** | `Clearance Ratio >= Star1_Ratio` | **Minimum Clear.** Required to unlock the next stage. |
| **2★** | `Clearance Ratio >= Star2_Ratio` | **Intermediate Achievement.** Mastery over the board. |
| **3★** | `Clearance Ratio == 1.0 (100%)` | **Perfect Clear.** All removable cells cleared. |

### Mandatory Failure Condition
Regardless of the clearance ratio, if a stage has an `is_core` cell and it is **not removed** before the turns run out, the result is always **FAIL (0 Stars)**.

---

## 3. Chapter Structure
Stages are organized into Chapters to provide thematic pacing.

### Chapter Properties
- **Chapter ID:** Sequential grouping ID.
- **Thematic Visuals:** Each chapter has a unique `theme_key` (Background, Cell skins, Color palette).
- **Stage Range:** e.g., Chapter 1 contains Stages 1-20.

---

## 4. Milestone Rewards (Chapter Completion)
To encourage replayability and perfectionism, the system rewards 100% completion per chapter.

### Chapter Completion Chest
- **Condition:** All stages within the current chapter must have **3 Stars**.
- **Interaction:** A "Golden Chest" appears on the Chapter Selection UI when the condition is met.
- **Reward:** High-tier items (e.g., 5x Bombs, 1x Unlimited Stamina for 30m).
- **Limit:** Claimable **once per chapter**.

---

## 5. Visual Theme Progression
Progression is visualized through environment changes.
- **Chapter 1:** Calm water, bright colors.
- **Chapter 2:** Deep sea, bioluminescent colors.
- **Chapter 3:** Urban flood, industrial/gritty colors.

*Visual assets are mapped via `theme_key` in the client's `VisualService`.*
