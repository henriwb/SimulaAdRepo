using System.Collections.Generic;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>Counts from resolving one landed bubble.</summary>
    public struct ResolveResult
    {
        public int Popped;
        public int Dropped;
        /// <summary>Rows wiped by popped special bubbles.</summary>
        public int RowsCleared;
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
        readonly List<Vector2Int> m_PoppedCells = new List<Vector2Int>();
        readonly List<Vector2Int> m_DroppedCells = new List<Vector2Int>();
        readonly List<Vector2Int> m_SpecialCells = new List<Vector2Int>();

        /// <summary>Special bubbles popped by the last <see cref="Resolve"/> (each one cleared its row).</summary>
        public List<Vector2Int> SpecialCells => m_SpecialCells;
        readonly bool[] m_Visited;

        /// <summary>Cells popped by the last <see cref="Resolve"/> or <see cref="PopAll"/>.</summary>
        public List<Vector2Int> PoppedCells => m_PoppedCells;

        /// <summary>Cells dropped (disconnected from the ceiling) by the last <see cref="Resolve"/>.</summary>
        public List<Vector2Int> DroppedCells => m_DroppedCells;

        readonly RandomSource m_Random;

        public BoardController(BubbleGameConfig config, BubbleGridModel grid, BoardView view, RandomSource random)
        {
            m_Config = config;
            m_Grid = grid;
            m_View = view;
            m_Random = random;
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

            // Fill the model first, then swap in the specials, then draw.
            m_Stack.Clear(); // reused as the list of filled cells
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    m_Grid.Set(row, col, m_Random.Range(0, colors));
                    m_Stack.Add(new Vector2Int(row, col));
                }
            }

            PlaceSpecials();

            for (int i = 0; i < m_Stack.Count; i++)
                m_View.Spawn(m_Stack[i].x, m_Stack[i].y, m_Grid.Get(m_Stack[i].x, m_Stack[i].y));
            m_Stack.Clear();
        }

        /// <summary>Turns <see cref="BubbleGameConfig.SpecialCount"/> random filled cells into special bubbles.</summary>
        void PlaceSpecials()
        {
            if (m_Config.SpecialBubble == null || m_Config.SpecialCount <= 0)
                return;

            // Pick distinct cells: candidates are m_Stack[0..remaining); a picked one is swapped to the end.
            int remaining = m_Stack.Count;
            int count = Mathf.Min(m_Config.SpecialCount, remaining);
            for (int i = 0; i < count; i++)
            {
                int pick = m_Random.Range(0, remaining);
                Vector2Int cell = m_Stack[pick];
                m_Grid.Set(cell.x, cell.y, BubbleGridModel.Special);

                remaining--;
                m_Stack[pick] = m_Stack[remaining];
                m_Stack[remaining] = cell;
            }
        }

        /// <summary>Bubbles currently on the board (regular + special).</summary>
        public int CountBubbles()
        {
            int count = 0;
            for (int row = 0; row < m_Grid.Rows; row++)
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                    if (m_Grid.Get(row, col) != BubbleGridModel.Empty)
                        count++;

            return count;
        }

        /// <summary>True while any regular (non-special) bubble remains.</summary>
        public bool HasRegularBubbles()
        {
            for (int row = 0; row < m_Grid.Rows; row++)
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                    if (m_Grid.Get(row, col) >= 0)
                        return true;

            return false;
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
                    if (color >= 0 && !result.Contains(color)) // regular colors only (skip empty/special)
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

        /// <summary>
        /// Pops the same-color cluster at <paramref name="cell"/> (special bubbles count as any color)
        /// if big enough; each popped special also clears its whole row. Then drops floating bubbles.
        /// </summary>
        public ResolveResult Resolve(Vector2Int cell)
        {
            ResolveResult result = new ResolveResult();
            m_PoppedCells.Clear();
            m_DroppedCells.Clear();
            m_SpecialCells.Clear();

            CollectCluster(cell);
            if (m_Cluster.Count < m_Config.MatchCount)
                return result;

            // Index loops (no foreach) throughout: enumerators are unreliable in Playworks builds.
            for (int i = 0; i < m_Cluster.Count; i++)
            {
                Vector2Int popped = m_Cluster[i];
                if (m_Grid.Get(popped.x, popped.y) == BubbleGridModel.Special)
                {
                    m_SpecialCells.Add(popped);
                    ClearRow(popped.x);
                    result.RowsCleared++;
                }
                PopCell(popped.x, popped.y);
            }
            result.Popped = m_PoppedCells.Count;

            MarkConnectedToCeiling();
            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    if (m_Grid.Get(row, col) == BubbleGridModel.Empty || m_Visited[Index(row, col)])
                        continue;

                    m_Grid.Set(row, col, BubbleGridModel.Empty);
                    m_View.Drop(row, col);
                    m_DroppedCells.Add(new Vector2Int(row, col));
                    result.Dropped++;
                }
            }

            return result;
        }

        /// <summary>Pops every bubble in <paramref name="row"/> (special bubble bonus).</summary>
        void ClearRow(int row)
        {
            for (int col = 0; col < m_Grid.RowLength(row); col++)
                PopCell(row, col);
        }

        /// <summary>Pops one cell if occupied (safe to call twice for the same cell).</summary>
        void PopCell(int row, int col)
        {
            if (m_Grid.Get(row, col) == BubbleGridModel.Empty)
                return;

            m_Grid.Set(row, col, BubbleGridModel.Empty);
            m_View.Pop(row, col);
            m_PoppedCells.Add(new Vector2Int(row, col));
        }

        /// <summary>Pops every placed bubble. Returns how many were popped.</summary>
        public int PopAll()
        {
            m_PoppedCells.Clear();
            m_DroppedCells.Clear();
            m_SpecialCells.Clear();

            int popped = 0;
            for (int row = 0; row < m_Grid.Rows; row++)
            {
                for (int col = 0; col < m_Grid.RowLength(row); col++)
                {
                    if (m_Grid.Get(row, col) == BubbleGridModel.Empty)
                        continue;

                    m_Grid.Set(row, col, BubbleGridModel.Empty);
                    m_View.Pop(row, col);
                    m_PoppedCells.Add(new Vector2Int(row, col));
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
                for (int i = 0; i < m_Neighbors.Count; i++)
                {
                    Vector2Int next = m_Neighbors[i];
                    int index = Index(next.x, next.y);
                    int nextColor = m_Grid.Get(next.x, next.y);
                    // Special bubbles are wildcards: they join (and extend) any color's cluster.
                    if (m_Visited[index] || (nextColor != color && nextColor != BubbleGridModel.Special))
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
                for (int i = 0; i < m_Neighbors.Count; i++)
                {
                    Vector2Int next = m_Neighbors[i];
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
            for (int i = 0; i < m_Neighbors.Count; i++)
                if (m_Grid.Get(m_Neighbors[i].x, m_Neighbors[i].y) != BubbleGridModel.Empty)
                    return true;

            return false;
        }

        /// <summary>Fills <see cref="m_Neighbors"/> with the in-grid neighbors of a cell.</summary>
        void GetNeighbors(int row, int col)
        {
            m_Neighbors.Clear();
            Vector2Int[] offsets = row % 2 == 0 ? m_EvenRowNeighbors : m_OddRowNeighbors;
            for (int i = 0; i < offsets.Length; i++)
            {
                int r = row + offsets[i].x;
                int c = col + offsets[i].y;
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
