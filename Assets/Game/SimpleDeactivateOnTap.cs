using UnityEngine;

public class SimpleDeactivateOnTap : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            gameObject.SetActive(false);
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
            // Example of events usage
            Luna.Unity.Analytics.LogEvent( Luna.Unity.Analytics.EventType.TutorialComplete );
            Luna.Unity.Analytics.LogEvent( "some custom event implementation example", 0 );
#endif
        }
    }
}
