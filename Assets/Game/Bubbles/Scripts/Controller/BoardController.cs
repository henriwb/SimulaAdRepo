using System.Collections.Generic;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>Counts from resolving one landed bubble.</summary>
    public struct ResolveResult
    {
        public int Popped;
        public int Dropped;
    }

    /// <summary>
    /// Board rules: generate the start layout, find where a shot lands, resolve matches
    /// and floating bubbles, and keep <see cref="BoardView"/> in sync with the grid.
    /// Controller: plain C#, owned by <see cref="GameLoopController"/>.
    /// </summary>
    public class BoardController
    {
        // Hex offset-row neighbors (row delta, col delta) for even and odd rows.
        readonly Vector2Int[] m_EvenRowNeighbors =
        {
            new Vector2Int(-1, -1), new Vector2Int(-1, 0),
            new Vector2Int(0, -1), new Vector2Int(0, 1),
            new Vector2Int(1, -1), new Vector2Int(1, 0)
        };

        readonly Vector2Int[] m_OddRowNeighbors =
        {
            new Vector2Int(-1, 0), new Vector2Int(-1, 1),
            new Vector2Int(0, -1), new Vector2Int(0, 1),
            new Vector2Int(1, 0), new Vector2Int(1, 1)
        };

        readonly BubbleGameConfig m_Config;
        readonly BubbleGridModel m_Grid;
        readonly BoardView m_View;

        readonly List<Vector2Int> m_Neighbors = new List<Vector2Int>();
        readonly List<Vector2Int> m_Cluster = new List<Vector2Int>();
        readonly List<Vector2Int> m_Stack = new List<Vector2Int>();
        readonly bool[] m_Visited;

        public BoardController(BubbleGameConfig config, BubbleGridModel grid, BoardView view)
        {
            m_Config = config;
            m_Grid = grid;
            m_View = view;
            m_Visited = new bool[grid.Columns * grid.Rows];
        }

        public int ColorCount
        {
            get
            {
                int available = m_Config.ColorSprites.Count;
                if (available == 0)
                    Debug.LogError($"[{nameof(BoardController)}] No bubble sprites in {nameof(BubbleGameConfig)}.{nameof(BubbleGameConfig.ColorSprites)}.");

                return Mathf.Max(1, Mathf.Min(m_Config.ColorCount, available));
            }
        }

        public void Generate()
        {
            m_Grid.Clear();
            m_View.ClearAll();

            int colors = ColorCount;
            int rows = Mathf.Min(m_Config.StartRows, m_Grid.Rows);
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    int color = Random.Range(0, colors);
                    m_Grid.Set(row, col, color);
                    m_View.Spawn(row, col, color);
                }
            }
        }

        public bool IsEmpty()
        {
            for (int row = 0; row < m_Grid.Rows; row++)
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                    if (m_Grid.Get(row, col) != BubbleGridModel.Empty)
                        return false;

            return true;
        }

        public void GetOccupiedColors(List<int> result)
        {
            result.Clear();
            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    int color = m_Grid.Get(row, col);
                    if (color != BubbleGridModel.Empty && !result.Contains(color))
                        result.Add(color);
                }
            }
        }

        /// <summary>True if a bubble centered at <paramref name="localPosition"/> touches a placed bubble.</summary>
        public bool TouchesBubble(Vector2 localPosition, float hitDistance)
        {
            float hitSqr = hitDistance * hitDistance;
            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    if (m_Grid.Get(row, col) == BubbleGridModel.Empty)
                        continue;

                    if ((m_View.CellToLocal(row, col) - localPosition).sqrMagnitude < hitSqr)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Nearest empty cell a shot at <paramref name="localPosition"/> can stick to
        /// (top row, or next to a placed bubble). Returns (-1, -1) if the grid is full.
        /// </summary>
        public Vector2Int FindLandingCell(Vector2 localPosition)
        {
            Vector2Int best = new Vector2Int(-1, -1);
            float bestSqr = float.MaxValue;

            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    if (m_Grid.Get(row, col) != BubbleGridModel.Empty)
                        continue;
                    if (row != 0 && !HasOccupiedNeighbor(row, col))
                        continue;

                    float sqr = (m_View.CellToLocal(row, col) - localPosition).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        best = new Vector2Int(row, col);
                    }
                }
            }

            return best;
        }

        public void Place(Vector2Int cell, int color, BubbleView view)
        {
            m_Grid.Set(cell.x, cell.y, color);
            m_View.Attach(view, cell.x, cell.y);
        }

        /// <summary>Pops the same-color cluster at <paramref name="cell"/> (if big enough), then drops floating bubbles.</summary>
        public ResolveResult Resolve(Vector2Int cell)
        {
            ResolveResult result = new ResolveResult();

            CollectCluster(cell);
            if (m_Cluster.Count < m_Config.MatchCount)
                return result;

            foreach (Vector2Int popped in m_Cluster)
            {
                m_Grid.Set(popped.x, popped.y, BubbleGridModel.Empty);
                m_View.Pop(popped.x, popped.y);
            }
            result.Popped = m_Cluster.Count;

            MarkConnectedToCeiling();
            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    if (m_Grid.Get(row, col) == BubbleGridModel.Empty || m_Visited[Index(row, col)])
                        continue;

                    m_Grid.Set(row, col, BubbleGridModel.Empty);
                    m_View.Drop(row, col);
                    result.Dropped++;
                }
            }

            return result;
        }

        /// <summary>Pops every placed bubble. Returns how many were popped.</summary>
        public int PopAll()
        {
            int popped = 0;
            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    if (m_Grid.Get(row, col) == BubbleGridModel.Empty)
                        continue;

                    m_Grid.Set(row, col, BubbleGridModel.Empty);
                    m_View.Pop(row, col);
                    popped++;
                }
            }

            return popped;
        }

        void CollectCluster(Vector2Int start)
        {
            m_Cluster.Clear();
            ClearVisited();

            int color = m_Grid.Get(start.x, start.y);
            m_Stack.Clear();
            m_Stack.Add(start);
            m_Visited[Index(start.x, start.y)] = true;

            while (m_Stack.Count > 0)
            {
                Vector2Int current = m_Stack[m_Stack.Count - 1];
                m_Stack.RemoveAt(m_Stack.Count - 1);
                m_Cluster.Add(current);

                GetNeighbors(current.x, current.y);
                foreach (Vector2Int next in m_Neighbors)
                {
                    int index = Index(next.x, next.y);
                    if (m_Visited[index] || m_Grid.Get(next.x, next.y) != color)
                        continue;

                    m_Visited[index] = true;
                    m_Stack.Add(next);
                }
            }
        }

        void MarkConnectedToCeiling()
        {
            ClearVisited();
            m_Stack.Clear();

            for (int col = 0; col < m_Grid.RowLength(0); col++)
            {
                if (m_Grid.Get(0, col) == BubbleGridModel.Empty)
                    continue;

                m_Visited[Index(0, col)] = true;
                m_Stack.Add(new Vector2Int(0, col));
            }

            while (m_Stack.Count > 0)
            {
                Vector2Int current = m_Stack[m_Stack.Count - 1];
                m_Stack.RemoveAt(m_Stack.Count - 1);

                GetNeighbors(current.x, current.y);
                foreach (Vector2Int next in m_Neighbors)
                {
                    int index = Index(next.x, next.y);
                    if (m_Visited[index] || m_Grid.Get(next.x, next.y) == BubbleGridModel.Empty)
                        continue;

                    m_Visited[index] = true;
                    m_Stack.Add(next);
                }
            }
        }

        bool HasOccupiedNeighbor(int row, int col)
        {
            GetNeighbors(row, col);
            foreach (Vector2Int next in m_Neighbors)
                if (m_Grid.Get(next.x, next.y) != BubbleGridModel.Empty)
                    return true;

            return false;
        }

        /// <summary>Fills <see cref="m_Neighbors"/> with the in-grid neighbors of a cell.</summary>
        void GetNeighbors(int row, int col)
        {
            m_Neighbors.Clear();
            Vector2Int[] offsets = row % 2 == 0 ? m_EvenRowNeighbors : m_OddRowNeighbors;
            foreach (Vector2Int offset in offsets)
            {
                int r = row + offset.x;
                int c = col + offset.y;
                if (m_Grid.IsInside(r, c))
                    m_Neighbors.Add(new Vector2Int(r, c));
            }
        }

        void ClearVisited()
        {
            for (int i = 0; i < m_Visited.Length; i++)
                m_Visited[i] = false;
        }

        int Index(int row, int col)
        {
            return row * m_Grid.Columns + col;
        }
    }
}
