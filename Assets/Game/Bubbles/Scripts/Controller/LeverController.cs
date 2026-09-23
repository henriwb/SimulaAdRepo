using System;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Turns horizontal swipes anywhere on the game screen into the aim angle
    /// (swipe right = aim right), shows it on the lever, and requests a shot when the drag is released.
    /// Controller: initialized by <see cref="GameLoopController"/>.
    /// </summary>
    public class LeverController : MonoBehaviour
    {
        [SerializeField] SwipeInputView m_Input;
        [SerializeField] LeverView m_View;

        GameSessionModel m_Session;
        BubbleGameConfig m_Config;
        bool m_Dragging;

        /// <summary>Raised whenever the aim angle changes (swipe or reset).</summary>
        public event Action AimChanged;

        /// <summary>Raised when an aiming drag is released: fire the shot.</summary>
        public event Action ShotReleased;

        public void Initialize(GameSessionModel session, BubbleGameConfig config)
        {
            m_Session = session;
            m_Config = config;
            m_Input.SwipeStarted += OnSwipeStarted;
            m_Input.Swiped += OnSwiped;
            m_Input.SwipeEnded += OnSwipeEnded;
        }

        void OnDestroy()
        {
            if (m_Input == null)
                return;

            m_Input.SwipeStarted -= OnSwipeStarted;
            m_Input.Swiped -= OnSwiped;
            m_Input.SwipeEnded -= OnSwipeEnded;
        }

        /// <summary>Enables/disables game input (off while the End Card is shown).</summary>
        public void SetInputEnabled(bool enabled)
        {
            if (!enabled)
                m_Dragging = false;
            m_Input.SetInputEnabled(enabled);
        }

        public void ResetAim()
        {
            m_Dragging = false;
            SetAim(0f);
        }

        void OnSwipeStarted()
        {
            m_Dragging = true;
        }

        void OnSwiped(float screenWidthFraction)
        {
            SetAim(m_Session.AimAngle + screenWidthFraction * m_Config.SwipeDegreesPerScreenWidth);
        }

        void OnSwipeEnded()
        {
            if (!m_Dragging)
                return;

            m_Dragging = false;
            if (ShotReleased != null)
                ShotReleased();
        }

        // Interrupted input (tab switch, app loses focus mid-drag): cancel instead of firing on release.
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                m_Dragging = false;
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
