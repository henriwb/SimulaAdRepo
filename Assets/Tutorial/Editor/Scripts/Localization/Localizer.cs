using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Tutorial.Localization
{
    [InitializeOnLoad]
    [CreateAssetMenu(fileName = "Localizer", menuName = "ScriptableObjects/Localizer", order = 1)]
    public class Localizer : ScriptableObject
    {
        private const string NO_TRANSLATION_CODE = "-";
        private static string locFilePath = Path.Combine( "Assets", "Tutorial", "Data", "Localization", "localization.csv" );
        
        static Localizer()
        {
            Loc.ReadCsvFromFile( locFilePath );
            Loc.LanguageChanged += TranslateAll;
            // we can't do it in static ctor, so delay
            EditorApplication.update += SetupLanguage;
            LunaWindowCriterion.OnInvalidate += () => LunaWindowCriterion.OpenLunaWindowHintStr.Translated = Loc.Get( LunaWindowCriterion.OpenLunaWindowHintStr.Untranslated );
            LunaWindowCriterion.OnInvalidate += () => LunaPackageInstalledCriteria.InstallPackageHintStr.Translated = Loc.Get(LunaPackageInstalledCriteria.InstallPackageHintStr.Untranslated);
        }

        private static void SetupLanguage()
        {
            EditorApplication.update -= SetupLanguage;
            var selectedLanguage = EditorPrefs.GetString(Loc.SELECTED_LANGUAGE_SAVE_KEY) ?? Loc.DEFAULT_LANGUAGE_CODE;
            Loc.ApplyLanguage(selectedLanguage);
        }

        [MenuItem("Tutorial Language/Ru")]
        public static void SetRussianLanguage()
        {
            Loc.ReadCsvFromFile( locFilePath );
            Loc.ApplyLanguage( Loc.RUSSIAN_LANGUAGE_CODE );
        }

        [MenuItem("Tutorial Language/ZH-cn")]
        public static void SetChineseLanguage()
        {
            Loc.ReadCsvFromFile( locFilePath );
            Loc.ApplyLanguage( Loc.CHINESE_LANGUAGE_CODE );
        }

        [MenuItem("Tutorial Language/EN")]
        public static void SetEnglishLanguage()
        {
            Loc.ReadCsvFromFile( locFilePath );
            Loc.ApplyLanguage( Loc.ENGLISH_LANGUAGE_CODE );
        }

        private static bool isLanguageNotSelected()
        {
            return Loc.LanguageIndex == -1;
        }

        public static void TranslateAll()
        {
            if (isLanguageNotSelected())
            {
                var selectedLanguage = EditorPrefs.GetString(Loc.SELECTED_LANGUAGE_SAVE_KEY, Loc.DEFAULT_LANGUAGE_CODE);
                Loc.ApplyLanguage( selectedLanguage );
            } 
            FindAssets<TutorialWelcomePage>().ToList().ForEach(TranslateTutorialWelcomePage);
            FindAssets<TutorialContainer>().ToList().ForEach(TranslateTutorialContainer);
            FindAssets<Unity.Tutorials.Core.Editor.Tutorial>().ToList().ForEach(TranslateTutorial);
            TutorialWindow.Instance.TableOfContentView.Refresh();
        }

        static void TranslateTutorialWelcomePage(TutorialWelcomePage welcomePg)
        {
            TranslateObject(welcomePg);
            foreach (var button in welcomePg.Buttons)
            {
                TranslateObject(button);
            }
        }

        static void TranslateTutorialContainer(TutorialContainer container)
        {
            TranslateObject(container);
            foreach (var section in container.Sections)
            {
                TranslateObject(section);
            }
        }

        static void TranslateTutorial(Unity.Tutorials.Core.Editor.Tutorial tutorial)
        {
            TranslateObject(tutorial);
            foreach (var pg in tutorial.PagesCollection)
            {
                TranslateTutorialPage(pg);
            }
        }

        static void TranslateTutorialPage(TutorialPage pg)
        {
            TranslateObject(pg);
            foreach (var paragraph in pg.Paragraphs)
            {
                TranslateObject(paragraph);
            }
        }

        static void TranslateObject(object obj)
        {
            const BindingFlags bindedTypes = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                             BindingFlags.NonPublic;
            var localizableStringType = typeof(LocalizableString);
            obj.GetType().GetProperties(bindedTypes)
                .Where(pi => pi.PropertyType == localizableStringType && pi.CanWrite)
                .ToList()
                .ForEach(pi =>
                {
                    var str = pi.GetValue(obj) as LocalizableString;
                    str.Translated = Loc.Get( str.Untranslated );
                    pi.SetValue(obj, str);
                });

            obj.GetType().GetFields(bindedTypes)
                .Where(fi => fi.FieldType == localizableStringType)
                .ToList()
                .ForEach(fi =>
                {
                    var str = fi.GetValue(obj) as LocalizableString;
                    if (str != null)
                    {
                        str.Translated = Loc.Get(str.Untranslated);
                        fi.SetValue(obj, str);
                    }
                });
        }

        static IEnumerable<T> FindAssets<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).FullName}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>);
    }
}
