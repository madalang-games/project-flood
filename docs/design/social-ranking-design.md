# Social & Ranking System Design

## 1. Overview
The Social & Ranking System leverages competitive psychology and community data to provide validation and "prestige" to active players.

---

## 2. Global Star Ranking (The "Prestige" Leaderboard)
A cross-player ranking based on the total number of stars collected across all chapters.

### Backend Implementation (Redis)
- **Data Structure:** `Sorted Set (ZSET)`
- **Key:** `ranking:global:stars`
- **Member:** `user_id`
- **Score:** `total_earned_stars`

### UI/UX
- **Lobby Entry:** Shows "Current Rank: #1,234".
- **Ranking Board:** Top 100 players with their Display Name, Avatar, and Total Stars.

---

## 3. Stage Performance Percentage (Social Comparison)
Real-time feedback on the result screen comparing the player's efficiency with the community.

### Mechanism
- Each stage tracks the distribution of "Turns Used" by players who achieved 3 Stars.
- When a player clears a stage, the system calculates their percentile.

### Calculation (Redis)
- **ZCOUNT** is used to count how many players cleared the stage with *fewer or equal* turns.
- **Percentage = (Rank / TotalClears) * 100**

### UI/UX
- **Result Screen:** "You cleared this stage faster than **95%** of players!"
- **Role:** Provides high emotional satisfaction without the need for material rewards.

---

## 4. Future Social Features (Backlog)
- **Friends Ranking:** Filter global ranking to show only platform friends.
- **Ghost Data:** (Speculative) Replay the "verified solution" or Top 1% player's tap sequence as a guide.
