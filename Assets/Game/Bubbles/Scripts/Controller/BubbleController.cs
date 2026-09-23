using System;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Flies a shot bubble along the aim, bounces it off the side walls and stops it on the
    /// ceiling or on contact with a placed bubble. All math is in the puzzle area's local
    /// space and uses Time.deltaTime, so pausing time pauses the shot.
    /// Controller: initialized by <see cref="GameLoopController"/>.
    /// </summary>
    public class BubbleController : MonoBehaviour
    {
        [Tooltip("Contact distance as a fraction of the bubble diameter (lower = shots slip through gaps more easily).")]
        [SerializeField] float m_HitDistanceFactor = 0.85f;

        BubbleGameConfig m_Config;
        BoardView m_BoardView;
        BoardController m_Board;

        BubbleView m_Flying;
        int m_FlyingColor;
        Vector2 m_Position;
        Vector2 m_Direction;
        Action<Vector2Int, int, BubbleView> m_OnLanded;

        public void Initialize(BubbleGameConfig config, BoardView boardView, BoardController board)
        {
            m_Config = config;
            m_BoardView = boardView;
            m_Board = board;
        }

        /// <param name="onLanded">Called with the landing cell ((-1,-1) if none), the color and the flying view.</param>
        public void Launch(int color, float aimDegrees, Vector2 origin, Action<Vector2Int, int, BubbleView> onLanded)
        {
            Cancel();

            float radians = aimDegrees * Mathf.Deg2Rad;
            m_Direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            m_Position = origin;
            m_FlyingColor = color;
            m_OnLanded = onLanded;
            m_Flying = m_BoardView.SpawnFlying(color, origin);
        }

        /// <summary>Removes an in-flight bubble without landing it (used on reset).</summary>
        public void Cancel()
        {
            if (m_Flying != null)
                m_BoardView.Release(m_Flying);

            m_Flying = null;
            m_OnLanded = null;
        }

        void Update()
        {
            if (m_Flying == null)
                return;

            float diameter = m_BoardView.Diameter;
            float remaining = m_Config.ShotSpeed * diameter * Time.deltaTime;
            float maxStep = diameter * 0.25f; // sub-steps so fast shots can't tunnel through bubbles

            while (remaining > 0f)
            {
                float step = Mathf.Min(maxStep, remaining);
                remaining -= step;
                Advance(step, diameter * 0.5f);

                if (HasHit(diameter))
                {
                    Land();
                    return;
                }
            }

            m_Flying.SetLocalPosition(m_Position);
        }

        void Advance(float distance, float radius)
        {
            m_Position += m_Direction * distance;

            Rect bounds = m_BoardView.Bounds;
            if (m_Position.x - radius < bounds.xMin)
            {
                m_Position.x = bounds.xMin + radius;
                m_Direction.x = Mathf.Abs(m_Direction.x);
            }
            else if (m_Position.x + radius > bounds.xMax)
            {
                m_Position.x = bounds.xMax - radius;
                m_Direction.x = -Mathf.Abs(m_Direction.x);
            }
        }

        bool HasHit(float diameter)
        {
            if (m_Position.y + diameter * 0.5f >= m_BoardView.Bounds.yMax)
                return true;

            return m_Board.TouchesBubble(m_Position, diameter * m_HitDistanceFactor);
        }

        void Land()
        {
            Vector2Int cell = m_Board.FindLandingCell(m_Position);
            BubbleView view = m_Flying;
            Action<Vector2Int, int, BubbleView> onLanded = m_OnLanded;

            m_Flying = null;
            m_OnLanded = null;

            if (onLanded != null)
                onLanded(cell, m_FlyingColor, view);
        }
    }
}
