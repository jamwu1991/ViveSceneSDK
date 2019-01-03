using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Htc.Viveport
{
    public static class FileUtils
    {
        public static string FixupSlashes(string source)
        {
            return source.Replace("\\", "/");
        }

        public static string UnFixupSlashes(string source)
        {
            return source.Replace("/", "\\");
        }

        public static string GetFixedUpWorkingDirectory()
        {
            return FixupSlashes(Directory.GetCurrentDirectory()) + "/";
        }

        public static string GetDirWithoutFixedupWorkingDirectory(string sourcePath)
        {
            return sourcePath.Replace(GetFixedUpWorkingDirectory(), "");
        }

        public static string GetDirWithoutUnFixedupWorkingDirectory(string sourcePath)
        {
            return sourcePath.Replace(Directory.GetCurrentDirectory(),"");
        }

        public static string GetDirWithWorkingDirectory(string sourcePath)
        {
            return GetFixedUpWorkingDirectory() + sourcePath;
        }
    }

    [CustomEditor(typeof(BuildDependencies))]
    public class BuildDependenciesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var isDirty = false;
            
            var dirProp = serializedObject.FindProperty("_directories");
            
            if (GUILayout.Button("Add Directory"))
            {
                var folderSelect = EditorUtility.OpenFolderPanel("Select directory to add.", "", "");
                folderSelect = FileUtils.GetDirWithoutFixedupWorkingDirectory(folderSelect);

                if (!string.IsNullOrEmpty(folderSelect) && !(serializedObject.targetObject as BuildDependencies).Directories.Contains(folderSelect))
                {
                    var oldSize = dirProp.arraySize;
                    dirProp.InsertArrayElementAtIndex(oldSize);
                    var folderProp = dirProp.GetArrayElementAtIndex(oldSize);
                    folderProp.stringValue = folderSelect;
                    isDirty = true;
                }
            }

            var removeIdx = -1;
            for (var idx = 0; idx < dirProp.arraySize; ++idx)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.enabled = false;
                        EditorGUILayout.LabelField(dirProp.GetArrayElementAtIndex(idx).stringValue);
                        GUI.enabled = true;

                        if (GUILayout.Button("Remove")) removeIdx = idx;
                    }
                }
            }

            if (removeIdx > -1)
            {
                isDirty = true;
                dirProp.DeleteArrayElementAtIndex(removeIdx);
            }

            isDirty |= serializedObject.ApplyModifiedProperties();

            if (!isDirty) return;

            EditorUtility.SetDirty(serializedObject.targetObject);
        }
    }


    [CreateAssetMenu(fileName = "BuildDependencies", menuName = "Build/Create Dependency list", order = 0)]
    public class BuildDependencies : ScriptableObject
    {
        [SerializeField] private List<string> _directories = new List<string>();
        
        public IEnumerable<string> Directories { get { return _directories; } }
    }

    public static class CopyBuildDependencies
    {
        public static IEnumerable<string> GetDependencyIds()
        {
            return AssetDatabase.FindAssets(string.Format("t:{0}", typeof(BuildDependencies).Name));
        }

        public static IEnumerable<string> GetDependencyPaths()
        {
            return GetDependencyIds()
                .Select(AssetDatabase.GUIDToAssetPath);
        }

        public static IEnumerable<BuildDependencies> GetAllDependencies()
        {
            return GetDependencyPaths()
                .Select(AssetDatabase.LoadAssetAtPath<BuildDependencies>);
        }

        public static IEnumerable<BuildDependencies> GetDependenciesWhere(Func<BuildDependencies, bool> predicate)
        {
            return GetAllDependencies()
                .Where(predicate);
        }
        
        [PostProcessBuild]
        private static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            var dependencies = GetAllDependencies();
            foreach (var bd in dependencies)
            {
                var directories = bd.Directories;

                var targetDir = Path.GetDirectoryName(pathToBuiltProject);

                foreach (var dir in directories.Distinct())
                    CopyDirectory(FileUtils.GetDirWithWorkingDirectory(dir), targetDir);
            }
        }
        
        public static void CopyDirectory(string source, string targetRoot)
        {
            var dest = Path.Combine(targetRoot, Path.GetFileName(source));

            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            foreach (var directory in Directory.GetDirectories(source))
            {
                var dirName = Path.GetFileName(directory);
                CopyDirectory(dirName, dest);
            }
            
            foreach (var file in Directory.GetFiles(source)
                .Where(f => !f.EndsWith(".meta") 
                && !f.EndsWith(".orig")))
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        }
    }

}