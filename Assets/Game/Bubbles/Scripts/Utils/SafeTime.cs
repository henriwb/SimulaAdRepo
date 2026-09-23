using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Frame delta clamped to a small maximum. When the page comes back from a hidden tab (or after
    /// a hitch) the first frame can carry a large deltaTime; clamping keeps shots and animations
    /// from jumping ahead, so gameplay resumes where it paused. Pure function, no state.
    /// </summary>
    public static class SafeTime
    {
        /// <summary>Largest step a single frame may advance gameplay, in seconds (~20 fps).</summary>
        public const float MaxDelta = 0.05f;

        /// <summary>Time.deltaTime (scaled, so Time.timeScale = 0 still pauses), capped at <see cref="MaxDelta"/>.</summary>
        public static float Delta
        {
            get { return Mathf.Min(Time.deltaTime, MaxDelta); }
        }
    }
}
