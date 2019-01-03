using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using NavMeshBuilder = UnityEditor.AI.NavMeshBuilder;

namespace Htc.Viveport.SDK
{

    public class VivePackageEditor : EditorWindow
    {
        #region Constants and One-time Cached Values

        private static readonly GUIContent EnterAppIdContent = new GUIContent("Enter a Viveport App Id:", "Id entered must be a valid App Id Name");
        private static readonly GUIContent ChangeSceneContent = new GUIContent("Change", "Opens a scene picker file browser.");
        private static readonly GUIContent AppIdContents = new GUIContent("App Id: ", "A Viveport App Id, the id must be a valid App Id");
        private static readonly GUIContent SceneContent = new GUIContent("Scene: ", "Currently loaded scene.");
        private static readonly GUIContent VrPreviewAndObjectsContent = new GUIContent("VR Preview Scene");
        private static readonly GUIContent SetAppIdBtnContent = new GUIContent("Set App Id", "Locks the Viveport App Id for the rest of the packaging process.");
        private static readonly GUIContent VrSkyboxContent = new GUIContent("VR Skybox");
        private static readonly GUIContent SDKPrefabSectionContent = new GUIContent( "Viveport Scene SDK Prefab" );
        private static readonly GUIContent CreateSDKPrefabContent = new GUIContent( "Create Viveport Scene SDK Prefab" );
        private static readonly GUIContent NavMeshContent = new GUIContent("Navigation Mesh (Optional)");
        private static readonly GUIContent VivePackageExportContent = new GUIContent("Vive Package Export");
        private static readonly GUIContent CompilingContent = new GUIContent("Compiling");
        private static readonly GUIContent PlayingContent = new GUIContent("Playing");
        private static readonly GUIContent ChangeAppIdBtnContent = new GUIContent("Change Current AppId");

        private static readonly GUIContent OpenNavMeshEditorContent = new GUIContent("Open Navmesh Editor", "You must setup the navmesh for the scene before you can bake data for the Teleport");
        private static readonly GUIContent ResetNavMeshSettingsContent = new GUIContent("Reset NavMesh Bake settings", "Resets the data under the Navigation Window -> Bake tab to match Viveport VR and the Testbed");
        private static readonly GUIContent CreateViveNavMeshContent = new GUIContent("Create ViveNavMesh", "Instantiates a ViveNavMesh from the ViveNavMesh prefab, and updates it with the current bake settings");
        private static readonly GUIContent UpdateViveNavMeshContent = new GUIContent( "Update ViveNavMesh Data", "Updates ViveNavMesh object to use current bake settings" );

        private static readonly GUIContent PackageScenesAndObjectsBtnContent = new GUIContent("Bundle Scene", "Bundles the VR Preview scene");
        private static readonly GUIContent PackageSkyboxBtnContent = new GUIContent("Bundle Skybox", "Renders and bundles the skybox cubemap, with the current settings");

        private static readonly GUIContent RenderSkyboxFromScene = new GUIContent("Option 1: Render Skybox From Scene");
        private static readonly GUIContent SizeLabelContent = new GUIContent("Size", "Size of the texture to be rendered");
        private static readonly GUIContent RenderFromPositionContent = new GUIContent("Render From Position: ", "Target transform that will be used as the perspective view point for the skybox cubemap render");
        private static readonly GUIContent CullingMaskContent = new GUIContent("Culling Mask: ", "Camera mask settings, uncheck layers for objects you don't want rendered");
        private static readonly GUIContent PrepareYourOwnSkyboxCubemap = new GUIContent("Option 2: Prepare your own Skybox Cubemap");
        private static readonly GUIContent PreparedCubemapContent = new GUIContent("Prepared Cubemap", "Assign a cubemap to this field to use it for packaging instead of rendering a new one");
        private static readonly GUIContent FarClipPlaneContent = new GUIContent("Far Clip Plane", "Far clipping plane value");
        private static readonly GUIContent NearClipPlaneContent = new GUIContent("Near Clip Plane", "Near clipping plane Value");

        private static readonly GUIContent SourceContent = new GUIContent("Source: ", "Path containing the package source contents");
        private static readonly GUIContent SceneBundleContent = new GUIContent("Scene Bundle: ", "Path to scene asset bundle");
        private static readonly GUIContent SkyboxBundleContent = new GUIContent("Skybox Bundle: ", "Path to skybox asset bundle");
        private static readonly GUIContent TargetContent = new GUIContent("Target: ", "Path containing the exported package.");
        private static readonly GUIContent PackageExportBtnContent = new GUIContent("Export All Contents", "Exports all packaged contents to a single package for testing and upload.");

        private const string IconsPath = "Assets/ViveportSceneSDK/Htc.Viveport.SDK/Editor/Icons/";
        private const string IconCompletePath = IconsPath + "Icon-Complete.png";
        private const string IconIncompletePath = IconsPath + "Icon-Incomplete.png";

        private const float MaxButtonWidth = 200.0f;
        private const float MaxSectionWidth = 600.0f;

        private static readonly string[] OpenFileFilters = { "Scenes", "unity" };
        private static readonly GUILayoutOption MaxButtonWidthOption = GUILayout.MaxWidth(MaxButtonWidth);
        private static readonly GUILayoutOption MaxSectionWidthOption = GUILayout.MaxWidth(MaxSectionWidth);
        private static readonly GUILayoutOption[] DefaultMaxButtonOpts = {MaxButtonWidthOption};
        private static readonly GUILayoutOption[] DefaultMaxSectionOpts = {MaxSectionWidthOption};
        private static readonly GUILayoutOption[] MaxWidth250Opts = {GUILayout.MaxWidth(250.0f)};
        private static readonly GUILayoutOption[] NoOpts = { };
        
        private static GUIStyle BoldLabelStyle;
        private static GUIStyle HelpBoxStyle;
        private static GUIStyle LargeLabelStyle;
        
        private static Texture2D _IconIncomplete;
        private static Texture2D _IconComplete;

        private static Texture2D IconIncomplete
        {
            get
            {
                if (_IconIncomplete == null)
                {
                    _IconIncomplete = AssetDatabase.LoadAssetAtPath<Texture2D>(IconIncompletePath);
                }

                return _IconIncomplete;
            }
        }

        private static Texture2D IconComplete
        {
            get
            {
                if (_IconComplete == null)
                {
                    _IconComplete = AssetDatabase.LoadAssetAtPath<Texture2D>(IconCompletePath);
                }

                return _IconComplete;
            }
        }
        
        private static readonly Action OpenNavMeshEditor;
        private static readonly MethodInfo ViveNavMeshOnValidateInfo;

