using System.Collections.Generic;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Draws the bubble grid inside the puzzle area (put this on the puzzle area RectTransform).
    /// Converts grid cells to local positions from the current area width, so a resize or
    /// rotation only re-lays out the bubbles. View: visuals and layout only.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BoardView : MonoBehaviour
    {
        const float k_RowHeightFactor = 0.8660254f; // sqrt(3) / 2 for packed circles

        [SerializeField] BubbleView m_BubbleTemplate;
        [SerializeField] BubbleGameConfig m_Config;
        [SerializeField] float m_PopDuration = 0.2f;
        [SerializeField] float m_DropDuration = 0.5f;

        RectTransform m_Area;
        int m_Columns;

        readonly Dictionary<int, BubbleView> m_Placed = new Dictionary<int, BubbleView>();
        readonly List<BubbleView> m_Animating = new List<BubbleView>();
        readonly List<BubbleView> m_Pool = new List<BubbleView>();
        readonly List<int> m_KeyBuffer = new List<int>();

        public float Diameter { get; private set; }
        public float RowHeight { get; private set; }
        public Rect Bounds => m_Area.rect;
        public float ResolveDuration => Mathf.Max(m_PopDuration, m_DropDuration);

        public void Setup(int columns)
        {
            m_Area = (RectTransform)transform;
            m_Columns = columns;

            if (m_BubbleTemplate != null)
                m_BubbleTemplate.gameObject.SetActive(false);

            Relayout();
        }

        public Sprite GetSprite(int color)
        {
            if (color < 0 || color >= m_Config.ColorSprites.Count)
                return null;

            return m_Config.ColorSprites[color];
        }

        public Vector2 CellToLocal(int row, int col)
        {
            Rect bounds = Bounds;
            float radius = Diameter * 0.5f;
            float x = bounds.xMin + radius + col * Diameter + (row % 2 == 1 ? radius : 0f);
            float y = bounds.yMax - radius - row * RowHeight;
            return new Vector2(x, y);
        }

        public Vector2 WorldToLocal(Vector3 worldPosition)
        {
            return m_Area.InverseTransformPoint(worldPosition);
        }

        public void Spawn(int row, int col, int color)
        {
            BubbleView view = Take();
            view.Show(GetSprite(color), Diameter);
            view.SetLocalPosition(CellToLocal(row, col));
            m_Placed[Key(row, col)] = view;
        }

        public BubbleView SpawnFlying(int color, Vector2 localPosition)
        {
            BubbleView view = Take();
            view.Show(GetSprite(color), Diameter);
            view.SetLocalPosition(localPosition);
            return view;
        }

        public void Attach(BubbleView view, int row, int col)
        {
            view.SetLocalPosition(CellToLocal(row, col));
            m_Placed[Key(row, col)] = view;
        }

        public void Pop(int row, int col)
        {
            BubbleView view = Detach(row, col);
            if (view != null)
                view.Pop(m_PopDuration, () => Release(view));
        }

        public void Drop(int row, int col)
        {
            BubbleView view = Detach(row, col);
            if (view != null)
                view.Drop(Diameter * 3f, m_DropDuration, () => Release(view));
        }

        public void Release(BubbleView view)
        {
            view.Hide();
            m_Animating.Remove(view);
            if (!m_Pool.Contains(view))
                m_Pool.Add(view);
        }

        /// <summary>Returns every bubble (placed or mid-animation) to the pool.</summary>
        public void ClearAll()
        {
            foreach (BubbleView view in m_Placed.Values)
                Release(view);
            m_Placed.Clear();

            for (int i = m_Animating.Count - 1; i >= 0; i--)
                Release(m_Animating[i]);
        }

        void OnRectTransformDimensionsChange()
        {
            if (m_Area != null && m_Columns > 0)
                Relayout();
        }

        void Relayout()
        {
            Diameter = m_Area.rect.width / m_Columns;
            RowHeight = Diameter * k_RowHeightFactor;

            m_KeyBuffer.Clear();
            m_KeyBuffer.AddRange(m_Placed.Keys);
            foreach (int key in m_KeyBuffer)
            {
                BubbleView view = m_Placed[key];
                view.SetDiameter(Diameter);
                view.SetLocalPosition(CellToLocal(key / m_Columns, key % m_Columns));
            }
        }

        BubbleView Detach(int row, int col)
        {
            int key = Key(row, col);
            BubbleView view;
            if (!m_Placed.TryGetValue(key, out view))
                return null;

            m_Placed.Remove(key);
            m_Animating.Add(view);
            return view;
        }

        BubbleView Take()
        {
            if (m_Pool.Count > 0)
            {
                BubbleView pooled = m_Pool[m_Pool.Count - 1];
                m_Pool.RemoveAt(m_Pool.Count - 1);
                return pooled;
            }

            BubbleView created = Instantiate(m_BubbleTemplate, m_Area);
            created.transform.localScale = Vector3.one;
            return created;
        }

        int Key(int row, int col)
        {
            return row * m_Columns + col;
        }
    }
}
