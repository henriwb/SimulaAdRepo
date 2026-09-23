using System.Collections;
using System.Collections.Generic;
using HyperCasual.Runner;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Composition root and state machine of the bubble game:
    /// Aiming → Shooting → Resolving → (Aiming | Cleared → End Card).
    /// Builds the models, wires controllers and views through serialized references
    /// (no static state) and restarts a run in place with <see cref="ResetGame"/>.
    /// </summary>
    public class GameLoopController : MonoBehaviour
    {
        [SerializeField] BubbleGameConfig m_Config;

        [Header("Views")]
        [SerializeField] BoardView m_BoardView;
        [SerializeField] HudView m_Hud;

        [Header("Controllers")]
        [SerializeField] BubbleController m_Bubbles;
        [SerializeField] LeverController m_Lever;

        [Header("Shared systems")]
        [SerializeField] GameObject m_EndCard;
        [SerializeField] AudioManager m_Audio;

        GameSessionModel m_Session;
        BubbleGridModel m_Grid;
        BoardController m_Board;

        readonly List<int> m_ColorBuffer = new List<int>();

        void Awake()
        {
            m_Session = new GameSessionModel();
            m_Grid = new BubbleGridModel(m_Config.Columns, m_Config.MaxRows);

            m_BoardView.Setup(m_Config.Columns);
            m_Board = new BoardController(m_Config, m_Grid, m_BoardView);

            m_Bubbles.Initialize(m_Config, m_BoardView, m_Board);
            m_Lever.Initialize(m_Session, m_Config);

            m_Hud.ShootPressed += OnShootPressed;
        }

        void OnDestroy()
        {
            if (m_Hud != null)
                m_Hud.ShootPressed -= OnShootPressed;
        }

        void Start()
        {
            ResetGame();
        }

        /// <summary>Starts a fresh run in place: new board, score 0, aim centered, no pending timers, End Card hidden.</summary>
        public void ResetGame()
        {
            StopAllCoroutines();
            m_Bubbles.Cancel();
            m_Board.Generate();
            m_Lever.ResetAim();

            m_Session.Score = 0;
            m_Session.CurrentColor = PickColor();
            m_Session.NextColor = PickColor();

            m_Hud.SetGameClearVisible(false);
            if (m_EndCard != null)
                m_EndCard.SetActive(false);
            RefreshHud();
            SetState(GameState.Aiming);
        }

        void OnShootPressed()
        {
            if (m_Session.State != GameState.Aiming)
                return;

            SetState(GameState.Shooting);
            PlaySound(SoundID.ButtonSound);

            int color = m_Session.CurrentColor;
            m_Session.CurrentColor = m_Session.NextColor;
            m_Session.NextColor = PickColor();
            RefreshHud();

            Vector2 origin = m_BoardView.WorldToLocal(m_Hud.CurrentBubbleWorldPosition);
            m_Bubbles.Launch(color, m_Session.AimAngle, origin, OnBubbleLanded);
        }

        void OnBubbleLanded(Vector2Int cell, int color, BubbleView view)
        {
            if (cell.x < 0)
            {
                // Grid full: discard the shot and keep playing.
                m_BoardView.Release(view);
                SetState(GameState.Aiming);
                return;
            }

            m_Board.Place(cell, color, view);
            StartCoroutine(ResolveShot(cell));
        }

        IEnumerator ResolveShot(Vector2Int cell)
        {
            SetState(GameState.Resolving);

            ResolveResult result = m_Board.Resolve(cell);
            if (result.Popped > 0)
            {
                m_Session.Score += result.Popped * m_Config.PointsPerPop + result.Dropped * m_Config.PointsPerDrop;
                RefreshHud();
                PlaySound(SoundID.CoinSound);
                yield return new WaitForSeconds(m_BoardView.ResolveDuration);
            }

            if (m_Board.IsEmpty())
            {
                SetState(GameState.Cleared);
                PlaySound(SoundID.EndSound);
                m_Hud.SetGameClearVisible(true);
                yield return new WaitForSeconds(m_Config.ClearDelay);

                if (m_EndCard != null)
                    m_EndCard.SetActive(true);
                yield break;
            }

            // Colors that left the board can't be matched anymore; swap them out.
            KeepQueueOnBoardColors();
            RefreshHud();
            SetState(GameState.Aiming);
        }

        void KeepQueueOnBoardColors()
        {
            m_Board.GetOccupiedColors(m_ColorBuffer);
            if (!m_ColorBuffer.Contains(m_Session.CurrentColor))
                m_Session.CurrentColor = PickColor();
            if (!m_ColorBuffer.Contains(m_Session.NextColor))
                m_Session.NextColor = PickColor();
        }

        /// <summary>Random color still present on the board, so the board is always clearable.</summary>
        int PickColor()
        {
            m_Board.GetOccupiedColors(m_ColorBuffer);
            if (m_ColorBuffer.Count == 0)
                return Random.Range(0, m_Board.ColorCount);

            return m_ColorBuffer[Random.Range(0, m_ColorBuffer.Count)];
        }

        void SetState(GameState state)
        {
            m_Session.State = state;
            m_Hud.SetShootEnabled(state == GameState.Aiming);
        }

        void RefreshHud()
        {
            m_Hud.SetScore(m_Session.Score);
            m_Hud.SetBubbles(m_BoardView.GetSprite(m_Session.CurrentColor), m_BoardView.GetSprite(m_Session.NextColor));
        }

        void PlaySound(SoundID sound)
        {
            if (m_Audio != null)
                m_Audio.PlayEffect(sound);
        }
    }
}
