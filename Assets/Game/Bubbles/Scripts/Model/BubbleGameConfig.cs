using System.Collections.Generic;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Tunable game settings. Model: data only.
    /// </summary>
    [CreateAssetMenu(fileName = "BubbleGameConfig", menuName = "SimulaAd/Bubble Game Config")]
    public class BubbleGameConfig : ScriptableObject
    {
        [Header("Board")]
        [Tooltip("Bubbles per even row; odd rows hold one less (hex offset).")]
        public int Columns = 8;
        [Tooltip("Rows filled when a game starts.")]
        public int StartRows = 5;
        [Tooltip("Grid capacity (rows a bubble can stick to).")]
        public int MaxRows = 14;

        [Header("Colors")]
        [Tooltip("One sprite per bubble color. The color index is the index in this list.")]
        public List<Sprite> ColorSprites = new List<Sprite>();
        [Tooltip("How many colors are used (clamped to the sprite count).")]
        public int ColorCount = 4;

        [Header("Rules")]
        public int MatchCount = 4;
        public int PointsPerPop = 10;
        public int PointsPerDrop = 20;

        [Header("Shot & Aim")]
        [Tooltip("Projectile speed in bubble diameters per second.")]
        public float ShotSpeed = 18f;
        [Tooltip("Max aim angle from straight up, in degrees.")]
        public float AimLimitDegrees = 75f;
        [Tooltip("Aim degrees added by swiping across the full screen width (swipe right = aim right).")]
        public float SwipeDegreesPerScreenWidth = 150f;

        [Header("Flow")]
        [Tooltip("Seconds between Game Clear and opening the End Card.")]
        public float ClearDelay = 1.2f;
    }
}
