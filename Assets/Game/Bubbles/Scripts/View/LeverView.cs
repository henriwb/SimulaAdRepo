using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Shows the aim on the lever: at angle 0 the arrow points straight up.
    /// Input comes from <see cref="SwipeInputView"/>. View: visuals only.
    /// </summary>
    public class LeverView : MonoBehaviour
    {
        [Tooltip("What rotates with the aim (defaults to this object, which carries the arrow child).")]
        [SerializeField] RectTransform m_Rotator;

        void Awake()
        {
            if (m_Rotator == null)
                m_Rotator = (RectTransform)transform;
        }

        public void SetAngle(float aimDegrees)
        {
            // Positive aim = clockwise (right); Unity's Z rotation is counter-clockwise.
            m_Rotator.localEulerAngles = new Vector3(0f, 0f, -aimDegrees);
        }
    }
}
