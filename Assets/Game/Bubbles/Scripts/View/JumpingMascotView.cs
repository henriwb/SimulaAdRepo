using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Idle character animation for a UI Image: small hops at random intervals, sometimes
    /// flipping X (looking the other way), and a bigger jump when a bubble is shot
    /// (<see cref="PlayShotJump"/>).
    /// Hand-written in Update (no DOTween): tween loops/yoyo ran endlessly in the Playworks
    /// build, making the character hop frantically. Uses scaled time, so it pauses with Time.timeScale.
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
        [Tooltip("Extra vertical scale at the top of each hop.")]
        [SerializeField] float m_Stretch = 0.15f;

        Vector2 m_BasePosition;
        Vector3 m_BaseScale;
        float m_FacingSign = 1f;
        bool m_Initialized;

        float m_IdleTimer;

        bool m_Jumping;
        float m_JumpElapsed;
        float m_JumpDuration;
        float m_JumpHeight;
        int m_JumpCount;

        // Own generator (UnityEngine.Random is missing in Playworks builds); created on first use.
        RandomSource m_Random;

        void Awake()
        {
            EnsureInitialized();
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;

            if (m_Jumping)
            {
                AdvanceJump(deltaTime);
                return;
            }

            m_IdleTimer -= deltaTime;
            if (m_IdleTimer > 0f)
                return;

            if (NextRandom() < m_FlipChance)
                m_FacingSign = -m_FacingSign;

            StartJump(m_IdleJumpHeight, m_IdleJumpDuration, 1);
        }

        /// <summary>Bigger jump, played when a bubble is shot.</summary>
        public void PlayShotJump()
        {
            EnsureInitialized();
            StartJump(m_ShotJumpHeight, m_ShotJumpDuration, m_ShotJumpCount < 1 ? 1 : m_ShotJumpCount);
        }

        /// <summary>Back to the scene pose (used on replay).</summary>
        public void ResetPose()
        {
            EnsureInitialized();
            m_Jumping = false;
            m_FacingSign = 1f;
            ApplyPose(0f, 1f);
            ScheduleNextIdleHop();
        }

        void EnsureInitialized()
        {
            if (m_Initialized)
                return;
            m_Initialized = true;

            if (m_Target == null)
                m_Target = (RectTransform)transform;

            m_BasePosition = m_Target.anchoredPosition;
            m_BaseScale = new Vector3(Mathf.Abs(m_Target.localScale.x), m_Target.localScale.y, m_Target.localScale.z);
            m_FacingSign = m_Target.localScale.x < 0f ? -1f : 1f;
            ScheduleNextIdleHop();
        }

        void StartJump(float height, float duration, int jumps)
        {
            m_Jumping = true;
            m_JumpElapsed = 0f;
            m_JumpDuration = duration > 0.01f ? duration : 0.01f;
            m_JumpHeight = height;
            m_JumpCount = jumps;
        }

        void AdvanceJump(float deltaTime)
        {
            m_JumpElapsed += deltaTime;
            float t = m_JumpElapsed / m_JumpDuration;

            if (t >= 1f)
            {
                m_Jumping = false;
                ApplyPose(0f, 1f);
                ScheduleNextIdleHop();
                return;
            }

            // Phase inside the current hop (0 → 1), float-only (no int casts).
            float hops = t * m_JumpCount;
            float phase = hops - Mathf.Floor(hops);

            float arc = 4f * phase * (1f - phase);              // 0 → 1 → 0 parabola
            float stretch = 1f + m_Stretch * Mathf.Sin(phase * Mathf.PI);
            ApplyPose(arc * m_JumpHeight, stretch);
        }

        void ApplyPose(float heightOffset, float verticalStretch)
        {
            m_Target.anchoredPosition = new Vector2(m_BasePosition.x, m_BasePosition.y + heightOffset);
            m_Target.localScale = new Vector3(m_BaseScale.x * m_FacingSign, m_BaseScale.y * verticalStretch, m_BaseScale.z);
        }

        void ScheduleNextIdleHop()
        {
            m_IdleTimer = m_MinIdleDelay + (m_MaxIdleDelay - m_MinIdleDelay) * NextRandom();
        }

        float NextRandom()
        {
            if (m_Random == null)
                m_Random = new RandomSource(7919);
            return m_Random.Value();
        }
    }
}
