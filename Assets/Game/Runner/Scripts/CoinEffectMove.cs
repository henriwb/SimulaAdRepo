using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinEffectMove : MonoBehaviour
{
    private Transform target;
    private float speed;
    public void Init(Transform target, float speed)
    {
        this.target = target;
        this.speed = speed;
    }
    void Update()
    {
        if (target == null)
        {
            return;
        }
        transform.position = Vector3.Slerp(transform.position, target.position, speed * Time.deltaTime);
        
    }

}
