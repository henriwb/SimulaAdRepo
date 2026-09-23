namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Hexagonal "offset rows" grid: even rows hold <see cref="Columns"/> cells,
    /// odd rows hold one less and are shifted half a bubble to the right.
    /// Each cell stores a color index or <see cref="Empty"/>. Model: data only.
    /// </summary>
    public class BubbleGridModel
    {
        public const int Empty = -1;

        public readonly int Columns;
        public readonly int Rows;

        readonly int[] m_Cells;

        public BubbleGridModel(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            m_Cells = new int[columns * rows];
            Clear();
        }

        public int RowLength(int row)
        {
            return row % 2 == 0 ? Columns : Columns - 1;
        }

        public bool IsInside(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < RowLength(row);
        }

        public int Get(int row, int col)
        {
            return m_Cells[row * Columns + col];
        }

        public void Set(int row, int col, int color)
        {
            m_Cells[row * Columns + col] = color;
        }

        public bool IsOccupied(int row, int col)
        {
            return IsInside(row, col) && Get(row, col) != Empty;
        }

        public void Clear()
        {
            for (int i = 0; i < m_Cells.Length; i++)
                m_Cells[i] = Empty;
        }
    }
}
