using System.Collections.Generic;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// A class used to hold a reference to the current
    /// level and provide access to other classes.
    /// </summary>
    [ExecuteInEditMode]
    public class LevelManager : MonoBehaviour
    {
        /// <summary>
        /// Returns the LevelManager.
        /// </summary>
        public static LevelManager Instance => s_Instance;
        static LevelManager s_Instance;

        List<Spawnable> m_ActiveSpawnables = new List<Spawnable>();

        /// <summary>
        /// Calling this method calls the Reset() method on all Spawnables in this level.
        /// </summary>
        public void ResetSpawnables()
        {
            for (int i = 0, c = m_ActiveSpawnables.Count; i < c; i++)
            {
                m_ActiveSpawnables[i].ResetSpawnable();
            }
        }

        void Awake()
        {
            SetupInstance();
        }

        void OnEnable()
        {
            SetupInstance();
        }

        void SetupInstance()
        {
            if (s_Instance != null && s_Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }

            s_Instance = this;
        }
    }
}
