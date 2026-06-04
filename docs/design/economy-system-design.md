# Economy System Design

## 1. Overview
The Economy System manages the flow of the primary soft currency, item acquisition, and consumption sinks to ensure balanced gameplay and long-term retention.

---

## 2. Currency Type (Soft Currency)
To maintain simplicity and focus on the Star system, the game uses a single primary soft currency.

| Currency | Thematic Name | Role | Acquisition |
|:---:|:---:|---|---|
| **Primary** | **Gold (or Coins)** | General utility. Used for shop, boosters, continues. | Stage Clear, Milestones, Events. |

---

## 3. Reward Formula (Stage Clear)
When a stage is cleared (>= 1 Star), Gold is awarded based on performance.

**`TotalReward = BaseReward(StageID) + (RemainingTurns * TurnBonusValue)`**

- **BaseReward:** Fixed amount per stage difficulty.
- **TurnBonusValue:** Encourages efficient play.

---

## 4. Economy Sinks (Consumption)
Players spend their accumulated Gold on:
1. **Pre-game Boosters:** e.g., "Start with +5 Turns".
2. **In-game Items:** Buying Bombs or Rockets when out of stock.
3. **Continue:** On failure (Turns == 0), spend Gold to get +3 additional turns (once per stage attempt).

---

## 5. Data Integration
- **`reward/` domain:** All Gold rewards are defined in `reward_group.csv`.
- **`shop/` domain:** Item prices and booster costs are defined in `shop_item.csv`.
- **`player_currencies` table:** Stores the current balance per `user_id` and `currency_id` (Gold ID: 1001).
