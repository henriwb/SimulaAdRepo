using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Score, current/next bubble indicators, shot button and "Game Clear" label.
    /// View: visuals and button forwarding only.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_ScoreText;
        [SerializeField] BubbleView m_CurrentBubble;
        [SerializeField] BubbleView m_NextBubble;
        [SerializeField] Button m_ShootButton;
        [SerializeField] GameObject m_GameClearLabel;

        public event Action ShootPressed;

        /// <summary>Where a shot starts, in world space.</summary>
        public Vector3 CurrentBubbleWorldPosition => m_CurrentBubble.transform.position;

        void Awake()
        {
            m_ShootButton.onClick.AddListener(OnShootClicked);
        }

        void OnDestroy()
        {
            if (m_ShootButton != null)
                m_ShootButton.onClick.RemoveListener(OnShootClicked);
        }

        public void SetScore(int score)
        {
            m_ScoreText.text = score.ToString();
        }

        public void SetBubbles(Sprite current, Sprite next)
        {
            m_CurrentBubble.SetSprite(current);
            m_NextBubble.SetSprite(next);
        }

        public void SetShootEnabled(bool enabled)
        {
            m_ShootButton.interactable = enabled;
        }

        public void SetGameClearVisible(bool visible)
        {
            if (m_GameClearLabel != null)
                m_GameClearLabel.SetActive(visible);
        }

        void OnShootClicked()
        {
            if (ShootPressed != null)
                ShootPressed();
        }
    }
}
