using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Tutorials.Core.Editor.TutorialContainer;

namespace Unity.Tutorials.Core.Editor
{
    public class TableOfContentView : View
    {
        internal const string k_Name = "TableOfContent";
        internal override string Name => k_Name;
        VisualElement m_Root;
        VisualElement m_TutorialsContainer;

        internal int CategoriesOrTutorialsCurrentlyVisibile => m_TutorialsContainer.childCount;

        TableOfContentModel Model => Application.Model.TableOfContent;
        List<Tuple<VisualElement, Section>> m_ActiveSections;
        bool m_SectionsInitialized = false;
        EditorCoroutine m_CheckmarksUpdateRoutine;

        private Color linksColor = new Color(0.2352f, 0.5082f, 0.90816f, 1f );

        public TableOfContentView() : base() { }

        internal void Initialize(VisualElement root)
        {
            m_Root = root;
            m_TutorialsContainer = m_Root.Q("lstTutorials");
            m_TutorialsContainer.style.alignItems = Align.Center;
            m_ActiveSections = new List<Tuple<VisualElement, Section>>();
            Refresh();
        }

        public void Refresh()
        {
            m_TutorialsContainer?.Clear();
            try
            {
                LoadHeader();
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                LoadCategories();
            }
            catch (Exception)
            {
                // ignored
            }
            
            try
            {
                LoadTutorialsAndLinks();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        void GoBackInContainerHierachy()
        {
            Application.Broadcast(new BackButtonClickedEvent());
        }

        void LoadHeader()
        {
            VisualElement imgTitleHeader = m_Root.Q("imgTitleHeader");
            TutorialContainer currentCategory = Model.CurrentCategory;
            string subtitle = string.Empty;
            string title = string.Empty;

            if (currentCategory)
            {
                subtitle = currentCategory.Subtitle.Value;
                title = currentCategory.Title.Value;
                //imgTitleHeader.style.backgroundImage = currentCategory.BackgroundImage;
            }
            else
            {
                title = currentCategory.Title.Value;
                subtitle = currentCategory.Subtitle.Value;
                imgTitleHeader.style.backgroundImage = null;
            }

            if (title == TutorialView.NO_TRANSLATION_CODE) {
                UIElementsUtils.Hide("lblTitle", imgTitleHeader );
            } else {
                UIElementsUtils.SetupLabel("lblTitle", title, imgTitleHeader, false);
            }

            if (subtitle == TutorialView.NO_TRANSLATION_CODE) {
                UIElementsUtils.Hide("lblSubtitle", imgTitleHeader);
            } else {
                UIElementsUtils.Show("lblSubtitle", imgTitleHeader);
                UIElementsUtils.SetupLabel("lblSubtitle", subtitle, imgTitleHeader, false);
            }

            bool enableBackButton = Model.CurrentCategory && (Model.CurrentCategory.ParentContainer || Model.RootCategoriesOfProject.Count() > 1);
            if (!enableBackButton)
            {
                UIElementsUtils.Hide( "btnExitCategory", imgTitleHeader);
            }
            else
            {
                UIElementsUtils.SetupButton("btnExitCategory", GoBackInContainerHierachy, true, imgTitleHeader, string.Empty, Localization.Tr(LocalizationKeys.k_TOCButtonBackTooltip));
            }
        }

        void LoadCategories()
        {
            IEnumerable<TutorialContainer> categoriesToLoad = Model.CurrentCategory == null ? Model.RootCategoriesOfProject
                                                                                            : Model.CurrentCategory.FindSubCategories();

            if (categoriesToLoad == null) { return; }
            VisualTreeAsset tutorialCategoryUIPrefab = UIElementsUtils.LoadUXML("TutorialCategoryUI");
            VisualElement categoryUI;
            foreach (var category in categoriesToLoad)
            {
                categoryUI = tutorialCategoryUIPrefab.CloneTree();
                SetupCategoryUI(categoryUI, category);
                m_TutorialsContainer.Add(categoryUI);
            }
        }

        internal void SetupSectionUI(VisualElement sectionUI, Section data)
        {
            if (data.Heading.Translated == TutorialView.NO_TRANSLATION_CODE)
            {
                UIElementsUtils.Hide("lblName", sectionUI);
            }
            else
            {
                var label = UIElementsUtils.SetupLabel("lblName", data.Heading, sectionUI, false);
                if (!data.IsTutorial)
                {
                    label.style.color = new StyleColor( linksColor );
                    //label.style.borderBottomColor = linksColor;
                    //label.style.borderBottomWidth = 1;
                    label.tooltip = data.Url;
                }
            }

            if (data.Text.Translated == TutorialView.NO_TRANSLATION_CODE)
            {
                UIElementsUtils.Hide("lblDescription", sectionUI);
            }
            else
            {
                var label = UIElementsUtils.SetupLabel("lblDescription", data.Text, sectionUI, false);
                if (!data.IsTutorial)
                {
                    label.style.color = new StyleColor( linksColor );
                    //label.style.borderBottomColor = linksColor;
                    //label.style.borderBottomWidth = 1;
                    label.tooltip = data.Url;
                }
            }

            if (data.Image != null)
            {
                sectionUI.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(data.Image);
            }

            sectionUI.UnregisterCallback<MouseUpEvent, Section>(OnSectionClicked);

            UIElementsUtils.ShowOrHide("imgErrorCheckmark", sectionUI, !data.IsConfiguredCorrectly);

            if (data.IsConfiguredCorrectly)
            {
                //sectionUI.tooltip = Localization.Tr(LocalizationKeys.k_TutorialSectionTooltip) + data.Text;
                if (data.IsTutorial)
                {
                    UIElementsUtils.Show("lblCompletionStatus", sectionUI);
                    UpdateCheckmark(sectionUI, data);
                }
                else
                {
                    UIElementsUtils.Hide("lblCompletionStatus", sectionUI);
                    UIElementsUtils.Hide("imgCheckmark", sectionUI);
                }
                sectionUI.RegisterCallback<MouseUpEvent, Section>(OnSectionClicked, data);
                return;
            }
            //sectionUI.tooltip = Localization.Tr(LocalizationKeys.k_TutorialLabelParseError);
            UIElementsUtils.Hide("lblCompletionStatus", sectionUI);
            UIElementsUtils.Hide("imgCheckmark", sectionUI);
        }

        internal void SetupCategoryUI(VisualElement categoryUI, TutorialContainer data)
        {
            if (data.Title.Translated == TutorialView.NO_TRANSLATION_CODE)
            {
                UIElementsUtils.Hide( "lblName", categoryUI );
            }
            else
            {
                UIElementsUtils.SetupLabel("lblName", data.Title, categoryUI, false);
            }

            if (data.Subtitle.Translated == TutorialView.NO_TRANSLATION_CODE)
            {
                UIElementsUtils.Hide( "lblDescription", categoryUI );
            }
            else
            {
                UIElementsUtils.SetupLabel("lblDescription", data.Subtitle, categoryUI, false);
            }

            if (data.Description.Translated != TutorialView.NO_TRANSLATION_CODE)
            {
                categoryUI.tooltip = data.Description;
            }

            if (data.BackgroundImage != null)
            {
                categoryUI.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(data.BackgroundImage);
            }
            categoryUI.RegisterCallback((MouseUpEvent evt) => OnCategoryClicked(evt, data));
        }

        void OnSectionClicked(MouseUpEvent evt, Section section)
        {
            Application.Broadcast(new SectionClickedEvent(section));
        }

        void OnCategoryClicked(MouseUpEvent evt, TutorialContainer category)
        {
            Application.Broadcast(new CategoryClickedEvent(category));
        }

        void UpdateCheckmark(VisualElement sectionUI, Section data)
        {
            bool progressTracking = (data.Tutorial != null && data.Tutorial.ProgressTrackingEnabled);
            bool completed = progressTracking && data.Tutorial.CompletedByUser;

            UIElementsUtils.SetupLabel("lblCompletionStatus", completed ? Localization.Tr(LocalizationKeys.k_TOCLabelCompleted) : string.Empty, sectionUI, false);
            VisualElement tutorialCheckmark = sectionUI.Q("imgCheckmark");
            if (completed)
            {
                UIElementsUtils.Show(tutorialCheckmark);
            }
            else
            {
                UIElementsUtils.Hide(tutorialCheckmark);
            }
        }

        void LoadTutorialsAndLinks()
        {
            m_ActiveSections.Clear();
            m_SectionsInitialized = false;
            IEnumerable<Section> sectionsToLoad;
            if (Model.CurrentCategory == null)
            {
                if (Model.RootCategoriesOfProject.Count() > 1)
                {
                    return; //nothing to load as we're in the 1st screen of the Table of Content
                }
                sectionsToLoad = Model.RootCategoriesOfProject
                                      .OrderBy(rootCategory => rootCategory.OrderInView)
                                      .SelectMany(rootCategory => rootCategory.Sections);
            }
            else
            {
                sectionsToLoad = Model.CurrentCategory.Sections;
            }

            if (sectionsToLoad == null)
            {
                m_SectionsInitialized = true;
                return;
            }

            if (sectionsToLoad.Any(section => section.Tutorial?.ProgressTrackingEnabled ?? false))
            {
                foreach (var section in sectionsToLoad)
                {
                    section.LoadState();
                }
                Application.StopAndNullifyEditorCoroutine(ref m_CheckmarksUpdateRoutine);
                m_CheckmarksUpdateRoutine = EditorCoroutineUtility.StartCoroutine(UpdateCheckmarksWhenStatesFetched(), Application);
            }

            VisualTreeAsset sectionUIPrefab = UIElementsUtils.LoadUXML("SectionUI");
            VisualElement sectionUI;
            foreach (var section in sectionsToLoad)
            {
                sectionUI = sectionUIPrefab.CloneTree();
                SetupSectionUI(sectionUI, section);
                m_TutorialsContainer.Add(sectionUI);
                m_ActiveSections.Add(new Tuple<VisualElement, Section>(sectionUI, section));
            }

            m_SectionsInitialized = true;
        }
        IEnumerator UpdateCheckmarksWhenStatesFetched()
        {
            while (!m_SectionsInitialized || !Model.FetchedTutorialStates)
            {
                yield return null;
            }

            foreach (var sectionUIAndData in m_ActiveSections)
            {
                if (sectionUIAndData.Item2.IsConfiguredCorrectly)
                {
                    UpdateCheckmark(sectionUIAndData.Item1, sectionUIAndData.Item2);
                }
            }
        }
    }
}
