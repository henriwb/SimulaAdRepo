using System.Collections.Generic;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Shot movement rules shared by the real shot (<see cref="BubbleController"/>) and the
    /// aim guide preview, so the guide always matches where the bubble really goes.
    /// Works in the puzzle area's local space. Controller: plain C#.
    /// </summary>
    public class TrajectoryController
    {
        const int k_MaxPredictionSteps = 4000;

        readonly BubbleGameConfig m_Config;
        readonly BoardView m_View;
        readonly BoardController m_Board;

        public TrajectoryController(BubbleGameConfig config, BoardView view, BoardController board)
        {
            m_Config = config;
            m_View = view;
            m_Board = board;
        }

        /// <summary>Largest safe step: small enough that a shot can't tunnel through a bubble.</summary>
        public float MaxStep => m_View.Diameter * 0.25f;

        public Vector2 AimDirection(float aimDegrees)
        {
            float radians = aimDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        /// <summary>
        /// Moves one step, bouncing off the side walls. Returns true when the bubble must stop
        /// (ceiling or contact with a placed bubble).
        /// </summary>
        public bool Step(ref Vector2 position, ref Vector2 direction, float distance, out bool bounced)
        {
            float diameter = m_View.Diameter;
            float radius = diameter * 0.5f;
            Rect bounds = m_View.Bounds;

            position += direction * distance;
            bounced = false;

            if (position.x - radius < bounds.xMin)
            {
                position.x = bounds.xMin + radius;
                direction.x = Mathf.Abs(direction.x);
                bounced = true;
            }
            else if (position.x + radius > bounds.xMax)
            {
                position.x = bounds.xMax - radius;
                direction.x = -Mathf.Abs(direction.x);
                bounced = true;
            }

            if (position.y + radius >= bounds.yMax)
                return true;

            return m_Board.TouchesBubble(position, diameter * m_Config.HitDistanceFactor);
        }

        /// <summary>
        /// Simulates a shot and fills <paramref name="points"/> with the origin, each bounce and the end.
        /// Returns the landing cell, or (-1, -1) if the path goes past <paramref name="maxBounces"/>.
        /// </summary>
        public Vector2Int PredictPath(Vector2 origin, float aimDegrees, int maxBounces, List<Vector2> points)
        {
            points.Clear();
            points.Add(origin);

            Vector2 position = origin;
            Vector2 direction = AimDirection(aimDegrees);
            float step = MaxStep;
            int bounces = 0;

            for (int i = 0; i < k_MaxPredictionSteps; i++)
            {
                bool bounced;
                bool stopped = Step(ref position, ref direction, step, out bounced);

                if (stopped)
                {
                    points.Add(position);
                    return m_Board.FindLandingCell(position);
                }

                if (bounced)
                {
                    points.Add(position);
                    bounces++;
                    if (bounces > maxBounces)
                        return new Vector2Int(-1, -1);
                }
            }

            points.Add(position);
            return new Vector2Int(-1, -1);
        }
    }
}
