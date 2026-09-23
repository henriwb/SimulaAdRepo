using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Forwards horizontal swipes (touch and mouse via the EventSystem) anywhere on the game UI.
    /// Put it on the game Canvas root: drags on any child graphic bubble up to it; add a
    /// full-screen transparent Image (Raycast Target on) as the first child to catch empty space.
    /// View: input forwarding only.
    /// </summary>
    public class SwipeInputView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>A drag started.</summary>
        public event Action SwipeStarted;

        /// <summary>Horizontal movement since the last event, as a fraction of the screen width (right = positive).</summary>
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
            if (Swiped == null || Screen.width <= 0)
                return;

            Swiped(eventData.delta.x / Screen.width);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (SwipeEnded != null)
                SwipeEnded();
        }
    }
}
