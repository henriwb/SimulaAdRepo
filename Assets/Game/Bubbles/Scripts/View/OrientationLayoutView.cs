using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Gives a UI element a second layout for landscape. The element's own scene layout is the
    /// portrait layout; <see cref="m_LandscapeLayout"/> is a placeholder RectTransform (same parent,
    /// no components needed, can be inactive) whose anchors/position/size are copied onto this
    /// element while the screen is landscape. Works for the game area, lever, HUD, etc.
    /// Systems that read this element's rect (e.g. BoardView) re-lay out on their own.
    /// View: layout only.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class OrientationLayoutView : MonoBehaviour
    {
        [Tooltip("Placeholder with the landscape layout. Must share this element's parent.")]
        [SerializeField] RectTransform m_LandscapeLayout;

        struct Layout
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector3 LocalScale;
            // Rotation is not copied: LeverView rotates its own transform to show the aim.
        }

        RectTransform m_Rect;
        Layout m_Portrait;
        Layout m_Landscape;
        bool m_HasLandscape;
        bool m_Initialized;
        bool m_IsLandscape;

        void Awake()
        {
            m_Rect = (RectTransform)transform;
            m_Portrait = Capture(m_Rect);

            if (m_LandscapeLayout != null)
            {
                if (m_LandscapeLayout.parent != m_Rect.parent)
                    Debug.LogWarning($"[{nameof(OrientationLayoutView)}] '{m_LandscapeLayout.name}' should share the parent of '{name}', or its values won't line up.", this);

                m_Landscape = Capture(m_LandscapeLayout);
                m_HasLandscape = true;
            }
        }

        void LateUpdate()
        {
            bool landscape = m_HasLandscape && Screen.width > Screen.height;
            if (m_Initialized && landscape == m_IsLandscape)
                return;

            m_Initialized = true;
            m_IsLandscape = landscape;
            Apply(landscape ? m_Landscape : m_Portrait);
        }

        static Layout Capture(RectTransform rect)
        {
            Layout layout;
            layout.AnchorMin = rect.anchorMin;
            layout.AnchorMax = rect.anchorMax;
            layout.Pivot = rect.pivot;
            layout.AnchoredPosition = rect.anchoredPosition;
            layout.SizeDelta = rect.sizeDelta;
            layout.LocalScale = rect.localScale;
            return layout;
        }

        void Apply(Layout layout)
        {
            m_Rect.anchorMin = layout.AnchorMin;
            m_Rect.anchorMax = layout.AnchorMax;
            m_Rect.pivot = layout.Pivot;
            m_Rect.anchoredPosition = layout.AnchoredPosition;
            m_Rect.sizeDelta = layout.SizeDelta;
            m_Rect.localScale = layout.LocalScale;
        }
    }
}
