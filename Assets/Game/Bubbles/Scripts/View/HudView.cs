using TMPro;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Score, current/next bubble indicators and "Game Clear" label. View: visuals only.
    /// On a shot, the "next" indicator jumps in an arc onto the "current" slot and takes its place,
    /// then a fresh "next" pops in (hand-animated, clamped scaled time — pauses with the game).
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_ScoreText;
        [SerializeField] BubbleView m_CurrentBubble;
        [SerializeField] BubbleView m_NextBubble;
        [SerializeField] GameObject m_GameClearLabel;

        [Header("Next → current jump")]
        [SerializeField] float m_AdvanceDuration = 0.28f;
        [Tooltip("Arc height of the jump, in the indicators' parent units.")]
        [SerializeField] float m_AdvanceArcHeight = 45f;
        [SerializeField] float m_NextPopInDuration = 0.18f;

        Sprite m_CurrentSprite;
        Sprite m_NextSprite;

        bool m_Advancing;
        float m_AdvanceElapsed;
        float m_PopInElapsed = -1f;

        Vector3 m_NextHome;
        Vector3 m_NextBaseScale = Vector3.one;
        bool m_HomeCaptured;
        float m_SizeRatio = 1f; // current slot's on-screen size / next indicator's, measured per jump

        /// <summary>Where a shot starts, in world space.</summary>
        public Vector3 CurrentBubbleWorldPosition => m_CurrentBubble.transform.position;

        public void SetScore(int score)
        {
            m_ScoreText.text = score.ToString();
        }

        /// <summary>Shows the queue. During the jump animation the sprites are applied when it ends.</summary>
        public void SetBubbles(Sprite current, Sprite next)
        {
            m_CurrentSprite = current;
            m_NextSprite = next;
            if (m_Advancing)
                return;

            m_CurrentBubble.SetSprite(current);
            m_NextBubble.SetSprite(next);
        }

        /// <summary>
        /// Shot fired: the current slot empties, the next indicator jumps onto it, then the new
        /// queue (<paramref name="newCurrent"/>, <paramref name="newNext"/>) is shown.
        /// </summary>
        public void PlayAdvance(Sprite newCurrent, Sprite newNext)
        {
            CaptureHome();
            m_CurrentSprite = newCurrent;
            m_NextSprite = newNext;

            RectTransform next = (RectTransform)m_NextBubble.transform;
            RectTransform current = (RectTransform)m_CurrentBubble.transform;
            next.localScale = m_NextBaseScale;
            float nextSize = next.rect.width * next.lossyScale.x;
            float currentSize = current.rect.width * current.lossyScale.x;
            m_SizeRatio = nextSize > 0f ? currentSize / nextSize : 1f;

            m_CurrentBubble.SetAlpha(0f); // it was just fired
            m_Advancing = true;
            m_AdvanceElapsed = 0f;
            m_PopInElapsed = -1f;
        }

        /// <summary>Snaps indicators to rest (used on reset).</summary>
        public void ResetIndicators()
        {
            CaptureHome();
            m_Advancing = false;
            m_PopInElapsed = -1f;

            RectTransform next = (RectTransform)m_NextBubble.transform;
            next.localPosition = m_NextHome;
            next.localScale = m_NextBaseScale;
            m_CurrentBubble.SetAlpha(1f);
            m_NextBubble.SetAlpha(1f);
        }

        public void SetGameClearVisible(bool visible)
        {
            if (m_GameClearLabel != null)
                m_GameClearLabel.SetActive(visible);
        }

        void Update()
        {
            float deltaTime = SafeTime.Delta;
            RectTransform next = (RectTransform)m_NextBubble.transform;

            if (m_Advancing)
            {
                m_AdvanceElapsed += deltaTime;
                float t = m_AdvanceElapsed / (m_AdvanceDuration > 0.01f ? m_AdvanceDuration : 0.01f);

                if (t >= 1f)
                {
                    // Landed on the current slot: hand over, send "next" home and pop the new one in.
                    m_Advancing = false;
                    m_CurrentBubble.SetSprite(m_CurrentSprite);
                    m_CurrentBubble.SetAlpha(1f);

                    next.localPosition = m_NextHome;
                    m_NextBubble.SetSprite(m_NextSprite);
                    next.localScale = Vector3.zero;
                    m_PopInElapsed = 0f;
                }
                else
                {
                    Vector3 target = next.parent.InverseTransformPoint(m_CurrentBubble.transform.position);
                    float eased = t * t * (3f - 2f * t); // smoothstep
                    Vector3 position = Vector3.Lerp(m_NextHome, target, eased);
                    position.y += m_AdvanceArcHeight * 4f * t * (1f - t);
                    next.localPosition = position;

                    // Grow to the current slot's on-screen size while travelling (absolute, no accumulation).
                    next.localScale = m_NextBaseScale * Mathf.Lerp(1f, m_SizeRatio, eased);
                }
            }

            if (m_PopInElapsed >= 0f)
            {
                m_PopInElapsed += deltaTime;
                float u = m_PopInElapsed / (m_NextPopInDuration > 0.01f ? m_NextPopInDuration : 0.01f);
                if (u >= 1f)
                {
                    next.localScale = m_NextBaseScale;
                    m_PopInElapsed = -1f;
                }
                else
                {
                    // Ease-out back: small overshoot.
                    float v = u - 1f;
                    float back = 1f + 2.70158f * v * v * v + 1.70158f * v * v;
                    next.localScale = m_NextBaseScale * back;
                }
            }
        }

        void CaptureHome()
        {
            if (m_HomeCaptured)
                return;

            RectTransform next = (RectTransform)m_NextBubble.transform;
            m_NextHome = next.localPosition;
            m_NextBaseScale = next.localScale;
            m_HomeCaptured = true;
        }
    }
}