        private const int DefaultSkyboxSize = 2048;
        private const string DefaultNavSettingsPath = "Assets/ViveportSceneSDK/Htc.Viveport.SDK/Editor/DefaultNavMeshSettings.asset";
        private const string ViveNavMeshPrefabPath = "Assets/ViveportSceneSDK/Htc.Viveport.SDK/Vive-Teleporter/Prefabs/Navmesh.prefab";
        private const string AudioMixerPath = "Assets/ViveportSceneSDK/Htc.Viveport.SDK/Editor/Audio/VPNextAudioMixer.mixer";
        private const string SdkPrefabPath = "Assets/ViveportSceneSDK/Prefabs/[Viveport Scene SDK].prefab";
        private const string TempAssetsDirName = "TEMP_ViveportSDK_Bundles";
        private const string TempAssetsDir = "Assets/" + TempAssetsDirName;
        private const string TempAssetsDirMeta = TempAssetsDir + ".meta";
        private const string EditorPrefsAppId = "EditorPrefsAppId";
        private const string EditorPrefsSkyboxRenderPos = "EditorPrefsSkyboxRenderPos";
        private const string SdkExportIgnoreTag = "SDK Export Ignore";
        private const string SdkPrefabTag = "SDK Prefab";

        private const string PackageInfoName = "info.txt";
        private const string ProcessingScene = "Processing Scene";

        private static readonly GUIContent[] SizeContents = {
            new GUIContent("1K", "A 1024x1024 texture"),
            new GUIContent("2K", "A 2048x2048 texture"),
            new GUIContent("4K", "A 4096x4096 texture"),
        };

        private static readonly int[] SkyboxSizes = {
            1024,
            DefaultSkyboxSize,
            4096,
        };

        private static readonly Vector2 MinSize = new Vector2(600.0f, 600.0f);

