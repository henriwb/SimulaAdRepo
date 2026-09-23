using SimulaAd.Bubbles;
using UnityEngine;

namespace HyperCasual.Gameplay
{
    /// <summary>
    /// Replay button: restarts the run in place (state reset, no scene reload).
    /// GameLoopController.ResetGame also hides the End Card.
    /// Hook <see cref="RestartRun"/> to the Replay button's OnClick.
    /// </summary>
    public class RunRestarter : MonoBehaviour
    {
        [SerializeField] GameLoopController m_GameLoop;

        public void RestartRun()
        {
            // Global state that survives a run; reset it explicitly.
            // (AudioListener.pause is not available in Playworks builds.)
            Time.timeScale = 1f;

            Debug.Log($"[{nameof(RunRestarter)}] Restarting run.");
            m_GameLoop.ResetGame();
        }
    }
}
