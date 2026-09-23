using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Forwards horizontal swipes (touch and mouse via the EventSystem) anywhere on the game UI.
    /// Put it on the game Canvas root: drags on any child graphic bubble up to it; add a
    /// full-screen transparent Image (Raycast Target on) as the first child to catch empty space.
    /// Swipe distance is measured against the game frame width, so it feels the same in portrait
    /// and pillarboxed landscape. View: input forwarding only.
    /// </summary>
    public class SwipeInputView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Width reference for swipe distance (the GameFrame). Empty = screen width.")]
        [SerializeField] RectTransform m_WidthReference;

        readonly Vector3[] m_Corners = new Vector3[4];
        GraphicRaycaster m_Raycaster;

        /// <summary>
        /// Turns the game canvas's raycasting on/off. Off while the End Card is up: with equal
        /// canvas sort orders the Playworks runtime can hit the game canvas's full-screen images
        /// first, swallowing clicks meant for the End Card buttons.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (m_Raycaster == null)
                m_Raycaster = GetComponent<GraphicRaycaster>();
            if (m_Raycaster != null)
                m_Raycaster.enabled = enabled;
        }

        /// <summary>A drag started.</summary>
        public event Action SwipeStarted;

        /// <summary>Horizontal movement since the last event, as a fraction of the reference width (right = positive).</summary>
        public event Action<float> Swiped;

        /// <summary>The drag was released.</summary>
        public event Action SwipeEnded;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (SwipeStarted != null)
                SwipeStarted();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Swiped == null)
                return;

            float width = GetReferenceWidthPixels(eventData.pressEventCamera);
            if (width <= 0f)
                return;

            Swiped(eventData.delta.x / width);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (SwipeEnded != null)
                SwipeEnded();
        }

        float GetReferenceWidthPixels(Camera eventCamera)
        {
            if (m_WidthReference == null)
                return Screen.width;

            m_WidthReference.GetWorldCorners(m_Corners); // 0 = bottom-left, 3 = bottom-right
            Vector2 left = RectTransformUtility.WorldToScreenPoint(eventCamera, m_Corners[0]);
            Vector2 right = RectTransformUtility.WorldToScreenPoint(eventCamera, m_Corners[3]);
            return Mathf.Abs(right.x - left.x);
        }
    }
}
