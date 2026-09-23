using TMPro;
using UnityEngine;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Score, current/next bubble indicators and "Game Clear" label. View: visuals only.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_ScoreText;
        [SerializeField] BubbleView m_CurrentBubble;
        [SerializeField] BubbleView m_NextBubble;
        [SerializeField] GameObject m_GameClearLabel;

        /// <summary>Where a shot starts, in world space.</summary>
        public Vector3 CurrentBubbleWorldPosition => m_CurrentBubble.transform.position;

        public void SetScore(int score)
        {
            m_ScoreText.text = score.ToString();
        }

        public void SetBubbles(Sprite current, Sprite next)
        {
            m_CurrentBubble.SetSprite(current);
            m_NextBubble.SetSprite(next);
        }

        public void SetGameClearVisible(bool visible)
        {
            if (m_GameClearLabel != null)
                m_GameClearLabel.SetActive(visible);
        }
    }
}
