using System;
using System.Collections;
using System.Collections.Generic;
using HyperCasual.Core;
using HyperCasual.Gameplay;
using HyperCasual.Runner;
using UnityEngine;
using UnityEngine.UI;

public class CoinEffectManager : AbstractSingleton<CoinEffectManager>, IGameEventListener
{

    [SerializeField] private CoinEffectMove coinEffectPrefab;
    [SerializeField] private RectTransform coinCounter;
    [SerializeField] private PlayerController player;
    [SerializeField] private float speed;
    [SerializeField] AbstractGameEvent m_CoinEvent;

    private CoinEffectMove[] coinEffects;
    [SerializeField] private int effectPoolCount;
    private int index;

    private float targetZ;
    [SerializeField] private float offsetZ = 12.5f;
    void Start()
    {
        m_CoinEvent.AddListener(this);
        // Get Z axis position of the target for the coins
        targetZ = Camera.main.nearClipPlane * offsetZ;
        PrepareEffects();
    }

    private void Update()
    {
        //Get position in world that the coins should move towards (coin counter)
        var screenPos = RectTransformUtility.WorldToScreenPoint(null, coinCounter.position);
        Vector3 worldPos =
            Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, targetZ));
        transform.position = worldPos;
        
    }

    void PrepareEffects()
    {
        coinEffects = new CoinEffectMove[effectPoolCount];
        for (int i = 0; i < effectPoolCount; i++)
        {
            var newCoinEffect = Instantiate(coinEffectPrefab, transform.position, Quaternion.identity);
            newCoinEffect.transform.parent = transform;
            coinEffects[i] = newCoinEffect;
        }
    }
    
    public void OnEventRaised()
    {
        CollectedCoin();
    }

    private void CollectedCoin()
    {
        var newCoinEffect = coinEffects[index];
        newCoinEffect.transform.position = player.transform.position;
        newCoinEffect.Init(transform, speed);
        index = (index + 1) % effectPoolCount;
    }

    public void Disable()
    {
        transform.gameObject.SetActive(false);
    }
    
}
