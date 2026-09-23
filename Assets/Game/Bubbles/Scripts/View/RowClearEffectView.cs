using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// "Line clear" feedback for special bubbles: when one pops, two copies spawn exactly on it and
    /// sweep along its row — one to the left edge, one to the right — accelerating, stretching in the
    /// direction of travel and fading out. Deterministic, hand-animated (no DOTween), pooled, advanced
    /// by <see cref="Tick"/> from GameLoopController. Same spawning pattern as the coin effect:
    /// clones are siblings of the template, kept last so they draw above the bubbles.
    /// View: visuals only.
    /// </summary>
    public class RowClearEffectView : MonoBehaviour
    {
        [Tooltip("Inactive Image used as the template (e.g. the special bubble sprite), sibling of the bubbles.")]
        [SerializeField] Image m_Template;
        [SerializeField] float m_Duration = 0.4f;
        [Tooltip("Stretch along the travel direction at full speed (1 = none).")]
        [SerializeField] float m_MaxStretch = 1.8f;
        [Tooltip("Size of each sweeper relative to the template's size.")]
        [SerializeField] float m_Scale = 1f;
        [Tooltip("Full turns over the lifetime (rolls in the travel direction).")]
        [SerializeField] float m_Spins = 1f;
        [Tooltip("Lifetime fraction where fading starts.")]
        [Range(0f, 0.95f)]
        [SerializeField] float m_FadeStart = 0.6f;

        readonly List<Image> m_Pool = new List<Image>();

        // Active sweepers: parallel lists (index i = one sweeper).
        readonly List<Image> m_Active = new List<Image>();
        readonly List<float> m_Elapsed = new List<float>();
        readonly List<Vector2> m_From = new List<Vector2>();
        readonly List<Vector2> m_To = new List<Vector2>();
        readonly List<float> m_Facing = new List<float>(); // +1 right, -1 left (mirrors the sprite)

        /// <summary>
        /// Two sweepers from a popped special bubble to both ends of its row (all positions in world space).
        /// </summary>
        public void Play(Vector3 worldCenter, Vector3 worldLeftEdge, Vector3 worldRightEdge)
        {
            if (m_Template == null)
                return;

            Transform layer = m_Template.transform.parent;
            Vector2 center = layer.InverseTransformPoint(worldCenter);
            Spawn(center, layer.InverseTransformPoint(worldLeftEdge), -1f);
            Spawn(center, layer.InverseTransformPoint(worldRightEdge), 1f);
        }

        /// <summary>Advances every sweeper; called each frame by GameLoopController.</summary>
        public void Tick(float deltaTime)
        {
            float duration = m_Duration > 0.01f ? m_Duration : 0.01f;

            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                float elapsed = m_Elapsed[i] + deltaTime;
                m_Elapsed[i] = elapsed;
                float t = elapsed / duration;
                if (t >= 1f)
                {
                    ReleaseAt(i);
                    continue;
                }

                Image sweeper = m_Active[i];
                RectTransform rect = sweeper.rectTransform;
                rect.SetAsLastSibling(); // stay above bubbles spawned after it

                // Accelerate: start at the bubble, rush to the edge.
                float eased = t * t;
                Vector2 position = Vector2.Lerp(m_From[i], m_To[i], eased);
                rect.localPosition = new Vector3(position.x, position.y, 0f);

                // One full turn over its lifetime, rolling in its travel direction.
                rect.localRotation = Quaternion.Euler(0f, 0f, -m_Spins * 360f * t * m_Facing[i]);

                // Stretch with speed (derivative of t² peaks at the end), squash vertically.
                float stretch = Mathf.Lerp(1f, m_MaxStretch, t);
                rect.localScale = new Vector3(m_Scale * stretch * m_Facing[i], m_Scale / Mathf.Sqrt(stretch), 1f);

                // Keep the template's own color; only fade its alpha.
                float fade = 1f;
                if (t > m_FadeStart)
                    fade = 1f - (t - m_FadeStart) / (1f - m_FadeStart);
                Color color = m_Template.color;
                color.a *= fade;
                sweeper.color = color;
            }
        }

        /// <summary>Stops every sweeper (used on reset).</summary>
        public void HideAll()
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
                ReleaseAt(i);
        }

        void Spawn(Vector2 from, Vector2 to, float facing)
        {
            Image sweeper = Take();
            RectTransform rect = sweeper.rectTransform;
            rect.localPosition = new Vector3(from.x, from.y, 0f);
            rect.localScale = new Vector3(m_Scale * facing, m_Scale, 1f);
            rect.localRotation = Quaternion.identity;
            sweeper.color = m_Template.color; // original template color
            rect.SetAsLastSibling();
            sweeper.gameObject.SetActive(true);

            m_Active.Add(sweeper);
            m_Elapsed.Add(0f);
            m_From.Add(from);
            m_To.Add(new Vector2(to.x, from.y)); // stay on the row's line
            m_Facing.Add(facing);
        }

        Image Take()
        {
            if (m_Pool.Count > 0)
            {
                Image pooled = m_Pool[m_Pool.Count - 1];
                m_Pool.RemoveAt(m_Pool.Count - 1);
                return pooled;
            }

            Image created = Instantiate(m_Template, m_Template.transform.parent);
            created.raycastTarget = false;
            return created;
        }

        void ReleaseAt(int index)
        {
            Image sweeper = m_Active[index];
            sweeper.gameObject.SetActive(false);

            m_Active.RemoveAt(index);
            m_Elapsed.RemoveAt(index);
            m_From.RemoveAt(index);
            m_To.RemoveAt(index);
            m_Facing.RemoveAt(index);

            if (!m_Pool.Contains(sweeper))
                m_Pool.Add(sweeper);
        }
    }
}
