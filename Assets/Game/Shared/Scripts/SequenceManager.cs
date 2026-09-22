using System;
using HyperCasual.Core;
using HyperCasual.Runner;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HyperCasual.Gameplay
{
    /// <summary>
    /// This singleton determines the state of the game based on observed game events.
    /// </summary>
    [Serializable]
    public class SequenceManager : AbstractSingleton<SequenceManager>
    {
        [SerializeField]
        GameObject[] m_LevelManagers;

        readonly StateMachine m_StateMachine = new StateMachine();
        IState m_SplashScreenState;

        SceneController m_SceneController;
        
        /// <summary>
        /// Initializes the SequenceManager
        /// </summary>
        public void Initialize()
        {
            m_SceneController = new SceneController(SceneManager.GetActiveScene());

            m_SplashScreenState = new State(ShowUI<SplashScreen>);

            m_StateMachine.Run(m_SplashScreenState);

            OnMainMenuDisplayed();
            OnGamePlayStarted();
        }

        /// <summary>
        /// Creates a level state from a level data
        /// </summary>
        /// <param name="levelData"></param>
        /// <returns></returns>
        IState CreateLevelState(AbstractLevelData levelData)
        {
            return new LoadLevelFromDef(m_SceneController, levelData, m_LevelManagers);
        }

        void ShowUI<T>() where T : View
        {
            UIManager.Instance.Show<T>();
        }
        
        void OnMainMenuDisplayed()
        {
            ShowUI<MainMenu>();
        }

        void OnLevelSelectionDisplayed()
        {
            AudioManager.Instance.PlayMusic(SoundID.MenuMusic);
        }

        public static void SendToEndcard()
        {
            UIManager.Instance.HideAll();
            EndCardController.Instance.OpenEndCard();
        }
        
        void OnGamePlayStarted()
        {
            ShowUI<Hud>();
            AudioManager.Instance.StopMusic();

            if (PlayableSettings.Instance.UseCTAScreen)
            {
                ShowUI<CTAScreen>();
            }
        }
    }
}
