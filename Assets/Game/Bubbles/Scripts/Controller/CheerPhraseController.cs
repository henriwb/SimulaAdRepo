using TMPro;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Cheer phrases on progress milestones: when the bubbles left on the board drop to 80%, 50% and
    /// 20% of the starting count, shows "Good!", "Great!", "Super!". Each milestone fires once per run;
    /// if one clear crosses several, only the furthest is shown.
    /// The phrase pops in (scale overshoot), flashes, wobbles, holds, fades out and despawns after
    /// <see cref="m_Duration"/> seconds — hand-animated (no Animator/DOTween), advanced by
    /// <see cref="Tick"/> from GameLoopController.
    /// Reused from the tutorial (was driven by the key-collect event and an Animator).
    /// </summary>
    public class CheerPhraseController : MonoBehaviour
    {
        const float k_BackOvershoot = 1.70158f;

#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        [LunaPlaygroundField("Cheer phrases", 4, "Cheer Phrase Options")]
#endif
        [Tooltip("Phrase per milestone (same order as Milestones).")]
        [SerializeField] private string[] phrases = new string[] { "Good!", "Great!", "Super!" };

        [Tooltip("Fraction of the starting bubbles left that triggers each phrase (descending).")]
        [SerializeField] private float[] milestones = new float[] { 0.8f, 0.5f, 0.2f };

#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        [LunaPlaygroundField("Cheer phrase colour", 3, "Cheer Phrase Options")]
#endif
        [SerializeField] private Color cheerPhraseColour = Color.white;

        [SerializeField] private TMP_Text cheerText;

        [Header("Animation")]
        [Tooltip("Seconds on screen before despawning.")]
        [SerializeField] float m_Duration = 2f;
        [SerializeField] float m_PopDuration = 0.25f;
        [SerializeField] float m_PeakScale = 1.3f;
        [SerializeField] float m_FlashDuration = 0.4f;
        [SerializeField] float m_FlashCount = 4f;
        [SerializeField] Color m_FlashColor = Color.white;
        [Tooltip("Max Z rotation of the wobble during the flash, in degrees.")]
        [SerializeField] float m_WobbleAngle = 8f;
        [SerializeField] float m_FadeOutDuration = 0.4f;

        int m_StartCount;
        int m_NextMilestone;

        bool m_Showing;
        float m_Elapsed;
        Vector3 m_BaseScale = Vector3.one;
        bool m_BaseScaleCaptured;

        /// <summary>New run: remembers the starting bubble count and hides the text.</summary>
        public void ResetRun(int startBubbleCount)
        {
            m_StartCount = startBubbleCount;
            m_NextMilestone = 0;
            Hide();
        }

        /// <summary>Called after each clear with the bubbles still on the board.</summary>
        public void OnBubblesRemaining(int remaining)
        {
            if (m_StartCount <= 0)
                return;

            // Index loop, float math only (no casts): Playworks-safe.
            float left = remaining / (m_StartCount * 1f);
            int reached = -1;
            while (m_NextMilestone < milestones.Length && left <= milestones[m_NextMilestone])
            {
                reached = m_NextMilestone;
                m_NextMilestone++;
            }

            if (reached >= 0)
                Show(reached);
        }

        /// <summary>Advances the phrase animation; called each frame by GameLoopController.</summary>
        public void Tick(float deltaTime)
        {
            if (!m_Showing)
                return;

            m_Elapsed += deltaTime;
            if (m_Elapsed >= m_Duration)
            {
                Hide();
                return;
            }

            RectTransform rect = cheerText.rectTransform;

            // Pop-in: ease-out back from 0 to the peak, then settle to 1.
            float scale = 1f;
            if (m_Elapsed < m_PopDuration)
            {
                float u = m_Elapsed / m_PopDuration - 1f;
                float back = 1f + (k_BackOvershoot + 1f) * u * u * u + k_BackOvershoot * u * u;
                scale = back * m_PeakScale;
            }
            else if (m_Elapsed < m_PopDuration * 2f)
            {
                scale = Mathf.Lerp(m_PeakScale, 1f, (m_Elapsed - m_PopDuration) / m_PopDuration);
            }
            rect.localScale = m_BaseScale * scale;

            // Flash + wobble at the start.
            Color color = cheerPhraseColour;
            float angle = 0f;
            if (m_Elapsed < m_FlashDuration)
            {
                float f = m_Elapsed / m_FlashDuration;
                float flash = Mathf.Abs(Mathf.Sin(f * m_FlashCount * Mathf.PI));
                color = Color.Lerp(cheerPhraseColour, m_FlashColor, flash);
                angle = m_WobbleAngle * Mathf.Sin(f * m_FlashCount * Mathf.PI) * (1f - f);
            }
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Fade out at the end.
            float fadeStart = m_Duration - m_FadeOutDuration;
            if (m_Elapsed > fadeStart && m_FadeOutDuration > 0f)
                color.a *= 1f - (m_Elapsed - fadeStart) / m_FadeOutDuration;

            cheerText.color = color;
        }

        void Show(int milestoneIndex)
        {
            if (cheerText == null || phrases.Length == 0)
                return;

            CaptureBaseScale();

            int phraseIndex = milestoneIndex < phrases.Length ? milestoneIndex : phrases.Length - 1;
            cheerText.text = phrases[phraseIndex];
            cheerText.color = m_FlashColor;
            cheerText.rectTransform.localScale = Vector3.zero;
            cheerText.rectTransform.localRotation = Quaternion.identity;
            cheerText.gameObject.SetActive(true);

            m_Showing = true;
            m_Elapsed = 0f; // a new phrase restarts the animation
        }

        void Hide()
        {
            m_Showing = false;
            m_Elapsed = 0f;
            if (cheerText == null)
                return;

            CaptureBaseScale();
            cheerText.rectTransform.localScale = m_BaseScale;
            cheerText.rectTransform.localRotation = Quaternion.identity;
            cheerText.gameObject.SetActive(false);
        }

        void CaptureBaseScale()
        {
            if (m_BaseScaleCaptured || cheerText == null)
                return;
            m_BaseScale = cheerText.rectTransform.localScale;
            m_BaseScaleCaptured = true;
        }
    }
}
