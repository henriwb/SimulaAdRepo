using UnityEditor;
using UnityEngine;

namespace SimulaAd.Bubbles.Editor
{
    /// <summary>
    /// Adds debug buttons to the GameLoopController inspector (Play Mode only).
    /// Lives in an Editor folder: excluded from player and Playworks builds.
    /// </summary>
    [CustomEditor(typeof(GameLoopController))]
    public class GameLoopControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Clear All Bubbles"))
                    ((GameLoopController)target).DebugClearBoard();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to use the debug buttons.", MessageType.None);
        }
    }
}
