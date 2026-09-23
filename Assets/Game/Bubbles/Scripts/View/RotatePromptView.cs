using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// "Rotate your device" overlay. Its full-screen Image (Raycast Target on) blocks game input
    /// while visible. The optional button lets the player keep playing in landscape.
    /// View: visuals and button forwarding only.
    /// </summary>
    public class RotatePromptView : MonoBehaviour
    {
        [Tooltip("Root of the overlay (full-screen panel with the message). Left inactive in the scene.")]
        [SerializeField] GameObject m_Overlay;
        [Tooltip("Optional \"Play anyway\" button.")]
        [SerializeField] Button m_ContinueButton;

        public event Action ContinuePressed;

        void Awake()
        {
            if (m_ContinueButton != null)
                m_ContinueButton.onClick.AddListener(OnContinueClicked);
        }

        void OnDestroy()
        {
            if (m_ContinueButton != null)
                m_ContinueButton.onClick.RemoveListener(OnContinueClicked);
        }

        public void SetVisible(bool visible)
        {
            if (m_Overlay.activeSelf != visible)
                m_Overlay.SetActive(visible);
        }

        void OnContinueClicked()
        {
            if (ContinuePressed != null)
                ContinuePressed();
        }
    }
}
