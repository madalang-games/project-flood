namespace Game.InGame.Board
{
    public class BoardState
    {
        public CellData?[,] Grid { get; }
        public int Width { get; }
        public int Height { get; }
        public int InitialValidCells { get; }
        public bool HasCore { get; }

        public System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)> InletToOutlet { get; }
        public System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)> OutletToInlet { get; }
        public System.Collections.Generic.List<System.Collections.Generic.List<(int r, int c)>> ConveyorPaths { get; }

        public BoardState(
            CellData?[,] grid,
            int initialValidCells,
            bool hasCore,
            System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)> inletToOutlet = null,
            System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)> outletToInlet = null,
            System.Collections.Generic.List<System.Collections.Generic.List<(int r, int c)>> conveyorPaths = null)
        {
            Grid = grid;
            Width = grid.GetLength(1);
            Height = grid.GetLength(0);
            InitialValidCells = initialValidCells;
            HasCore = hasCore;
            InletToOutlet = inletToOutlet ?? new System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)>();
            OutletToInlet = outletToInlet ?? new System.Collections.Generic.Dictionary<(int r, int c), (int r, int c)>();
            ConveyorPaths = conveyorPaths ?? new System.Collections.Generic.List<System.Collections.Generic.List<(int r, int c)>>();
        }

        public void Rotate180()
        {
            int h = Height, w = Width;
            for (int r = 0; r < h / 2; r++)
            for (int c = 0; c < w; c++)
            {
                (Grid[r, c], Grid[h - 1 - r, w - 1 - c]) = (Grid[h - 1 - r, w - 1 - c], Grid[r, c]);
            }
            // Handle middle row for odd-height boards (swap left/right halves).
            if (h % 2 == 1)
            {
                int mid = h / 2;
                for (int c = 0; c < w / 2; c++)
                    (Grid[mid, c], Grid[mid, w - 1 - c]) = (Grid[mid, w - 1 - c], Grid[mid, c]);
            }
        }
    }
}
