using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tutorials.Localization;
using Unity.Tutorials.Core.Editor;
using UnityEngine;
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
using Luna.Unity.Tutorial;
using LunaTutorial;
#endif
using UnityEditor;

namespace Tutorial.Localization
{
    [InitializeOnLoad]
    public class Loc
    {
        public const string ENGLISH_LANGUAGE_CODE = "en-UK"; 
        public const string RUSSIAN_LANGUAGE_CODE = "ru-RU";
        public const string CHINESE_LANGUAGE_CODE = "zh-CN";
        public const string DEFAULT_LANGUAGE_CODE = ENGLISH_LANGUAGE_CODE;
        public const string SELECTED_LANGUAGE_SAVE_KEY = "LunaTutorial-SelectedLanguage";
        public static LocalizationDictionary TranslationDictionary;
        public static Dictionary<string, LocalizationDictionary> LanguageDictionaries;
        public static string[] AvailableLanguages = { DEFAULT_LANGUAGE_CODE };
        public static event Action LanguageChanged;
        public static int LanguageIndex { get; private set; } = -1;
        private static bool isLanguageAppliedAlready;

        static Loc()
        {
            EditorApplication.update += () => isLanguageAppliedAlready = false;
        }
        
        public static void ReadCsvFromFile(string path)
        {
            try
            {
                using (var reader = new System.IO.StreamReader(path))
                {
                    ReadCsv(reader);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Localisation initialisation failed. Phrases keys will be shown.\n{e.Message}");
                TranslationDictionary = new LocalizationDictionary(DEFAULT_LANGUAGE_CODE);
                LanguageDictionaries = new Dictionary<string, LocalizationDictionary>()
                {
                    { DEFAULT_LANGUAGE_CODE, TranslationDictionary }
                };
            }
        }

        public static void ReadCsv(TextReader reader)
        {
            var csvReader = new CsvReader(reader, ",");

            var localizations = new List<LocalizationDictionary>();
            var languageNames = new List<string>();
            LanguageDictionaries = new Dictionary<string, LocalizationDictionary>();
            var missingTranslations = new StringBuilder();

            if (csvReader.Read())
            {
                for (var i = 1; i < csvReader.FieldsCount; i++)
                {
                    var language = csvReader[i];
                    language = language.Trim();
                    languageNames.Add(language);
                    localizations.Add(new LocalizationDictionary(language));
                }
            }

            while (csvReader.Read())
            {
                var phrase = csvReader[0];
                if (string.IsNullOrEmpty(phrase))
                {
                    continue;
                }

                for (var i = 1; i < csvReader.FieldsCount; i++)
                {
                    var translation = csvReader[i];
                    if (string.IsNullOrEmpty(translation))
                    {
                        missingTranslations.AppendLine($"[ {localizations[i - 1].LanguageCode} ] {phrase}");
                        continue;
                    }

                    // Unescapes special symbols from file (\\n \\t) to become actual special symbols (\n and \t)
                    translation = Regex.Unescape(translation);
                    localizations[i - 1].Add(phrase, translation);
                }
            }

            for (int i = 0; i < localizations.Count; i++)
            {
                var languageName = languageNames[i];
                LanguageDictionaries.Add(languageName, localizations[i]);
            }

            AvailableLanguages = LanguageDictionaries.Keys.ToArray();

            // Notify developers that translations are missing while using sources,
            // as UNITY_EDITOR will strip this code out for clients and we don't want to bother them with missing
#if UNITY_EDITOR
            if (missingTranslations.Length > 0)
            {
                Debug.LogError($"Localisation is missing for these keys:\n{missingTranslations}");
            }
#endif
        }
                
        public static void ApplyLanguage( string language ) {
            if (isLanguageAppliedAlready)
            {
                return;
            }
            if ( !LanguageDictionaries.ContainsKey( language ) ) {
                language = DEFAULT_LANGUAGE_CODE;
            }

            LanguageIndex = Array.IndexOf( AvailableLanguages, language );
            EditorPrefs.SetString( SELECTED_LANGUAGE_SAVE_KEY, language );
            TranslationDictionary = LanguageDictionaries[ language ];

            //LunaWindowAPI.Language.ChangeLanguage( language );
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
            if (TutorialMediator.IsLunaWindowInit)
            {
                try
                {
                    TutorialMediator.ChangeLanguage(language);
                } catch (Exception e) {
                    // ignored
                }
            }
#endif
            LanguageChanged?.Invoke();
            
            LunaWindowCriterion.OpenLunaWindowHintStr.Translated = Loc.Get( LunaWindowCriterion.OpenLunaWindowHintStr.Untranslated );
            LunaPackageInstalledCriteria.InstallPackageHintStr.Translated = Loc.Get(LunaPackageInstalledCriteria.InstallPackageHintStr.Untranslated);
            isLanguageAppliedAlready = true;
        }

        public static string Get(string phrase)
        {
            try
            {
                var translation = TranslationDictionary.ContainsKey(phrase)
                    ? TranslationDictionary[phrase]
                    : phrase;
                return translation;
            }
            catch (Exception)
            {
                Debug.LogError( "default phrase returned: " + phrase );
                return phrase;
            }
        }

        public class LocalizationDictionary : Dictionary<string, string>
        {
            public string LanguageCode;
            public string Language;

            public LocalizationDictionary(string languageCode)
            {
                LanguageCode = languageCode;
                Language = LanguageCode.Split('-')[0];
            }
        }
    }
}