        static VivePackageEditor()
        {
            var editorAssembly = typeof(NavMeshBuilder).Assembly;
            
            var nmew = editorAssembly.GetType("UnityEditor.NavMeshEditorWindow");
            var swmi = nmew.GetMethod(
                "SetupWindow",
                BindingFlags.DeclaredOnly
                | BindingFlags.Static
                | BindingFlags.Public);

            OpenNavMeshEditor = Delegate.CreateDelegate(typeof(Action), swmi) as Action;
            
            var navMeshType = typeof(ViveNavMesh);
            ViveNavMeshOnValidateInfo = navMeshType.GetMethod("OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            Assert.IsNotNull(OpenNavMeshEditor);
            Assert.IsNotNull(ViveNavMeshOnValidateInfo);
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private string _viveportAppId;
        [SerializeField] private bool _hasLockedAppId;
        
        [SerializeField, HideInInspector] private int _skyboxSize = DefaultSkyboxSize;
        [SerializeField] private Transform _renderFromPosition;
        [SerializeField] private LayerMask _skyboxCullingMask = -1;
        [SerializeField] private Cubemap _preparedCubemap;
        [SerializeField] private float _nearClipPlane = 0.3f;
        [SerializeField] private float _farClipPlane = 1000.0f;

        [SerializeField, HideInInspector] private DefaultNavMeshSettings _defaultNavSettings;
        [SerializeField, HideInInspector] private ViveNavMesh _navMeshPrefab;
        [SerializeField, HideInInspector] private ViveNavMesh _sceneNavMesh;
        private Editor _sceneNavMeshEditor;
        [SerializeField, HideInInspector] private AudioMixer _guideMixer;
        [SerializeField, HideInInspector] private Vector2 _scrollPosition;

        [SerializeField, HideInInspector] private GameObject _sdkObject;
        [SerializeField, HideInInspector] private GameObject _sdkPrefab;

        #endregion

        #region Variables

        private string _tempPackagingDir;
        
        private SerializedObject _settingsObject;
        private bool _hasNavMesh = false;
        private bool _hasMultipleNavmeshes = false;
        private static readonly GUIContent TextImageContent = new GUIContent();
        private EditorApplication.CallbackFunction _cachedDelayCreateScene;
        private EditorApplication.CallbackFunction _cachedDelayCreateSkybox;
        private EditorApplication.CallbackFunction _cachedDelayCreatePackage;
        private Action _cachedCreateScene;
        private Action _cachedCreateSkybox;
        private Action _cachedCreatePackage;
        private Action _cacheDrawCreateSdkButton;
        private Action _cacheDrawBundleSceneButton;
        private Action _cacheDrawBundleSkyboxButton;
        private Action _cacheDrawCreatePackageButton;

        #endregion

        #region Properties

        #region Is Valid

        private bool IsAppIdValid
        {
            get { return DlcPath.IsValidName(_viveportAppId); }
        }

        private bool HasSettingsObject
        {
            get { return _settingsObject != null && _settingsObject.targetObject != null; }
        }

        private bool IsNavSettingsValid
        {
            get { return _defaultNavSettings != null && HasSettingsObject && !_defaultNavSettings.IsDifferent(_settingsObject); }
        }

        private static bool HasNavigableSurface
        {
            get
            {
                var navMesh = NavMesh.CalculateTriangulation();
                return navMesh.vertices.Length > 0;
            }
        }
        
        private static bool IsPreviewValid
        {
            get
            {
                var mainCam = Camera.main;
                return !(mainCam != null
                         && mainCam.isActiveAndEnabled
                         && mainCam.gameObject.scene == SceneManager.GetActiveScene());
            }
        }

        private bool IsSkyboxValid
        {
            get
            {
                return (_preparedCubemap != null) || (_renderFromPosition != null) && Mathf.Min(SkyboxSizes) <= _skyboxSize && _skyboxSize <= Mathf.Max(SkyboxSizes);
            }
        }

        private bool IsPackageValid
        {
            get { return Directory.Exists(SelectedDirectory) && HasPreviewFile && HasSkyboxFile; }
        }

        #endregion

        #region Has Contents

        private bool HasPreviewFile
        {
            get { return File.Exists(DlcPath.GetAppScene(_viveportAppId)); }
        }

        private bool HasSkyboxFile
        {
            get { return File.Exists(DlcPath.GetAppSkybox(_viveportAppId)); }
        }

        private bool HasPackageFile
        {
            get { return File.Exists(DlcPath.GetAppZip(_viveportAppId)); }
        }

        #endregion

        #region Package props

        private string SelectedDirectory
        {
            get { return DlcPath.GetAppDlcDirectory(_viveportAppId); }
        }

        private static string GetTargetParent(string archiveSource)
        {
            return DlcPath.CleanSeparators(Directory.GetParent(archiveSource).FullName);
        }

        private string GetTargetZip(string archiveSource)
        {
            return GetTargetParent(archiveSource) + "/" + GetArchiveName();
        }

        private string GetArchiveName()
        {
            return DlcPath.ToValidName(_viveportAppId) + DlcPath.PackageExtention;
        }

        #endregion

        #endregion

        #region Methods

        #region Unity API

        private void OnEnable()
        {
            //load saved editor prefs
            if( EditorPrefs.HasKey( EditorPrefsAppId ) )
                _viveportAppId = EditorPrefs.GetString( EditorPrefsAppId );
            if( EditorPrefs.HasKey( EditorPrefsSkyboxRenderPos ) )
                _renderFromPosition = EditorUtility.InstanceIDToObject( EditorPrefs.GetInt( EditorPrefsSkyboxRenderPos ) ) as Transform;

            CheckSkyboxRenderPos();

            autoRepaintOnSceneChange = true;
            _hasLockedAppId = IsAppIdValid;
            minSize = MinSize;
            _sdkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( SdkPrefabPath );
            _defaultNavSettings = AssetDatabase.LoadAssetAtPath<DefaultNavMeshSettings>(DefaultNavSettingsPath);
            _navMeshPrefab = AssetDatabase.LoadAssetAtPath<ViveNavMesh>(ViveNavMeshPrefabPath);
            _guideMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>( AudioMixerPath );

            _cachedDelayCreateScene = DelayCreateScene;
            _cachedCreateScene = CreateScene;

            _cachedDelayCreateSkybox = DelayCreateSkybox;
            _cachedCreateSkybox = CreateSkybox;

            _cachedDelayCreatePackage = DelayCreatePackage;
            _cachedCreatePackage = CreatePackage;

            _cacheDrawCreateSdkButton = DrawCreateSdkPrefabBUtton;
            _cacheDrawBundleSceneButton = DrawBundleSceneButton;
            _cacheDrawBundleSkyboxButton = DrawBundleSkyboxButton;
            _cacheDrawCreatePackageButton = DrawCreatePackageButton;
        }

        private void OnDisable()
        {
            if( IsAppIdValid )
                EditorPrefs.SetString( EditorPrefsAppId, _viveportAppId );

            if( _renderFromPosition != null )
                EditorPrefs.SetInt( EditorPrefsSkyboxRenderPos, _renderFromPosition.GetInstanceID() );

            DestroyNavMeshEditorIfNeeded();
        }

        #endregion

        [MenuItem("Vive/Export Editor")]
        private static void OpenEditor()
        {
            GetWindow<VivePackageEditor>(false, "Vive Preview Package Editor");
        }

        #region Temp Dir Functions

        private void BeginTempDir()
        {
            _tempPackagingDir = FileUtil.GetUniqueTempPathInProject();

            if (Directory.Exists(_tempPackagingDir)) { Directory.Delete(_tempPackagingDir, true); }
            Directory.CreateDirectory(_tempPackagingDir);

            if (Directory.Exists(TempAssetsDir)) Directory.Delete(TempAssetsDir, true);
            AssetDatabase.CreateFolder("Assets", TempAssetsDirName);
        }

        private void EndTempDir()
        {
            // remove temporary directory
            if (!string.IsNullOrEmpty(_tempPackagingDir) && Directory.Exists(_tempPackagingDir))
                Directory.Delete(_tempPackagingDir, true);

            if (Directory.Exists(TempAssetsDir))
                FileUtil.DeleteFileOrDirectory(TempAssetsDir);

            if (File.Exists(TempAssetsDirMeta))
                File.Delete(TempAssetsDirMeta);
        }

        #endregion

        #region Draw GUI

        private static Texture2D GetTaskIcon(bool isCompleted)
        {
            return isCompleted ? IconComplete : IconIncomplete;
        }
        
        private static GUIContent TempContent(string text, Texture image)
        {
            TextImageContent.text = text;
            TextImageContent.image = image;
            return TextImageContent;
        }
        
        private static void TaskBox(string message, bool isCompleted)
        {
            EditorGUILayout.LabelField(GUIContent.none, TempContent(message, GetTaskIcon(isCompleted)), HelpBoxStyle, NoOpts);
        }

        private void DestroyNavMeshEditorIfNeeded()
        {
            if (_sceneNavMeshEditor == null) return;
            
            DestroyImmediate(_sceneNavMeshEditor);
            _sceneNavMeshEditor = null;
        }

        private void OnGUI()
        {
            BoldLabelStyle = new GUIStyle("BoldLabel");
            HelpBoxStyle = new GUIStyle("HelpBox");
            LargeLabelStyle = new GUIStyle("LargeLabel");
            
            if(_sceneNavMesh == null && _sceneNavMeshEditor != null)
                DestroyNavMeshEditorIfNeeded();
            
            var isCompiling = EditorApplication.isCompiling;
            var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            var canRunEditor = !isCompiling && !isPlaying;

            if (!canRunEditor)
            {
                if (isCompiling) EditorGUILayout.LabelField(CompilingContent, NoOpts);
                if (isPlaying) EditorGUILayout.LabelField(PlayingContent, NoOpts);
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition, NoOpts))
            {
                using (new EditorGUI.DisabledScope(!canRunEditor))
                {
                    DrawChangeScene();

                    EditorGUILayout.Separator();

                    var needsValidAppId = !_hasLockedAppId || !IsAppIdValid;

                    if (needsValidAppId) DrawAppIdEntry();

                    using (new EditorGUI.DisabledScope(needsValidAppId))
                    {
                        DrawHeader();

                        DrawHorizontalBar();

                        DrawSDKPrefabSection();

                        DrawHorizontalBar();

                        using( new EditorGUI.DisabledScope( _sdkObject == null ) )
                        {
                            DrawNavMesh();

                            DrawHorizontalBar();

                            DrawPreviewWindow();

                            DrawHorizontalBar();

                            DrawSkyboxWindow();

                            DrawHorizontalBar();

                            DrawPackageWindow();
                        }
                    }
                }

                _scrollPosition = scrollView.scrollPosition;
            }
        }
        
        private static void DrawHorizontalBar()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, DefaultMaxSectionOpts);
        }

        private static void DrawButtonsAligned(Action toDraw)
        {
            Assert.IsNotNull(toDraw);
            
            using (new EditorGUILayout.HorizontalScope(DefaultMaxSectionOpts))
            {
                GUILayout.FlexibleSpace();
                toDraw();
            }
        }
        
