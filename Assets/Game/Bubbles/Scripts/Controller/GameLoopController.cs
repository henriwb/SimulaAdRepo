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
        [Tooltip("Optional coins flying from cleared bubbles to the score icon.")]
        [SerializeField] CoinEffectManager m_CoinEffect;
        [Tooltip("Optional screen shake when bubbles pop.")]
        [SerializeField] ScreenShakeView m_Shake;
        [Tooltip("Optional left/right sweep along the row when a special bubble clears it.")]
        [SerializeField] RowClearEffectView m_RowClear;
        [Tooltip("Optional fading trail behind the shot bubble.")]
        [SerializeField] ShotTrailView m_Trail;
        [Tooltip("Optional character that jumps when a bubble is shot.")]
        [SerializeField] JumpingMascotView m_Mascot;

        [Header("Controllers")]
        [SerializeField] BubbleController m_Bubbles;
        [SerializeField] LeverController m_Lever;
        [Tooltip("Optional cheer phrases on progress milestones.")]
        [SerializeField] CheerPhraseController m_Cheer;

        [Header("Shared systems")]
        [SerializeField] GameObject m_EndCard;
        [SerializeField] AudioManager m_Audio;

        GameSessionModel m_Session;
        BubbleGridModel m_Grid;
        BoardController m_Board;
        TrajectoryController m_Trajectory;
        RandomSource m_Random;
        EndCardController m_EndCardController;

        readonly List<int> m_ColorBuffer = new List<int>();
        readonly List<Vector2> m_GuidePoints = new List<Vector2>();

        void Awake()
        {
            m_Session = new GameSessionModel();
            m_Grid = new BubbleGridModel(m_Config.Columns, m_Config.MaxRows);

            m_BoardView.Setup(m_Config.Columns, m_Config.MaxRows);
            m_Random = new RandomSource(12345);
            m_Board = new BoardController(m_Config, m_Grid, m_BoardView, m_Random);
            m_Trajectory = new TrajectoryController(m_Config, m_BoardView, m_Board);

            m_Bubbles.Initialize(m_Config, m_BoardView, m_Board, m_Trajectory);
            m_Lever.Initialize(m_Session, m_Config);

            // The End Card animator idles at scale 0; OpenEndCard() plays its intro animation.
            if (m_EndCard != null)
            {
                m_EndCardController = m_EndCard.GetComponentInChildren<EndCardController>(true);

                // The Replay button lives inside the End Card prefab, which can't reference scene objects.
                // Singular lookup (same call that finds the EndCardController); RunRestarter also
                // self-resolves if this injection doesn't happen.
                RunRestarter restarter = m_EndCard.GetComponentInChildren<RunRestarter>(true);
                if (restarter != null)
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

        void Update()
        {
            // Score popups animate by hand; their view may sit on an inactive template (no Update of its own).
            float deltaTime = SafeTime.Delta;
            if (m_ScorePopups != null)
                m_ScorePopups.Tick(deltaTime);
            if (m_CoinEffect != null)
                m_CoinEffect.Tick(deltaTime);
            if (m_RowClear != null)
                m_RowClear.Tick(deltaTime);
            if (m_Trail != null)
                m_Trail.Tick(m_Bubbles.FlyingView, m_BoardView.GetSprite(m_Bubbles.FlyingColor), deltaTime);
            if (m_Cheer != null)
                m_Cheer.Tick(deltaTime);
        }

        /// <summary>Starts a fresh run in place: new board, score 0, aim centered, no pending timers, End Card hidden.</summary>
        public void ResetGame()
        {
            StopAllCoroutines();
            m_Bubbles.Cancel();
            if (m_ScorePopups != null)
                m_ScorePopups.HideAll();
            if (m_Shake != null)
                m_Shake.Stop();
            if (m_RowClear != null)
                m_RowClear.HideAll();
            if (m_Trail != null)
                m_Trail.HideAll();
            m_Hud.ResetIndicators();
            if (m_CoinEffect != null)
                m_CoinEffect.HideAll();
            if (m_Mascot != null)
                m_Mascot.ResetPose();
            m_Board.Generate();
            if (m_Cheer != null)
                m_Cheer.ResetRun(m_Board.CountBubbles());
            m_Lever.ResetAim();
            m_Lever.SetInputEnabled(true);

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
            // The next indicator jumps onto the current slot, then the new next pops in.
            m_Hud.PlayAdvance(m_BoardView.GetSprite(m_Session.CurrentColor), m_BoardView.GetSprite(m_Session.NextColor));

            Vector2 origin = m_BoardView.WorldToLocal(m_Hud.CurrentBubbleWorldPosition);
            m_Bubbles.Launch(color, m_Session.AimAngle, origin, OnBubbleLanded);
            m_Lever.PlayRecoil();

            if (m_Mascot != null)
                m_Mascot.PlayShotJump();
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
            m_BoardView.WobbleAround(cell.x, cell.y); // impact ripple on the landed bubble and its neighbors
            StartCoroutine(ResolveShot(cell));
        }

        IEnumerator ResolveShot(Vector2Int cell)
        {
            SetState(GameState.Resolving);

            ResolveResult result = m_Board.Resolve(cell);
            if (result.Popped > 0)
            {
                m_Session.Score += result.Popped * m_Config.PointsPerPop + result.Dropped * m_Config.PointsPerDrop;
                BeginClearFeedback();
                ShowClearFeedback(m_Board.PoppedCells, m_Config.PointsPerPop);
                ShowClearFeedback(m_Board.DroppedCells, m_Config.PointsPerDrop);
                RefreshHud();
                ReportProgress();
                ShakeForClear(result.Popped + result.Dropped, result.RowsCleared);
                PlayRowClears();
                PlaySound(SoundID.CoinSound);
                yield return new WaitForSeconds(m_BoardView.ResolveDuration);
            }

            // Only special bubbles left: they can't form a match on their own, so they pop as a bonus.
            if (!m_Board.IsEmpty() && !m_Board.HasRegularBubbles())
            {
                int bonus = m_Board.PopAll();
                m_Session.Score += bonus * m_Config.PointsPerPop;
                BeginClearFeedback();
                ShowClearFeedback(m_Board.PoppedCells, m_Config.PointsPerPop);
                RefreshHud();
                ReportProgress();
                ShakeForClear(bonus, 0);
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
            BeginClearFeedback();
            ShowClearFeedback(m_Board.PoppedCells, m_Config.PointsPerPop);
            RefreshHud();
            ShakeForClear(popped, 0);
            PlaySound(SoundID.CoinSound);
            yield return new WaitForSeconds(m_BoardView.ResolveDuration);

            StartCoroutine(GameClear());
        }

        /// <summary>Tells the cheer phrases how many bubbles are left (milestones at 80/50/20%).</summary>
        void ReportProgress()
        {
            if (m_Cheer != null)
                m_Cheer.OnBubblesRemaining(m_Board.CountBubbles());
        }

        /// <summary>Starts a new feedback burst (coins of one clear get staggered delays).</summary>
        void BeginClearFeedback()
        {
            if (m_CoinEffect != null)
                m_CoinEffect.BeginBurst();
        }

        /// <summary>
        /// Per cleared cell: a "+points" label and a coin flying to the score icon
        /// (positions come from the grid, not the animating views).
        /// </summary>
        void ShowClearFeedback(List<Vector2Int> cells, int points)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 world = m_BoardView.CellToWorld(cells[i].x, cells[i].y);
                if (m_ScorePopups != null)
                    m_ScorePopups.Show(world, points);
                if (m_CoinEffect != null)
                    m_CoinEffect.Launch(world);
            }
        }

        void ShowEndCard()
        {
            if (m_EndCard == null)
            {
                Debug.LogWarning($"[{nameof(GameLoopController)}] End Card not assigned.");
                return;
            }

            // Game canvas stops receiving clicks so only the End Card buttons (CTA, Replay) get them.
            m_Lever.SetInputEnabled(false);

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
                return m_Random.Range(0, m_Board.ColorCount);

            return m_ColorBuffer[m_Random.Range(0, m_ColorBuffer.Count)];
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

        /// <summary>Line-clear sweep for each special bubble popped by the last resolve (left and right along its row).</summary>
        void PlayRowClears()
        {
            if (m_RowClear == null)
                return;

            List<Vector2Int> specials = m_Board.SpecialCells;
            for (int i = 0; i < specials.Count; i++)
            {
                Vector3 left;
                Vector3 right;
                m_BoardView.GetRowEdgesWorld(specials[i].x, out left, out right);
                m_RowClear.Play(m_BoardView.CellToWorld(specials[i].x, specials[i].y), left, right);
            }
        }

        /// <summary>Shake scaled by how much was cleared: a 4-match is a small bump, big clears and row wipes hit harder.</summary>
        void ShakeForClear(int bubblesCleared, int rowsCleared)
        {
            if (m_Shake == null || bubblesCleared <= 0)
                return;

            float strength = 0.35f + 0.05f * bubblesCleared + 0.4f * rowsCleared;
            m_Shake.Shake(Mathf.Min(strength, 1.5f));
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
