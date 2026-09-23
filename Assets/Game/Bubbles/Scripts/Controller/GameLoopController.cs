using System.Collections;
using System.Collections.Generic;
using HyperCasual.Gameplay;
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
        [SerializeField] AimGuideView m_AimGuide;
        [SerializeField] ScorePopupView m_ScorePopups;

        [Header("Controllers")]
        [SerializeField] BubbleController m_Bubbles;
        [SerializeField] LeverController m_Lever;

        [Header("Shared systems")]
        [SerializeField] GameObject m_EndCard;
        [SerializeField] AudioManager m_Audio;

        GameSessionModel m_Session;
        BubbleGridModel m_Grid;
        BoardController m_Board;
        TrajectoryController m_Trajectory;
        EndCardController m_EndCardController;

        readonly List<int> m_ColorBuffer = new List<int>();
        readonly List<Vector2> m_GuidePoints = new List<Vector2>();

        void Awake()
        {
            m_Session = new GameSessionModel();
            m_Grid = new BubbleGridModel(m_Config.Columns, m_Config.MaxRows);

            m_BoardView.Setup(m_Config.Columns);
            m_Board = new BoardController(m_Config, m_Grid, m_BoardView);
            m_Trajectory = new TrajectoryController(m_Config, m_BoardView, m_Board);

            m_Bubbles.Initialize(m_Config, m_BoardView, m_Board, m_Trajectory);
            m_Lever.Initialize(m_Session, m_Config);

            // The End Card animator idles at scale 0; OpenEndCard() plays its intro animation.
            if (m_EndCard != null)
            {
                m_EndCardController = m_EndCard.GetComponentInChildren<EndCardController>(true);

                // The Replay button lives inside the End Card prefab, which can't reference scene objects.
                foreach (RunRestarter restarter in m_EndCard.GetComponentsInChildren<RunRestarter>(true))
                    restarter.Initialize(this);
            }

            m_Lever.ShotReleased += OnShotReleased;
            m_Lever.AimChanged += RefreshAimGuide;
            m_BoardView.LayoutChanged += RefreshAimGuide;
        }

        void OnDestroy()
        {
            if (m_Lever != null)
            {
                m_Lever.ShotReleased -= OnShotReleased;
                m_Lever.AimChanged -= RefreshAimGuide;
            }
            if (m_BoardView != null)
                m_BoardView.LayoutChanged -= RefreshAimGuide;
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
            if (m_ScorePopups != null)
                m_ScorePopups.HideAll();
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

        /// <summary>Aiming drag released: fire (ignored unless the game is waiting for a shot).</summary>
        void OnShotReleased()
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
                ShowScorePopups(m_Board.PoppedCells, m_Config.PointsPerPop);
                ShowScorePopups(m_Board.DroppedCells, m_Config.PointsPerDrop);
                RefreshHud();
                PlaySound(SoundID.CoinSound);
                yield return new WaitForSeconds(m_BoardView.ResolveDuration);
            }

            if (m_Board.IsEmpty())
            {
                StartCoroutine(GameClear());
                yield break;
            }

            // Colors that left the board can't be matched anymore; swap them out.
            KeepQueueOnBoardColors();
            RefreshHud();
            SetState(GameState.Aiming);
        }

        IEnumerator GameClear()
        {
            SetState(GameState.Cleared);
            PlaySound(SoundID.EndSound);
            m_Hud.SetGameClearVisible(true);
            yield return new WaitForSeconds(m_Config.ClearDelay);

            ShowEndCard();
        }

        /// <summary>
        /// Debug: pops every bubble and runs the normal Game Clear flow (End Card, Replay).
        /// Triggered from the GameLoopController inspector button in Play Mode.
        /// </summary>
        public void DebugClearBoard()
        {
            if (m_Session == null || m_Session.State == GameState.Cleared)
                return;

            StopAllCoroutines();
            m_Bubbles.Cancel();
            StartCoroutine(DebugClearRoutine());
        }

        IEnumerator DebugClearRoutine()
        {
            SetState(GameState.Resolving);

            int popped = m_Board.PopAll();
            m_Session.Score += popped * m_Config.PointsPerPop;
            ShowScorePopups(m_Board.PoppedCells, m_Config.PointsPerPop);
            RefreshHud();
            PlaySound(SoundID.CoinSound);
            yield return new WaitForSeconds(m_BoardView.ResolveDuration);

            StartCoroutine(GameClear());
        }

        /// <summary>One "+points" label on each cell (positions come from the grid, not the animating views).</summary>
        void ShowScorePopups(List<Vector2Int> cells, int points)
        {
            if (m_ScorePopups == null)
                return;

            foreach (Vector2Int cell in cells)
                m_ScorePopups.Show(m_BoardView.CellToWorld(cell.x, cell.y), points);
        }

        void ShowEndCard()
        {
            if (m_EndCard == null)
            {
                Debug.LogWarning($"[{nameof(GameLoopController)}] End Card not assigned.");
                return;
            }

            // Activating first runs EndCardController.Awake (registers the CTA) before opening.
            // Deactivating on ResetGame resets its Animator, so every open animates in again.
            m_EndCard.SetActive(true);
            if (m_EndCardController != null)
                m_EndCardController.OpenEndCard();
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
            RefreshAimGuide();
        }

        /// <summary>Predicts the shot with the same rules as the real flight; shown only while aiming.</summary>
        void RefreshAimGuide()
        {
            if (m_AimGuide == null || m_Session == null)
                return;

            if (m_Session.State != GameState.Aiming)
            {
                m_AimGuide.Hide();
                return;
            }

            Vector2 origin = m_BoardView.WorldToLocal(m_Hud.CurrentBubbleWorldPosition);
            Vector2Int landing = m_Trajectory.PredictPath(origin, m_Session.AimAngle, m_Config.GuideMaxBounces, m_GuidePoints);
            m_AimGuide.Show(m_GuidePoints, landing, m_Session.CurrentColor);
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
