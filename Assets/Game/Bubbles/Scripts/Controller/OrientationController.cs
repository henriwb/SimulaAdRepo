using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Landscape handling. The game always adapts (PortraitFrameView pillarboxes it); on touch
    /// devices in landscape a rotate prompt is also shown and the game is paused behind it.
    /// Desktop (no touch) never gets the prompt, since a monitor can't be rotated.
    /// "Play anyway" dismisses it until the device returns to portrait.
    /// Controller.
    /// </summary>
    public class OrientationController : MonoBehaviour
    {
        [SerializeField] RotatePromptView m_Prompt;

        [Tooltip("Only prompt on touch devices (phones/tablets). Off = prompt in any landscape window.")]
        [SerializeField] bool m_TouchDevicesOnly = true;

        [Header("Debug")]
        [SerializeField] bool m_ForcePrompt;

        bool m_Dismissed;
        bool m_PausedByPrompt;
        bool m_Visible;

        void Awake()
        {
            m_Prompt.ContinuePressed += OnContinuePressed;
            m_Prompt.SetVisible(false);
        }

        void OnDestroy()
        {
            if (m_Prompt != null)
                m_Prompt.ContinuePressed -= OnContinuePressed;
        }

        void Update()
        {
            bool landscape = Screen.width > Screen.height;
            if (!landscape)
                m_Dismissed = false; // back in portrait: prompt again next time

            bool touchOk = !m_TouchDevicesOnly || Input.touchSupported;
            bool show = m_ForcePrompt || (landscape && touchOk && !m_Dismissed);

            if (show != m_Visible)
                SetPromptVisible(show);
        }

        void OnContinuePressed()
        {
            m_Dismissed = true;
            m_ForcePrompt = false;
            SetPromptVisible(false);
        }

        void SetPromptVisible(bool visible)
        {
            m_Visible = visible;
            m_Prompt.SetVisible(visible);

            // Only undo a pause this controller made, so other pause owners aren't overridden.
            if (visible && Time.timeScale > 0f)
            {
                Time.timeScale = 0f;
                m_PausedByPrompt = true;
            }
            else if (!visible && m_PausedByPrompt)
            {
                Time.timeScale = 1f;
                m_PausedByPrompt = false;
            }
        }
    }
}
