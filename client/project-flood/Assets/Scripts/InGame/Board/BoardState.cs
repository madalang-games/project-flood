namespace Game.InGame.Board
{
    public class BoardState
    {
        public CellData?[,] Grid { get; }
        public int Width { get; }
        public int Height { get; }
        public int InitialValidCells { get; }
        public bool HasCore { get; }

        public BoardState(CellData?[,] grid, int initialValidCells, bool hasCore)
        {
            Grid = grid;
            Width = grid.GetLength(1);
            Height = grid.GetLength(0);
            InitialValidCells = initialValidCells;
            HasCore = hasCore;
        }
    }
}
