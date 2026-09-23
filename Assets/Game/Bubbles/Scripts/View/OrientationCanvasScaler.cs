using UnityEngine;
using UnityEngine.UI;

namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Keeps UI elements the same on-screen size in portrait and landscape by swapping the
    /// CanvasScaler reference resolution (390×844 ↔ 844×390) with Screen Match Mode = Expand.
    /// Example: 390×844 and 844×390 both scale 1.0; 320×568 and 568×320 both scale ~0.67.
    /// Runs in the editor too, so the Game view shows the real landscape scale while laying out
    /// landscape placeholders. Put it on the Canvas that has the CanvasScaler.
    /// View: layout only.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class OrientationCanvasScaler : MonoBehaviour
    {
        [SerializeField] Vector2 m_PortraitResolution = new Vector2(390f, 844f);
        [SerializeField] Vector2 m_LandscapeResolution = new Vector2(844f, 390f);

        CanvasScaler m_Scaler;

        void OnEnable()
        {
            m_Scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void Update()
        {
            Apply();
        }

        void Apply()
        {
            if (m_Scaler == null)
                return;

            Vector2 target = Screen.width > Screen.height ? m_LandscapeResolution : m_PortraitResolution;
            if (m_Scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (m_Scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
                m_Scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (m_Scaler.referenceResolution != target)
                m_Scaler.referenceResolution = target;
        }
    }
}
