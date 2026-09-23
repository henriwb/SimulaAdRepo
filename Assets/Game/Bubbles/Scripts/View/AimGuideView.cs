using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Draws the predicted shot path as pooled dots, plus a semi-transparent "ghost" bubble
    /// on the predicted landing cell. Put it on the puzzle area (same local space as the board).
    /// View: visuals only; the path comes from <see cref="TrajectoryController"/>.
    /// </summary>
    public class AimGuideView : MonoBehaviour
    {
        [SerializeField] BoardView m_BoardView;
        [Tooltip("Small dot Image under the puzzle area, left inactive; cloned for each dot.")]
        [SerializeField] Image m_DotTemplate;
        [Tooltip("The board's bubble template; cloned once for the ghost bubble.")]
        [SerializeField] BubbleView m_BubbleTemplate;

        [Tooltip("Distance between dots, in bubble diameters.")]
        [SerializeField] float m_DotSpacing = 0.6f;
        [Tooltip("Dot size, in bubble diameters.")]
        [SerializeField] float m_DotSize = 0.25f;
        [Range(0f, 1f)]
        [SerializeField] float m_GhostAlpha = 0.45f;

        [Header("Debug")]
        [Tooltip("Show the ghost bubble on the predicted landing cell.")]
        [SerializeField] bool m_ShowGhostBubble = true;

        readonly List<Image> m_Dots = new List<Image>();
        BubbleView m_Ghost;
        int m_ActiveDots;

        /// <param name="points">Origin, bounce points and end point, in puzzle area local space.</param>
        /// <param name="landingCell">(-1, -1) hides the ghost.</param>
        public void Show(List<Vector2> points, Vector2Int landingCell, int color)
        {
            float diameter = m_BoardView.Diameter;
            float spacing = Mathf.Max(1f, m_DotSpacing * diameter);
            float dotSize = m_DotSize * diameter;

            m_ActiveDots = 0;
            float carry = spacing; // skip a dot on top of the shooter
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 from = points[i - 1];
                Vector2 segment = points[i] - from;
                float length = segment.magnitude;
                if (length <= 0f)
                    continue;

                Vector2 direction = segment / length;
                float distance = carry;
                while (distance <= length)
                {
                    PlaceDot(from + direction * distance, dotSize);
                    distance += spacing;
                }
                carry = distance - length;
            }

            for (int i = m_ActiveDots; i < m_Dots.Count; i++)
                m_Dots[i].gameObject.SetActive(false);

            UpdateGhost(landingCell, color, diameter);
        }

        public void Hide()
        {
            for (int i = 0; i < m_Dots.Count; i++)
                m_Dots[i].gameObject.SetActive(false);
            m_ActiveDots = 0;

            if (m_Ghost != null)
                m_Ghost.Hide();
        }

        void PlaceDot(Vector2 position, float size)
        {
            Image dot;
            if (m_ActiveDots < m_Dots.Count)
            {
                dot = m_Dots[m_ActiveDots];
            }
            else
            {
                dot = Instantiate(m_DotTemplate, transform);
                dot.raycastTarget = false;
                m_Dots.Add(dot);
            }

            RectTransform rect = dot.rectTransform;
            rect.sizeDelta = new Vector2(size, size);
            rect.localPosition = new Vector3(position.x, position.y, 0f);
            rect.localScale = Vector3.one;
            dot.gameObject.SetActive(true);
            m_ActiveDots++;
        }

        void UpdateGhost(Vector2Int landingCell, int color, float diameter)
        {
            if (!m_ShowGhostBubble || landingCell.x < 0)
            {
                if (m_Ghost != null)
                    m_Ghost.Hide();
                return;
            }

            if (m_Ghost == null)
            {
                m_Ghost = Instantiate(m_BubbleTemplate, transform);
                m_Ghost.transform.localScale = Vector3.one;
                Image ghostImage = m_Ghost.GetComponent<Image>();
                ghostImage.raycastTarget = false;
            }

            m_Ghost.Show(m_BoardView.GetSprite(color), diameter);
            m_Ghost.SetAlpha(m_GhostAlpha);
            m_Ghost.SetLocalPosition(m_BoardView.CellToLocal(landingCell.x, landingCell.y));
        }
    }
}
