using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationManager : MonoBehaviour
{
    private float lastScreenRatio;
    private bool isCurrentlyPortrait;
    public bool IsCurrentlyPortrait { get {
            CheckIfPortrait();
            return isCurrentlyPortrait; 
        } }
    public static event Action<bool> OrientationChangeEvent;

    #region Singleton
    public static OrientationManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

    }
    #endregion

    private void Start()
    {
        InitializeOrientation();
    }

    private void InitializeOrientation()
    {
        CheckIfPortrait();
        UpdateOrientation(isCurrentlyPortrait);
    }

    private void Update()
    {
        HandleScreenOrientationChange();
    }

    private void UpdateOrientation(bool portrait)
    {
        OrientationChangeEvent?.Invoke(portrait);
    }


    private void HandleScreenOrientationChange()
    {
        if(CheckIfPortrait())
            UpdateOrientation(isCurrentlyPortrait);
    }

    private bool CheckIfPortrait()
    {
        bool changedOrientation = false;
        float screenRatio = ((float)Screen.width / (float)Screen.height);   

        if (screenRatio != lastScreenRatio)
        {
            changedOrientation = true;

            if (screenRatio >= 1)
                // Landscape Layout
                isCurrentlyPortrait = false;

            else if (screenRatio < 1)
                // Portrait Layout
                isCurrentlyPortrait = true;
        }

        lastScreenRatio = screenRatio;

        return changedOrientation;
    }

    private void OnDestroy()
    {
        OrientationChangeEvent = null;
    }
}

