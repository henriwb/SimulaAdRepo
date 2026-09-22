using UnityEngine;

namespace Game
{
    public class SomeNotSupportedAPIUsageExample1
    {
        public void NotSupportedAPIUsageExample()
        {
#if PHC_TUTORIAL
            // I am not supported in Luna - please remove me or comment
            var label = new UnityEngine.UIElements.Label( "I am not supported in luna" );
            #endif
        }
    }
}