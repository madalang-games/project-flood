namespace Game.InGame.Controller
{
    public class TurnManager
    {
        public int RemainingTurns { get; private set; }

        public TurnManager(int turns) => RemainingTurns = turns;

        // Returns true if turns remain after consuming
        public bool Consume()
        {
            if (RemainingTurns > 0) RemainingTurns--;
            return RemainingTurns > 0;
        }
    }
}
