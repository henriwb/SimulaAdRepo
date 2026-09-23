using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// "Juicy" coin feedback: every cleared bubble launches a UI coin that flies in an arc to the
    /// score coin icon (staggered, accelerating, spinning, pulsing), and each arrival punches the icon.
    /// UI rewrite of the tutorial's CoinEffectManager (which was 3D, a static singleton and event-driven).
    /// Hand-written animation (no DOTween, no UnityEngine.Random, no casts — see README), pooled,
    /// advanced by <see cref="Tick"/> from GameLoopController. View: visuals only.
    /// </summary>
    public class CoinEffectManager : MonoBehaviour
    {
        [Tooltip("Inactive coin Image used as the template (e.g. under GoalBoard, like PointsUIText); clones spawn as its siblings, drawn last.")]
        [SerializeField] Image m_CoinTemplate;
        [Tooltip("Where coins fly to (e.g. the score coin icon). It gets a scale punch on each arrival.")]
        [SerializeField] RectTransform m_Target;

        [Header("Flight")]
        [SerializeField] float m_FlightDuration = 0.65f;
        [Tooltip("Delay between consecutive coins of one burst.")]
        [SerializeField] float m_Stagger = 0.04f;
        [Tooltip("Arc height above the straight line, in the coin layer's units.")]
        [SerializeField] float m_ArcHeight = 120f;
        [Tooltip("Random sideways spread of the arc control point.")]
        [SerializeField] float m_ArcSpread = 80f;
        [Tooltip("Full turns a coin spins during its flight.")]
        [SerializeField] float m_Spins = 1.5f;
        [Tooltip("Max coins in flight; extra bursts are skipped to keep it readable and cheap.")]
        [SerializeField] int m_MaxCoins = 24;

        [Header("Scale")]
        [Tooltip("Overall coin size multiplier (applied on top of the start/peak/end scales).")]
        [SerializeField] float m_CoinSize = 0.5f;
        [SerializeField] float m_StartScale = 0.4f;
        [SerializeField] float m_PeakScale = 1.25f;
        [SerializeField] float m_EndScale = 0.7f;

        [Header("Target punch")]
        [SerializeField] float m_PunchStrength = 0.3f;
        [SerializeField] float m_PunchDuration = 0.18f;

        readonly List<Image> m_Pool = new List<Image>();

        // Active coins: parallel lists (index i = one coin).
        readonly List<Image> m_Active = new List<Image>();
        readonly List<float> m_Elapsed = new List<float>(); // starts negative = staggered delay
        readonly List<Vector2> m_From = new List<Vector2>();
        readonly List<Vector2> m_Control = new List<Vector2>();

        Vector3 m_TargetBaseScale = Vector3.one;
        bool m_TargetScaleCaptured;
        float m_PunchElapsed = -1f;
        int m_BurstIndex;

        RandomSource m_Random;

        /// <summary>Starts a new burst: the next coins launched get increasing delays.</summary>
        public void BeginBurst()
        {
            m_BurstIndex = 0;
        }

        /// <summary>Launches one coin from a world position (e.g. a cleared bubble's cell).</summary>
        public void Launch(Vector3 worldFrom)
        {
            if (m_CoinTemplate == null || m_Target == null || m_Active.Count >= m_MaxCoins)
                return;

            if (m_Random == null)
                m_Random = new RandomSource(4201);
            CaptureTargetScale();

            // Same pattern as ScorePopupView (proven in the web build): positions in the template's parent space.
            Transform layer = m_CoinTemplate.transform.parent;
            Vector2 from = layer.InverseTransformPoint(worldFrom);
            Vector2 to = layer.InverseTransformPoint(m_Target.position);

            // Quadratic Bézier control point: above the midpoint, randomly pushed sideways.
            Vector2 middle = (from + to) * 0.5f;
            float side = m_Random.Range(-m_ArcSpread, m_ArcSpread);
            Vector2 control = new Vector2(middle.x + side, Mathf.Max(from.y, to.y) + m_ArcHeight);

            // Like ScorePopupView.Show: clone = sibling of the template, moved last, active right away.
            // During its stagger delay it just waits at the bubble spot (small), then flies.
            Image coin = Take();
            RectTransform rect = coin.rectTransform;
            rect.localPosition = new Vector3(from.x, from.y, 0f);
            float spawnScale = m_StartScale * m_CoinSize;
            rect.localScale = new Vector3(spawnScale, spawnScale, 1f);
            rect.localRotation = Quaternion.identity;
            rect.SetAsLastSibling();
            coin.gameObject.SetActive(true);

            m_Active.Add(coin);
            m_Elapsed.Add(-m_Stagger * m_BurstIndex);
            m_From.Add(from);
            m_Control.Add(control);
            m_BurstIndex++;
        }

        /// <summary>Advances coins and the target punch; called each frame by GameLoopController.</summary>
        public void Tick(float deltaTime)
        {
            float duration = m_FlightDuration > 0.01f ? m_FlightDuration : 0.01f;
            Transform layer = m_CoinTemplate != null ? m_CoinTemplate.transform.parent : null;
            // Same target for every coin this frame; follows the target if it moves (layout/orientation).
            Vector2 to = m_Active.Count > 0 && layer != null ? (Vector2)layer.InverseTransformPoint(m_Target.position) : Vector2.zero;

            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                float elapsed = m_Elapsed[i] + deltaTime;
                m_Elapsed[i] = elapsed;

                Image coin = m_Active[i];
                if (elapsed < 0f)
                    continue; // waiting for its stagger slot: stays at the bubble spot

                float t = elapsed / duration;
                if (t >= 1f)
                {
                    ReleaseAt(i);
                    StartPunch();
                    continue;
                }

                // Ease-in: coins start lazily and rush into the counter.
                float eased = t * t;
                Vector2 a = Vector2.Lerp(m_From[i], m_Control[i], eased); // 'to' follows the target if it moves
                Vector2 b = Vector2.Lerp(m_Control[i], to, eased);
                Vector2 position = Vector2.Lerp(a, b, eased);

                // Scale: grow to a peak, then shrink into the icon.
                float scale = (t < 0.35f
                    ? Mathf.Lerp(m_StartScale, m_PeakScale, t / 0.35f)
                    : Mathf.Lerp(m_PeakScale, m_EndScale, (t - 0.35f) / 0.65f)) * m_CoinSize;

                RectTransform rect = coin.rectTransform;
                rect.localPosition = new Vector3(position.x, position.y, 0f);
                rect.localScale = new Vector3(scale, scale, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, -360f * m_Spins * t);
            }

            TickPunch(deltaTime);
        }

        /// <summary>Stops every coin and the punch (used on reset).</summary>
        public void HideAll()
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
                ReleaseAt(i);

            m_PunchElapsed = -1f;
            if (m_TargetScaleCaptured)
                m_Target.localScale = m_TargetBaseScale;
            m_BurstIndex = 0;
        }

        void StartPunch()
        {
            m_PunchElapsed = 0f;
        }

        void TickPunch(float deltaTime)
        {
            if (m_PunchElapsed < 0f || !m_TargetScaleCaptured)
                return;

            m_PunchElapsed += deltaTime;
            float u = m_PunchElapsed / (m_PunchDuration > 0.01f ? m_PunchDuration : 0.01f);
            if (u >= 1f)
            {
                m_PunchElapsed = -1f;
                m_Target.localScale = m_TargetBaseScale;
                return;
            }

            float punch = 1f + m_PunchStrength * Mathf.Sin(u * Mathf.PI);
            m_Target.localScale = m_TargetBaseScale * punch;
        }

        void CaptureTargetScale()
        {
            if (m_TargetScaleCaptured)
                return;
            m_TargetBaseScale = m_Target.localScale;
            m_TargetScaleCaptured = true;
        }

        Image Take()
        {
            if (m_Pool.Count > 0)
            {
                Image pooled = m_Pool[m_Pool.Count - 1];
                m_Pool.RemoveAt(m_Pool.Count - 1);
                return pooled;
            }

            Image created = Instantiate(m_CoinTemplate, m_CoinTemplate.transform.parent);
            created.raycastTarget = false;
            return created;
        }

        void ReleaseAt(int index)
        {
            Image coin = m_Active[index];
            coin.gameObject.SetActive(false);

            m_Active.RemoveAt(index);
            m_Elapsed.RemoveAt(index);
            m_From.RemoveAt(index);
            m_Control.RemoveAt(index);

            if (!m_Pool.Contains(coin))
                m_Pool.Add(coin);
        }
    }
}
