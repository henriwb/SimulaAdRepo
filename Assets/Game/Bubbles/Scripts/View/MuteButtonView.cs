using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Sound on/off button: forwards clicks and shows the matching icon.
    /// Icons are swapped with SetActive (not Image.sprite) to stay on APIs proven in the web build.
    /// View: visuals and click forwarding only.
    /// </summary>
    public class MuteButtonView : MonoBehaviour
    {
        [Tooltip("Defaults to the Button on this object.")]
        [SerializeField] Button m_Button;
        [Tooltip("Shown while sound is on.")]
        [SerializeField] GameObject m_SoundOnIcon;
        [Tooltip("Shown while sound is muted.")]
        [SerializeField] GameObject m_SoundOffIcon;

        public event Action Clicked;

        void Awake()
        {
            if (m_Button == null)
                m_Button = GetComponent<Button>();
            if (m_Button != null)
                m_Button.onClick.AddListener(OnClicked);
        }

        void OnDestroy()
        {
            if (m_Button != null)
                m_Button.onClick.RemoveListener(OnClicked);
        }

        public void SetMuted(bool muted)
        {
            if (m_SoundOnIcon != null)
                m_SoundOnIcon.SetActive(!muted);
            if (m_SoundOffIcon != null)
                m_SoundOffIcon.SetActive(muted);
        }

        void OnClicked()
        {
            if (Clicked != null)
                Clicked();
        }
    }
}
