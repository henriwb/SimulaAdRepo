using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Floating "+10" labels shown where bubbles pop, from a pool of TextMeshProUGUI clones.
    /// Each popup pops in, wobbles, cycles through rainbow colors, rises with an ease,
    /// fades out and returns to the pool. Clones spawn under the template's parent as its last child
    /// (drawn on top); positions arrive in world space and are converted here.
    /// View: visuals only; the controller decides positions and points.
    /// </summary>
    public class ScorePopupView : MonoBehaviour
    {
        [Tooltip("Inactive TextMeshProUGUI template (e.g. under GoalBoard). Clones spawn under the same parent, as its last child.")]
        [SerializeField] TextMeshProUGUI m_Template;
        [Tooltip("Text format; {0} is the points.")]
        [SerializeField] string m_Format = "+{0}";
        [Tooltip("Whole popup lifetime, in seconds.")]
        [SerializeField] float m_Duration = 0.9f;

        [Header("Rise")]
        [Tooltip("How far a popup rises, in the template parent's local units.")]
        [SerializeField] float m_RiseDistance = 45f;
        [SerializeField] Ease m_RiseEase = Ease.OutCubic;

        [Header("Pop-in")]
        [SerializeField] float m_StartScale = 0.4f;
        [Tooltip("Fraction of the lifetime spent scaling in.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] float m_PopInPortion = 0.2f;

        [Header("Wobble")]
        [Tooltip("Max Z rotation of the wobble, in degrees.")]
        [SerializeField] float m_WobbleAngle = 18f;
        [SerializeField] int m_WobbleVibrato = 8;

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
        readonly List<TextMeshProUGUI> m_Active = new List<TextMeshProUGUI>();

        /// <param name="worldPosition">Where the popup appears (e.g. a bubble's cell), in world space.</param>
        public void Show(Vector3 worldPosition, int points)
        {
            Transform parent = m_Template.transform.parent;
            Vector3 localPosition = parent.InverseTransformPoint(worldPosition);

            TextMeshProUGUI popup = Take();
            RectTransform rect = popup.rectTransform;
            float hueOffset = Random.value; // popups spawned together start on different colors

            popup.text = string.Format(m_Format, points);
            rect.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * m_StartScale;
            rect.SetAsLastSibling(); // newest popup draws on top of its siblings
            ApplyColor(popup, 0f, hueOffset);
            popup.gameObject.SetActive(true);
            m_Active.Add(popup);

            rect.DOScale(1f, m_Duration * m_PopInPortion).SetEase(Ease.OutBack);
            rect.DOLocalMoveY(localPosition.y + m_RiseDistance, m_Duration).SetEase(m_RiseEase);
            rect.DOPunchRotation(new Vector3(0f, 0f, m_WobbleAngle), m_Duration, m_WobbleVibrato, 1f);

            // One tween drives both hue and alpha so they never overwrite each other's color.
            float progress = 0f;
            DOTween.To(() => progress, value =>
                {
                    progress = value;
                    ApplyColor(popup, progress, hueOffset);
                }, 1f, m_Duration)
                .SetEase(Ease.Linear)
                .SetTarget(popup)
                .OnComplete(() => Release(popup));
        }

        /// <summary>Stops and hides every popup (used on reset).</summary>
        public void HideAll()
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
                Release(m_Active[i]);
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

        // Local HSV conversion: keeps this View on plain math only.
        static Color HsvToRgb(float h, float s, float v)
        {
            float scaled = h * 6f;
            int sector = Mathf.FloorToInt(scaled) % 6;
            float f = scaled - Mathf.Floor(scaled);
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);

            switch (sector)
            {
                case 0: return new Color(v, t, p);
                case 1: return new Color(q, v, p);
                case 2: return new Color(p, v, t);
                case 3: return new Color(p, q, v);
                case 4: return new Color(t, p, v);
                default: return new Color(v, p, q);
            }
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

        void Release(TextMeshProUGUI popup)
        {
            popup.rectTransform.DOKill();
            popup.DOKill(); // also kills the color tween (SetTarget(popup))
            popup.rectTransform.localRotation = Quaternion.identity;
            popup.gameObject.SetActive(false);

            m_Active.Remove(popup);
            if (!m_Pool.Contains(popup))
                m_Pool.Add(popup);
        }
    }
}
