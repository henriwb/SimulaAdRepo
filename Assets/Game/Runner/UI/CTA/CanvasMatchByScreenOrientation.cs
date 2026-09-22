using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMatchByScreenOrientation : MonoBehaviour
{
    private CanvasScaler scaler;
    [Range(0f, 1f)]
    [SerializeField] private float portraitWidthOrHeight = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float landscapeWidthOrHeight = 0f;


    // Start is called before the first frame update
    void Start()
    {
        scaler = GetComponent<CanvasScaler>();
        OrientationManager.OrientationChangeEvent += UpdateMatch;
        InitializeMatch();
    }

    private void InitializeMatch()
    {
        float screenRatio = (Screen.width / Screen.height);
        bool isPortrait = screenRatio < 1 ? true : false;
        UpdateMatch(isPortrait);
    }

    private void UpdateMatch(bool isPortrait)
    {
        if (isPortrait)
        {
            scaler.matchWidthOrHeight = portraitWidthOrHeight;
        }
        else if (!isPortrait)
        {
            scaler.matchWidthOrHeight = landscapeWidthOrHeight;
        }
    }
}