        private void DrawAppIdEntry()
        {
            EditorGUILayout.PrefixLabel(EnterAppIdContent, LargeLabelStyle, LargeLabelStyle);
            _viveportAppId = DlcPath.ToValidName(EditorGUILayout.TextField(_viveportAppId, NoOpts));

            if (IsAppIdValid && GUILayout.Button(SetAppIdBtnContent, DefaultMaxButtonOpts))
                _hasLockedAppId = true;
        }

        private static void DrawChangeScene()
        {
            var altScene = string.Empty;
            using (new EditorGUILayout.HorizontalScope(DefaultMaxSectionOpts))
            {
                using (new EditorGUILayout.HorizontalScope(NoOpts))
                {
                    EditorGUILayout.PrefixLabel(SceneContent, LargeLabelStyle, LargeLabelStyle);
                    EditorGUILayout.LabelField(SceneManager.GetActiveScene().path, NoOpts);
                }

                if (GUILayout.Button(ChangeSceneContent, DefaultMaxButtonOpts))
                {
                    var path = EditorUtility.OpenFilePanelWithFilters("Select Scene", "Assets", OpenFileFilters);

                    if (!string.IsNullOrEmpty(path))
                    {
                        path = path.Replace(Application.dataPath, "Assets");
                        altScene = path;
                    }
                }

                if (!string.IsNullOrEmpty(altScene))
                    EditorApplication.delayCall += () => EditorSceneManager.OpenScene(altScene);
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(DefaultMaxSectionOpts))
            {
                EditorGUILayout.PrefixLabel(AppIdContents);
                EditorGUILayout.LabelField(_viveportAppId);

                if (GUILayout.Button(ChangeAppIdBtnContent, DefaultMaxButtonOpts))
                {
                    _hasLockedAppId = false;
                }
            }
        }

        private void CheckSDKObject()
        {
            _sdkObject = GameObject.FindGameObjectWithTag( SdkPrefabTag );
        }

        private void DrawSDKPrefabSection()
        {
            CheckSDKObject();

            using (new EditorGUI.DisabledScope( _sdkObject != null ))
            {
                using (new EditorGUILayout.VerticalScope(DefaultMaxSectionOpts))
                {
                    EditorGUILayout.LabelField( SDKPrefabSectionContent, BoldLabelStyle, NoOpts);

                    EditorGUILayout.HelpBox( "Click the button below to create the Viveport Scene SDK prefab object. The prefab includes all the objects necessary to test and export your scene.", MessageType.None, true);

                    EditorGUILayout.Separator();
                    
                    DrawButtonsAligned(_cacheDrawCreateSdkButton);
                }
                
            }
        }

        private void DrawCreateSdkPrefabBUtton()
        {
            if (GUILayout.Button(CreateSDKPrefabContent, MaxWidth250Opts)) CreateSdkPrefab();
        }

        #region Draw Nav Mesh

        private void DrawNavMesh()
        {
            using (new EditorGUILayout.VerticalScope(DefaultMaxSectionOpts))
            {
                EditorGUILayout.LabelField(NavMeshContent, BoldLabelStyle, NoOpts);
            
                EditorGUILayout.Separator();
    
                if (_settingsObject == null)
                {
                    var settingsObj = NavMeshBuilder.navMeshSettingsObject;
                    _settingsObject = settingsObj != null ? new SerializedObject(NavMeshBuilder.navMeshSettingsObject) : null;
                }
                
                if(HasSettingsObject) //account for destroyed object
                    _settingsObject.UpdateIfRequiredOrScript();
                
                var canNavigate = HasNavigableSurface;
                CheckNavMesh();
                
                if(_hasNavMesh && IsNavSettingsValid && !_hasMultipleNavmeshes && canNavigate 
                   && _sceneNavMesh.SelectableMeshBorder != null && _sceneNavMesh.SelectableMeshBorder.Length > 0)
                {
                    TaskBox(
                        "Teleport Navigation setup complete. Ready to teleport!\n" +
                        "When testing, remember that teleport is assigned to the left touchpad!", true);
                }
                else
                {
                    const string baseHelpMessage =
                        "In Order to have a teleportable scene, you must first bake your navmesh in Unity, then setup the ViveNavMesh prefab. " +
                        "All other teleporter elements will be furnished dynamically by the viewer environment.\n\n" +
                        "If you don't want the user to teleport, ignore this setup stage.";

                    var builder = new StringBuilder(baseHelpMessage.Length + 2048);

                    var msgType = MessageType.None;
                    
                    if (!IsNavSettingsValid)
                    {
                        builder.Append("In order to have navmesh data work properly, you must use the default agent settings.");
                        msgType = MessageType.Error;
                    }
                
                    if (!canNavigate)
                    {
                        if (msgType == MessageType.Error)
                            builder.Append("\n\n!!! ");
                        
                        builder.Append(
                            "Please bake at least 1 navigable surface that you intend to use for teleportation.");
                        msgType = MessageType.Error;
                    }
                
                    if (_hasMultipleNavmeshes)
                    {
                        if (msgType == MessageType.Error)
                            builder.Append("\n\n!!! ");
                        
                        builder.Append(
                            "Please delete excess ViveNavMeshes: you only need one. Any more may cause issues");
                        msgType = MessageType.Error;
                    }
                    else if (!_hasNavMesh)
                    {
                        if (msgType == MessageType.Error)
                            builder.Append("\n\n!!! ");
                        
                        builder.Append(
                            "Please drop the ViveNavMesh Prefab into the scene or click the button below " +
                            "and the editor will perform the initial setup for you (you may still need to setup material fields)");
                        msgType = MessageType.Error;
                    }
                    else if (_sceneNavMesh.SelectableMeshBorder == null || _sceneNavMesh.SelectableMeshBorder.Length == 0)
                    {
                        if (msgType == MessageType.Error)
                            builder.Append("\n\n!!! ");
                        
                        builder.Append(
                            "ViveNavMesh doesn't have any navmesh data baked into it, please manually update with the ViveNavMesh inspector! " +
                            "If you package the scene now, the user will not be able to teleport anywhere.");
                        msgType = MessageType.Error;
                    }

                    if (msgType == MessageType.Info)
                    {
                        builder.Append(baseHelpMessage);
                    }
                
                    EditorGUILayout.HelpBox(builder.ToString(), msgType);
                }
                
                EditorGUILayout.Separator();

                using (new EditorGUILayout.HorizontalScope(DefaultMaxSectionOpts))
                {
                    GUILayout.FlexibleSpace();
                    if (!IsNavSettingsValid)
                    {
                        if (_defaultNavSettings != null && HasSettingsObject && GUILayout.Button(ResetNavMeshSettingsContent, DefaultMaxButtonOpts))
                            _defaultNavSettings.ApplyToSettings(_settingsObject);
                    }
                    else if (!_hasMultipleNavmeshes && !_hasNavMesh)
                    {
                        using (new EditorGUI.DisabledScope(!canNavigate))
                        {
                            if (GUILayout.Button(CreateViveNavMeshContent, DefaultMaxButtonOpts))
                            {
                                CreateViveNavMesh();
                            }
                        }
                    }
                    else if (_hasNavMesh && !_hasMultipleNavmeshes && _sceneNavMesh.SelectableMeshBorder != null && _sceneNavMesh.SelectableMeshBorder.Length >= 0)
                    {
                        using (new EditorGUI.DisabledScope(!canNavigate))
                        {
                            if( GUILayout.Button( UpdateViveNavMeshContent, DefaultMaxButtonOpts) )
                            {
                                var viveNavMeshEditor = _sceneNavMeshEditor as ViveNavMeshEditor;
                                Assert.IsNotNull(viveNavMeshEditor);
                                viveNavMeshEditor.UpdateNavMesh();
                            }
                        }
                    }
                    
                    if (GUILayout.Button(OpenNavMeshEditorContent, DefaultMaxButtonOpts))
                        OpenNavMeshEditor();
                    
                    // NOTE: Only needed to generate the source default nav settings, as I was unable to clone
                    // or create a scriptable object copy of them. While there is a way to reset the bake settings
                    // demonstrated inside NavMeshEditorWindow, I have been unable to find another way to tell if the
                    // settings are off from the correct ones without resorting to comparing data fields manually
//                    if (GUILayout.Button("Clone object", DefaultMaxButtonOpts))
//                    {
//                        _defaultNavSettings = DefaultNavMeshSettings.CreateFromSettings(_settingsObject);
//        
//                        AssetDatabase.CreateAsset(_defaultNavSettings, DefaultNavSettingsPath);
//                    }
                    
                }
            }
        }

