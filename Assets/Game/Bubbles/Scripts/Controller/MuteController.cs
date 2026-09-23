using HyperCasual.Runner;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Mute toggle required by the brief: switches all game audio (effects and music) on/off
    /// through the existing AudioManager and keeps the button icon in sync, for the current session
    /// (no saved settings). Controller.
    /// </summary>
    public class MuteController : MonoBehaviour
    {
        [SerializeField] AudioManager m_Audio;
        [SerializeField] MuteButtonView m_View;

        void Start()
        {
            // Start, not Awake: AudioManager applies its startup settings in OnEnable.
            m_View.Clicked += OnClicked;
            m_View.SetMuted(IsMuted());
        }

        void OnDestroy()
        {
            if (m_View != null)
                m_View.Clicked -= OnClicked;
        }

        void OnClicked()
        {
            bool mute = !IsMuted();
            m_Audio.EnableSfx = !mute;
            m_Audio.EnableMusic = !mute;
            m_View.SetMuted(mute);
        }

        bool IsMuted()
        {
            return m_Audio == null || !m_Audio.EnableSfx;
        }
    }
}
