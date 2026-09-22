using HyperCasual.Core;
using HyperCasual.Gameplay;
using UnityEngine;

public class PlayableSettings : AbstractSingleton<PlayableSettings>
{
    [Header("Must have for playables")]

    [SerializeField] private bool m_SkipMainMenu;
    public bool SkipMainMenu => m_SkipMainMenu;

    [SerializeField] private bool m_UseCTAScreen;
    public bool UseCTAScreen => m_UseCTAScreen;

    [SerializeField] private bool m_EndcardActivationSendsToStore;
    public bool EndcardActivationSendsToStore { get { return m_EndcardActivationSendsToStore; } private set { m_EndcardActivationSendsToStore = value; } }

    [Header("Endcard after X taps")]
    
    [SerializeField]
    bool m_ShowEndcardAfterXTaps;
    
    [SerializeField]
    int m_TapsBeforeShowingEndcard = 3;
    int m_CurrentTapAmount = 0;

    //"Recieve Tap" should be called by what you percieve as player input
    //For this base version, it will be called after each tap on the screen, regardless of whether the tap did something or not.
    public void RecieveTap()
    {
        if (m_CurrentTapAmount >= m_TapsBeforeShowingEndcard)
            return;

        m_CurrentTapAmount++;

        if (m_CurrentTapAmount >= m_TapsBeforeShowingEndcard && m_ShowEndcardAfterXTaps)
            SequenceManager.SendToEndcard();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            RecieveTap();
    }

    public void CallRetryAnalytics()
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.LevelRetry);
#endif
    }

    public void CallDeathAnalytics()
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.LevelFailed);
#endif
    }

    public void CallLevelCompleteAnalytics()
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.LevelWon);
#endif
    }
}

