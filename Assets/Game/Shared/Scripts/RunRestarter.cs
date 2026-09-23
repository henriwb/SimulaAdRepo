using SimulaAd.Bubbles;
using UnityEngine;
using UnityEngine.UI;

namespace HyperCasual.Gameplay
{
    /// <summary>
    /// Replay button: restarts the run in place (state reset, no scene reload).
    /// GameLoopController.ResetGame also hides the End Card.
    /// Self-wiring so it doesn't depend on Inspector/persistent-call setup surviving the
    /// Playworks build: registers its Button's click in Awake, and finds the GameLoopController
    /// itself if it wasn't injected. A persistent OnClick → RestartRun may also exist; calls in
    /// the same frame are collapsed into one.
    /// </summary>
    public class RunRestarter : MonoBehaviour
    {
        [SerializeField] GameLoopController m_GameLoop;
        [Tooltip("Replay button (defaults to the Button on this object).")]
        [SerializeField] Button m_Button;

        int m_LastRestartFrame = -1;

        void Awake()
        {
            if (m_Button == null)
                m_Button = GetComponent<Button>();
            if (m_Button != null)
                m_Button.onClick.AddListener(RestartRun);
        }

        void OnDestroy()
        {
            if (m_Button != null)
                m_Button.onClick.RemoveListener(RestartRun);
        }

        public void Initialize(GameLoopController gameLoop)
        {
            m_GameLoop = gameLoop;
        }

        public void RestartRun()
        {
            if (Time.frameCount == m_LastRestartFrame)
                return;
            m_LastRestartFrame = Time.frameCount;

            if (m_GameLoop == null)
                m_GameLoop = FindObjectOfType<GameLoopController>();

            if (m_GameLoop == null)
            {
                Debug.LogError($"[{nameof(RunRestarter)}] No GameLoopController found.");
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
