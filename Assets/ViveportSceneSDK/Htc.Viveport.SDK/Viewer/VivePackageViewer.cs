using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Htc.Viveport.SDK
{
    [DisallowMultipleComponent]
    public class VivePackageViewer : MonoBehaviour, Interactivity.ITeleportProvider
    {
        [SerializeField] private AudioMixerGroup _audioMixerGroup;

        #region Constants and Cached Values
        
        private static readonly GUIContent UnpackBtnContent = new GUIContent("Unpack");
        private static readonly GUIContent CleanupBtnContent = new GUIContent("Cleanup");
        private static readonly GUIContent LoadSceneBtnContent = new GUIContent("Load Scene");
        private static readonly GUIContent UnloadSceneBtnContent = new GUIContent("Unload Scene");
        private static readonly GUIContent LoadSkyboxBtnContent = new GUIContent("Load Skybox");
        private static readonly GUIContent LoadDefaultSkyboxContent = new GUIContent("Load Default Skybox");

        private static int _texPropId;
        
        #endregion

        #region Editable Variables

        [SerializeField] private ViveNavMesh _navMesh;
        [SerializeField] private Animator _navMeshAnimator;
        [SerializeField] private TeleportVive _teleporter;
        [SerializeField] private ParabolicPointer _pointer;
        [SerializeField] private Transform _cameraRig;
        [SerializeField] private GameObject[] _gosToHideInPreview = new GameObject[0];
        [SerializeField] private GameObject[] _gosToShowInPreview = new GameObject[0];
        [SerializeField] private Material _defaultSkyboxMaterial;
        [SerializeField] private Cubemap _defaultSkyboxCubemap;
        [SerializeField] private bool _isTestScene = false;

        #endregion

        #region Variables

        private FileSystemWatcher _viveZipWatcher;
        
        private readonly Dictionary<string, VivePackage> _vivePackages = new Dictionary<string, VivePackage>(8);

        private VivePackage _loadedScenePackage;

        private IDisposable _activity;

        private Scene _baseActiveScene;

        private Material _skyboxMaterial;
        private Cubemap _skyboxCubemap;
        
        #endregion

        #region Methods

        #region Unity API

        private void Awake()
        {
            _texPropId = Shader.PropertyToID("_Tex");

            ZipUtility.Init();
            DlcPath.Init();

            _skyboxMaterial = new Material(_defaultSkyboxMaterial);
            _skyboxMaterial.SetTexture(_texPropId, _defaultSkyboxCubemap);
            
            FindExistingVivePackages();

            SetupWatcher();
            _baseActiveScene = SceneManager.GetActiveScene();
        }

        private IEnumerator Start()
        {
            yield return null;
            ToggleGosForPreview(true, _gosToHideInPreview);
            ToggleGosForPreview(false, _gosToShowInPreview);

            //If user has checked the "Is Test Scene" checkbox load the current scene
            if( _isTestScene )
            {
                //make sure we have a navmesh
                if( _navMesh == null )
                {
                    _navMesh = FindObjectOfType<ViveNavMesh>();
                    if( _navMesh != null )
                    {
                        _navMeshAnimator = _navMesh.GetComponent<Animator>();

                        ParabolicPointer pointer = FindObjectOfType<ParabolicPointer>();
                        if( pointer != null )
                            pointer.NavMesh = _navMesh;

                        TeleportVive teleporter = FindObjectOfType<TeleportVive>();
                        if( teleporter != null )
                            teleporter.NavAnimator = _navMeshAnimator;
                    }
                }

                LoadTestScene();
            }
        }

        private void OnGUI()
        {
            if( _isTestScene )
                return;

            GUI.enabled = _activity == null;
            
            if(GUILayout.Button(LoadDefaultSkyboxContent))
                ApplySkybox(_defaultSkyboxCubemap);

            for (var idx = 0; idx < _vivePackages.Count; idx++)
            {
                var vp = _vivePackages.Values.ElementAt(idx);
                DrawPackageGui(vp);
            }

            GUI.enabled = true;
        }

        private void OnDestroy()
        {
            _viveZipWatcher.Dispose();
            _vivePackages.Clear();
        }

        #endregion

        #region Package Response API

        private void OnPackageChanged(object source, FileSystemEventArgs e)
        {
            VivePackage vp;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    
                    if (!_vivePackages.TryGetValue(e.Name, out vp)) return;

                    StartCoroutine(Reload(vp));
                    
                    return;
                case WatcherChangeTypes.Deleted:
                    
                    if(!_vivePackages.TryGetValue(e.Name, out vp)) return;
                    
                    StartCoroutine(FullUnload(vp));
                    
                    return;
                case WatcherChangeTypes.Created:

                    if (_vivePackages.TryGetValue(e.Name, out vp))
                    {
                        StartCoroutine(Reload(vp));
                        return;
                    }
                    
                    vp = new VivePackage(e.FullPath);
                    _vivePackages.Add(vp.PackageName, vp);
                    
                    return;
            }
        }

        private void OnPackageRenamed(object source, RenamedEventArgs e)
        {
            var on = e.OldName;

            VivePackage vp;
            if (_vivePackages.TryGetValue(on, out vp))
                StartCoroutine(FullUnload(vp));

            var nvp = new VivePackage(e.FullPath);
            _vivePackages.Add(nvp.PackageName, nvp);
        }

        private void FindExistingVivePackages()
        {
            var dlcDir = DlcPath.DlcDirectory;
            if (!Directory.Exists(dlcDir))
                Directory.CreateDirectory(dlcDir);

            var vivePackagePaths = Directory.GetFiles(dlcDir, DlcPath.PackageWildcard).Distinct();

            foreach (var packagePath in vivePackagePaths)
            {
                var vp = new VivePackage(packagePath);
                _vivePackages.Add(vp.PackageName, vp);
            }
        }

        #endregion

        #region GUI API

        private void DrawPackageGui(VivePackage vp)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label(vp.PackageName);

                var isCurrentSceneLoaded = _loadedScenePackage == vp;

                if (!vp.IsUnPacked && GUILayout.Button(UnpackBtnContent))
                {
                    StartUnpack(vp);
                }
                else if (vp.IsUnPacked && GUILayout.Button(CleanupBtnContent))
                {
                    StartCleanup(vp);
                }

                if (vp.IsUnPacked)
                {
                    if (vp.HasScene)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (!isCurrentSceneLoaded && GUILayout.Button(LoadSceneBtnContent))
                            {
                                StartSceneLoad(vp);
                            }
                            else if(isCurrentSceneLoaded && GUILayout.Button(UnloadSceneBtnContent))
                            {
                                StartSceneUnload(vp);
                            }

                            GUILayout.Space(10.0f);
                            GUILayout.Label(vp.SceneName);
                        }
                    }
                    
                    if (vp.HasSkybox)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(LoadSkyboxBtnContent))
                                StartSkyboxLoad(vp);
                        }
                    }

                    // TODO: Load/Display/Unload objects
                }
            }
            
        }

        #endregion

        private void SetupWatcher()
        {
            _viveZipWatcher = new FileSystemWatcher
            {
                Path = DlcPath.DlcDirectory,
                Filter = DlcPath.PackageWildcard,
                NotifyFilter = NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.FileName
                               | NotifyFilters.Size
                               | NotifyFilters.CreationTime
            };
            
            _viveZipWatcher.Changed += OnPackageChanged;
            _viveZipWatcher.Created += OnPackageChanged;
            _viveZipWatcher.Deleted += OnPackageChanged;
            _viveZipWatcher.Renamed += OnPackageRenamed;

            _viveZipWatcher.EnableRaisingEvents = true;
        }

        #region Load/Unload Functions

        private AudioMixerGroup GetMixer(AudioMixerGroupBindings bindingsKey)
        {
            return _audioMixerGroup;
        }

        private IEnumerator WaitForActivity()
        {
            while (_activity != null) yield return null;
        }

        private IEnumerator Reload(VivePackage vp)
        {
            yield return StartCoroutine(WaitForActivity());

            var wasSceneLoaded = false;
            if (vp == _loadedScenePackage)
            {
                wasSceneLoaded = true;
                StartSceneUnload(vp);

                yield return StartCoroutine(WaitForActivity());
            }

            StartCleanup(vp);

            yield return StartCoroutine(WaitForActivity());

            if (!wasSceneLoaded) yield break;

            yield return StartCoroutine(WaitForActivity());

            StartSceneLoad(vp);
        }

        private static IEnumerator Clean()
        {
            yield return Resources.UnloadUnusedAssets();

            var generation = 0;
            while (generation < 3)
            {
                GC.Collect(generation, GCCollectionMode.Forced);
                yield return null;
                ++generation;
            }
        }

        private void StartCleanup(VivePackage vp, bool cleanResources = false)
        {
            Directory.Delete(vp.PackageDirectoryPath, true);
            vp.CheckFileReferences();

            if(cleanResources)
                _activity = Observable.FromMicroCoroutine(Clean)
                    .Subscribe(
                    _ => _activity = null, 
                    ex =>
                    {
                        Debug.LogException(ex);
                        _activity = null;
                    });
        }
        
        private IEnumerator FullUnload(VivePackage vp)
        {
            _vivePackages.Remove(vp.PackageName);

            if (_loadedScenePackage == vp)
            {
                StartSceneUnload(vp);
                yield return StartCoroutine(WaitForActivity());
            }

            StartCleanup(vp);
        }
        
        private void StartUnpack(VivePackage vp)
        {
            _activity = Extraction.ExtractPackage(
                    new Extraction.Parameters(
                        vp.AppId,
                        vp.PackageFilePath,
                        vp.PackageDirectoryPath))
                .Subscribe(_ =>
                    {
                        vp.CheckFileReferences();

                        _activity = null;
                    },
                    ex =>
                    {
                        Debug.LogException(ex);
                        _activity = null;
                    });
        }
        
        private void StartSceneLoad(VivePackage vp)
        {
            Action load = () =>
            {
                _activity = Loader.LoadScene(vp.SceneFile)
                    .Subscribe(res =>
                    {
                        vp.Scene = res;
                        _loadedScenePackage = vp;

                        ToggleGosForPreview(false, _gosToHideInPreview);
                        ToggleGosForPreview(true, _gosToShowInPreview);

                        Interactivity.Setup(
                            new Interactivity.Parameters(
                                vp.Scene,
                                this,
                                GetMixer));

                        SceneManager.SetActiveScene(vp.Scene);

                        ApplySkybox(_skyboxCubemap);
                        //RenderSettings.fog = false;
                        RenderSettings.ambientMode = AmbientMode.Skybox;
                        
                        _activity = null;
                    },
                    OnSceneError);
            };

            if (_loadedScenePackage != null)
            {
                _activity = Loader.UnloadScene(_loadedScenePackage.Scene)
                    .Subscribe(err =>
                    {
                        OnSceneUnloadComplete();
                        load();
                    },
                    OnSceneError);
            }
            else
            {
                load();
            }
        }
        
        private void StartSceneUnload(VivePackage vp)
        {
            _activity = Loader.UnloadScene(vp.Scene)
                .Subscribe(_ => OnSceneUnloadComplete(),
                OnSceneError);
        }

        private void LoadTestScene()
        {
            Interactivity.Setup(
                new Interactivity.Parameters(
                    SceneManager.GetActiveScene(),
                    this,
                    GetMixer ) );

            ToggleGosForPreview( false, _gosToHideInPreview );
            ToggleGosForPreview( true, _gosToShowInPreview );
        }

        private void StartSkyboxLoad(VivePackage vp)
        {
            _activity = Loader.LoadSkybox(vp.SkyboxFile)
                .Subscribe(res =>
                {
                    ApplySkybox(res);
                    _activity = null;
                    
                    
                    
                },
                ex =>
                {
                    Debug.LogException(ex);
                    _activity = null;
                });
        }

        private void OnSceneUnloadComplete()
        {
            _loadedScenePackage = null;
            _activity = null;

            Interactivity.ResetTeleport(this);
            SceneManager.SetActiveScene(_baseActiveScene);

            ToggleGosForPreview(true, _gosToHideInPreview);
            ToggleGosForPreview(false, _gosToShowInPreview);

            ApplySkybox(_skyboxCubemap);
            //RenderSettings.fog = true;
            RenderSettings.ambientMode = AmbientMode.Skybox;
        }

        private void OnSceneError(Exception exception)
        {
            OnError(exception);
            OnSceneUnloadComplete();
        }

        private void OnError(Exception exception)
        {
            Debug.LogException(exception);

            _activity = null;
        }
        
        private static void ToggleGosForPreview(bool isActive, GameObject[] gameObjects)
        {
            var len = gameObjects.Length;
            for (var idx = 0; idx < len; idx++)
            {
                var go = gameObjects[idx];
                if (go == null) continue;

                go.SetActive(isActive);
            }
        }

        #endregion

        #region Teleport Provider

        ViveNavMesh Interactivity.ITeleportProvider.NavMesh
        {
            get { return _navMesh; }
        }

        Animator Interactivity.ITeleportProvider.NavMeshAnimator
        {
            get { return _navMeshAnimator; }
        }

        TeleportVive Interactivity.ITeleportProvider.Teleporter
        {
            get { return _teleporter; }
        }

        ParabolicPointer Interactivity.ITeleportProvider.Pointer
        {
            get { return _pointer; }
        }

        Transform Interactivity.ITeleportProvider.CameraRig
        {
            get { return _cameraRig; }
        }

        Vector3 Interactivity.ITeleportProvider.OriginPosition
        {
            get { return new Vector3(); }
        }

        Quaternion Interactivity.ITeleportProvider.OriginRotation
        {
            get { return Quaternion.identity; }
        }

        #endregion
        
        private void ApplySkybox(Cubemap cubemap)
        {
            if (cubemap == null)
            {
                cubemap = _defaultSkyboxCubemap;
            }

            _skyboxCubemap = cubemap;
            _skyboxMaterial.SetTexture(_texPropId, _skyboxCubemap);
            
            RenderSettings.skybox = _skyboxMaterial;
            RenderSettings.ambientMode = AmbientMode.Skybox;
        }

        #endregion
        
        #region Helper Types

        private class VivePackage
        {
            #region Variables

            public readonly string PackageName;
            public readonly string PackageFilePath;
            public readonly string AppId;
            public readonly string PackageDirectoryPath;

            public readonly string SceneName;
            public readonly string SkyboxName;

            #endregion

            #region Properties

            public Scene Scene { get; set; }

            public string SceneFile { get; private set; }

            public string SkyboxFile { get; private set; }

            public bool IsUnPacked { get; private set; }

            public bool HasSkybox
            {
                get { return SkyboxFile != null; }
            }

            public bool HasScene
            {
                get { return SceneFile != null; }
            }

            public bool IsSceneLoaded
            {
                get { return Scene.IsValid() && Scene.isLoaded; }
            }
            
            #endregion

            #region Methods

            public VivePackage(string packageFilePath)
            {
                Assert.IsFalse(string.IsNullOrEmpty(packageFilePath));

                packageFilePath = DlcPath.CleanSeparators(packageFilePath);

                PackageFilePath = packageFilePath;
                PackageName = Path.GetFileName(PackageFilePath);
                AppId = PackageName.Replace(DlcPath.PackageExtention, "");
                PackageDirectoryPath = DlcPath.GetAppDlcDirectory(AppId);

                SceneName = DlcPath.GetSceneFileName(AppId);
                SkyboxName = DlcPath.GetSkyboxFileName(AppId);

                CheckFileReferences();
            }

            public void CheckFileReferences()
            {
                IsUnPacked = Directory.Exists(PackageDirectoryPath);
                SceneFile = CheckFileReference(DlcPath.GetAppScene);
                SkyboxFile = CheckFileReference(DlcPath.GetAppSkybox);
            }

            private string CheckFileReference(Func<string, string> getPath)
            {
                Assert.IsNotNull(getPath);
                var path = getPath(AppId);
                return IsUnPacked && File.Exists(path) ? path : null;
            }
            
            #endregion
        }

        #endregion
    }
}