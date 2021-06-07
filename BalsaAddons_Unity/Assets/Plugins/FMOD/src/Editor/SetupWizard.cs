using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FMODUnity
{
    [InitializeOnLoad]
    public class SetupWizardWindow : EditorWindow
    {
        private static SetupWizardWindow instance;

        enum PAGES : int
        {
            Welcome = 0,
            Updating,
            Linking,
            Listener,
            UnityAudio,
            UnitySources,
            End,
            Max
        }

        static readonly List<string> pageNames = new List<string>
        {
            "Welcome",
            "Updating",
            "Linking",
            "Listener",
            "Unity Audio",
            "Unity Sources",
            "End"
        };

        static readonly List<bool> pageComplete = new List<bool>(new bool[(int) PAGES.Max]);

        public enum UpdateTaskType
        {
            ReorganizePluginFiles,
            UpdateEventReferences,
        }

        class UpdateTask
        {
            public UpdateTaskType Type;
            public string Name;
            public string Description;
            public bool IsComplete;
            public Action Execute;
            public Func<bool> CheckComplete;

            public static UpdateTask Create(UpdateTaskType type, string name, string description,
                Action execute, Func<bool> checkComplete)
            {
                return new UpdateTask() {
                    Type = type,
                    Name = name,
                    Description = description,
                    Execute = execute,
                    CheckComplete = checkComplete
                };
            }
        }

        static readonly List<UpdateTask> updateTasks = new List<UpdateTask>() {
            UpdateTask.Create(
                type: UpdateTaskType.ReorganizePluginFiles,
                name: "Reorganize Plugin Files",
                description: "Move FMOD for Unity files to match the latest layout.",
                execute: FileReorganizer.ShowWindow,
                checkComplete: FileReorganizer.IsUpToDate
            ),
            UpdateTask.Create(
                type: UpdateTaskType.UpdateEventReferences,
                name: "Update Event References",
                description: "Find event references that use the obsolete [EventRef] attribute " +
                    "and update them to use the EventReference type.",
                execute: EventReferenceUpdater.ShowWindow,
                checkComplete: EventReferenceUpdater.IsUpToDate
            ),
        };

        public static void SetUpdateTaskComplete(UpdateTaskType type)
        {
            foreach (UpdateTask task in updateTasks.Where(t => t.Type == type))
            {
                task.IsComplete = true;
            }
        }

        static bool updateTaskStatusChecked = false;

        static void CheckUpdateTaskStatus()
        {
            if (!updateTaskStatusChecked)
            {
                updateTaskStatusChecked = true;

                foreach (UpdateTask task in updateTasks)
                {
                    task.IsComplete = task.CheckComplete();
                }
            }
        }

        PAGES currentPage = PAGES.Welcome;

        AudioListener[] unityListeners;
        StudioListener[] fmodListeners;
        Vector2 scroll1, scroll2;
        Vector2 stagingDetailsScroll;
        bool bFoundUnityListener;
        bool bFoundFmodListener;

        AudioSource[] unityAudioSources;

        GUIStyle titleStyle;
        GUIStyle titleLeftStyle;
        GUIStyle bodyStyle;
        GUIStyle buttonStyle;
        GUIStyle navButtonStyle;
        GUIStyle sourceButtonStyle;
        GUIStyle descriptionStyle;
        GUIStyle crumbStyle;
        GUIStyle columnStyle;
        Color crumbDefault;
        Color crumbHighlight;

        const string logoBlack = "FMOD/FMODLogoBlack.png";
        const string logoWhite = "FMOD/FMODLogoWhite.png";

        Texture2D logoTexture;
        Texture2D tickTexture;
        Texture2D crossTexture;
        GUIStyle iconStyle;

        const string backButtonText = "Back";

        SimpleTreeView m_SimpleTreeView;
        TreeViewState m_TreeViewState;

        bool bStudioLinked;

        static SetupWizardWindow()
        {
            EditorApplication.delayCall += Startup;
        }

        static StagingSystem.UpdateStep nextStagingStep;

        static bool IsStagingUpdateInProgress => nextStagingStep != null;

        static void DoNextStagingStep()
        {
            nextStagingStep.Execute();
            nextStagingStep = StagingSystem.GetNextUpdateStep();
        }

        static void Startup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Settings settings = Settings.Instance;

            if (settings.CurrentVersion != FMOD.VERSION.number)
            {
                // We're updating an existing installation; unhide the setup wizard if needed

                CheckUpdateTaskStatus();

                if (settings.HideSetupWizard && updateTasks.Any(t => !t.IsComplete))
                {
                    settings.HideSetupWizard = false;
                }

                settings.CurrentVersion = FMOD.VERSION.number;
                EditorUtility.SetDirty(settings);
            }

            nextStagingStep = StagingSystem.Startup();

            if (!settings.HideSetupWizard || IsStagingUpdateInProgress)
            {
                ShowAssistant();
            }
        }

        [MenuItem("FMOD/Setup Wizard")]
        public static void ShowAssistant()
        {
            instance = (SetupWizardWindow)GetWindow(typeof(SetupWizardWindow), true, "FMOD Setup Wizard");
            instance.ShowUtility();
            instance.minSize = new Vector2(600, 500);
            instance.maxSize = instance.minSize;
            var position = new Rect(Vector2.zero, instance.minSize);
            Vector2 screenCenter = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) / 2;
            position.center = screenCenter / EditorGUIUtility.pixelsPerPoint;
            instance.position = position;
        }

        private void OnEnable()
        {
            CheckUpdateTaskStatus();

            logoTexture = EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? logoWhite : logoBlack) as Texture2D;

            crossTexture = EditorGUIUtility.Load("FMOD/CrossYellow.png") as Texture2D;
            tickTexture = EditorGUIUtility.Load("FMOD/TickGreen.png") as Texture2D;

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            titleStyle.wordWrap = true;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            bodyStyle = new GUIStyle(titleStyle);
            crumbStyle = new GUIStyle(titleStyle);
            crumbDefault = EditorGUIUtility.isProSkin ? Color.gray : Color.gray;
            crumbHighlight = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            scroll1 = scroll2 = new Vector2();

            iconStyle = new GUIStyle();
            iconStyle.alignment = TextAnchor.MiddleCenter;

            CheckUpdatesComplete();
            CheckStudioLinked();
            CheckListeners();
            CheckSources();
            CheckUnityAudio();
        }

        void OnGUI()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle("Button");
                buttonStyle.fixedHeight = 30;

                sourceButtonStyle = new GUIStyle("button");
                sourceButtonStyle.fixedWidth = 150;
                sourceButtonStyle.fixedHeight = 35;
                sourceButtonStyle.margin = new RectOffset();

                descriptionStyle = new GUIStyle(titleStyle);
                descriptionStyle.fontStyle = FontStyle.Normal;
                descriptionStyle.alignment = TextAnchor.MiddleLeft;
                descriptionStyle.margin = new RectOffset(5,0,0,0);

                titleLeftStyle = new GUIStyle(descriptionStyle);
                titleLeftStyle.fontStyle = FontStyle.Bold;

                columnStyle = new GUIStyle();
                columnStyle.margin.left = 50;
                columnStyle.margin.right = 50;
            }

            // Draw Header
            EditorGUILayout.Space();
            GUILayout.Box(logoTexture, titleStyle);
            EditorGUILayout.Space();

            if (IsStagingUpdateInProgress)
            {
                StagingUpdatePage();
                return;
            }

            Breadcrumbs();

            // Draw Body
            using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                switch (currentPage)
                {
                    case PAGES.Welcome: WelcomePage(); break;
                    case PAGES.Updating: UpdatingPage(); break;
                    case PAGES.Linking: LinkingPage(); break;
                    case PAGES.Listener: ListenerPage(); break;
                    case PAGES.UnityAudio: DisableUnityAudioPage(); break;
                    case PAGES.UnitySources: UnitySources(); break;
                    case PAGES.End: EndPage(); break;
                }

                Buttons();

                if (currentPage == PAGES.Welcome)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            bool hide = Settings.Instance.HideSetupWizard;

                            hide = EditorGUILayout.Toggle("Do not display this again", hide);

                            if (check.changed)
                            {
                                Settings.Instance.HideSetupWizard = hide;
                                EditorUtility.SetDirty(Settings.Instance);
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
                else
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }

        private void OnInspectorUpdate()
        {
            switch (currentPage)
            {
                case PAGES.Welcome:                         break;
                case PAGES.Updating: CheckUpdatesComplete();break;
                case PAGES.Linking:     CheckStudioLinked();break;
                case PAGES.Listener:    CheckListeners();   break;
                case PAGES.UnityAudio:                      break;
                case PAGES.UnitySources:CheckSources();     break;
                case PAGES.End:                             break;
                case PAGES.Max:                             break;
                default:                                    break;
            }
        }

        void CheckUpdatesComplete()
        {
            pageComplete[(int)PAGES.Updating] = updateTasks.All(t => t.IsComplete);
        }

        void CheckStudioLinked()
        {
            pageComplete[(int)PAGES.Linking] = IsStudioLinked();
        }

        bool IsStudioLinked()
        {
            return !string.IsNullOrEmpty(Settings.Instance.SourceBankPath);
        }

        void CheckListeners()
        {
            var UListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            var FListeners = Resources.FindObjectsOfTypeAll<StudioListener>();
            if ((unityListeners == null || fmodListeners == null) || (!unityListeners.SequenceEqual(UListeners) || !fmodListeners.SequenceEqual(FListeners)))
            {
                unityListeners = UListeners;
                bFoundUnityListener = unityListeners.Length > 0;
                fmodListeners = FListeners;
                bFoundFmodListener = fmodListeners.Length > 0;
                Repaint();
            }
            pageComplete[(int)PAGES.Listener] = (!bFoundUnityListener && bFoundFmodListener);
        }

        void CheckSources()
        {
            var ASources = Resources.FindObjectsOfTypeAll<AudioSource>();
            if (unityAudioSources == null || !ASources.SequenceEqual(unityAudioSources))
            {
                unityAudioSources = ASources;
                if (m_SimpleTreeView != null && unityAudioSources.Length > 0)
                {
                    m_SimpleTreeView.Reload();
                }
            }
            pageComplete[(int)PAGES.UnitySources] = ASources != null ? (ASources.Length == 0) : true;
        }

        void CheckUnityAudio()
        {
            var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
            var serializedManager = new SerializedObject(audioManager);
            var prop = serializedManager.FindProperty("m_DisableAudio");
            pageComplete[(int)PAGES.UnityAudio] = prop.boolValue;
        }

        void WelcomePage()
        {
            GUILayout.FlexibleSpace();

            string message = string.Format("Welcome to FMOD for Unity {0}.",
                EditorUtils.VersionString(FMOD.VERSION.number));

            EditorGUILayout.LabelField(message, titleStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("This setup wizard will help you configure your project to use FMOD.", titleStyle);

            GUILayout.FlexibleSpace();
        }

        void Breadcrumbs()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
            {
                GUILayout.FlexibleSpace();
                crumbStyle.alignment = TextAnchor.MiddleCenter;
                Color oldColor = GUI.backgroundColor;

                for (int i = 0; i < pageNames.Count; i++)
                {
                    if (i > 0 && i < pageNames.Count - 1)
                    {
                        GUI.backgroundColor = (pageComplete[i] ? Color.green : Color.yellow);
                    }
                    using (new EditorGUILayout.HorizontalScope("box", GUILayout.Height(22)))
                    {
                        crumbStyle.normal.textColor = (i == (int)currentPage ? crumbHighlight : crumbDefault);
                        if (GUILayout.Button(pageNames[i], crumbStyle))
                        {
                            currentPage = (PAGES)i;
                        }
                        if (i > 0 && i < pageNames.Count - 1)
                        {
                            EditorGUILayout.Space();
                            GUILayout.Label(pageComplete[i] ? tickTexture : crossTexture, iconStyle);
                        }
                    }
                    GUILayout.Space(5);
                    GUI.backgroundColor = oldColor;
                }
                GUILayout.FlexibleSpace();
            }
        }

        void UpdatingPage()
        {
            EditorGUILayout.LabelField("If you are updating an existing FMOD installation, you may need to " +
                "perform some update tasks.", titleStyle);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Choose an update task to perform:", titleStyle);

            EditorGUILayout.Space();

            float buttonWidth = 0;

            foreach (UpdateTask task in updateTasks)
            {
                buttonWidth = Math.Max(buttonWidth, buttonStyle.CalcSize(new GUIContent(task.Name)).x);
            }

            float buttonHeight = buttonStyle.CalcSize(GUIContent.none).y;

            float margin = 50;
            float descriptionWidth = position.width
                - (buttonWidth + iconStyle.CalcSize(new GUIContent(tickTexture)).x + (margin * 2));

            foreach (UpdateTask task in updateTasks)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GUILayout.Label(task.IsComplete ? tickTexture : crossTexture, iconStyle,
                        GUILayout.Height(buttonHeight));

                    if (GUILayout.Button(task.Name, buttonStyle, GUILayout.Width(buttonWidth)))
                    {
                        task.Execute();
                    }

                    GUILayout.Label(task.Description, descriptionStyle,
                        GUILayout.MinHeight(buttonHeight), GUILayout.Width(descriptionWidth));

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space();
            }

            GUILayout.FlexibleSpace();
        }

        void LinkingPage()
        {
            EditorGUILayout.LabelField("In order to access your FMOD Studio content you need to locate the FMOD Studio Project or the .bank files that FMOD Studio produces, and configure a few other settings.", titleStyle);
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Choose how to access your FMOD Studio content:", titleStyle);

            EditorGUILayout.Space(); 
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    float indent = 25;
                    var serializedObject = new SerializedObject(Settings.Instance);

                    var boxStyle = new GUIStyle();
                    boxStyle.fixedHeight = 10;
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(indent);
                        if (GUILayout.Button("FMOD Studio Project", sourceButtonStyle))
                        {
                            SettingsEditor.BrowseForSourceProjectPath(serializedObject);
                        }
                        GUILayout.Label("If you have the complete FMOD Studio Project.",
                            descriptionStyle, GUILayout.Height(sourceButtonStyle.fixedHeight));
                    }
                    EditorGUILayout.Space();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(indent);
                        if (GUILayout.Button("Single Platform Build", sourceButtonStyle))
                        {
                            SettingsEditor.BrowseForSourceBankPath(serializedObject);
                        }
                        EditorGUILayout.LabelField("If you have the contents of the Build folder for a single platform.",
                            descriptionStyle, GUILayout.Height(sourceButtonStyle.fixedHeight));
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.Space();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(indent);
                        if (GUILayout.Button("Multiple Platform Build", sourceButtonStyle))
                        {
                            SettingsEditor.BrowseForSourceBankPath(serializedObject, true);
                        }
                        EditorGUILayout.LabelField("If you have the contents of the Build folder for multiple platforms, " +
                            "with each platform in its own subdirectory.",
                            descriptionStyle, GUILayout.Height(sourceButtonStyle.fixedHeight));
                    }
                }
            }

            if (IsStudioLinked())
            {
                EditorGUILayout.Space();

                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;

                using (new GUILayout.HorizontalScope("box"))
                {
                    GUILayout.FlexibleSpace();

                    GUILayout.Label(tickTexture, iconStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));

                    EditorGUILayout.Space();

                    using (new GUILayout.VerticalScope())
                    {
                        Settings settings = Settings.Instance;

                        if (settings.HasSourceProject)
                        {
                            EditorGUILayout.LabelField("Using the FMOD Studio project at:", descriptionStyle);
                            EditorGUILayout.LabelField(settings.SourceProjectPath, descriptionStyle);
                        }
                        else if (settings.HasPlatforms)
                        {
                            EditorGUILayout.LabelField("Using the multiple platform build at:", descriptionStyle);
                            EditorGUILayout.LabelField(settings.SourceBankPath, descriptionStyle);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Using the single platform build at:", descriptionStyle);
                            EditorGUILayout.LabelField(settings.SourceBankPath, descriptionStyle);
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUI.backgroundColor = oldColor;
            }

            GUILayout.FlexibleSpace();
        }

        void ListenerPage()
        {
            EditorGUILayout.LabelField("If you do not intend to use the built in Unity audio, you can choose to replace the Audio Listener with the FMOD Studio Listener.\n", bodyStyle);
            EditorGUILayout.LabelField("Adding the FMOD Studio Listener component to the main camera provides the FMOD Engine with the information it needs to play 3D events correctly.", bodyStyle);
            EditorGUILayout.Space();
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                // Display found objects containing Unity listeners
                DisplayListeners(unityListeners, ref scroll1);

                // Show FMOD Listeners
                DisplayListeners(fmodListeners, ref scroll2);

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(!bFoundUnityListener))
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Replace Unity " + ((unityListeners != null && unityListeners.Length > 1) ? "Listeners" : "Listener") + " with FMOD Audio Listener.", buttonStyle))
                    {
                        for (int i = 0; i < unityListeners.Length; i++)
                        {
                            var listener = unityListeners[i];
                            if (listener)
                            {
                                Debug.Log("[FMOD Assistant] Replacing Unity Listener with FMOD Listener on " + listener.gameObject.name);
                                if (listener.GetComponent<StudioListener>() == null)
                                {
                                    listener.gameObject.AddComponent(typeof(StudioListener));
                                }
                                DestroyImmediate(unityListeners[i]);
                                Repaint();
                            }
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        void DisplayListeners<T>(T[] listeners, ref Vector2 scrollPos)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                bool bUnityListenerType = false;
                if (typeof(T) == typeof(AudioListener))
                {
                    bUnityListenerType = true;
                }

                if (listeners != null && listeners.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(listeners.Length + " " + (bUnityListenerType ? "Unity" : "FMOD") + " " + (listeners.Length > 1 ? "Listeners" : "Listener") + " found.", titleStyle, GUILayout.ExpandWidth(true));
                        GUILayout.FlexibleSpace();
                    }

                    using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos, GUILayout.ExpandWidth(true)))
                    {
                        scrollPos = scrollView.scrollPosition;
                        foreach (T l in listeners)
                        {
                            var listener = l as Component;
                            if (listener != null && GUILayout.Button(listener.gameObject.name, GUILayout.ExpandWidth(true)))
                            {
                                Selection.activeGameObject = listener.gameObject;
                                EditorGUIUtility.PingObject(listener);
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("No " + (bUnityListenerType ? "Unity" : "FMOD") + " Listeners found.", titleStyle);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }

        void DisableUnityAudioPage()
        {
            EditorGUILayout.LabelField("We recommend that you disable the built-in Unity audio for all platforms, to prevent it from consuming system audio resources that the FMOD Engine needs.", titleStyle);
            GUILayout.FlexibleSpace();

            var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
            var serializedManager = new SerializedObject(audioManager);
            var prop = serializedManager.FindProperty("m_DisableAudio");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(prop.boolValue))
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(prop.boolValue ? "Built in audio has been disabled" : "Disable built in audio", buttonStyle))
                    {
                        prop.boolValue = true;
                        serializedManager.ApplyModifiedProperties();
                        Debug.Log("[FMOD Assistant] Built in Unity audio has been disabled.");
                        Repaint();
                    }

                }
                GUILayout.FlexibleSpace();
            }
            pageComplete[(int)PAGES.UnityAudio] = prop.boolValue;

            GUILayout.FlexibleSpace();
        }

        void UnitySources()
        {
            if (unityAudioSources != null && unityAudioSources.Length > 0)
            {
                EditorGUILayout.LabelField("Listed below are all the Unity Audio Sources found in the currently loaded scenes and the Assets directory.\nSelect an Audio Source and replace it with an FMOD Studio Event Emitter.", titleStyle);
                EditorGUILayout.Space();

                if (m_SimpleTreeView == null)
                {
                    if (m_TreeViewState == null)
                    {
                        m_TreeViewState = new TreeViewState();
                    }
                    m_SimpleTreeView = new SimpleTreeView(m_TreeViewState);
                }

                m_SimpleTreeView.Drawlayout();
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("No Unity Audio Sources have been found!", titleStyle);
                GUILayout.FlexibleSpace();
            }
        }

        void EndPage()
        {
            GUILayout.FlexibleSpace();
            bool completed = true;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 1; i < pageNames.Count - 1; i++)
                    {
                        EditorGUILayout.LabelField(pageNames[i], titleStyle);
                        EditorGUILayout.Space();
                    }
                }
                        EditorGUILayout.Space();
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 1; i < pageComplete.Count - 1; i++)
                    {
                        GUILayout.Label(pageComplete[i] ? tickTexture : crossTexture, iconStyle);
                        GUILayout.Space(8);

                        if (pageComplete[i] == false)
                        {
                            completed = false;
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(completed ? "FMOD for Unity has been set up successfully!" : "FMOD for Unity has not finished being set up.", titleStyle);
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(" Integration Manual ", buttonStyle))
                {
                    EditorUtils.OnlineManual();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(20);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(" FMOD Settings ", buttonStyle))
                {
                    Settings.EditSettings();
                }

                GUILayout.FlexibleSpace();
            }

            if (completed)
            {
                Settings.Instance.HideSetupWizard = true;
            }
        }

        void Buttons()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                navButtonStyle = new GUIStyle("Button");
                navButtonStyle.fixedHeight = 45;
                navButtonStyle.fixedWidth = 75;

                GUILayout.FlexibleSpace();
                if (currentPage != 0)
                {
                    if (GUILayout.Button(backButtonText, navButtonStyle))
                    {
                        if (currentPage != 0)
                        {
                            currentPage--;
                        }
                    }
                }

                string button2Text = "Next";
                if (currentPage == 0) button2Text = "Start";
                else if (currentPage == PAGES.End) button2Text = "Close";
                else button2Text = "Next";

                EditorGUILayout.Space();
                if (GUILayout.Button(button2Text, navButtonStyle))
                {
                    if (currentPage == PAGES.End)
                    {
                        this.Close();
                    }
                    currentPage++;
                }

                GUILayout.FlexibleSpace();
            }
        }

        void StagingUpdatePage()
        {
            GUILayout.Space(25);

            string message = string.Format("Welcome to FMOD for Unity {0}.",
                EditorUtils.VersionString(FMOD.VERSION.number));

            EditorGUILayout.LabelField(message, titleStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(
                "To complete the installation, we need to update the FMOD native libraries.\n" +
                "This involves a few steps:", titleStyle);

            EditorGUILayout.Space();

            float nameWidth = 200;

            using (new GUILayout.VerticalScope(columnStyle))
            {

                foreach (StagingSystem.UpdateStep step in StagingSystem.UpdateSteps)
                {
                    bool complete = step.Stage < nextStagingStep.Stage;

                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = complete ? Color.green : Color.yellow;

                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label(complete ? tickTexture : crossTexture, iconStyle);

                        EditorGUILayout.LabelField(step.Name, titleLeftStyle, GUILayout.Width(nameWidth));
                        EditorGUILayout.LabelField(step.Description, descriptionStyle);
                    }

                    GUI.backgroundColor = oldColor;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Next step:", titleStyle);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(nextStagingStep.Name, buttonStyle, GUILayout.ExpandWidth(false)))
                    {
                        EditorApplication.delayCall += DoNextStagingStep;
                    }

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space();

                using (var scope = new EditorGUILayout.ScrollViewScope(stagingDetailsScroll))
                {
                    stagingDetailsScroll = scope.scrollPosition;
                    EditorGUILayout.LabelField(nextStagingStep.Details, descriptionStyle);
                }
            }
        }
    }

    class SimpleTreeView : TreeView
    {
        public SimpleTreeView(TreeViewState state) : base(state)
        {
            Reload();
            Repaint();
            ExpandAll();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            return !(item is AudioSourceItem);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count > 0)
            {
                var item = FindItem(selectedIds[0], rootItem);
                GameObject go = null;
                if (item.hasChildren)
                {
                    if (item is ParentItem)
                    {
                        go = ((ParentItem)item).gameObject;
                    }
                }
                else
                {
                    go = ((ParentItem)((AudioSourceItem)item).parent).gameObject;
                }
                Selection.activeGameObject = go;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem (-1, -1);

            CreateItems(root, Resources.FindObjectsOfTypeAll<AudioSource>());
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        private class AudioSourceItem : TreeViewItem
        {
            const string audioIcon = "AudioSource Icon";
            public AudioSourceItem(AudioSource source) : base(source.GetHashCode())
            {
                displayName = (source.clip ? source.clip.name : "None");
                icon = (Texture2D)EditorGUIUtility.IconContent(audioIcon).image;
            }
        }

        private class ParentItem : TreeViewItem
        {
            public GameObject gameObject;
            const string goIcon = "GameObject Icon";
            const string prefabIcon = "Prefab Icon";
            const string prefabModelIcon = "PrefabModel Icon";
            const string prefabVariantIcon = "PrefabVariant Icon";

            public ParentItem(GameObject go) : base(go.GetHashCode(), 0, go.name)
            {
                gameObject = go;
                var foundAudio = gameObject.GetComponents<AudioSource>();
                for (int i = 0; i < foundAudio.Length; i++)
                {
                    AddChild(new AudioSourceItem(foundAudio[i]));
                }

                switch (PrefabUtility.GetPrefabAssetType(go))
                {
                    case PrefabAssetType.NotAPrefab:
                    icon = (Texture2D)EditorGUIUtility.IconContent(goIcon).image;
                        break;
                    case PrefabAssetType.Regular:
                    icon = (Texture2D)EditorGUIUtility.IconContent(prefabIcon).image;
                        break;
                    case PrefabAssetType.Model:
                    icon = (Texture2D)EditorGUIUtility.IconContent(prefabModelIcon).image;
                        break;
                    case PrefabAssetType.Variant:
                    icon = (Texture2D)EditorGUIUtility.IconContent(prefabVariantIcon).image;
                        break;
                }
            }
        }

        private class SceneItem : TreeViewItem
        {
            public Scene m_scene;
            const string sceneIcon = "SceneAsset Icon";
            const string folderIcon = "Folder Icon";

            public SceneItem(Scene scene) : base (scene.GetHashCode())
            {
                m_scene = scene;
                if (m_scene.IsValid())
                {
                    displayName = m_scene.name;
                    icon = (Texture2D)EditorGUIUtility.IconContent(sceneIcon).image;
                }
                else
                {
                    displayName = "Assets";
                    icon = (Texture2D)EditorGUIUtility.IconContent(folderIcon).image;
                }
            }
        }

        private void CreateItems(TreeViewItem root, AudioSource[] audioSources)
        {
            for(int i = 0; i < audioSources.Length; i++)
            {
                AudioSource audioSource = audioSources[i];

                GameObject obj = audioSource.gameObject;
                var sourceItem = FindItem(obj.GetHashCode(), root);
                if (sourceItem == null)
                {
                    List<GameObject> gameObjects = new List<GameObject>();
                    gameObjects.Add(obj);
                    while (obj.transform.parent != null)
                    {
                        obj = obj.transform.parent.gameObject;
                        gameObjects.Add(obj);
                    }
                    gameObjects.Reverse();

                    var parentItem = FindItem(obj.scene.GetHashCode(), root);
                    if (parentItem == null)
                    {
                        parentItem = new SceneItem(obj.scene);
                        root.AddChild(parentItem);
                    }

                    foreach (var go in gameObjects)
                    {
                        var objItem = FindItem(go.GetHashCode(), root);
                        if (objItem == null)
                        {
                            objItem = new ParentItem(go);
                            parentItem.AddChild(objItem);
                        }
                        parentItem = objItem;
                    }
                }
            }
        }

        const float BodyHeight = 200;

        public void Drawlayout()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, BodyHeight);
            rect = EditorGUI.IndentedRect(rect);

            OnGUI(rect);
            Toolbar();
        }

        public void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var style = "miniButton";
                if (GUILayout.Button("Expand All", style))
                {
                    ExpandAll();
                }

                if (GUILayout.Button("Collapse All", style))
                {
                    CollapseAll();
                }
            }
        }
    }
}