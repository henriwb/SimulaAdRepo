using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Draws the bubble grid inside the puzzle area (put this on the puzzle area RectTransform).
    /// The board has a fixed design width, so bubbles keep the same size whatever the area's
    /// width (portrait or landscape layout); the grid is centered horizontally at the top of the
    /// area and only shrinks if the area is narrower than the board. View: visuals and layout only.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BoardView : MonoBehaviour
    {
        const float k_RowHeightFactor = 0.8660254f; // sqrt(3) / 2 for packed circles

        [Tooltip("Board width in canvas design units (bubble diameter = this / columns). Keeps bubble size identical across layouts.")]
        [SerializeField] float m_BoardWidth = 360f;
        [Tooltip("Bubble size multiplier while the screen is landscape.")]
        [Range(0.3f, 1f)]
        [SerializeField] float m_LandscapeScale = 0.7f;
        [SerializeField] BubbleView m_BubbleTemplate;
        [SerializeField] BubbleGameConfig m_Config;
        [SerializeField] float m_PopDuration = 0.2f;
        [SerializeField] float m_DropDuration = 0.5f;

        [Header("Impact wobble")]
        [Tooltip("Bubbles within this many diameters of a landing shot wobble.")]
        [SerializeField] float m_WobbleRadius = 2.2f;
        [Tooltip("Squash/stretch strength at the impact point (fades with distance).")]
        [SerializeField] float m_WobbleStrength = 0.18f;
        [Tooltip("Ripple delay per diameter of distance, in seconds.")]
        [SerializeField] float m_WobbleRippleDelay = 0.04f;

        RectTransform m_Area;
        int m_Columns;
        float m_LaidOutWidth = -1f;
        bool m_LaidOutLandscape;

        // Placed bubbles by cell index (row * columns + col). A plain array, not a Dictionary:
        // Dictionary.Values/Keys enumerators throw ('moveNext' of undefined) in Playworks builds.
        BubbleView[] m_Placed = new BubbleView[0];
        readonly List<BubbleView> m_Animating = new List<BubbleView>();
        readonly List<BubbleView> m_Pool = new List<BubbleView>();

        /// <summary>Raised after a resize/rotation re-layout (bubble size and positions changed).</summary>
        public event Action LayoutChanged;

        public float Diameter { get; private set; }
        public float RowHeight { get; private set; }
        /// <summary>
        /// Playfield in local space: the grid's width (columns × diameter), centered in the area,
        /// full area height. Shots bounce off its sides and stop at its top.
        /// </summary>
        public Rect Bounds
        {
            get
            {
                Rect area = m_Area.rect;
                float width = Diameter * m_Columns;
                return new Rect(area.center.x - width * 0.5f, area.yMin, width, area.height);
            }
        }
        public float ResolveDuration => Mathf.Max(m_PopDuration, m_DropDuration);

        public void Setup(int columns, int rows)
        {
            m_Area = (RectTransform)transform;
            m_Columns = columns;
            m_Placed = new BubbleView[columns * rows];

            if (m_BubbleTemplate != null)
                m_BubbleTemplate.gameObject.SetActive(false);

            Relayout();
        }

        public Sprite GetSprite(int color)
        {
            if (color == BubbleGridModel.Special)
                return m_Config.SpecialBubble;
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

        public Vector3 CellToWorld(int row, int col)
        {
            return m_Area.TransformPoint(CellToLocal(row, col));
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

        /// <summary>
        /// Ripple of squash & stretch around a cell (the bubble that just landed and its neighbors):
        /// stronger and earlier close to the impact, fading with distance. Deterministic.
        /// </summary>
        public void WobbleAround(int row, int col)
        {
            Vector2 impact = CellToLocal(row, col);
            float radius = m_WobbleRadius * Diameter;
            if (radius <= 0f)
                return;

            for (int key = 0; key < m_Placed.Length; key++)
            {
                BubbleView view = m_Placed[key];
                if (view == null)
                    continue;

                float distance = (CellToLocal(key / m_Columns, key % m_Columns) - impact).magnitude;
                if (distance > radius)
                    continue;

                float falloff = 1f - distance / radius;
                view.Wobble(m_WobbleStrength * falloff, m_WobbleRippleDelay * distance / Diameter);
            }
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
            for (int i = 0; i < m_Placed.Length; i++)
            {
                if (m_Placed[i] == null)
                    continue;

                Release(m_Placed[i]);
                m_Placed[i] = null;
            }

            for (int i = m_Animating.Count - 1; i >= 0; i--)
                Release(m_Animating[i]);
        }

        void OnRectTransformDimensionsChange()
        {
            if (m_Area != null && m_Columns > 0)
                Relayout();
        }

        // The Playworks runtime may size the canvas after Awake and may not send
        // OnRectTransformDimensionsChange, so also watch the width every frame (one float compare).
        void LateUpdate()
        {
            if (m_Area == null || m_Columns <= 0)
                return;

            if (!Mathf.Approximately(m_Area.rect.width, m_LaidOutWidth) || IsLandscape() != m_LaidOutLandscape)
                Relayout();
        }

        void Relayout()
        {
            m_LaidOutWidth = m_Area.rect.width;
            m_LaidOutLandscape = IsLandscape();

            // Fixed board width keeps the bubble size across layouts; landscape applies its own scale;
            // shrink further only if the area is narrower than the board.
            float designWidth = m_BoardWidth * (m_LaidOutLandscape ? m_LandscapeScale : 1f);
            float boardWidth = m_LaidOutWidth > 0f ? Mathf.Min(designWidth, m_LaidOutWidth) : designWidth;
            Diameter = boardWidth / m_Columns;
            RowHeight = Diameter * k_RowHeightFactor;
            // No custom numeric formats (e.g. {x:0.#}): Bridge.Int.customFormat throws in Playworks builds.
            Debug.Log($"[{nameof(BoardView)}] Layout: area width={m_LaidOutWidth}, bubble diameter={Diameter}");

            for (int key = 0; key < m_Placed.Length; key++)
            {
                BubbleView view = m_Placed[key];
                if (view == null)
                    continue;

                view.SetDiameter(Diameter);
                view.SetLocalPosition(CellToLocal(key / m_Columns, key % m_Columns));
            }

            if (LayoutChanged != null)
                LayoutChanged();
        }

        bool IsLandscape()
        {
            return Screen.width > Screen.height;
        }

        BubbleView Detach(int row, int col)
        {
            int key = Key(row, col);
            BubbleView view = m_Placed[key];
            if (view == null)
                return null;

            m_Placed[key] = null;
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
