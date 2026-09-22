using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HyperCasual.Core
{
    /// <summary>
    /// The base class for all UI elements that can be registered in UIManager
    /// </summary>
    public abstract class View : MonoBehaviour
    {
        /// <summary>
        /// Makes the View visible
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the view
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}