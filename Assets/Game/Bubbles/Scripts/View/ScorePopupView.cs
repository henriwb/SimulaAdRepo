using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Floating "+10" labels shown where bubbles pop, from a pool of TextMeshProUGUI clones.
    /// Each popup pops in, wobbles, cycles through rainbow colors, rises with an ease,
    /// fades out and returns to the pool. Clones spawn under the template's parent as its last child
    /// (drawn on top); positions arrive in world space and are converted here.
    /// Hand-written animation (no DOTween), advanced by <see cref="Tick"/> from GameLoopController:
    /// this component may sit on the inactive template, where Update never runs.
    /// View: visuals only; the controller decides positions and points.
    /// </summary>
    public class ScorePopupView : MonoBehaviour
    {
        const float k_BackOvershoot = 1.70158f;

        [Tooltip("Inactive TextMeshProUGUI template (e.g. under GoalBoard). Clones spawn under the same parent, as its last child.")]
        [SerializeField] TextMeshProUGUI m_Template;
        [Tooltip("Text format; {0} is replaced by the points.")]
        [SerializeField] string m_Format = "+{0}";
        [Tooltip("Whole popup lifetime, in seconds.")]
        [SerializeField] float m_Duration = 0.9f;

        [Header("Rise (ease-out cubic)")]
        [Tooltip("How far a popup rises, in the template parent's local units.")]
        [SerializeField] float m_RiseDistance = 45f;

        [Header("Pop-in (ease-out back)")]
        [SerializeField] float m_StartScale = 0.4f;
        [Tooltip("Fraction of the lifetime spent scaling in.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] float m_PopInPortion = 0.2f;

        [Header("Wobble")]
        [Tooltip("Max Z rotation of the wobble, in degrees (damps to 0).")]
        [SerializeField] float m_WobbleAngle = 18f;
        [Tooltip("Half-swings over the lifetime.")]
        [SerializeField] float m_WobbleSwings = 8f;

        [Header("Rainbow")]
        [Tooltip("Full hue cycles over the popup lifetime.")]
        [SerializeField] float m_HueCycles = 2f;
        [Range(0f, 1f)]
        [SerializeField] float m_Saturation = 0.85f;

        [Header("Fade")]
        [Tooltip("Lifetime fraction where fading starts.")]
        [Range(0f, 0.95f)]
        [SerializeField] float m_FadeStart = 0.55f;

        readonly List<TextMeshProUGUI> m_Pool = new List<TextMeshProUGUI>();

        // Active popups: parallel lists (index i = one popup).
        readonly List<TextMeshProUGUI> m_Active = new List<TextMeshProUGUI>();
        readonly List<float> m_Elapsed = new List<float>();
        readonly List<float> m_StartY = new List<float>();
        readonly List<float> m_HueOffset = new List<float>();

        // Own generator (UnityEngine.Random is missing in Playworks builds); created on first use.
        RandomSource m_Random;

        /// <param name="worldPosition">Where the popup appears (e.g. a bubble's cell), in world space.</param>
        public void Show(Vector3 worldPosition, int points)
        {
            Transform parent = m_Template.transform.parent;
            Vector3 localPosition = parent.InverseTransformPoint(worldPosition);

            if (m_Random == null)
                m_Random = new RandomSource(104729);
            float hueOffset = m_Random.Value(); // popups spawned together start on different colors

            TextMeshProUGUI popup = Take();
            RectTransform rect = popup.rectTransform;

            // Plain int → string (Bridge.toString): String.Format's number path is broken in Playworks builds.
            popup.text = m_Format.Replace("{0}", points.ToString());
            rect.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(m_StartScale, m_StartScale, 1f);
            rect.SetAsLastSibling(); // newest popup draws on top of its siblings
            ApplyColor(popup, 0f, hueOffset);
            popup.gameObject.SetActive(true);

            m_Active.Add(popup);
            m_Elapsed.Add(0f);
            m_StartY.Add(localPosition.y);
            m_HueOffset.Add(hueOffset);
        }

        /// <summary>Advances every popup; called each frame by GameLoopController.</summary>
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

                TextMeshProUGUI popup = m_Active[i];
                RectTransform rect = popup.rectTransform;

                // Pop-in: ease-out back from start scale to 1.
                float popIn = t / m_PopInPortion;
                float scale = 1f;
                if (popIn < 1f)
                {
                    float u = popIn - 1f;
                    float back = 1f + (k_BackOvershoot + 1f) * u * u * u + k_BackOvershoot * u * u;
                    scale = m_StartScale + (1f - m_StartScale) * back;
                }
                rect.localScale = new Vector3(scale, scale, 1f);

                // Rise: ease-out cubic.
                float inverse = 1f - t;
                float rise = 1f - inverse * inverse * inverse;
                Vector3 position = rect.localPosition;
                rect.localPosition = new Vector3(position.x, m_StartY[i] + m_RiseDistance * rise, 0f);

                // Wobble: damped swing.
                float angle = m_WobbleAngle * Mathf.Sin(t * m_WobbleSwings * Mathf.PI) * inverse;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);

                ApplyColor(popup, t, m_HueOffset[i]);
            }
        }

        /// <summary>Stops and hides every popup (used on reset).</summary>
        public void HideAll()
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
                ReleaseAt(i);
        }

        void ApplyColor(TextMeshProUGUI popup, float progress, float hueOffset)
        {
            float hue = Mathf.Repeat(hueOffset + progress * m_HueCycles, 1f);

            float alpha = 1f;
            if (progress > m_FadeStart)
            {
                float fade = Mathf.InverseLerp(m_FadeStart, 1f, progress);
                alpha = 1f - fade * fade; // ease-in fade
            }

            Color color = HsvToRgb(hue, m_Saturation, 1f);
            color.a = alpha;
            popup.color = color;
        }

        // Local HSV conversion, float-only: no float→int casts / FloorToInt
        // (they compile to Bridge.Int.clip32, missing in Playworks builds).
        static Color HsvToRgb(float h, float s, float v)
        {
            float scaled = Mathf.Repeat(h, 1f) * 6f;
            float f = scaled - Mathf.Floor(scaled);
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);

            if (scaled < 1f) return new Color(v, t, p);
            if (scaled < 2f) return new Color(q, v, p);
            if (scaled < 3f) return new Color(p, v, t);
            if (scaled < 4f) return new Color(p, q, v);
            if (scaled < 5f) return new Color(t, p, v);
            return new Color(v, p, q);
        }

        TextMeshProUGUI Take()
        {
            if (m_Pool.Count > 0)
            {
                TextMeshProUGUI pooled = m_Pool[m_Pool.Count - 1];
                m_Pool.RemoveAt(m_Pool.Count - 1);
                return pooled;
            }

            TextMeshProUGUI created = Instantiate(m_Template, m_Template.transform.parent);
            created.raycastTarget = false;

            // If this component sits on the template, the clone copied it too; clones only display.
            ScorePopupView copiedView = created.GetComponent<ScorePopupView>();
            if (copiedView != null && copiedView != this)
                Destroy(copiedView);

            return created;
        }

        void ReleaseAt(int index)
        {
            TextMeshProUGUI popup = m_Active[index];
            popup.rectTransform.localRotation = Quaternion.identity;
            popup.gameObject.SetActive(false);

            m_Active.RemoveAt(index);
            m_Elapsed.RemoveAt(index);
            m_StartY.RemoveAt(index);
            m_HueOffset.RemoveAt(index);

            if (!m_Pool.Contains(popup))
                m_Pool.Add(popup);
        }
    }
}
