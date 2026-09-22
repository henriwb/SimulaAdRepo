using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using HyperCasual.Core;
using HyperCasual.Gameplay;
using HyperCasual.Runner;
using UnityEngine;

public class OutroAnimationController : MonoBehaviour, IGameEventListener
{
    private PlayerController playerController;
    [SerializeField] private AbstractGameEvent winEvent;

    private SoundID endMusic = SoundID.EndSound;
    
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundField("End Card delay", 3, "Endcard Clickable Options")]
#endif
    [SerializeField]
    private float endCardDelay = 2;

    [SerializeField]
    private ParticleSystem[] confettiParticles;
    
    private static readonly string OutroTrigger = "TriggerOutro";

    void Start()
    {
        playerController = PlayerController.Instance;
        winEvent.AddListener(this);
    }
    
    public void OnEventRaised()
    {
        StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        AudioManager.Instance.PlayEffect(endMusic);
        playerController.transform.DORotate(new Vector3(0, 180, 0), 1, RotateMode.Fast);
        for (int i = 0; i < confettiParticles.Length; i++)
        {
            confettiParticles[i].gameObject.SetActive(true);
        }
        CoinEffectManager.Instance.Disable();
        playerController.PlayerWon();
        CameraManager.Instance.GetComponent<Animator>().SetTrigger(OutroTrigger);
        yield return new WaitForSeconds(endCardDelay);
        SequenceManager.SendToEndcard();
    }
    
}
