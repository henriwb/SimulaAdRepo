using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Core.Editor
{
    public class LunaWindowCriterion : Criterion
    {
        [SerializeField] private bool showHighlightIfClosed = true;
        
        public static Action OnInvalidate;
        public static LocalizableString OpenLunaWindowHintStr = new LocalizableString("openLunaWindowHint");
        
        SerializedType m_EditorWindowType = new SerializedType(null);

        EditorWindow m_WindowInstance;
        private bool isWindowOpen;
        private VisualElement openLunaWindowHint;

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
            UpdateCompletion();
            EditorApplication.update += UpdateCompletion;
            //OnWindowClosed();
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            EditorApplication.update -= UpdateCompletion;
            openLunaWindowHint = null;
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns></returns>
        protected override bool EvaluateCompletion()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            foreach (var w in windows)
            {
                if (w.GetType().ToString().Contains( "LunaExportWindow" ))
                {
                    if (!isWindowOpen)
                    {
                        OnWindowOpen( (EditorWindow) w );
                    }

                    return true;
                }
            }

            //if (isWindowOpen)
            //{
                OnWindowClosed();
            //}
            return false;
        }

        private void OnWindowOpen( EditorWindow w )
        {
            isWindowOpen = true;
            m_WindowInstance = w;
            //m_WindowInstance.Focus();
            if ( showHighlightIfClosed && openLunaWindowHint?.parent != null)
            {
                openLunaWindowHint.RemoveFromHierarchy();
                openLunaWindowHint = null;
            }
        }

        private void OnWindowClosed()
        {
            if (showHighlightIfClosed && openLunaWindowHint == null)
            {
                OnInvalidate?.Invoke();
                openLunaWindowHint = new Label(OpenLunaWindowHintStr)
                {
                    style =
                    {
                        color = Color.red,
                        whiteSpace = new StyleEnum<WhiteSpace>( WhiteSpace.Normal ),
                    }
                };
                TutorialWindow.TutorialViewInstance.tutorialScrollview.Add(openLunaWindowHint);
                isWindowOpen = false;
            }
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            if (m_EditorWindowType.Type == null)
            {
                return false;
            }

            EditorWindow.GetWindow(m_EditorWindowType.Type);
            return true;
        }
    }
}