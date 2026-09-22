using UnityEngine;
using DG.Tweening;

public class DOTWeenPulseAnim : MonoBehaviour
{
    [SerializeField] private Vector2 pulseSize = new Vector2(1.1f, 1.1f);
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutSine;
    private Vector3 originalSize;
    bool initialized;

    private void OnEnable()
    {
        ConfirmScale();
        transform.DOScale(transform.localScale * pulseSize, pulseDuration).SetEase(ease).SetLoops(-1, LoopType.Yoyo);
    }

    private void ConfirmScale()
    {
        if (!initialized)
        {
            originalSize = transform.localScale;
            initialized = true;
        }
        else
        {
            transform.localScale = originalSize;
        }
    }

    private void OnDisable()
    {
        transform.DOKill();
    }
}