        private void CheckNavMesh()
        {
            var mainScene = SceneManager.GetActiveScene();

            if (!mainScene.IsValid() || !mainScene.isLoaded)
            {
                _hasNavMesh = false;
                _hasMultipleNavmeshes = false;
                _sceneNavMesh = null;
                DestroyNavMeshEditorIfNeeded();

                return;
            }

            var navMeshes = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ViveNavMesh>(false))
                .ToArray();

            _hasNavMesh = navMeshes.Length > 0;
            _hasMultipleNavmeshes = _hasNavMesh && navMeshes.Length > 1;
            _sceneNavMesh = navMeshes.FirstOrDefault();
            
            DestroyNavMeshEditorIfNeeded();
            Editor.CreateCachedEditor(_sceneNavMesh, typeof(ViveNavMeshEditor), ref _sceneNavMeshEditor);
        }
        
        #endregion

        #region Draw Scene Preview

        private void DrawPreviewHelp()
        {
            EditorGUILayout.HelpBox("Click the button below to bundle scene and objects for packaging.\n\nOptionally setup ViveObjectProps on objects to make them interactive.", MessageType.None);
        }

        private void DrawPreviewStatus()
        {
            TaskBox(!HasPreviewFile ? "VR Preview Scene is required for Viveport package export" : "VR Preview Scene Created", HasPreviewFile);
        }

        private void DrawPreviewWindow()
        {
            using (new EditorGUILayout.VerticalScope(DefaultMaxSectionOpts))
            {
                EditorGUILayout.LabelField(VrPreviewAndObjectsContent, BoldLabelStyle, NoOpts);
            
                DrawPreviewStatus();

                DrawPreviewHelp();

                EditorGUILayout.Separator();
                
                DrawButtonsAligned(_cacheDrawBundleSceneButton);
            }
        }

        private void DrawBundleSceneButton()
        {
            using (new EditorGUI.DisabledScope(!IsAppIdValid))
            {
                if (GUILayout.Button(PackageScenesAndObjectsBtnContent, DefaultMaxButtonOpts)) EditorApplication.delayCall += _cachedDelayCreateScene;
            }
        }

        private void DelayCreateScene()
        {
            Create(_cachedCreateScene);
        }

        #endregion

        #region Draw Skybox

        private void CheckSkyboxRenderPos()
        {
            if (_renderFromPosition != null) return;
            
            var vrsrp = GameObject.Find( "VR Skybox Render Pos" );
            if( vrsrp != null )
                _renderFromPosition = vrsrp.transform;
        }

        private void CleanInitialNavMeshSetup()
        {
            if(_sdkObject == null) return;
            
            CheckNavMesh();
            
            if(_sceneNavMesh == null || !_sdkObject.GetComponentsInChildren<ViveNavMesh>().Contains(_sceneNavMesh)) return;
            
            EditorUtility.CopySerializedIfDifferent(_navMeshPrefab, _sceneNavMesh);
        }

        private void DrawSkyboxHelp()
        {
            var msgType = MessageType.None;
            var helpText = "Place VR Skybox Render Pos at eye level wherever you want the skybox to be centered. " +
                           "Alternatively, you can use an existing cubemap that has already be prepared.";

            if (!IsSkyboxValid)
            {
                helpText += "\n\n!!! Select transform to render from";
                msgType = MessageType.Error;
            }
            
            EditorGUILayout.HelpBox(helpText, msgType);
        }

        private void DrawSkyboxStatus()
        {
            TaskBox(!HasSkyboxFile ? "VR Preview Skybox is required for Viveport package export" : "VR Preview Skybox Created", HasSkyboxFile);
        }

        private void DrawSkyboxWindow()
        {
            using (new EditorGUILayout.VerticalScope(DefaultMaxSectionOpts))
            {
                EditorGUILayout.LabelField(VrSkyboxContent, BoldLabelStyle, NoOpts);
            
                DrawSkyboxStatus();
            
                DrawSkyboxHelp();
            
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField(RenderSkyboxFromScene, LargeLabelStyle, NoOpts);

                EditorGUILayout.HelpBox("Note: Only static, non-ViveportObject contents will be rendered to the skybox", MessageType.None);

                using (new EditorGUI.DisabledScope(_preparedCubemap != null))
                {
                    _skyboxSize = EditorGUILayout.IntPopup(SizeLabelContent, _skyboxSize, SizeContents, SkyboxSizes, NoOpts);
                    _renderFromPosition = EditorGUILayout.ObjectField(RenderFromPositionContent, _renderFromPosition, typeof(Transform), true, NoOpts) as Transform;
                    _skyboxCullingMask = EditorGUILayout.MaskField(CullingMaskContent, _skyboxCullingMask, InternalEditorUtility.layers, NoOpts);
                    _farClipPlane = EditorGUILayout.FloatField(FarClipPlaneContent, _farClipPlane, NoOpts);
                    _nearClipPlane = EditorGUILayout.FloatField(NearClipPlaneContent, _nearClipPlane, NoOpts);
                }
                
                DrawHorizontalBar();

                EditorGUILayout.LabelField(PrepareYourOwnSkyboxCubemap, LargeLabelStyle, NoOpts);
                EditorGUILayout.LabelField(PreparedCubemapContent, MaxWidth250Opts);
                _preparedCubemap = EditorGUILayout.ObjectField(_preparedCubemap, typeof(Cubemap), false, MaxWidth250Opts) as Cubemap;
                
                EditorGUILayout.Separator();
                
                DrawButtonsAligned(_cacheDrawBundleSkyboxButton);
            }
        }

        private void DrawBundleSkyboxButton()
        {
            using (new EditorGUI.DisabledScope(!IsSkyboxValid))
            {
                if (GUILayout.Button(PackageSkyboxBtnContent, DefaultMaxButtonOpts)) EditorApplication.delayCall += _cachedDelayCreateSkybox;
            }
        }

        private void DelayCreateSkybox()
        {
            Create(_cachedCreateSkybox);
        }

        #endregion

        #region Draw Package

        private static void DrawLabel(GUIContent prefix, string label)
        {
            using (new EditorGUILayout.HorizontalScope(NoOpts))
            {
                EditorGUILayout.PrefixLabel(prefix, LargeLabelStyle, LargeLabelStyle);
                EditorGUILayout.LabelField(label, EditorStyles.wordWrappedLabel, NoOpts);
            }
        }

        private void DrawPackageHelp()
        {
            EditorGUILayout.HelpBox(IsPackageValid
                ? "Review the contents of the created folder, and continue when you are ready."
                : "Please package at least a VR Preview scene and a Skybox to continue.",
                MessageType.None);
        }

        private void DrawPackageStatus()
        {
            TaskBox(HasPackageFile ? "Package file created." : "No package file for testing/submission!", HasPackageFile);
        }

        private void DrawPackageWindow()
        {
            using (new EditorGUILayout.VerticalScope(DefaultMaxSectionOpts))
            {
                EditorGUILayout.LabelField(VivePackageExportContent, BoldLabelStyle, NoOpts);
                DrawPackageStatus();
                DrawPackageHelp();
            }

            using (new EditorGUILayout.VerticalScope(NoOpts))
            {
                using (new EditorGUI.DisabledScope(!IsAppIdValid || !IsPackageValid))
                {
                    DrawLabel(SourceContent, SelectedDirectory);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var sceneDlcPath = DlcPath.GetAppScene(_viveportAppId);
                        if(File.Exists(sceneDlcPath))
                            DrawLabel(SceneBundleContent, sceneDlcPath);

                        var skyboxDlcPath = DlcPath.GetAppSkybox(_viveportAppId);
                        if(File.Exists(skyboxDlcPath))
                            DrawLabel(SkyboxBundleContent, skyboxDlcPath);
                    }
                
                    DrawLabel(TargetContent, GetTargetZip(SelectedDirectory));
                }
            }

            using (new EditorGUILayout.VerticalScope(DefaultMaxSectionOpts))
            {
                DrawButtonsAligned(_cacheDrawCreatePackageButton);
            }
        }

        private void DrawCreatePackageButton()
        {
            if (GUILayout.Button(PackageExportBtnContent, DefaultMaxButtonOpts)) EditorApplication.delayCall += _cachedDelayCreatePackage;
        }

        private void DelayCreatePackage()
        {
            Create(_cachedCreatePackage, false, false);
        }

        #endregion

        #endregion

        #region Create Functions

        private void CreateSdkPrefab()
        {
            if( _sdkObject == null )
                _sdkObject = PrefabUtility.InstantiatePrefab( _sdkPrefab ) as GameObject;

            CheckSkyboxRenderPos();
            CleanInitialNavMeshSetup();
        }

        private void CreateViveNavMesh()
        {
            _sceneNavMesh = PrefabUtility.InstantiatePrefab(_navMeshPrefab) as ViveNavMesh;
            Assert.IsNotNull(_sceneNavMesh);
            
            var navMeshOnValidate = Delegate.CreateDelegate(typeof(Action), _sceneNavMesh, ViveNavMeshOnValidateInfo) as Action;
            Assert.IsNotNull(navMeshOnValidate);
            navMeshOnValidate(); // force validate

            // adapted from the editor class. Trying to invoke the editor directly or with reflectionlet to many
            // nullref exceptions, will try again later
            var tri = NavMesh.CalculateTriangulation();

            var verts = tri.vertices;
            var tris = tri.indices;
            var areas = tri.areas;

            int vertSize;
            var triSize = tris.Length;
            ViveNavMeshEditor.RemoveMeshDuplicates(verts, tris, out vertSize, 0.01f);
            ViveNavMeshEditor.DewarpMesh(verts, _sceneNavMesh.DewarpingMethod, _sceneNavMesh.SampleRadius);
            ViveNavMeshEditor.CullNavmeshTriangulation(verts, tris, areas, _sceneNavMesh.NavAreaMask, _sceneNavMesh.IgnoreSlopedSurfaces, ref vertSize, ref triSize);

            var m = ViveNavMeshEditor.ConvertNavmeshToMesh(verts, tris, vertSize, triSize);
            _sceneNavMesh.SelectableMeshBorder = ViveNavMeshEditor.FindBorderEdges(m);
            _sceneNavMesh.SelectableMesh = m; // Make sure that setter is called
            
            EditorUtility.SetDirty(_sceneNavMesh);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private void Create(Action act, bool needsTempDir = true, bool requiresRefresh = true)
        {
            Assert.IsNotNull(act);

            if (needsTempDir) BeginTempDir();

            try
            {
                act();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                
                if (needsTempDir) EndTempDir();

                if (requiresRefresh) AssetDatabase.Refresh();
            }
        }

        private void CopyToDlc(string srcFilePath)
        {
            var appDlcDir = DlcPath.GetAppDlcDirectory(_viveportAppId);
            
            Assert.IsNotNull(srcFilePath);
            Assert.IsFalse(string.IsNullOrEmpty(srcFilePath));
            
            try
            {
                if(!Directory.Exists(DlcPath.DlcDirectory))
                    Directory.CreateDirectory(DlcPath.DlcDirectory);

                if (!Directory.Exists(appDlcDir))
                    Directory.CreateDirectory(appDlcDir);

                var fileName = Path.GetFileName(srcFilePath);
                var dstFilePath = Path.Combine(appDlcDir, fileName);
                    
                File.Copy(srcFilePath, dstFilePath, true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void BuildBundle(string bundlename, string assetPath)
        {
            BuildPipeline.BuildAssetBundles(
                _tempPackagingDir,
                new[]
                {
                    new AssetBundleBuild
                    {
                        assetBundleName = bundlename,
                        assetBundleVariant = string.Empty,
                        assetNames = new[] { assetPath }
                    }
                },
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64);
        }

        #region Scene

        private void CreateScene()
        {
            var mainScene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(mainScene);
            
            var bundleName = DlcPath.GetSceneFileName(_viveportAppId);
            var sceneTempDir = TempAssetsDir + "/" + bundleName;
            var sceneTempPath = sceneTempDir + ".unity";
            var oldScenePath = mainScene.path;

            const string creatingTempScene = "Creating Temp Scene";
            EditorUtility.DisplayProgressBar(creatingTempScene, creatingTempScene, 0.0f);
            
            if(AssetDatabase.CopyAsset(mainScene.path, sceneTempPath))
            {
                var oldSceneBakedPath = oldScenePath.Replace(".unity", "");

                if (AssetDatabase.IsValidFolder(oldSceneBakedPath))
                {
                    EditorUtility.DisplayProgressBar(creatingTempScene, creatingTempScene, 0.5f);
                    var folderId = AssetDatabase.CreateFolder(TempAssetsDir, bundleName);
                    Assert.IsFalse(string.IsNullOrEmpty(folderId));
                    
                    var sceneTempBakedPath = sceneTempDir;
                
                    if (AssetDatabase.IsValidFolder(sceneTempBakedPath))
                    {
                        var projectPath = Application.dataPath.Replace("/Assets", "");
                    
                        var sceneDataAssets = Directory.GetFiles(oldSceneBakedPath)
                            .Where(fi => !fi.EndsWith(".meta"))
                            .Select(fi => fi.Replace(projectPath, ""))
                            .ToArray();

                        var len = sceneDataAssets.Length;
                        var slice = 0.5f / len;
                        var progress = 0.5f;

                        for (var i = 0; i < sceneDataAssets.Length; i++)
                        {
                            var assetPath = sceneDataAssets[i];
                            var fileName = Path.GetFileName(assetPath);
                            var copyInfo = string.Format("Copying: {0}", fileName);
                            EditorUtility.DisplayProgressBar(creatingTempScene, copyInfo, progress);
                            var tempPath = Path.Combine(sceneTempBakedPath, fileName);
                            if (!AssetDatabase.CopyAsset(assetPath, tempPath))
                            {
                                Debug.LogErrorFormat(
                                    "Couldn't copy {0} {1} {2} to {3}", 
                                    assetPath, assetPath, fileName, tempPath);
                            }
                            progress += slice;
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayProgressBar(creatingTempScene, creatingTempScene, 1.0f);
                }
            }
            
            var tempScene = EditorSceneManager.OpenScene(sceneTempPath, OpenSceneMode.Single); //ensure guides / other scenes unloaded
            
            EditorUtility.ClearProgressBar();
            
            SceneManager.SetActiveScene(tempScene);
            
            CheckSDKObject();
            CheckNavMesh();
            
            PreserveWantedSdkObjects();

            DestroyUnwantedObjects();
//
            CleanMixers(tempScene.GetRootGameObjects());
            
            ScrubAnimatorControllers();
            
            EditorUtility.ClearProgressBar();

            RenderSettings.skybox = null;
            EditorSceneManager.MarkSceneDirty(tempScene);
            EditorSceneManager.SaveScene(tempScene);

            BuildBundle(bundleName, sceneTempPath);

            if (File.Exists(sceneTempPath)) AssetDatabase.DeleteAsset(sceneTempPath);

            var bundlePath = Path.Combine(_tempPackagingDir, bundleName);
            
            CopyToDlc(bundlePath);

            EditorSceneManager.OpenScene(oldScenePath, OpenSceneMode.Single);

            AssetDatabase.RemoveUnusedAssetBundleNames();
        }

        private void PreserveWantedSdkObjects()
        {
            if (_sceneNavMesh == null || 
                !_sdkObject.GetComponentsInChildren<ViveNavMesh>(true)
                    .Contains(_sceneNavMesh) 
                && !_sceneNavMesh.CompareTag(SdkPrefabTag))
                return;

            var preservedData = Instantiate(_navMeshPrefab);
            
            EditorUtility.CopySerializedIfDifferent(_sceneNavMesh, preservedData);
        }

        private static void DestroyUnwantedObjects()
        {
            const string deletingSdkObjects = "Deleting SDK Objects";

            var unwantedObjects = GameObject.FindGameObjectsWithTag(SdkExportIgnoreTag)
                .Concat(GameObject.FindGameObjectsWithTag("MainCamera"))
                .Concat(GameObject.FindGameObjectsWithTag("EditorOnly"))
                .Concat(GameObject.FindGameObjectsWithTag(SdkPrefabTag))
                .ToArray();

            //delete unwanted objects
            var unwantedLen = unwantedObjects.Length;
            var unwantedSlice = 1.0f / unwantedLen;
            for (var i = 0; i < unwantedLen; ++i)
            {
                EditorUtility.DisplayProgressBar(
                    ProcessingScene,
                    deletingSdkObjects,
                    i * unwantedSlice);

                var go = unwantedObjects[i];
                DestroyImmediate(go);
            }
        }

        private static void ScrubAnimatorControllers()
        {
            const string prepareViveObjectProps = "Preparing Vive Object Props";
            var animators = FindObjectsOfType<ViveObjectProps>()
                .Select(vpo => vpo.GetComponent<Animator>())
                .Where(anim => anim != null)
                .ToArray();
            
            var scrubAnimatorLen = animators.Length;
            var scrubAnimatorSlice = 1.0f / scrubAnimatorLen;
            for (var i = 0; i < scrubAnimatorLen; i++)
            {
                EditorUtility.DisplayProgressBar(
                    ProcessingScene,
                    prepareViveObjectProps,
                    i * scrubAnimatorSlice);
                
                var animator = animators[i];
                animator.runtimeAnimatorController = null;
            }
        }

        private static bool CheckIfMixerInChain(AudioMixerGroup startGroup, AudioMixer checkMixer)
        {
            var curMixer = startGroup.audioMixer;
            while (curMixer != null)
            {
                if (curMixer == checkMixer) return true;

                if (curMixer.outputAudioMixerGroup == null) break;

                curMixer = curMixer.outputAudioMixerGroup.audioMixer;
            }

            return false;
        }

        private bool CleanMixers(IEnumerable<GameObject> rootGos)
        {
//            Assert.IsNotNull(_guideMixer);

            var audioSources = rootGos
                .SelectMany(go => go.GetComponentsInChildren<AudioSource>(true))
                .Where(src => src != null)
                .ToArray();

            return CleanSources(audioSources, _guideMixer);
        }

        private static bool CleanSources(IEnumerable<AudioSource> audioSources, AudioMixer guideMixer)
        {
            var needsChange = false;

            foreach (var source in audioSources)
            {
                var audioMixerGroup = source.outputAudioMixerGroup;
                if (audioMixerGroup == null || !CheckIfMixerInChain(audioMixerGroup, guideMixer)) continue;

                source.outputAudioMixerGroup = null;
                needsChange = true;
            }

            return needsChange;
        }

        #endregion

        #region Skybox

        private void CreateSkybox()
        {
            if (_preparedCubemap == null)
            {
                //display a "busy" dialog
                EditorUtility.DisplayProgressBar( "Working...", "Please wait while the skybox is rendered", 0 );

                // create temporary camera for rendering
                var camGo = new GameObject("CubemapCamera");
                var camTransform = camGo.transform;
                var cam = camGo.AddComponent<Camera>();
                cam.cullingMask = _skyboxCullingMask;

                // place it on the object
                camTransform.position = _renderFromPosition.position;
                camTransform.rotation = _renderFromPosition.rotation;
                cam.farClipPlane = _farClipPlane;
                cam.nearClipPlane = _nearClipPlane;

                var gosToHide = EditorSceneManager
                    .GetActiveScene()
                    .GetRootGameObjects()
                    .SelectMany(go => go.transform.GetComponentsInChildren<Transform>(false))
                    .Select(t => t.gameObject)
                    .Where(go => go.activeSelf && go.activeInHierarchy)
                    .Where(go => go.GetComponent<ViveObjectProps>() != null || go.GetComponent<ViveNavMesh>() != null)
                    .ToArray();

                foreach (var go in gosToHide) go.SetActive(false);

                //Don't include any sdk prefab objects. We need to get a local reference because we are periodically checking for _sdkObject in other places, and setting it to inactive will make it un-findable
                var tempSdkObj = _sdkObject;
                if( tempSdkObj != null )
                {
                    tempSdkObj.SetActive( false );
                }

                SceneView.RepaintAll();
                
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.Step();

                    BeginTempDir();

                    var cubemap = new Cubemap(_skyboxSize, TextureFormat.ARGB32, false);
                    cam.RenderToCubemap(cubemap);

                    foreach (var go in gosToHide) go.SetActive(true);
                    if( tempSdkObj != null )
                        tempSdkObj.SetActive( true );

                    var path = GenerateCubemapAsset(DlcPath.GetSkyboxFileName(_viveportAppId), cubemap);
                    BuildSkybox(path);

                    // destroy temporary camera
                    DestroyImmediate(camGo);

                    EndTempDir();

                    EditorUtility.ClearProgressBar();
                };
            }
            else
            {
                try
                {
                    var cubemapPath = AssetDatabase.GetAssetPath(_preparedCubemap);
                    var fileName = DlcPath.GetSkyboxFileName(_viveportAppId);
                    var tempCubemapPath = Path.Combine(TempAssetsDir, fileName + ".jpg");

                    AssetDatabase.StartAssetEditing();

                    var copySuccess = AssetDatabase.CopyAsset(cubemapPath, tempCubemapPath);
                    
                    AssetDatabase.StopAssetEditing();
                    
                    var texImporter = AssetImporter.GetAtPath(tempCubemapPath) as TextureImporter;
                    Assert.IsNotNull(texImporter);
                    texImporter.alphaSource = TextureImporterAlphaSource.None;
                    texImporter.mipmapEnabled = false;

                    var texPlatformSettings = texImporter.GetPlatformTextureSettings("Standalone");
                    texPlatformSettings.format = TextureImporterFormat.DXT1Crunched;
                    texPlatformSettings.compressionQuality = 50;
                    
                    texImporter.SetPlatformTextureSettings(texPlatformSettings);
                    

                    if (copySuccess)
                    {
                        BuildSkybox(tempCubemapPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void BuildSkybox(string assetPath)
        {
            var bundlename = DlcPath.GetSkyboxFileName(_viveportAppId);

            AssetDatabase.RemoveUnusedAssetBundleNames();

            BuildBundle(bundlename, assetPath);
            
            if (File.Exists(assetPath)) AssetDatabase.DeleteAsset(assetPath);
            
            // Copy to DLC path

            var bundlePath = Path.Combine(_tempPackagingDir, bundlename);
            
            CopyToDlc(bundlePath);

            // clean up assetbundle names
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }

        private static string GenerateCubemapAsset(string skyboxName, Cubemap source)
        {
            var path = string.Format("{0}/{1}.cubemap", TempAssetsDir, skyboxName);
            AssetDatabase.CreateAsset(source, path);
            return path;
        }

        #endregion

        #region Package

        [Serializable]
        public class PackageInfo {
            public string unityVersion;
            public PackageInfo(string version) {
                unityVersion = version;
            }
        }

        private void CreatePackage()
        {
            var archiveSource = SelectedDirectory;
            var archiveDest = GetTargetZip(archiveSource);
            var archiveName = GetArchiveName();
            var archiveParent = GetTargetParent(archiveSource);

            const string packagingText = "Packaging";
            var packagingMessage = string.Format("Packaging contents into {0}", archiveName);

            EditorUtility.DisplayProgressBar(packagingText, packagingMessage, 0.5f);

            var error = string.Empty;
            try
            {
                //Add a meta file for dev console before packing
                var info = new PackageInfo(Application.unityVersion);
                var content = JsonUtility.ToJson(info);
                var infoPath = archiveSource + "\\" + PackageInfoName;
                File.WriteAllText(infoPath, content, System.Text.Encoding.UTF8);

                error = ZipUtility.AddToZipPackage(archiveParent, archiveSource, archiveDest);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                error = ex.Message;
            }
            finally
            {
                EditorUtility.DisplayProgressBar(packagingText, packagingMessage, 1.0f);

                var dialogTitle = "Packging Complete";
                var dialogText = string.Format("Your vive content was packaged at: {0}", archiveDest);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);

                    dialogTitle = "Packaging Error";
                    dialogText = string.Format("There was an error: {0}. Pleaes check {1} and try again.", error, archiveSource);
                }
                else
                {
                    EditorUtility.RevealInFinder(archiveDest);
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    dialogTitle,
                    dialogText,
                    "Ok");
            }

        }

        #endregion

        #endregion

        #endregion
    }
}