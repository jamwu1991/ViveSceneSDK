using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Htc.Viveport.SDK
{
    public static class ZipUtility
    {
        public const string ZipExecName = "7za.exe";
        public const string ZipExecError = "7z executable not found";
        public static readonly string ZipExecPath;

        static ZipUtility()
        {
            var exe7zPath = string.Empty;
            if (!TryGetExe(out exe7zPath, Path.GetDirectoryName(Application.dataPath), ZipExecName))
            {
                ZipExecPath = null;
                return;
            }
            
            ZipExecPath = exe7zPath;
        }

        public static void Init() { } // force static constructor on main thread
        
        private static bool TryGetExe(out string result, string searchDirectory, string exeName)
        {
            result = exeName;
            if (File.Exists(result))
                return true;
            FileInfo[] files = new DirectoryInfo(searchDirectory).GetFiles(exeName, SearchOption.AllDirectories);
            int index = 0;
            if (index < files.Length)
            {
                FileInfo fileInfo = files[index];
                result = fileInfo.FullName;
                return true;
            }
            result = string.Empty;
            return false;
        }

        public static bool CheckZipError(ref string error)
        {
            if (!string.IsNullOrEmpty(ZipExecPath)) return false;

            error = ZipExecError;
            return true;
        }
        
        // TODO: Move all zip commands to asyncronously wait on the external process
        // with UniRx
        
        private static void Zip(ref string error, string directory, string arguments)
        {
            if(CheckZipError(ref error)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = ZipExecPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = directory,
                Arguments = arguments
            };

            using (var exeProcess = Process.Start(startInfo))
            {
                exeProcess.BeginOutputReadLine();

                error = exeProcess.StandardError.ReadToEnd();

                exeProcess.WaitForExit();
            }
        }

        public static string AddToZipPackage(string directory, string source, string destination)
        {
            var error = string.Empty;
            if (CheckZipError(ref error)) return error;

            Zip(ref error, 
                directory, 
                "a -y -r -aoa \"" + destination + "\" \"" + source + "\\*\"");
            
            return error;
        }

        public static string UnzipPackage(string workingDirectory, string archiveFile, string outputPath)
        {
            var error = string.Empty;
            if (CheckZipError(ref error)) return error;

            if (!string.IsNullOrEmpty(outputPath) && !Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            Zip(ref error,
                workingDirectory, 
                "x -y -aoa \"" + archiveFile + "\" " +
                (string.IsNullOrEmpty(outputPath) ? "" : (" -o\"" + outputPath + "\"")));
            
            return error;
        }

        public static string UnzipPackage(string workingDirectory, string archiveFile)
        {
            return UnzipPackage(workingDirectory, archiveFile, string.Empty);
        }
    }

}