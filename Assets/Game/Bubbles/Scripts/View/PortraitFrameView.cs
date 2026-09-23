using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Keeps the game UI in a fixed portrait design frame (390×844 by default), centered and
    /// uniformly scaled to fit its parent. In landscape this pillarboxes the game; in portrait
    /// it letterboxes slightly on unusual aspects. Children keep their design-space layout at
    /// any resolution. Put it on a "GameFrame" RectTransform that holds all game UI.
    /// Checked in LateUpdate (the Playworks runtime may size the canvas late).
    /// View: layout only.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PortraitFrameView : MonoBehaviour
    {
        [SerializeField] Vector2 m_DesignSize = new Vector2(390f, 844f);

        RectTransform m_Frame;
        RectTransform m_Parent;
        Vector2 m_LastParentSize = new Vector2(-1f, -1f);

        void Awake()
        {
            m_Frame = (RectTransform)transform;
            m_Parent = m_Frame.parent as RectTransform;

            Vector2 center = new Vector2(0.5f, 0.5f);
            m_Frame.anchorMin = center;
            m_Frame.anchorMax = center;
            m_Frame.pivot = center;
            m_Frame.anchoredPosition = Vector2.zero;
            m_Frame.sizeDelta = m_DesignSize;
        }

        void LateUpdate()
        {
            if (m_Parent == null)
                return;

            Vector2 parentSize = m_Parent.rect.size;
            if (parentSize == m_LastParentSize || parentSize.x <= 0f || parentSize.y <= 0f)
                return;

            m_LastParentSize = parentSize;
            float scale = Mathf.Min(parentSize.x / m_DesignSize.x, parentSize.y / m_DesignSize.y);
            m_Frame.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
