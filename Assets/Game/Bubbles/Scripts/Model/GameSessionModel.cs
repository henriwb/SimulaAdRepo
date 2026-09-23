namespace SimulaAd.Bubbles
{
    public enum GameState
    {
        Aiming,
        Shooting,
        Resolving,
        Cleared
    }

    /// <summary>
    /// State of the current run. Model: data only; reset by GameLoopController.
    /// </summary>
    public class GameSessionModel
    {
        public GameState State;
        public int Score;
        public int CurrentColor;
        public int NextColor;

        /// <summary>Degrees from straight up; positive aims right.</summary>
        public float AimAngle;
    }
}
