using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HyperCasual.Runner
{
    /// <summary>
    /// A base class for all objects which populate a
    /// LevelDefinition. This class includes all logic 
    /// necessary for snapping an object to a level's grid.
    /// </summary>
    [ExecuteInEditMode]
    public class Spawnable : MonoBehaviour
    {
        protected Transform m_Transform;
        Vector3 m_Position;
        bool m_SnappedThisFrame;
        float m_PreviousGridSize;

        MeshRenderer[] m_MeshRenderers;

        [SerializeField]
        bool m_SnapToGrid = true;

        protected virtual void Awake()
        {
            m_Transform = transform;

            if (m_MeshRenderers == null || m_MeshRenderers.Length == 0)
            {
                m_MeshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            }

            if (LevelManager.Instance != null)
            {
#if UNITY_EDITOR
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject))
#endif
                m_Transform.SetParent(LevelManager.Instance.transform);
            }
        }

        /// <summary>
        /// Sets the local scale of this spawnable object.
        /// </summary>
        /// <param name="scale">
        /// The scale to apply to this spawnable object.
        /// </param>
        protected virtual void SetScale(Vector3 scale)
        {
           m_Transform.localScale = scale;
        }

        /// <summary>
        /// This method can be overriden in classes that extend Spawnable
        /// to hold any logic needed to reset that object to its default state.
        /// </summary>
        public virtual void ResetSpawnable() {}

        protected virtual void OnEnable()
        {
            m_Transform = transform;
            m_Position = m_Transform.position;
            m_Transform.hasChanged = false;

            if (LevelManager.Instance != null && !Application.isPlaying)
            {
                SnapToGrid();
            }
        }

        protected virtual void Update()
        {
            if (!Application.isPlaying)
            {
                if (m_Transform.hasChanged)
                {
                    m_Position = m_Transform.position;
                    m_Transform.hasChanged = false;

                    SetScale(m_Transform.localScale);
                }
            }
        }

        /// <summary>
        /// If applicable, snap this spawnable object to the grid of the 
        /// current LevelDefinition.
        /// </summary>
        private void SnapToGrid()
        {
            if (!m_SnapToGrid)
            {
                return;
            }

            var position = m_Position;

            m_Transform.position = position;
            m_Transform.hasChanged = false;
        }
    }
}