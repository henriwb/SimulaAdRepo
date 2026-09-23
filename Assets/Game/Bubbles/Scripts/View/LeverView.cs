using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Shows the aim on the lever: at angle 0 the arrow points straight up.
    /// On a shot, plays a backfire: the lever kicks back opposite the aim and squashes along its
    /// barrel, then springs back (damped, deterministic, hand-animated with clamped scaled time).
    /// Input comes from <see cref="SwipeInputView"/>. View: visuals only.
    /// </summary>
    public class LeverView : MonoBehaviour
    {
        [Tooltip("What rotates with the aim (defaults to this object, which carries the arrow child).")]
        [SerializeField] RectTransform m_Rotator;

        [Header("Backfire")]
        [Tooltip("How far the lever kicks back, in its parent's units.")]
        [SerializeField] float m_RecoilDistance = 16f;
        [Tooltip("Squash along the barrel (and stretch across it) at the peak of the kick.")]
        [SerializeField] float m_RecoilSquash = 0.2f;
        [SerializeField] float m_RecoilDuration = 0.3f;

        float m_AimDegrees;
        Vector3 m_BaseScale = Vector3.one;
        bool m_BaseScaleCaptured;

        float m_RecoilElapsed = -1f;
        Vector3 m_AppliedOffset;
        Vector3 m_LastWrittenPosition;

        void Awake()
        {
            if (m_Rotator == null)
                m_Rotator = (RectTransform)transform;
            CaptureBaseScale();
        }

        public void SetAngle(float aimDegrees)
        {
            m_AimDegrees = aimDegrees;
            // Positive aim = clockwise (right); Unity's Z rotation is counter-clockwise.
            m_Rotator.localEulerAngles = new Vector3(0f, 0f, -aimDegrees);
        }

        /// <summary>Kick back + squash, played when a bubble is shot.</summary>
        public void PlayRecoil()
        {
            CaptureBaseScale();
            m_RecoilElapsed = 0f;
        }

        void LateUpdate()
        {
            if (m_RecoilElapsed < 0f)
                return;

            m_RecoilElapsed += SafeTime.Delta;
            float t = m_RecoilElapsed / (m_RecoilDuration > 0.01f ? m_RecoilDuration : 0.01f);
            if (t >= 1f)
            {
                m_RecoilElapsed = -1f;
                ApplyOffset(Vector3.zero);
                m_Rotator.localScale = m_BaseScale;
                return;
            }

            // Damped spring: fast kick, small overshoot past rest, settle.
            float decay = (1f - t) * (1f - t);
            float spring = Mathf.Sin(t * 2.5f * Mathf.PI) * decay;

            // Backwards along the barrel = opposite of the aim direction (in the parent's space).
            float radians = m_AimDegrees * Mathf.Deg2Rad;
            Vector3 back = new Vector3(-Mathf.Sin(radians), -Mathf.Cos(radians), 0f);
            ApplyOffset(back * (m_RecoilDistance * spring));

            // Squash along the barrel (local Y), stretch across it (local X).
            float squash = m_RecoilSquash * spring;
            m_Rotator.localScale = new Vector3(m_BaseScale.x * (1f + squash), m_BaseScale.y * (1f - squash), m_BaseScale.z);
        }

        // Additive offset that tolerates other code repositioning the lever (e.g. orientation layouts).
        void ApplyOffset(Vector3 offset)
        {
            RectTransform rect = (RectTransform)transform;
            Vector3 basePosition = rect.localPosition;
            if (m_AppliedOffset != Vector3.zero && basePosition == m_LastWrittenPosition)
                basePosition -= m_AppliedOffset;

            rect.localPosition = basePosition + offset;
            m_AppliedOffset = offset;
            m_LastWrittenPosition = rect.localPosition;
        }

        void CaptureBaseScale()
        {
            if (m_BaseScaleCaptured || m_Rotator == null)
                return;
            m_BaseScale = m_Rotator.localScale;
            m_BaseScaleCaptured = true;
        }
    }
}
