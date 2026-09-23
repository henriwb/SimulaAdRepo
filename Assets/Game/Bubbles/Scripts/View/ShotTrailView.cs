using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Simulated trail for the shot bubble: exact copies of the bubble template, created as siblings
    /// of the flying bubble (same parent) and kept right above it in the hierarchy, so they draw just
    /// behind it. Copy i shows where the shot was i × delay seconds ago (time-delayed, interpolated),
    /// each one smaller and more transparent. After landing, the copies catch up to the landing spot
    /// while fading out. Deterministic, hand-animated (clamped scaled time), advanced by
    /// <see cref="Tick"/> from GameLoopController. View: visuals only.
    /// </summary>
    public class ShotTrailView : MonoBehaviour
    {
        [Tooltip("Inactive bubble template (e.g. GoalBoard/BubblePrefab); copies take the shot's sprite and size.")]
        [SerializeField] Image m_Template;

        [SerializeField] int m_Count = 5;
        [Tooltip("Delay between consecutive copies, in seconds.")]
        [SerializeField] float m_Delay = 0.025f;
        [Tooltip("Scale of the last copy (the first is close to full size).")]
        [SerializeField] float m_MinScale = 0.35f;
        [Tooltip("Alpha of the first copy (fades down along the trail).")]
        [Range(0f, 1f)]
        [SerializeField] float m_MaxAlpha = 0.55f;
        [Tooltip("Seconds for the trail to fade out after the shot lands.")]
        [SerializeField] float m_FadeOutDuration = 0.18f;

        const int k_MaxSamples = 128;

        readonly List<Image> m_Copies = new List<Image>();

        // Time-stamped positions of the shot (in its parent's local space).
        readonly List<float> m_SampleTimes = new List<float>();
        readonly List<Vector3> m_SamplePositions = new List<Vector3>();

        BubbleView m_Shot;           // current (or last) shot view
        RectTransform m_ShotParent;
        float m_Time;
        float m_Visibility;

        /// <param name="flying">The bubble in the air, or null when nothing is flying.</param>
        public void Tick(BubbleView flying, Sprite sprite, float deltaTime)
        {
            if (m_Template == null)
                return;

            m_Time += deltaTime;

            if (flying != null)
            {
                if (flying != m_Shot || !m_Shot.gameObject.activeSelf || m_Visibility <= 0f)
                    BeginShot(flying, sprite);

                RecordSample(flying.transform.localPosition);
                m_Visibility = 1f;
            }
            else if (m_Visibility > 0f)
            {
                m_Visibility -= deltaTime / (m_FadeOutDuration > 0.01f ? m_FadeOutDuration : 0.01f);
            }

            Layout(flying);
        }

        /// <summary>Hides the trail immediately (used on reset).</summary>
        public void HideAll()
        {
            m_Visibility = 0f;
            m_Shot = null;
            m_SampleTimes.Clear();
            m_SamplePositions.Clear();
            for (int i = 0; i < m_Copies.Count; i++)
                m_Copies[i].gameObject.SetActive(false);
        }

        void BeginShot(BubbleView flying, Sprite sprite)
        {
            m_Shot = flying;
            m_SampleTimes.Clear();
            m_SamplePositions.Clear();
            EnsureCopies((RectTransform)flying.transform.parent);

            Vector2 size = ((RectTransform)flying.transform).sizeDelta;
            for (int i = 0; i < m_Copies.Count; i++)
            {
                m_Copies[i].sprite = sprite;
                m_Copies[i].rectTransform.sizeDelta = size;
            }
        }

        void RecordSample(Vector3 position)
        {
            m_SampleTimes.Add(m_Time);
            m_SamplePositions.Add(position);
            if (m_SampleTimes.Count > k_MaxSamples)
            {
                m_SampleTimes.RemoveAt(0);
                m_SamplePositions.RemoveAt(0);
            }
        }

        void Layout(BubbleView flying)
        {
            if (m_Visibility <= 0f || m_SampleTimes.Count == 0)
            {
                for (int i = 0; i < m_Copies.Count; i++)
                    m_Copies[i].gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < m_Copies.Count; i++)
            {
                Image copy = m_Copies[i];
                float targetTime = m_Time - (i + 1) * m_Delay;

                Vector3 position;
                if (!TrySampleAt(targetTime, out position))
                {
                    copy.gameObject.SetActive(false); // the shot wasn't there yet
                    continue;
                }

                float along = (i + 1) / (m_Copies.Count * 1f); // 0 → 1 from head to tail
                float scale = Mathf.Lerp(1f, m_MinScale, along);
                float alpha = m_MaxAlpha * (1f - along * 0.85f) * m_Visibility;

                RectTransform rect = copy.rectTransform;
                rect.localPosition = position;
                rect.localScale = new Vector3(scale, scale, 1f);
                copy.color = new Color(1f, 1f, 1f, alpha);
                copy.gameObject.SetActive(true);
            }

            // Siblings right above the shot in the hierarchy (drawn just behind it), tail first.
            if (flying != null)
            {
                for (int i = m_Copies.Count - 1; i >= 0; i--)
                    m_Copies[i].transform.SetSiblingIndex(flying.transform.GetSiblingIndex());
            }
        }

        /// <summary>Shot position at <paramref name="time"/>, interpolated between samples.</summary>
        bool TrySampleAt(float time, out Vector3 position)
        {
            position = Vector3.zero;
            int last = m_SampleTimes.Count - 1;
            if (last < 0 || time < m_SampleTimes[0])
                return false;

            if (time >= m_SampleTimes[last])
            {
                position = m_SamplePositions[last];
                return true;
            }

            for (int i = last; i > 0; i--)
            {
                if (time < m_SampleTimes[i - 1])
                    continue;

                float span = m_SampleTimes[i] - m_SampleTimes[i - 1];
                float u = span > 0f ? (time - m_SampleTimes[i - 1]) / span : 1f;
                position = Vector3.Lerp(m_SamplePositions[i - 1], m_SamplePositions[i], u);
                return true;
            }

            position = m_SamplePositions[0];
            return true;
        }

        void EnsureCopies(RectTransform parent)
        {
            while (m_Copies.Count < m_Count)
            {
                Image created = Instantiate(m_Template, parent);
                created.raycastTarget = false;
                created.gameObject.SetActive(false);

                // Exact copy of the bubble template, display-only: drop its BubbleView behaviour.
                BubbleView copiedView = created.GetComponent<BubbleView>();
                if (copiedView != null)
                    Destroy(copiedView);

                m_Copies.Add(created);
            }

            // Shots may come from a different parent than the last time; keep copies next to the shot.
            if (m_ShotParent != parent)
            {
                for (int i = 0; i < m_Copies.Count; i++)
                    m_Copies[i].transform.SetParent(parent, false);
                m_ShotParent = parent;
            }
        }
    }
}
