using System;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Flies a shot bubble using <see cref="TrajectoryController"/> (same rules as the aim guide)
    /// and reports where it lands. Uses Time.deltaTime, so pausing time pauses the shot.
    /// Controller: initialized by <see cref="GameLoopController"/>.
    /// </summary>
    public class BubbleController : MonoBehaviour
    {
        BubbleGameConfig m_Config;
        BoardView m_BoardView;
        BoardController m_Board;
        TrajectoryController m_Trajectory;

        BubbleView m_Flying;
        int m_FlyingColor;
        Vector2 m_Position;
        Vector2 m_Direction;
        Action<Vector2Int, int, BubbleView> m_OnLanded;

        public void Initialize(BubbleGameConfig config, BoardView boardView, BoardController board, TrajectoryController trajectory)
        {
            m_Config = config;
            m_BoardView = boardView;
            m_Board = board;
            m_Trajectory = trajectory;
        }

        /// <param name="onLanded">Called with the landing cell ((-1,-1) if none), the color and the flying view.</param>
        public void Launch(int color, float aimDegrees, Vector2 origin, Action<Vector2Int, int, BubbleView> onLanded)
        {
            Cancel();

            m_Direction = m_Trajectory.AimDirection(aimDegrees);
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

            float remaining = m_Config.ShotSpeed * m_BoardView.Diameter * Time.deltaTime;
            float maxStep = m_Trajectory.MaxStep;

            while (remaining > 0f)
            {
                float step = Mathf.Min(maxStep, remaining);
                remaining -= step;

                bool bounced;
                if (m_Trajectory.Step(ref m_Position, ref m_Direction, step, out bounced))
                {
                    Land();
                    return;
                }
            }

            m_Flying.SetLocalPosition(m_Position);
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
