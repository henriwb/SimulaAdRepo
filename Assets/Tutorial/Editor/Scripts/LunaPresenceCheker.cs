using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LunaTutorial
{
    [ InitializeOnLoad ]
    public class LunaPresenceCheker
    {
        private static int i = 0;
        static LunaPresenceCheker()
        {
            EditorApplication.update += Check;
        }

        private static void Check()
        {
            if (i % 100 == 0)
            {
                var textInManifest = File.ReadAllText( manifestPath );

                var match = Regex.Match(textInManifest, "uk.lunalabs.luna");
                if (match.Success) {
                    #if LUNA_IS_PRESENT
                    return;
                    #endif
                    var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out string[] otherDefines );
                    var newDefines = otherDefines.Concat(new string[] { "LUNA_IS_PRESENT" }).ToArray();
                    
                    PlayerSettings.SetScriptingDefineSymbolsForGroup( buildTargetGroup, newDefines );
                } else {
                    #if !LUNA_IS_PRESENT 
                    return;
                    #endif
                    var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out string[] usedDefines );
                    var newDefines = usedDefines.Where(d => d != "LUNA_IS_PRESENT").ToArray();
                    PlayerSettings.SetScriptingDefineSymbolsForGroup( buildTargetGroup, newDefines );
                }
            }
        }

        public static string manifestPath {
            get => Path.Combine( projectPath, "Packages", "manifest.json" );
        }
        
        public static string projectPath =>
            Path.GetFullPath( Path.Combine( Application.dataPath, ".." ) ) +
            Path.DirectorySeparatorChar;
    }
}