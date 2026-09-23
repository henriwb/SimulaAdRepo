using System;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Turns horizontal swipes anywhere on the game screen into the aim angle
    /// (swipe right = aim right) and shows it on the lever.
    /// Controller: initialized by <see cref="GameLoopController"/>.
    /// </summary>
    public class LeverController : MonoBehaviour
    {
        [SerializeField] SwipeInputView m_Input;
        [SerializeField] LeverView m_View;

        GameSessionModel m_Session;
        BubbleGameConfig m_Config;

        /// <summary>Raised whenever the aim angle changes (swipe or reset).</summary>
        public event Action AimChanged;

        public void Initialize(GameSessionModel session, BubbleGameConfig config)
        {
            m_Session = session;
            m_Config = config;
            m_Input.Swiped += OnSwiped;
        }

        void OnDestroy()
        {
            if (m_Input != null)
                m_Input.Swiped -= OnSwiped;
        }

        public void ResetAim()
        {
            SetAim(0f);
        }

        void OnSwiped(float screenWidthFraction)
        {
            SetAim(m_Session.AimAngle + screenWidthFraction * m_Config.SwipeDegreesPerScreenWidth);
        }

        void SetAim(float degrees)
        {
            m_Session.AimAngle = Mathf.Clamp(degrees, -m_Config.AimLimitDegrees, m_Config.AimLimitDegrees);
            m_View.SetAngle(m_Session.AimAngle);

            if (AimChanged != null)
                AimChanged();
        }
    }
}
