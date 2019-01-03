
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Htc.Viveport.SDK
{
    public static class Loader
    {
        public interface ILoadResults
        {
            string ResultError { get; }
            bool HasError { get; }
        }

        public struct SceneResults : ILoadResults
        {
            public readonly string Error;
            public readonly Scene Scene;

            public SceneResults(string error, Scene scene)
            {
                Error = error;
                Scene = scene;
            }

            public SceneResults(string error)
            {
                Error = error;
                Scene = new Scene();
            }

            public SceneResults(Scene scene)
            {
                Error = null;
                Scene = scene;
            }

            public string ResultError { get { return Error; } }
            public bool HasError { get { return !string.IsNullOrEmpty(Error); } }
        }

        public struct SkyboxResults : ILoadResults
        {
            public readonly string Error;
            public readonly Cubemap Skybox;

            public SkyboxResults(string error, Cubemap skybox)
            {
                Error = error;
                Skybox = skybox;
            }

            public SkyboxResults(string error)
            {
                Error = error;
                Skybox = null;
            }

            public SkyboxResults(Cubemap skybox)
            {
                Error = null;
                Skybox = skybox;
            }
            
            public string ResultError { get { return Error; } }
            public bool HasError { get { return !string.IsNullOrEmpty(Error); } }
        }

        // TODO: Objects

        private static IObservable<AssetBundle> Load(string path)
        {
            if (!File.Exists(path))
                return Observable.Throw<AssetBundle>(new Exception(string.Format("Unable to open asset bundle {0}", path)));

            return AssetBundle.LoadFromFileAsync(path)
                .AsAsyncOperationObservable()
                .Catch<AssetBundleCreateRequest, NullReferenceException>(argEx =>
                {
                    throw new Exception("AssetBundle.LoadFromFileAsync " + path + " fail");
                })
                .Last() // last sequence is load completed
                .Select(abr => abr.assetBundle);
        }

        public static IObservable<Scene> LoadScene(string sceneFilePath)
        {
            Assert.IsFalse(string.IsNullOrEmpty(sceneFilePath));

            return Load(sceneFilePath)
                .Select(bundle => new Tuple<AssetBundle, string[]>(bundle, bundle.GetAllScenePaths()))
                .Do(bundleAndPaths =>
                {
                    if (bundleAndPaths.Item2.Length != 0) return;

                    bundleAndPaths.Item1.Unload(true);
                    throw new Exception(string.Format("No Scenes in bundle {0}", sceneFilePath));
                })
                .Select(bundleAndPaths => new Tuple<AssetBundle, string>(bundleAndPaths.Item1,
                    Path.GetFileName(bundleAndPaths.Item2[0]).Replace(".unity", ""))) // get first path -> scene name
                .Select(bundleAndSceneName =>
                {
                    // spin off sub operation stream to load scene
                    // once complete, reforward parameters
                    return SceneManager.LoadSceneAsync(bundleAndSceneName.Item2, LoadSceneMode.Additive)
                        .AsAsyncOperationObservable()
                        .Last()
                        .Catch<AsyncOperation, ArgumentNullException>(argEx =>
                        {
                            // reforward exceptions
                            bundleAndSceneName.Item1.Unload(true);
                            throw new Exception(
                                string.Format("Unable to load scene {0} from bundle {1}",
                                bundleAndSceneName.Item2,
                                sceneFilePath));
                        })
                        .Select(x => bundleAndSceneName);
                })
                .SelectMany(loading => loading)
                .Select(bundleAndScene => new Tuple<AssetBundle, Scene>(bundleAndScene.Item1,
                    SceneManager.GetSceneByName(bundleAndScene.Item2)))
                .Do(bundleAndScene =>
                {
                    // if there was a load problem, report it
                    var mainScene = bundleAndScene.Item2;
                    if (!mainScene.IsValid() || !mainScene.isLoaded)
                    {
                        var mainSceneName = mainScene.name;
                        bundleAndScene.Item1.Unload(true);
                        throw new Exception(
                            string.Format("Scene {0} was not loaded correctly from {1}", 
                            mainSceneName,
                            sceneFilePath));
                    }
                })
                .Do(duo => duo.Item1.Unload(false))
                .Select(duo => duo.Item2);
        }

        public static IObservable<Unit> UnloadScene(Scene scene)
        {
            Assert.IsFalse(string.IsNullOrEmpty(scene.name));

            if (!scene.IsValid())
                return Observable.Throw<Unit>(new Exception(string.Format("{0} is not a valid scene!", scene.name)));

            if (!scene.isLoaded)
                return Observable.Throw<Unit>(new Exception(string.Format("{0} is not loaded!", scene.name)));

            return SceneManager.UnloadSceneAsync(scene)
                .AsAsyncOperationObservable()
                .AsUnitObservable()
                .Catch<Unit, Exception>(ex =>
                {
                    throw new Exception(string.Format("{0} unable to unload!", scene.name));
                });
        }

        public static IObservable<Cubemap> LoadSkybox(string skyboxFilePath)
        {
            Assert.IsFalse(string.IsNullOrEmpty(skyboxFilePath));
            
            return Load(skyboxFilePath)
                .Select(bundle =>
                {
                    var bundleName = Path.GetFileName(skyboxFilePath);

                    var desiredBundleItem = Path.GetFileName(bundle
                        .GetAllAssetNames()
                        .FirstOrDefault(x => x.Contains(bundleName))) ?? bundleName;
                    
                    return bundle.LoadAssetAsync<Cubemap>(desiredBundleItem)
                        .AsAsyncOperationObservable()
                        .Last()
                        .Catch<AssetBundleRequest, ArgumentNullException>(argEx =>
                        {
                            bundle.Unload(true);
                            throw new Exception(string.Format("Unable to load skybox: {0}", bundleName));
                        })
                        .Do(abr => bundle.Unload(false))
                        .Select(abr => abr.asset as Cubemap);
                })
                .SelectMany(cubes => cubes);
        }
    }

}
