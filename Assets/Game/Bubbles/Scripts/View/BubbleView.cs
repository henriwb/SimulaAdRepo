using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// One bubble on screen (UI Image). View: sprite, size, position and animations only.
    /// Pop/drop are hand-written in Update (no DOTween) to avoid tween behavior differences in
    /// Playworks builds. Uses scaled time, so it pauses with Time.timeScale.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class BubbleView : MonoBehaviour
    {
        enum Animation
        {
            None,
            Pop,
            Drop
        }

        Image m_Image;
        RectTransform m_Rect;

        Animation m_Animation;
        float m_Elapsed;
        float m_Duration;
        float m_StartY;
        float m_DropDistance;
        Action m_OnComplete;

        // Wobble (placed bubbles hit by a shot): damped squash & stretch, deterministic.
        const float k_WobbleDuration = 0.4f;
        const float k_WobbleCycles = 3f;
        bool m_Wobbling;
        float m_WobbleElapsed;
        float m_WobbleAmplitude;

        void Awake()
        {
            EnsureCache();
        }

        void EnsureCache()
        {
            if (m_Image != null)
                return;

            m_Image = GetComponent<Image>();
            m_Rect = (RectTransform)transform;
        }

        public void Show(Sprite sprite, float diameter)
        {
            EnsureCache();
            StopAnimation();
            m_Rect.localScale = Vector3.one;
            m_Image.color = Color.white;
            m_Image.sprite = sprite;
            SetDiameter(diameter);
            gameObject.SetActive(true);
        }

        public void SetSprite(Sprite sprite)
        {
            EnsureCache();
            m_Image.sprite = sprite;
        }

        public void SetAlpha(float alpha)
        {
            EnsureCache();
            m_Image.color = new Color(1f, 1f, 1f, alpha);
        }

        public void SetDiameter(float diameter)
        {
            EnsureCache();
            m_Rect.sizeDelta = new Vector2(diameter, diameter);
        }

        public void SetLocalPosition(Vector2 position)
        {
            EnsureCache();
            m_Rect.localPosition = new Vector3(position.x, position.y, 0f);
        }

        /// <summary>Shrinks to nothing, then calls <paramref name="onComplete"/>.</summary>
        public void Pop(float duration, Action onComplete)
        {
            StartAnimation(Animation.Pop, duration, onComplete);
        }

        /// <summary>Falls by <paramref name="distance"/> while fading out, then calls <paramref name="onComplete"/>.</summary>
        public void Drop(float distance, float duration, Action onComplete)
        {
            m_DropDistance = distance;
            StartAnimation(Animation.Drop, duration, onComplete);
        }

        /// <summary>
        /// Damped squash & stretch, e.g. when a shot lands nearby. <paramref name="delay"/> makes a
        /// ripple across neighbors; ignored while popping/dropping.
        /// </summary>
        public void Wobble(float amplitude, float delay)
        {
            if (m_Animation != Animation.None)
                return;

            m_Wobbling = true;
            m_WobbleAmplitude = amplitude;
            m_WobbleElapsed = -delay;
        }

        /// <summary>Stops animations without firing their callbacks and hides the bubble.</summary>
        public void Hide()
        {
            EnsureCache();
            StopAnimation();
            gameObject.SetActive(false);
        }

        void StartAnimation(Animation animation, float duration, Action onComplete)
        {
            EnsureCache();
            StopWobble();
            m_Animation = animation;
            m_Elapsed = 0f;
            m_Duration = duration > 0.01f ? duration : 0.01f;
            m_StartY = m_Rect.localPosition.y;
            m_OnComplete = onComplete;
        }

        void StopAnimation()
        {
            m_Animation = Animation.None;
            m_OnComplete = null;
            StopWobble();
        }

        void StopWobble()
        {
            if (!m_Wobbling)
                return;
            m_Wobbling = false;
            if (m_Rect != null)
                m_Rect.localScale = Vector3.one;
        }

        void TickWobble()
        {
            m_WobbleElapsed += SafeTime.Delta;
            if (m_WobbleElapsed < 0f)
                return; // waiting for the ripple to reach this bubble

            float u = m_WobbleElapsed / k_WobbleDuration;
            if (u >= 1f)
            {
                StopWobble();
                return;
            }

            // Squash one axis while stretching the other, decaying to rest.
            float s = m_WobbleAmplitude * Mathf.Sin(u * k_WobbleCycles * 2f * Mathf.PI) * (1f - u) * (1f - u);
            m_Rect.localScale = new Vector3(1f + s, 1f - s, 1f);
        }

        void Update()
        {
            if (m_Animation == Animation.None)
            {
                if (m_Wobbling)
                    TickWobble();
                return;
            }

            m_Elapsed += SafeTime.Delta;
            float t = m_Elapsed / m_Duration;
            if (t > 1f)
                t = 1f;

            if (m_Animation == Animation.Pop)
            {
                // Small swell, then shrink (ease-in-back feel).
                float scale = t < 0.3f ? 1f + 0.2f * (t / 0.3f) : 1.2f * (1f - (t - 0.3f) / 0.7f);
                m_Rect.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                Vector3 position = m_Rect.localPosition;
                m_Rect.localPosition = new Vector3(position.x, m_StartY - m_DropDistance * t * t, 0f);
                Color color = m_Image.color;
                m_Image.color = new Color(color.r, color.g, color.b, 1f - t);
            }

            if (t < 1f)
                return;

            Action onComplete = m_OnComplete;
            StopAnimation();
            if (onComplete != null)
                onComplete();
        }
    }
}
