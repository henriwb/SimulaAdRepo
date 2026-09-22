#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES

using Luna.Unity.EditorScripts;
using Luna.Unity.Tutorial;
using Unity.Tutorials.Core.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LunaTutorial {

    [CreateAssetMenu(fileName = "TutorialCallbacks", menuName = "ScriptableObjects/TutorialCallbacks", order = 1)]
    public class TutorialCallbacks : ScriptableObject {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        private static bool isOpenedInBrowser;
        private static bool isStartedCheckingIfOpenedInBrowser;
        private static HashSet<string> pressedControls = new HashSet<string>();
        private static bool isCompressionSettingsChanged;
        private bool isBuiltStarted;

        public static bool isOverrideTypeSelected;

        [InitializeOnLoadMethod]
        public static void InitializeTutorialCallback() {

            TutorialMediator.OnOpenInBrowser += () => isOpenedInBrowser = isStartedCheckingIfOpenedInBrowser;
            TutorialMediator.StatisticsElementSelected += (newValue) => isOverrideTypeSelected = newValue;
            TutorialMediator.ConfigFileChanged += () => isCompressionSettingsChanged = isCompressionSettingsChangedCheckingStarted;
        }

        public void ResetStates()
        {
            pressedControls.Clear();
            isCompressionSettingsChanged = false;
            isOpenedInBrowser = false;
            isBuiltStarted = false;
            isCompressionSettingsChangedCheckingStarted = false;
        }

        public bool IsSBTreeAssetSelected()
        {
            return isOverrideTypeSelected;
        }
        
        public bool IsControlPressed( string controlName )
        {
            if (!TutorialMediator.IsLunaWindowInit)
            {
                return false;
            }

            if (TutorialMediator.IsControlPressed(controlName))
            {
                pressedControls.Add( controlName );
            }

            return pressedControls.Contains(controlName);
        }

        public bool IsPHCIssueSelected()
        {
            return IsControlPressed( "PHCIssue" );
        }

        public bool IsTreeViewItemEnabled()
        {
            return IsControlPressed( "TreeViewEnableItem" );
        }

        public bool IsTreeViewItemDisabled()
        {

            return IsControlPressed( "TreeViewDisableItem" );

        }
        
        public bool IsRecalculateAssetSize()
        {
            return IsControlPressed( "SizeBreakdownSection_RecalculateAssetSize" );
        }
        
        public void EndPHCTutorial()
        {
            TutorialMediator.RemoveExternalDefine("PHC_TUTORIAL");
        }

        public bool IsLunaSolutionOpen()
        {
            if (IsControlPressed("CodeSection_OpenSolution"))
            {
                EditorPrefs.SetBool( "CodeSection_OpenSolution", true );
                return true;
            }
            return EditorPrefs.GetBool( "CodeSection_OpenSolution", false );
        }

        public void ResetLunaSolutionOpen()
        {
            EditorPrefs.SetBool( "CodeSection_OpenSolution", false );
        }

        public bool IsStartupScene( string scenePath = "Assets/Game/Shared/Scenes/Boot.unity" )
        {
            return TutorialMediator.StartupScene() == scenePath;
        }
        
        public void ResetSectionAndTabs()
        {
            EditorApplication.update += ResetWindowWhenPossible;
        }

        public void ResetWindowWhenPossible()
        {
            if (TutorialMediator.IsLunaWindowInit)
            {
                TutorialMediator.ResetSectionAndTabs();
                EditorApplication.update -= ResetWindowWhenPossible;
            }
        }
        
        public void PHCTutorialStart()
        {
            ResetStates();
            ResetSectionAndTabs();
            TutorialMediator.AddExternalDefine("PHC_TUTORIAL");
        }
        
        public bool IsCodeExcluded(string filePath)
        {
            return TutorialMediator.ExcludedScripts.Contains( filePath );
        }
        
        public bool IsFixAllErrorsPressed()
        {
            return IsControlPressed( "ProjectDiagnostics_FixAll" );
        }

        public bool isSceneJustIncluded( string scenePath )
        {
            return TutorialMediator.ScenesInBuild.Contains(scenePath);
        }

        public bool IsSectionSelected( string sectionName ) {
            if (TutorialMediator.IsLunaWindowInit)
            {
                return TutorialMediator.SelectedSectionName() == sectionName;
            }

            return false;
        }

        public bool IsTabSelected( string tabName ) {
            if (TutorialMediator.IsLunaWindowInit)
            {
                return TutorialMediator.SelectedTabName() == tabName;
            }

            return false;
        }

        public bool IsUserSignedIn() {
            return TutorialMediator.isUserSignedIn;
        }
        
        public bool IsPHCCategoryOpened()
        {
            return IsControlPressed( "PHCCategoryDropdown" );
        }

        public bool IsBuildStartedAndFinished()
        {
            if (IsBuildInProcess())
            {
                isBuiltStarted = true;
            }

            return isBuiltStarted && IsBuildNotInProcess();
        }
        
        public bool IsBuildInProcess() {
            return TutorialMediator.IsBuildInProcess;
        }

        public bool IsBuildNotInProcess() {
            return !IsBuildInProcess();
        }

        public void OpenCodeFile( string filePath )
        {
            var fullFilePath = Path.Combine(Application.dataPath, filePath);
            var psi = new ProcessStartInfo(fullFilePath)
            {
                Verb = "open",
                UseShellExecute = true,
                CreateNoWindow = false,
                FileName = fullFilePath
            };
            Process.Start( psi );
        }

        private static bool isCompressionSettingsChangedCheckingStarted;
        public bool IsCompressionSettingsChanged()
        {
            isCompressionSettingsChangedCheckingStarted = true;
            return isCompressionSettingsChanged;
        }

        public bool IsOpenedInBrowser() {
            isStartedCheckingIfOpenedInBrowser = true;
            return isOpenedInBrowser;
        }

#endif
        public void OpenPackageManagerWindow()
        {
            try
            {
                UnityEditor.PackageManager.UI.Window.Open("");
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }

}
