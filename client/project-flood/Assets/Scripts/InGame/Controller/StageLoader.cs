using System;
using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;
using ProjectFlood.Data.Generated;

namespace Game.InGame.Controller
{
    public static class StageLoader
    {
        public static BoardState Load(Stage stage)
        {
            int width = stage.board_width;
            int height = stage.board_height;
            var grid = new CellData?[height, width];
            int initialValid = 0;
            bool hasCore = false;

            string cells = stage.cells;
            int cellCount = cells.Length / 3;
            for (int i = 0; i < cellCount; i++)
            {
                int row = i / width;
                int col = i % width;
                int colorId = Convert.ToInt32(cells[i * 3].ToString(), 16);
                int typeVal  = Convert.ToInt32(cells[i * 3 + 1].ToString(), 16);
                int meta     = Convert.ToInt32(cells[i * 3 + 2].ToString(), 16);

                var cellType = (CellType)typeVal;
                bool isCoreCell = (meta & 0x4) != 0;

                grid[row, col] = new CellData
                {
                    color_id          = colorId,
                    cell_type         = cellType,
                    protector_strength = meta & 0x3,
                    is_core           = isCoreCell,
                };

                if (cellType != CellType.Obstacle)
                {
                    initialValid++;
                    if (isCoreCell) hasCore = true;
                }
            }

            return new BoardState(grid, initialValid, hasCore);
        }

        public static int[] ParseColorIds(string colorIds)
        {
            var parts = colorIds.Split(',');
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = int.Parse(parts[i].Trim());
            return result;
        }
    }
}
