using DG.Tweening;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Idle character animation for a UI Image: small hops at random intervals, sometimes
    /// flipping X (looking the other way), and a bigger jump when a bubble is shot
    /// (<see cref="PlayShotJump"/>). Uses scaled time, so it pauses with Time.timeScale.
    /// View: visuals only; GameLoopController triggers the shot jump and the reset.
    /// </summary>
    public class JumpingMascotView : MonoBehaviour
    {
        [Tooltip("Animated UI element (defaults to this object).")]
        [SerializeField] RectTransform m_Target;

        [Header("Idle")]
        [SerializeField] float m_MinIdleDelay = 1.2f;
        [SerializeField] float m_MaxIdleDelay = 3f;
        [SerializeField] float m_IdleJumpHeight = 12f;
        [SerializeField] float m_IdleJumpDuration = 0.35f;
        [Tooltip("Chance to flip X before an idle hop.")]
        [Range(0f, 1f)]
        [SerializeField] float m_FlipChance = 0.5f;

        [Header("Shot jump")]
        [SerializeField] float m_ShotJumpHeight = 35f;
        [SerializeField] float m_ShotJumpDuration = 0.45f;
        [SerializeField] int m_ShotJumpCount = 1;

        [Header("Squash & stretch")]
        [Tooltip("Vertical stretch punch applied with every jump.")]
        [SerializeField] float m_Stretch = 0.15f;

        Vector2 m_BasePosition;
        Vector3 m_BaseScale;
        float m_IdleTimer;
        Tween m_JumpTween;
        Tween m_StretchTween;

        void Awake()
        {
            if (m_Target == null)
                m_Target = (RectTransform)transform;

            m_BasePosition = m_Target.anchoredPosition;
            m_BaseScale = m_Target.localScale;
            ScheduleNextIdleHop();
        }

        void OnDestroy()
        {
            KillTweens();
        }

        void Update()
        {
            if (IsJumping())
                return;

            m_IdleTimer -= Time.deltaTime;
            if (m_IdleTimer > 0f)
                return;

            if (Random.value < m_FlipChance)
                FlipX();

            Jump(m_IdleJumpHeight, m_IdleJumpDuration, 1);
            ScheduleNextIdleHop();
        }

        /// <summary>Bigger jump, played when a bubble is shot.</summary>
        public void PlayShotJump()
        {
            Jump(m_ShotJumpHeight, m_ShotJumpDuration, Mathf.Max(1, m_ShotJumpCount));
            ScheduleNextIdleHop();
        }

        /// <summary>Back to the scene pose (used on replay).</summary>
        public void ResetPose()
        {
            KillTweens();
            m_Target.anchoredPosition = m_BasePosition;
            m_Target.localScale = m_BaseScale;
            ScheduleNextIdleHop();
        }

        void Jump(float height, float duration, int jumps)
        {
            KillTweens();

            // Restart from the base pose, keeping the current facing (sign of X).
            m_Target.anchoredPosition = m_BasePosition;
            Vector3 scale = m_BaseScale;
            scale.x = Mathf.Abs(m_BaseScale.x) * Mathf.Sign(m_Target.localScale.x);
            m_Target.localScale = scale;

            m_JumpTween = m_Target.DOJumpAnchorPos(m_BasePosition, height, jumps, duration);
            m_StretchTween = m_Target.DOPunchScale(new Vector3(0f, m_Stretch * m_BaseScale.y, 0f), duration, 4, 0.5f);
        }

        // Instant flip (no tween), done only between jumps so it never fights the stretch punch.
        void FlipX()
        {
            Vector3 scale = m_Target.localScale;
            scale.x = -scale.x;
            m_Target.localScale = scale;
        }

        bool IsJumping()
        {
            return m_JumpTween != null && m_JumpTween.IsActive() && m_JumpTween.IsPlaying();
        }

        void ScheduleNextIdleHop()
        {
            m_IdleTimer = Random.Range(m_MinIdleDelay, m_MaxIdleDelay);
        }

        void KillTweens()
        {
            if (m_JumpTween != null)
                m_JumpTween.Kill();
            if (m_StretchTween != null)
                m_StretchTween.Kill();

            m_JumpTween = null;
            m_StretchTween = null;
        }
    }
}
