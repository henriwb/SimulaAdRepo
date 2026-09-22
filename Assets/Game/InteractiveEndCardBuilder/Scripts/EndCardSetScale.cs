using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndCardSetScale : MonoBehaviour
{
    /// <summary>
    /// This script and method is needed because EndCardController.cs is running [ExecuteInEditMode]
    /// meaning the Start method is already called, and will not be called going into play mode.
    /// </summary>
    void Awake()
    {
        transform.localScale = Vector3.zero;
    }
}