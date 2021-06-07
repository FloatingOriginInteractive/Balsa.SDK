using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace FMODUnity
{
    public enum PreviewState
    {
        Stopped,
        Playing,
        Paused,
    }

    [InitializeOnLoad]
    class EditorUtils : MonoBehaviour
    {
        public static void CheckResult(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                UnityEngine.Debug.LogError(string.Format("FMOD Studio: Encounterd Error: {0} {1}", result, FMOD.Error.String(result)));
            }
        }

        public const string BuildFolder = "Build";

        public static void ValidateSource(out bool valid, out string reason)
        {
            valid = true;
            reason = "";
            var settings = Settings.Instance;
            if (settings.HasSourceProject)
            {
                if (string.IsNullOrEmpty(settings.SourceProjectPath))
                {
                    valid = false;
                    reason = "The FMOD Studio project path must be set to an .fspro file.";
                    return;
                }
                if (!File.Exists(settings.SourceProjectPath))
                {
                    valid = false;
                    reason = string.Format("The FMOD Studio project path '{0}' does not exist.", settings.SourceProjectPath);
                    return;
                }

                string projectPath = settings.SourceProjectPath;
                string projectFolder = Path.GetDirectoryName(projectPath);
                string buildFolder = Path.Combine(projectFolder, BuildFolder);
                if (!Directory.Exists(buildFolder) ||
                    Directory.GetDirectories(buildFolder).Length == 0 ||
                    Directory.GetFiles(Directory.GetDirectories(buildFolder)[0], "*.bank", SearchOption.AllDirectories).Length == 0
                    )
                {
                    valid = false;
                    reason = string.Format("The FMOD Studio project '{0}' does not contain any built banks. Please build your project in FMOD Studio.", settings.SourceProjectPath);
                    return;
                }
            }
            else
            {
                if (String.IsNullOrEmpty(settings.SourceBankPath))
                {
                    valid = false;
                    reason = "The build path has not been set.";
                    return;
                }
                if (!Directory.Exists(settings.SourceBankPath))
                {
                    valid = false;
                    reason = string.Format("The build path '{0}' does not exist.", settings.SourceBankPath);
                    return;
                }

                if (settings.HasPlatforms)
                {
                    if (Directory.GetDirectories(settings.SourceBankPath).Length == 0)
                    {
                        valid = false;
                        reason = string.Format("Build path '{0}' does not contain any platform sub-directories. Please check that the build path is correct.", settings.SourceBankPath);
                        return;
                    }
                }
                else
                {
                    if (Directory.GetFiles(settings.SourceBankPath, "*.strings.bank").Length == 0)
                    {
                        valid = false;
                        reason = string.Format("Build path '{0}' does not contain any built banks.", settings.SourceBankPath);
                        return;
                    }
                }
            }
        }

        public static string[] GetBankPlatforms()
        {
            string buildFolder = Settings.Instance.SourceBankPath;
            try
            {
                if (Directory.GetFiles(buildFolder, "*.bank").Length == 0)
                {
                    string[] buildDirectories = Directory.GetDirectories(buildFolder);
                    string[] buildNames = new string[buildDirectories.Length];
                    for (int i = 0; i < buildDirectories.Length; i++)
                    {
                        buildNames[i] = Path.GetFileNameWithoutExtension(buildDirectories[i]);
                    }
                    return buildNames;
                }
            }
            catch
            {
            }
            return new string[0];
        }

        public static string VersionString(uint version)
        {
            uint major = (version & 0x00FF0000) >> 16;
            uint minor = (version & 0x0000FF00) >> 8;
            uint patch = (version & 0x000000FF);

            return string.Format("{0:X1}.{1:X2}.{2:X2}", major, minor, patch);
        }

        public static string DurationString(float seconds)
        {
            float minutes = seconds / 60;
            float hours = minutes / 60;

            if (hours >= 1)
            {
                return Pluralize(Mathf.FloorToInt(hours), "hour", "hours");
            }
            else if (minutes >= 1)
            {
                return Pluralize(Mathf.FloorToInt(minutes), "minute", "minutes");
            }
            else if (seconds >= 1)
            {
                return Pluralize(Mathf.FloorToInt(seconds), "second", "seconds");
            }
            else
            {
                return "a moment";
            }
        }

        public static string SeriesString(string separator, string finalSeparator, string[] elements)
        {
            if (elements.Length == 0)
            {
                return string.Empty;
            }
            else if (elements.Length == 1)
            {
                return elements[0];
            }
            else if (elements.Length == 2)
            {
                return elements[0] + finalSeparator + elements[1];
            }
            else
            {
                return string.Join(separator, elements, 0, elements.Length - 1)
                    + finalSeparator + elements[elements.Length - 1];
            }
        }

        public static string Pluralize(int count, string singular, string plural)
        {
            return string.Format("{0} {1}", count, (count == 1) ? singular : plural);
        }

        public static string GameObjectPath(Component component, GameObject root = null)
        {
            Transform transform = component.transform;

            StringBuilder objectPath = new StringBuilder();

            while(transform != null && transform.gameObject != root)
            {
                if (objectPath.Length > 0)
                {
                    objectPath.Insert(0, "/");
                }

                objectPath.Insert(0, transform.name);

                transform = transform.parent;
            }

            return objectPath.ToString();
        }

        public static bool HasAttribute<T>(MemberInfo member)
            where T : Attribute
        {
            Attribute[] attributes = Attribute.GetCustomAttributes(member, typeof(Attribute), true);

            return attributes.Any(a => typeof(T).IsAssignableFrom(a.GetType()));
        }

        public static bool AssetExists(string path)
        {
            string fullPath = $"{Environment.CurrentDirectory}/{path}";

            // We check that the file or directory exists as well because recently deleted assets remain in the database
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))
                && (File.Exists(fullPath) || Directory.Exists(fullPath));
        }

        public static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = GetParentFolder(folderPath);

                EnsureFolderExists(parentFolder);

                string folderName = Path.GetFileName(folderPath);

                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        // Path.GetDirectoryName replaces '/' with '\\' in some scripting runtime versions,
        // so we have to roll our own.
        public static string GetParentFolder(string assetPath)
        {
            int endIndex = assetPath.LastIndexOf('/');

            return (endIndex > 0) ? assetPath.Substring(0, endIndex) : string.Empty;
        }

        public static void DrawLegacyEvent(SerializedProperty property, string migrationTarget)
        {
            // Display the legacy event field if it is not empty
            if (!string.IsNullOrEmpty(property.stringValue))
            {
                EditorGUILayout.PropertyField(property, new GUIContent("Legacy Event"));

                using (new EditorGUI.IndentLevelScope())
                {
                    GUIContent content = new GUIContent(
                        string.Format("Will be migrated to <b>{0}</b>", migrationTarget),
                        EditorGUIUtility.IconContent("console.infoicon.sml").image);
                    GUIStyle style = new GUIStyle(GUI.skin.label) { richText = true };

                    Rect rect = EditorGUILayout.GetControlRect(false, style.CalcSize(content).y);
                    rect = EditorGUI.IndentedRect(rect);

                    GUI.Label(rect, content, style);
                }
            }
        }

        // Gets a control rect, draws a help button at the end of the line,
        // and returns a rect describing the remaining space.
        public static Rect DrawHelpButtonLayout(Func<PopupWindowContent> createContent)
        {
            Vector2 helpSize = GetHelpButtonSize();

            Rect rect = EditorGUILayout.GetControlRect(true, helpSize.y);

            return DrawHelpButton(rect, createContent);
        }

        public static Rect DrawHelpButton(Rect rect, Func<PopupWindowContent> createContent)
        {
            GUIContent content;
            GUIStyle style;
            GetHelpButtonData(out content, out style);

            Vector2 helpSize = style.CalcSize(content);

            Rect helpRect = rect;
            helpRect.xMin = helpRect.xMax - helpSize.x;

            if (GUI.Button(helpRect, content, style))
            {
                PopupWindow.Show(helpRect, createContent());
            }

            Rect remainderRect = rect;
            remainderRect.xMax = helpRect.xMin;

            return remainderRect;
        }

        public static float DrawParameterValueLayout(float value, EditorParamRef paramRef)
        {
            if (paramRef.Type == ParameterType.Labeled)
            {
                return EditorGUILayout.Popup((int)value, paramRef.Labels);
            }
            else if (paramRef.Type == ParameterType.Discrete)
            {
                return EditorGUILayout.IntSlider((int)value, (int)paramRef.Min, (int)paramRef.Max);
            }
            else
            {
                return EditorGUILayout.Slider(value, paramRef.Min, paramRef.Max);
            }
        }

        public static Vector2 GetHelpButtonSize()
        {
            GUIContent content;
            GUIStyle style;
            GetHelpButtonData(out content, out style);

            return style.CalcSize(content);
        }

        private static void GetHelpButtonData(out GUIContent content, out GUIStyle style)
        {
            content = EditorGUIUtility.IconContent("_Help");
            style = GUI.skin.label;
        }

        static EditorUtils()
        {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
            EditorApplication.pauseStateChanged += HandleOnPausedModeChanged;
        }

        static void HandleBeforeAssemblyReload()
        {
            DestroySystem();
        }

        static void HandleOnPausedModeChanged(PauseState state)
        {
            if (RuntimeManager.IsInitialized && RuntimeManager.HaveMasterBanksLoaded)
            {
                RuntimeManager.GetBus("bus:/").setPaused(EditorApplication.isPaused);
                RuntimeManager.StudioSystem.update();
            }
        }

        static void HandleOnPlayModeChanged(PlayModeStateChange state)
        {
            // Entering Play Mode will cause scripts to reload, losing all state
            // This is the last chance to clean up FMOD and avoid a leak.
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                DestroySystem();
            }
        }

        static void Update()
        {
            // Update the editor system
            if (system.isValid())
            {
                CheckResult(system.update());

                if (speakerMode != Settings.Instance.GetEditorSpeakerMode())
                {
                    PreviewStop();
                    DestroySystem();
                    CreateSystem();
                }
            }

            if (previewEventInstance.isValid())
            {
                FMOD.Studio.PLAYBACK_STATE state;
                previewEventInstance.getPlaybackState(out state);
                if (previewState == PreviewState.Playing && state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    PreviewStop();
                }
            }
        }

        static FMOD.Studio.System system;
        static FMOD.SPEAKERMODE speakerMode;

        static void DestroySystem()
        {
            if (system.isValid())
            {
                UnityEngine.Debug.Log("FMOD Studio: Destroying editor system instance");
                system.release();
                system.clearHandle();
            }
        }

        static void CreateSystem()
        {
            UnityEngine.Debug.Log("FMOD Studio: Creating editor system instance");
            RuntimeUtils.EnforceLibraryOrder();

            FMOD.RESULT result = FMOD.Debug.Initialize(FMOD.DEBUG_FLAGS.LOG, FMOD.DEBUG_MODE.FILE, null, "fmod_editor.log");
            if (result != FMOD.RESULT.OK)
            {
                UnityEngine.Debug.LogWarning("FMOD Studio: Cannot open fmod_editor.log. Logging will be disabled for importing and previewing");
            }

            CheckResult(FMOD.Studio.System.create(out system));

            FMOD.System lowlevel;
            CheckResult(system.getCoreSystem(out lowlevel));

            // Use play-in-editor speaker mode for event browser preview and metering
            speakerMode = Settings.Instance.GetEditorSpeakerMode();
            CheckResult(lowlevel.setSoftwareFormat(0, speakerMode, 0));

            CheckResult(system.initialize(256, FMOD.Studio.INITFLAGS.ALLOW_MISSING_PLUGINS | FMOD.Studio.INITFLAGS.SYNCHRONOUS_UPDATE, FMOD.INITFLAGS.NORMAL, IntPtr.Zero));

            FMOD.ChannelGroup master;
            CheckResult(lowlevel.getMasterChannelGroup(out master));
            FMOD.DSP masterHead;
            CheckResult(master.getDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, out masterHead));
            CheckResult(masterHead.setMeteringEnabled(false, true));
        }

        public static void UpdateParamsOnEmitter(SerializedObject serializedObject, string path)
        {
            if (string.IsNullOrEmpty(path) || EventManager.EventFromPath(path) == null)
            {
                return;
            }

            var eventRef = EventManager.EventFromPath(path);
            serializedObject.ApplyModifiedProperties();
            if (serializedObject.isEditingMultipleObjects)
            {
                foreach (var obj in serializedObject.targetObjects)
                {
                    UpdateParamsOnEmitter(obj, eventRef);
                }
            }
            else
            {
                UpdateParamsOnEmitter(serializedObject.targetObject, eventRef);
            }
            serializedObject.Update();
        }

        private static void UpdateParamsOnEmitter(UnityEngine.Object obj, EditorEventRef eventRef)
        {
            var emitter = obj as StudioEventEmitter;
            if (emitter == null)
            {
                // Custom game object
                return;
            }

            for (int i = 0; i < emitter.Params.Length; i++)
            {
                if (!eventRef.LocalParameters.Exists((x) => x.Name == emitter.Params[i].Name))
                {
                    int end = emitter.Params.Length - 1;
                    emitter.Params[i] = emitter.Params[end];
                    Array.Resize<ParamRef>(ref emitter.Params, end);
                    i--;
                }
            }
        }

        public static FMOD.Studio.System System
        {
            get
            {
                if (!system.isValid())
                {
                    CreateSystem();
                }
                return system;
            }
        }

        [MenuItem("FMOD/Help/Getting Started", priority = 2)]
        static void OnlineGettingStarted()
        {
            OpenOnlineDocumentation("unity", "user-guide");
        }

        [MenuItem("FMOD/Help/Integration Manual", priority = 3)]
        public static void OnlineManual()
        {
            OpenOnlineDocumentation("unity");
        }

        [MenuItem("FMOD/Help/API Manual", priority = 4)]
        static void OnlineAPIDocs()
        {
            OpenOnlineDocumentation("api");
        }

        [MenuItem("FMOD/Help/Support Forum", priority = 16)]
        static void OnlineQA()
        {
            Application.OpenURL("https://qa.fmod.com/");
        }

        [MenuItem("FMOD/Help/Revision History", priority = 5)]
        static void OnlineRevisions()
        {
            OpenOnlineDocumentation("api", "welcome-revision-history");
        }

        public static void OpenOnlineDocumentation(string section, string page = null, string anchor = null)
        {
            const string Prefix = "https://fmod.com/resources/documentation-";
            string version = string.Format("{0:X}.{1:X}", FMOD.VERSION.number >> 16, (FMOD.VERSION.number >> 8) & 0xFF);
            string url;

            if (!string.IsNullOrEmpty(page))
            {
                if (!string.IsNullOrEmpty(anchor))
                {
                    url = string.Format("{0}{1}?version={2}&page={3}.html#{4}", Prefix, section, version, page, anchor);
                }
                else
                {
                    url = string.Format("{0}{1}?version={2}&page={3}.html", Prefix, section, version, page);
                }
            }
            else
            {
                url = string.Format("{0}{1}?version={2}", Prefix, section, version);
            }
                
            Application.OpenURL(url);
        }

        [MenuItem("FMOD/About Integration", priority = 7)]
        public static void About()
        {
            FMOD.System lowlevel;
            CheckResult(System.getCoreSystem(out lowlevel));

            uint version;
            CheckResult(lowlevel.getVersion(out version));

            string text = string.Format(
                "Version: {0}\n\nCopyright \u00A9 Firelight Technologies Pty, Ltd. 2014-2021 \n\n" +
                "See LICENSE.TXT for additional license information.",
                VersionString(version));

            EditorUtility.DisplayDialog("FMOD Studio Unity Integration", text, "OK");
        }

        static List<FMOD.Studio.Bank> masterBanks = new List<FMOD.Studio.Bank>();
        static List<FMOD.Studio.Bank> previewBanks = new List<FMOD.Studio.Bank>();
        static FMOD.Studio.EventDescription previewEventDesc;
        static FMOD.Studio.EventInstance previewEventInstance;

        static PreviewState previewState;
        public static PreviewState PreviewState
        {
            get { return previewState; }
        }

        public static void PreviewEvent(EditorEventRef eventRef, Dictionary<string, float> previewParamValues)
        {
            bool load = true;
            if (previewEventDesc.isValid())
            {
                FMOD.GUID guid;
                previewEventDesc.getID(out guid);
                if (guid == eventRef.Guid)
                {
                    load = false;
                }
                else
                {
                    PreviewStop();
                }
            }

            if (load)
            {
                masterBanks.Clear();
                previewBanks.Clear();

                foreach (EditorBankRef masterBankRef in EventManager.MasterBanks)
                {
                    FMOD.Studio.Bank masterBank;
                    CheckResult(System.loadBankFile(masterBankRef.Path, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out masterBank));
                    masterBanks.Add(masterBank);
                }

                if (!EventManager.MasterBanks.Exists(x => eventRef.Banks.Contains(x)))
                {
                    string bankName = eventRef.Banks[0].Name;
                    var banks = EventManager.Banks.FindAll(x => x.Name.Contains(bankName));
                    foreach (var bank in banks)
                    {
                        FMOD.Studio.Bank previewBank;
                        CheckResult(System.loadBankFile(bank.Path, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out previewBank));
                        previewBanks.Add(previewBank);
                    }
                }
                else
                {
                    foreach (var previewBank in previewBanks)
                    {
                        previewBank.clearHandle();
                    }
                }

                CheckResult(System.getEventByID(eventRef.Guid, out previewEventDesc));
                CheckResult(previewEventDesc.createInstance(out previewEventInstance));
            }

            foreach (EditorParamRef param in eventRef.Parameters)
            {
                FMOD.Studio.PARAMETER_DESCRIPTION paramDesc;
                CheckResult(previewEventDesc.getParameterDescriptionByName(param.Name, out paramDesc));
                param.ID = paramDesc.id;
                if (param.IsGlobal)
                {
                    CheckResult(System.setParameterByID(param.ID, previewParamValues[param.Name]));
                }
                else
                {
                    PreviewUpdateParameter(param.ID, previewParamValues[param.Name]);
                }
            }

            CheckResult(previewEventInstance.start());
            previewState = PreviewState.Playing;
        }

        public static void PreviewUpdateParameter(FMOD.Studio.PARAMETER_ID id, float paramValue)
        {
            if (previewEventInstance.isValid())
            {
                CheckResult(previewEventInstance.setParameterByID(id, paramValue));
            }
        }

        public static void PreviewUpdatePosition(float distance, float orientation)
        {
            if (previewEventInstance.isValid())
            {
                // Listener at origin
                FMOD.ATTRIBUTES_3D pos = new FMOD.ATTRIBUTES_3D();
                pos.position.x = (float)Math.Sin(orientation) * distance;
                pos.position.y = (float)Math.Cos(orientation) * distance;
                pos.forward.x = 1.0f;
                pos.up.z = 1.0f;
                CheckResult(previewEventInstance.set3DAttributes(pos));
            }
        }

        public static void PreviewPause()
        {
            if (previewEventInstance.isValid())
            {
                bool paused;
                CheckResult(previewEventInstance.getPaused(out paused));
                CheckResult(previewEventInstance.setPaused(!paused));
                previewState = paused ? PreviewState.Playing : PreviewState.Paused;
            }
        }

        public static void PreviewStop()
        {
            if (previewEventInstance.isValid())
            {
                previewEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                previewEventInstance.release();
                previewEventInstance.clearHandle();
                previewEventDesc.clearHandle();
                previewBanks.ForEach(x => { x.unload(); x.clearHandle(); });
                masterBanks.ForEach(x => { x.unload(); x.clearHandle(); });
                previewState = PreviewState.Stopped;
            }
        }

        public static float[] GetMetering()
        {
            FMOD.System lowlevel;
            CheckResult(System.getCoreSystem(out lowlevel));
            FMOD.ChannelGroup master;
            CheckResult(lowlevel.getMasterChannelGroup(out master));
            FMOD.DSP masterHead;
            CheckResult(master.getDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, out masterHead));

            FMOD.DSP_METERING_INFO outputMetering;
            CheckResult(masterHead.getMeteringInfo(IntPtr.Zero, out outputMetering));

            FMOD.SPEAKERMODE mode;
            int rate, raw;
            lowlevel.getSoftwareFormat(out rate, out mode, out raw);
            int channels;
            lowlevel.getSpeakerModeChannels(mode, out channels);

            float[] data = new float[channels];
            if (outputMetering.numchannels > 0)
            {
                Array.Copy(outputMetering.rmslevel, data, channels);
            }
            return data;
        }


        const int StudioScriptPort = 3663;
        static NetworkStream networkStream = null;
        static Socket socket = null;
        static IAsyncResult socketConnection = null;

        static NetworkStream ScriptStream
        {
            get
            {
                if (networkStream == null)
                {
                    try
                    {
                        if (socket == null)
                        {
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        }

                        if (!socket.Connected)
                        {
                            socketConnection = socket.BeginConnect("127.0.0.1", StudioScriptPort, null, null);
                            socketConnection.AsyncWaitHandle.WaitOne();
                            socket.EndConnect(socketConnection);
                            socketConnection = null;
                        }

                        networkStream = new NetworkStream(socket);

                        byte[] headerBytes = new byte[128];
                        int read = ScriptStream.Read(headerBytes, 0, 128);
                        string header = Encoding.UTF8.GetString(headerBytes, 0, read - 1);
                        if (header.StartsWith("log():"))
                        {
                            UnityEngine.Debug.Log("FMOD Studio: Script Client returned " + header.Substring(6));
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log("FMOD Studio: Script Client failed to connect - Check FMOD Studio is running");

                        socketConnection = null;
                        socket = null;
                        networkStream = null;

                        throw e;
                    }
                }
                return networkStream;
            }
        }

        private static void AsyncConnectCallback(IAsyncResult result)
        {
            try
            {
                socket.EndConnect(result);
            }
            catch (Exception)
            {
            }
            finally
            {
                socketConnection = null;
            }
        }

        public static bool IsConnectedToStudio()
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    if (SendScriptCommand("true"))
                    {
                        return true;
                    }
                }

                if (socketConnection == null)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketConnection = socket.BeginConnect("127.0.0.1", StudioScriptPort, AsyncConnectCallback, null);
                }

                return false;

            }
            catch(Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public static bool SendScriptCommand(string command)
        {
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            try
            {
                ScriptStream.Write(commandBytes, 0, commandBytes.Length);
                byte[] commandReturnBytes = new byte[128];
                int read = ScriptStream.Read(commandReturnBytes, 0, 128);
                string result = Encoding.UTF8.GetString(commandReturnBytes, 0, read - 1);
                return (result.Contains("true"));
            }
            catch (Exception)
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream = null;
                }
                return false;
            }
        }


        public static string GetScriptOutput(string command)
        {
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            try
            {
                ScriptStream.Write(commandBytes, 0, commandBytes.Length);
                byte[] commandReturnBytes = new byte[2048];
                int read = ScriptStream.Read(commandReturnBytes, 0, commandReturnBytes.Length);
                string result = Encoding.UTF8.GetString(commandReturnBytes, 0, read - 1);
                if (result.StartsWith("out():"))
                {
                    return result.Substring(6).Trim();
                }
                return null;
            }
            catch (Exception)
            {
                networkStream.Close();
                networkStream = null;
                return null;
            }
        }

        private static string GetMasterBank()
        {
            GetScriptOutput(string.Format("masterBankFolder = studio.project.workspace.masterBankFolder;"));
            string bankCountString = GetScriptOutput(string.Format("masterBankFolder.items.length;"));
            int bankCount = int.Parse(bankCountString);
            for (int i = 0; i < bankCount; i++)
            {
                string isMaster = GetScriptOutput(string.Format("masterBankFolder.items[{1}].isOfExactType(\"MasterBank\");", i));
                if (isMaster == "true")
                {
                    string guid = GetScriptOutput(string.Format("masterBankFolder.items[{1}].id;", i));
                    return guid;
                }
            }
            return "";
        }

        private static bool CheckForNameConflict(string folderGuid, string eventName)
        {
            const string checkForNameConflictFunc =
                @"function(folderGuid, eventName) {
                    var nameConflict = false;
                    studio.project.lookup(folderGuid).items.forEach(function(val) {
                        nameConflict |= val.name == eventName;
                    });
                    return nameConflict;
                }";

            string conflictBool = GetScriptOutput(string.Format("({0})(\"{1}\", \"{2}\")", checkForNameConflictFunc, folderGuid, eventName));
            return conflictBool == "1";
        }

        public static string CreateStudioEvent(string eventPath, string eventName)
        {
            var folders = eventPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string folderGuid = GetScriptOutput("studio.project.workspace.masterEventFolder.id;");

            const string getFolderGuidFunc =
                @"function(parentGuid, folderName) {
                    folderGuid = """";
                    studio.project.lookup(parentGuid).items.forEach(function(val) {
                        folderGuid = val.isOfType(""EventFolder"") && val.name == folderName ? val.id : folderGuid;
                    });
                    if (folderGuid == """")
                    {
                        var newFolder = studio.project.create(""EventFolder"");
                        newFolder.name = folderName;
                        newFolder.folder = studio.project.lookup(parentGuid);
                        folderGuid = newFolder.id;
                    }
                    return folderGuid;
                }";

            for (int i = 0; i < folders.Length; i++)
            {
                string parentGuid = folderGuid;
                folderGuid = GetScriptOutput(string.Format("({0})(\"{1}\", \"{2}\")", getFolderGuidFunc, parentGuid, folders[i]));
            }

            if (CheckForNameConflict(folderGuid, eventName))
            {
                EditorUtility.DisplayDialog("Name Conflict", string.Format("The event {0} already exists under {1}", eventName, eventPath), "OK");
                return null;
            }

            const string createEventFunc =
                @"function(eventName, folderGuid) {
                    event = studio.project.create(""Event"");
                    event.note = ""Placeholder created via Unity"";
                    event.name = eventName;
                    event.folder = studio.project.lookup(folderGuid);

                    track = studio.project.create(""GroupTrack"");
                    track.mixerGroup.output = event.mixer.masterBus;
                    track.mixerGroup.name = ""Audio 1"";
                    event.relationships.groupTracks.add(track);

                    tag = studio.project.create(""Tag"");
                    tag.name = ""placeholder"";
                    tag.folder = studio.project.workspace.masterTagFolder;
                    event.relationships.tags.add(tag);

                    return event.id;
                }";

            string eventGuid = GetScriptOutput(string.Format("({0})(\"{1}\", \"{2}\")", createEventFunc, eventName, folderGuid));
            return eventGuid;
        }

        [InitializeOnLoadMethod]
        private static void CleanObsoleteFiles()
        {
            if (Environment.GetCommandLineArgs().Any(a => a == "-exportPackage"))
            {
                // Don't delete anything or it won't be included in the package
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Messing with the asset database while entering play mode causes a NullReferenceException
                return;
            }
            if (AssetDatabase.IsValidFolder("Assets/Plugins/FMOD/obsolete"))
            {
                EditorApplication.LockReloadAssemblies();

                string[] guids = AssetDatabase.FindAssets(string.Empty, new string[] { "Assets/Plugins/FMOD/obsolete" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        Debug.LogFormat("FMOD: Removed obsolete file {0}", path);
                    }
                }
                if(AssetDatabase.MoveAssetToTrash("Assets/Plugins/FMOD/obsolete"))
                {
                    Debug.LogFormat("FMOD: Removed obsolete folder Assets/Plugins/FMOD/obsolete");
                }
                AssetDatabase.Refresh();
                EditorApplication.UnlockReloadAssemblies();
            }
        }
    }

    public class StagingSystem
    {
        struct LibInfo
        {
            public string cpu;
            public string os;
            public string lib;
            public string platform;
            public BuildTarget buildTarget;
        };

        const string PlatformsFolder = "Assets/Plugins/FMOD/platforms";
        const string StagingFolder = "Assets/Plugins/FMOD/staging";
        const string AnyCPU = "AnyCPU";

        private static string GetTargetPath(LibInfo libInfo)
        {
            foreach (Platform.FileLayout layout in Platform.OldFileLayouts)
            {
                string path = GetTargetPath(libInfo, layout);

                if (EditorUtils.AssetExists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static string GetTargetPath(LibInfo libInfo, Platform.FileLayout layout)
        {
            switch (layout)
            {
                case Platform.FileLayout.Release_1_10:
                    return $"Assets/Plugins/{CPUAndLibPath(libInfo)}";
                case Platform.FileLayout.Release_2_0:
                    return $"Assets/Plugins/FMOD/lib/{libInfo.platform}/{CPUAndLibPath(libInfo)}";
                case Platform.FileLayout.Release_2_1:
                case Platform.FileLayout.Release_2_2:
                    return $"{PlatformsFolder}/{libInfo.platform}/lib/{CPUAndLibPath(libInfo)}";
                default:
                    throw new ArgumentException("Unrecognised file layout: " + layout);
            }
        }

        private static string CPUAndLibPath(LibInfo libInfo)
        {
            return (libInfo.cpu == AnyCPU) ? libInfo.lib : $"{libInfo.cpu}/{libInfo.lib}";
        }

        private static string GetSourcePath(LibInfo libInfo)
        {
            return $"{StagingFolder}/{libInfo.platform}/lib/{CPUAndLibPath(libInfo)}";
        }

        static readonly LibInfo[] LibrariesToUpdate = {
            new LibInfo() {cpu = "x86", os = "Windows",  lib = "fmodstudioL.dll", platform = "win", buildTarget = BuildTarget.StandaloneWindows},
            new LibInfo() {cpu = "x86_64", os = "Windows", lib = "fmodstudioL.dll", platform = "win", buildTarget = BuildTarget.StandaloneWindows64},
            new LibInfo() {cpu = "x86_64", os = "Linux", lib = "libfmodstudioL.so", platform = "linux", buildTarget = BuildTarget.StandaloneLinux64},
            new LibInfo() {cpu = AnyCPU, os = "OSX", lib = "fmodstudioL.bundle", platform = "mac", buildTarget = BuildTarget.StandaloneOSX},
        };

        public class UpdateStep
        {
            public Settings.SharedLibraryUpdateStages Stage;
            public string Name;
            public string Description;
            public string Details;
            public Action Execute;

            public void CacheDetails()
            {
                Details = GetDetails();
            }

            private Func<string> GetDetails;

            public static UpdateStep Create(Settings.SharedLibraryUpdateStages stage, string name, string description,
                Func<string> details, Action execute)
            {
                return new UpdateStep() {
                    Stage = stage,
                    Name = name,
                    Description = description,
                    GetDetails = details,
                    Execute = execute,
                };
            }
        }

        public static readonly UpdateStep[] UpdateSteps = {
            UpdateStep.Create(
                stage: Settings.SharedLibraryUpdateStages.DisableExistingLibraries,
                name: "Disable Existing Native Libraries",
                description: "Disable the existing FMOD native libraries so that Unity will not load them " +
                    "at startup time.",
                details: () => {
                    IEnumerable<PluginImporter> importers =
                        LibrariesToUpdate.Select(GetPluginImporter).Where(p => p != null);

                    if (!importers.Any())
                    {
                        return string.Empty;
                    }

                    IEnumerable<string> paths = importers.Select(p => $"\n* {p.assetPath}");

                    return $"This will disable these native libraries:{string.Join(string.Empty, paths)}";
                },
                execute: () => {
                    foreach (LibInfo libInfo in LibrariesToUpdate)
                    {
                        PluginImporter pluginImporter = GetPluginImporter(libInfo);
                        if (pluginImporter != null && pluginImporter.GetCompatibleWithEditor())
                        {
                            pluginImporter.SetCompatibleWithEditor(false);
                            pluginImporter.SetCompatibleWithAnyPlatform(false);
                            EditorUtility.SetDirty(pluginImporter);
                            pluginImporter.SaveAndReimport();
                        }
                    }

                    Settings.Instance.SharedLibraryUpdateStage = Settings.SharedLibraryUpdateStages.RestartUnity;
                    Settings.Instance.SharedLibraryTimeSinceStart = EditorApplication.timeSinceStartup;
                    EditorUtility.SetDirty(Settings.Instance);
                }
            ),

            UpdateStep.Create(
                stage: Settings.SharedLibraryUpdateStages.RestartUnity,
                name: "Restart Unity",
                description: "Restart Unity so that it releases its lock on the existing FMOD native libraries.",
                details: () => {
                    return "This will restart Unity. You will be prompted to save your work if you have unsaved " +
                        "scene modifications.";
                },
                execute: () => {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorApplication.OpenProject(Environment.CurrentDirectory);
                    }
                }
            ),

            UpdateStep.Create(
                stage: Settings.SharedLibraryUpdateStages.CopyNewLibraries,
                name: "Copy New Native Libraries",
                description: "Copy the new FMOD native libraries to the correct location and enable them.",
                details: () => {
                    List<string> actions = new List<string>();

                    foreach (LibInfo libInfo in LibrariesToUpdate)
                    {
                        string sourcePath = GetSourcePath(libInfo);
                        string targetPath = GetTargetPath(libInfo);

                        if (EditorUtils.AssetExists(sourcePath))
                        {
                            if (targetPath != null)
                            {
                                actions.Add($"Delete {targetPath}");
                            }

                            targetPath = GetTargetPath(libInfo, Platform.FileLayout.Latest);

                            actions.Add($"Copy {sourcePath} to {targetPath}");
                        }

                        if (targetPath != null)
                        {
                            actions.Add($"Enable {targetPath}");
                        }
                    }

                    actions.Add($"Remove {StagingFolder}");

                    return $"This will do the following:\n* {string.Join("\n* ", actions)}";
                },
                execute: () => {
                    bool allCopiesSucceeded = true;

                    foreach (LibInfo libInfo in LibrariesToUpdate)
                    {
                        string sourcePath = GetSourcePath(libInfo);
                        string targetPath = GetTargetPath(libInfo);

                        if (EditorUtils.AssetExists(sourcePath))
                        {
                            if (targetPath != null)
                            {
                                if (!AssetDatabase.DeleteAsset(targetPath))
                                {
                                    Debug.LogError(string.Format("FMOD: Could not delete {0}", targetPath));
                                }
                            }

                            targetPath = GetTargetPath(libInfo, Platform.FileLayout.Latest);

                            EditorUtils.EnsureFolderExists(EditorUtils.GetParentFolder(targetPath));

                            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
                            {
                                Debug.LogError(string.Format("FMOD: Could not copy {0} to {1}", sourcePath, targetPath));
                                allCopiesSucceeded = false;
                            }
                        }

                        PluginImporter pluginImporter = AssetImporter.GetAtPath(targetPath) as PluginImporter;

                        if (pluginImporter != null)
                        {
                            pluginImporter.ClearSettings();
                            pluginImporter.SetCompatibleWithEditor(true);
                            pluginImporter.SetCompatibleWithAnyPlatform(false);
                            pluginImporter.SetCompatibleWithPlatform(libInfo.buildTarget, true);
                            pluginImporter.SetEditorData("CPU", libInfo.cpu);
                            pluginImporter.SetEditorData("OS", libInfo.os);
                            EditorUtility.SetDirty(pluginImporter);
                            pluginImporter.SaveAndReimport();
                        }
                    }

                    if (allCopiesSucceeded)
                    {
                        if (AssetDatabase.MoveAssetToTrash(StagingFolder))
                        {
                            Debug.LogFormat("FMOD: Removed staging folder {0}", StagingFolder);
                        }
                        else
                        {
                            Debug.LogError(string.Format("FMOD: Could not remove staging folder {0}", StagingFolder));
                        }
                    }

                    ResetUpdateStage();

                    // This is so that Unity finds the new libraries
                    ReloadScripts();
                }
            ),
        };

        private static void ReloadScripts()
        {
            EditorUtility.RequestScriptReload();
        }

        private static PluginImporter GetPluginImporter(LibInfo libInfo)
        {
            string targetPath = GetTargetPath(libInfo);

            if (targetPath != null)
            {
                return AssetImporter.GetAtPath(targetPath) as PluginImporter;
            }
            else
            {
                return null;
            }
        }

        static UpdateStep FindUpdateStep(Settings.SharedLibraryUpdateStages stage)
        {
            return UpdateSteps.FirstOrDefault(s => s.Stage == stage);
        }

        static void ResetUpdateStage()
        {
            Settings.Instance.SharedLibraryUpdateStage = Settings.SharedLibraryUpdateStages.Start;
            Settings.Instance.SharedLibraryTimeSinceStart = 0;
            EditorUtility.SetDirty(Settings.Instance);
        }

        public static UpdateStep Startup()
        {
            if (!AssetDatabase.IsValidFolder(StagingFolder))
            {
                ResetUpdateStage();
                return null;
            }

            if (Settings.Instance.SharedLibraryUpdateStage == Settings.SharedLibraryUpdateStages.Start)
            {
                bool targetLibsExist = LibrariesToUpdate.Any(info => GetPluginImporter(info) != null);

                if (targetLibsExist)
                {
                    Settings.Instance.SharedLibraryUpdateStage = Settings.SharedLibraryUpdateStages.DisableExistingLibraries;
                    EditorUtility.SetDirty(Settings.Instance);
                }
                else
                {
                    FindUpdateStep(Settings.SharedLibraryUpdateStages.CopyNewLibraries).Execute();

                    ResetUpdateStage();
                    return null;
                }
            }

            if (Settings.Instance.SharedLibraryUpdateStage == Settings.SharedLibraryUpdateStages.RestartUnity
                && EditorApplication.timeSinceStartup < Settings.Instance.SharedLibraryTimeSinceStart)
            {
                // Unity has been restarted
                Settings.Instance.SharedLibraryUpdateStage = Settings.SharedLibraryUpdateStages.CopyNewLibraries;
                EditorUtility.SetDirty(Settings.Instance);
            }

            return GetNextUpdateStep();
        }

        public static UpdateStep GetNextUpdateStep()
        {
            UpdateStep step = FindUpdateStep(Settings.Instance.SharedLibraryUpdateStage);

            if (step != null)
            {
                step.CacheDetails();
            }

            return step;
        }
    }

    public abstract class HelpContent : PopupWindowContent
    {
        protected abstract void Prepare();
        protected abstract Vector2 GetContentSize();
        protected abstract void DrawContent();

        private GUIContent icon;

        public override void OnOpen()
        {
            icon = EditorGUIUtility.IconContent("console.infoicon");

            Prepare();
        }

        public override Vector2 GetWindowSize()
        {
            Vector2 contentSize = GetContentSize();

            Vector2 iconSize = GUI.skin.label.CalcSize(icon);

            return new Vector2(contentSize.x + iconSize.x,
                Math.Max(contentSize.y, iconSize.y) + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect rect)
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(icon);
                }

                using (new GUILayout.VerticalScope())
                {
                    DrawContent();
                }
            }
        }
    }

    public class SimpleHelp : HelpContent
    {
        public SimpleHelp(string text, float textWidth = 300)
        {
            this.text = new GUIContent(text);
            this.textWidth = textWidth;
        }

        private GUIContent text;
        private GUIStyle style;
        private float textWidth;

        protected override void Prepare()
        {
            style = new GUIStyle(GUI.skin.label) {
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
            };
        }

        protected override Vector2 GetContentSize()
        {
            float textHeight = style.CalcHeight(text, textWidth) + style.margin.bottom;

            return new Vector2(textWidth, textHeight);
        }

        protected override void DrawContent()
        {
            GUILayout.Label(text, style);
        }
    }

    public static class SerializedPropertyExtensions
    {
        public static bool ArrayContains(this SerializedProperty array, Func<SerializedProperty, bool> predicate)
        {
            return FindArrayIndex(array, predicate) >= 0;
        }

        public static bool ArrayContains(this SerializedProperty array, string subPropertyName,
            Func<SerializedProperty, bool> predicate)
        {
            return FindArrayIndex(array, subPropertyName, predicate) >= 0;
        }

        public static int FindArrayIndex(this SerializedProperty array, Func<SerializedProperty, bool> predicate)
        {
            for (int i = 0; i < array.arraySize; ++i)
            {
                SerializedProperty current = array.GetArrayElementAtIndex(i);

                if (predicate(current))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindArrayIndex(this SerializedProperty array, string subPropertyName,
            Func<SerializedProperty, bool> predicate)
        {
            for (int i = 0; i < array.arraySize; ++i)
            {
                SerializedProperty current = array.GetArrayElementAtIndex(i);
                SerializedProperty subProperty = current.FindPropertyRelative(subPropertyName);

                if (predicate(subProperty))
                {
                    return i;
                }
            }

            return -1;
        }

        public static void ArrayAdd(this SerializedProperty array, Action<SerializedProperty> initialize)
        {
            array.InsertArrayElementAtIndex(array.arraySize);
            initialize(array.GetArrayElementAtIndex(array.arraySize - 1));
        }

        public static void ArrayClear(this SerializedProperty array)
        {
            while (array.arraySize > 0)
            {
                array.DeleteArrayElementAtIndex(array.arraySize - 1);
            }
        }

        private static FMOD.GUID GetGuid(this SerializedProperty property)
        {
            return new FMOD.GUID() {
                Data1 = property.FindPropertyRelative("Data1").intValue,
                Data2 = property.FindPropertyRelative("Data2").intValue,
                Data3 = property.FindPropertyRelative("Data3").intValue,
                Data4 = property.FindPropertyRelative("Data4").intValue,
            };
        }

        public static void SetGuid(this SerializedProperty property, FMOD.GUID guid)
        {
            property.FindPropertyRelative("Data1").intValue = guid.Data1;
            property.FindPropertyRelative("Data2").intValue = guid.Data2;
            property.FindPropertyRelative("Data3").intValue = guid.Data3;
            property.FindPropertyRelative("Data4").intValue = guid.Data4;
        }

        public static void SetEventReference(this SerializedProperty property, FMOD.GUID guid, string path)
        {
            SerializedProperty guidProperty = property.FindPropertyRelative("Guid");
            guidProperty.SetGuid(guid);

            SerializedProperty pathProperty = property.FindPropertyRelative("Path");
            pathProperty.stringValue = path;
        }

        public static EventReference GetEventReference(this SerializedProperty property)
        {
            SerializedProperty pathProperty = property.FindPropertyRelative("Path");
            SerializedProperty guidProperty = property.FindPropertyRelative("Guid");

            return new EventReference() {
                Path = pathProperty.stringValue,
                Guid = guidProperty.GetGuid(),
            };
        }
    }

    public class NoIndentScope : IDisposable
    {
        int oldIndentLevel;

        public NoIndentScope()
        {
            oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
        }

        public void Dispose()
        {
            EditorGUI.indentLevel = oldIndentLevel;
        }
    }

    public class NaturalComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return EditorUtility.NaturalCompare(a, b);
        }
    }
}
