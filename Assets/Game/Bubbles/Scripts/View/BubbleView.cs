using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// One bubble on screen (UI Image). View: sprite, size, position and animations only.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class BubbleView : MonoBehaviour
    {
        Image m_Image;
        RectTransform m_Rect;

        void Awake()
        {
            EnsureCache();
        }

        void EnsureCache()
        {
            if (m_Image != null)
                return;

            m_Image = GetComponent<Image>();
            m_Rect = (RectTransform)transform;
        }

        public void Show(Sprite sprite, float diameter)
        {
            EnsureCache();
            KillTweens();
            m_Rect.localScale = Vector3.one;
            m_Image.color = Color.white;
            m_Image.sprite = sprite;
            SetDiameter(diameter);
            gameObject.SetActive(true);
        }

        public void SetSprite(Sprite sprite)
        {
            EnsureCache();
            m_Image.sprite = sprite;
        }

        public void SetDiameter(float diameter)
        {
            EnsureCache();
            m_Rect.sizeDelta = new Vector2(diameter, diameter);
        }

        public void SetLocalPosition(Vector2 position)
        {
            EnsureCache();
            m_Rect.localPosition = new Vector3(position.x, position.y, 0f);
        }

        public void Pop(float duration, Action onComplete)
        {
            EnsureCache();
            KillTweens();
            m_Rect.DOScale(0f, duration).SetEase(Ease.InBack).OnComplete(() => onComplete());
        }

        public void Drop(float distance, float duration, Action onComplete)
        {
            EnsureCache();
            KillTweens();
            m_Rect.DOLocalMoveY(m_Rect.localPosition.y - distance, duration).SetEase(Ease.InQuad);
            m_Image.DOFade(0f, duration).OnComplete(() => onComplete());
        }

        /// <summary>Stops animations without firing their callbacks and hides the bubble.</summary>
        public void Hide()
        {
            EnsureCache();
            KillTweens();
            gameObject.SetActive(false);
        }

        void KillTweens()
        {
            m_Rect.DOKill();
            m_Image.DOKill();
        }
    }
}
