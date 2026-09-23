using SimulaAd.Bubbles;
using UnityEngine;

namespace HyperCasual.Gameplay
{
    /// <summary>
    /// Replay button: restarts the run in place (state reset, no scene reload).
    /// GameLoopController.ResetGame also hides the End Card.
    /// Hook <see cref="RestartRun"/> to the Replay button's OnClick.
    /// The End Card is a prefab and can't reference scene objects, so GameLoopController
    /// injects itself through <see cref="Initialize"/> (the serialized field is an optional override).
    /// </summary>
    public class RunRestarter : MonoBehaviour
    {
        [SerializeField] GameLoopController m_GameLoop;

        public void Initialize(GameLoopController gameLoop)
        {
            m_GameLoop = gameLoop;
        }

        public void RestartRun()
        {
            if (m_GameLoop == null)
            {
                Debug.LogError($"[{nameof(RunRestarter)}] No GameLoopController. Is this End Card assigned to GameLoopController's End Card field?");
                return;
            }

            // Global state that survives a run; reset it explicitly.
            // (AudioListener.pause is not available in Playworks builds.)
            Time.timeScale = 1f;

            Debug.Log($"[{nameof(RunRestarter)}] Restarting run.");
            m_GameLoop.ResetGame();
        }
    }
}
