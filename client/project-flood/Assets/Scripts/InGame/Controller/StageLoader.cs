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
            int maxCells = width * height;
            for (int i = 0; i < cellCount && i < maxCells; i++)
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

                if (cellType != CellType.Obstacle && cellType != CellType.Void)
                {
                    initialValid++;
                    if (isCoreCell) hasCore = true;
                }
            }

            var inletToOutlet = new System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)>();
            var outletToInlet = new System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)>();
            if (!string.IsNullOrEmpty(stage.portal_data))
            {
                var portals = stage.portal_data.Split(';');
                foreach (var p in portals)
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    var parts = p.Split(new[] { "->" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        var inletParts = parts[0].Split(',');
                        var outletParts = parts[1].Split(',');
                        if (inletParts.Length == 2 && outletParts.Length == 2)
                        {
                            int inR = int.Parse(inletParts[0].Trim());
                            int inC = int.Parse(inletParts[1].Trim());
                            int outR = int.Parse(outletParts[0].Trim());
                            int outC = int.Parse(outletParts[1].Trim());
                            inletToOutlet[(inR, inC)] = (outR, outC);
                            outletToInlet[(outR, outC)] = (inR, inC);
                        }
                    }
                }
            }

            var conveyorPaths = new System.Collections.Generic.List<System.Collections.Generic.List<(int r, int c)>>();
            if (!string.IsNullOrEmpty(stage.conveyor_data))
            {
                var paths = stage.conveyor_data.Split(';');
                foreach (var path in paths)
                {
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    var parts = path.Split(new[] { "->" }, StringSplitOptions.None);
                    var coords = new System.Collections.Generic.List<(int r, int c)>();
                    foreach (var part in parts)
                    {
                        var xy = part.Split(',');
                        if (xy.Length == 2)
                        {
                            coords.Add((int.Parse(xy[0].Trim()), int.Parse(xy[1].Trim())));
                        }
                    }
                    if (coords.Count > 1)
                    {
                        conveyorPaths.Add(coords);
                    }
                }
            }

            return new BoardState(grid, initialValid, hasCore, inletToOutlet, outletToInlet, conveyorPaths);
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
