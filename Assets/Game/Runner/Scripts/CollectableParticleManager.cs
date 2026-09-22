using System.Collections;
using System.Collections.Generic;
using HyperCasual.Core;
using UnityEngine;

public class CollectableParticleManager : AbstractSingleton<CollectableParticleManager>
{
    [SerializeField] private GameObject keyParticlePrefab;
    [SerializeField] private GameObject coinParticlePrefab;
    [SerializeField] private int keyParticleCount;
    [SerializeField] private int coinParticlesCount;
    private ParticleSystem[] keyParticles;
    private ParticleSystem[] coinParticles;

    private int coinIndex;
    private int keyIndex;
    
    void Start()
    {
        coinParticles = PreparePool(coinParticlesCount, coinParticlePrefab);
        keyParticles = PreparePool(keyParticleCount, keyParticlePrefab);
    }

    public void PlayParticle(Transform collectable, CollectableParticleID particleID)
    {
        if (particleID == CollectableParticleID.Coin)
        {
            coinIndex = Play(collectable.position, coinParticlesCount, coinParticles, coinIndex);
        }
        if (particleID == CollectableParticleID.Key)
        {
            keyIndex = Play(collectable.position, keyParticleCount, keyParticles, keyIndex);
        }
    }

    ParticleSystem[] PreparePool(int count, GameObject prefab)
    {
        var particles = new ParticleSystem[count];
        for (int i = 0; i < count; i++)
        {
            particles[i] = Instantiate(prefab).GetComponent<ParticleSystem>();
        }
        return particles;
    }

    private int Play(Vector3 position, int count, ParticleSystem[] pool, int index)
    {
        var newEffect = pool[index];
        newEffect.transform.position = position;
        newEffect.Play();
        return (index + 1) % count;
    }
}


public enum CollectableParticleID
{
    None,
    Coin,
    Key
}