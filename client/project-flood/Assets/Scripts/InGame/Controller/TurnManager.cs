namespace Game.InGame.Controller
{
    public class TurnManager
    {
        public int RemainingTurns { get; private set; }
        public int TotalTurns     { get; private set; }

        public TurnManager(int turns) { RemainingTurns = turns; TotalTurns = turns; }

        public bool Consume()
        {
            if (RemainingTurns > 0) RemainingTurns--;
            return RemainingTurns > 0;
        }

        public void AddTurns(int count) => RemainingTurns += count;

        public int UsedTurns => TotalTurns - RemainingTurns;
    }
}
