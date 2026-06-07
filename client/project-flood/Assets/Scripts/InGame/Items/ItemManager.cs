using System;
using System.Collections.Generic;
using Game.InGame.Board;

namespace Game.InGame.Items
{
    public class ItemManager
    {
        public bool IsInUsePhase { get; private set; }
        public ItemType? SelectedItem { get; private set; }
        public bool IsDevMode => _inventory.IsDevMode;
        public (int row, int col)? FirstSelectedCell { get; private set; }

        // null = exited use phase; non-null = entered use phase with this item
        public event Action<ItemType?> OnUsePhaseChanged;

        private readonly ItemInventory _inventory;
        private readonly Dictionary<ItemType, IItemEffect> _effects;

        public ItemManager(ItemInventory inventory)
        {
            _inventory = inventory;
            _effects = new Dictionary<ItemType, IItemEffect>
            {
                { ItemType.Bomb,       new BombEffect() },
                { ItemType.HRocket,    new HRocketEffect() },
                { ItemType.ColorSweep, new ColorSweepEffect() },
                { ItemType.RowShift,   new RowShiftEffect() },
                { ItemType.CellSwap,   new CellSwapEffect() },
            };
        }

        public bool CanUse(ItemType type) => _inventory.CanUse(type);
        public int GetCount(ItemType type) => _inventory.GetCount(type);

        public void SelectItem(ItemType type)
        {
            if (!_inventory.CanUse(type)) return;

            if (IsInUsePhase && SelectedItem == type)
            {
                Cancel();
                return;
            }

            IsInUsePhase = true;
            SelectedItem = type;
            FirstSelectedCell = null;
            OnUsePhaseChanged?.Invoke(type);
        }

        public void Cancel()
        {
            if (!IsInUsePhase) return;
            IsInUsePhase = false;
            SelectedItem = null;
            FirstSelectedCell = null;
            OnUsePhaseChanged?.Invoke(null);
        }

        // Sets the first selected cell for CellSwap.
        public void SetFirstSelectedCell(int row, int col)
        {
            if (SelectedItem == ItemType.CellSwap)
            {
                FirstSelectedCell = (row, col);
            }
        }

        // Returns affected cells. Consumes the item and exits use phase.
        // Returns null if not completed or not in use phase.
        public List<(int row, int col)> UseItem(BoardState board, int targetRow, int targetCol, out bool completed)
        {
            completed = false;
            if (!IsInUsePhase || SelectedItem == null) return null;

            var type = SelectedItem.Value;

            if (type == ItemType.CellSwap)
            {
                if (FirstSelectedCell == null)
                {
                    var validation = _effects[type].GetAffectedCells(board, targetRow, targetCol);
                    if (validation.Count > 0)
                    {
                        FirstSelectedCell = (targetRow, targetCol);
                    }
                    return null; // First selection complete, wait for second
                }

                var first = FirstSelectedCell.Value;
                if (first.row == targetRow && first.col == targetCol)
                {
                    // Tapping the same cell cancels selection
                    FirstSelectedCell = null;
                    return null;
                }

                // Verify second cell is valid
                var secondValidation = _effects[type].GetAffectedCells(board, targetRow, targetCol);
                if (secondValidation.Count == 0)
                {
                    return null; // Invalid second cell, wait for a valid one
                }

                // Perform logical swap
                var temp = board.Grid[first.row, first.col];
                board.Grid[first.row, first.col] = board.Grid[targetRow, targetCol];
                board.Grid[targetRow, targetCol] = temp;

                var cells = new List<(int, int)> { first, (targetRow, targetCol) };

                _inventory.Consume(type);
                IsInUsePhase = false;
                SelectedItem = null;
                FirstSelectedCell = null;
                OnUsePhaseChanged?.Invoke(null);
                completed = true;

                return cells;
            }
            else
            {
                var cells = _effects[type].GetAffectedCells(board, targetRow, targetCol);
                if (cells == null || cells.Count == 0) return null;

                _inventory.Consume(type);
                IsInUsePhase = false;
                SelectedItem = null;
                OnUsePhaseChanged?.Invoke(null);
                completed = true;

                return cells;
            }
        }

        public void UseRowShift(BoardState board, ShiftDirection direction)
        {
            if (!IsInUsePhase || SelectedItem != ItemType.RowShift) return;

            var effect = _effects[ItemType.RowShift] as RowShiftEffect;
            if (effect != null)
            {
                effect.Apply(board, direction);
            }

            _inventory.Consume(ItemType.RowShift);
            IsInUsePhase = false;
            SelectedItem = null;
            OnUsePhaseChanged?.Invoke(null);
        }
    }
}
