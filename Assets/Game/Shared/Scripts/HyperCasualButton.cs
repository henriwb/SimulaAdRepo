using UnityEngine;
using UnityEngine.UI;

namespace HyperCasual.Runner
{
    /// <summary>
    /// A base class for the buttons of the hyper-casual game template that
    /// contains basic functionalities like button sound effect
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HyperCasualButton : MonoBehaviour
    {
        /// <summary>
        /// Adds a listener the button event.
        /// </summary>
        public void AddListener()
        {
        }

        /// <summary>
        /// Removes a listener from the button event.
        /// </summary>
        public void RemoveListener()
        {
        }
    }
}
