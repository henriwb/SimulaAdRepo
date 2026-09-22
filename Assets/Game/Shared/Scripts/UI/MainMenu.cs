using System.Collections;
using System.Collections.Generic;
using HyperCasual.Core;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// This View contains main menu functionalities
    /// </summary>
    public class MainMenu : View
    {
        [SerializeField]
        HyperCasualButton m_StartButton;

        [SerializeField]
        AbstractGameEvent m_StartButtonEvent;

        void OnEnable()
        {
            m_StartButton.AddListener();
        }
        
        void OnDisable()
        {
            m_StartButton.RemoveListener();
        }

        void OnStartButtonClick()
        {
            m_StartButtonEvent.Raise();
            AudioManager.Instance.PlayEffect(SoundID.ButtonSound);
        }
    }
}