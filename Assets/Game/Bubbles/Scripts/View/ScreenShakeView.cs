using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// "Camera" shake for a UI game: shakes a RectTransform (e.g. the play area) instead of the camera,
    /// because Screen Space canvases follow the camera, so moving it would not move the UI.
    /// Deterministic (two out-of-phase sine waves, no random), decaying; additive, so it doesn't fight
    /// other code that positions the target. Uses clamped scaled time (pauses with the game).
    /// View: visuals only; GameLoopController decides when and how hard.
    /// </summary>
    public class ScreenShakeView : MonoBehaviour
    {
        [Tooltip("What shakes (defaults to this object's RectTransform), e.g. the play area.")]
        [SerializeField] RectTransform m_Target;
        [Tooltip("Max offset for a strength-1 shake, in the target's parent units.")]
        [SerializeField] float m_MaxOffset = 12f;
        [SerializeField] float m_Duration = 0.3f;
        [Tooltip("Oscillations per second.")]
        [SerializeField] float m_Frequency = 22f;

        Vector3 m_AppliedOffset;
        Vector3 m_LastWrittenPosition;
        float m_Elapsed = -1f;
        float m_Strength;

        void Awake()
        {
            if (m_Target == null)
                m_Target = (RectTransform)transform;
        }

        /// <summary>Starts (or strengthens) a shake. Strength 0–1+; overlapping shakes keep the stronger one.</summary>
        public void Shake(float strength)
        {
            if (m_Elapsed >= 0f && strength < m_Strength * (1f - m_Elapsed / m_Duration))
                return; // a stronger shake is still running

            m_Strength = strength;
            m_Elapsed = 0f;
        }

        /// <summary>Stops immediately and restores the target (used on reset).</summary>
        public void Stop()
        {
            m_Elapsed = -1f;
            ApplyOffset(Vector3.zero);
        }

        void LateUpdate()
        {
            if (m_Elapsed < 0f)
                return;

            m_Elapsed += SafeTime.Delta;
            float u = m_Elapsed / m_Duration;
            if (u >= 1f)
            {
                Stop();
                return;
            }

            float decay = (1f - u) * (1f - u);
            float phase = m_Elapsed * m_Frequency * 2f * Mathf.PI;
            float amount = m_MaxOffset * m_Strength * decay;
            ApplyOffset(new Vector3(Mathf.Sin(phase) * amount, Mathf.Sin(phase * 1.3f + 1f) * amount * 0.6f, 0f));
        }

        // Additive: remove last frame's offset, add this frame's. If something else moved the target
        // since our last write (e.g. OrientationLayoutView applying the landscape layout mid-shake),
        // that new position is the base: our old offset is already gone, so don't subtract it.
        void ApplyOffset(Vector3 offset)
        {
            if (m_Target == null)
                return;

            Vector3 basePosition = m_Target.localPosition;
            if (m_AppliedOffset != Vector3.zero && basePosition == m_LastWrittenPosition)
                basePosition -= m_AppliedOffset;

            m_Target.localPosition = basePosition + offset;
            m_AppliedOffset = offset;
            m_LastWrittenPosition = m_Target.localPosition;
        }
    }
}
