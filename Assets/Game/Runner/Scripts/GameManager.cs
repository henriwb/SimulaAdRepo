using HyperCasual.Core;
using HyperCasual.Gameplay;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace HyperCasual.Runner
{
    /// <summary>
    /// A class used to store game state information, 
    /// load levels, and save/load statistics as applicable.
    /// The GameManager class manages all game-related 
    /// state changes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Returns the GameManager.
        /// </summary>
        public static GameManager Instance => s_Instance;
        static GameManager s_Instance;

        [SerializeField]
        AbstractGameEvent m_LoseEvent;
        
        GameObject m_CurrentLevelGO;
        GameObject m_CurrentTerrainGO;
        GameObject m_LevelMarkersGO;
        GameObject m_CurrentLevelHolderGO;
        Transform m_CurrentLevelParent;

#if UNITY_EDITOR
        bool m_LevelEditorMode;
#endif

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

#if UNITY_EDITOR

            // If LevelManager already exists, user is in the LevelEditorWindow
            if (LevelManager.Instance != null)
            {
                StartGame();
                m_LevelEditorMode = true;
            }
#endif
        }

        /// <summary>
        /// This method calls all methods necessary to load and
        /// instantiate a level from a level definition.
        /// </summary>
        public void LoadLevel()
        {
            if (m_CurrentLevelHolderGO != null) Destroy(m_CurrentLevelHolderGO);
            //LoadLevel(m_CurrentLevel, ref m_CurrentLevelGO);
            //CreateTerrain(m_CurrentLevel, ref m_CurrentTerrainGO);
            //PlaceLevelMarkers(m_CurrentLevel, ref m_LevelMarkersGO);
            StartGame();
        }

        /// <summary>
        /// This method calls all methods necessary to restart a level,
        /// including resetting the player to their starting position
        /// </summary>
        public void ResetLevel()
        {
            PlayerController.Instance.ResetPlayer();
            CameraManager.Instance.ResetCamera();

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ResetSpawnables();
            }
        }

        void StartGame()
        {
            ResetLevel();
        }

        public void Win()
        {
            PlayableSettings.Instance.CallLevelCompleteAnalytics();

#if UNITY_EDITOR
            if (m_LevelEditorMode)
            {
                ResetLevel();
            }
#endif
        }

        public void Lose()
        {
            m_LoseEvent.Raise();

#if UNITY_EDITOR
            if (m_LevelEditorMode)
            {
                ResetLevel();
            }
#endif
        }
    }
}