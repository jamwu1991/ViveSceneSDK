//========= Copyright 2016, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Htc.Viveport.SDK
{
    public static class DlcPath
    {
        #region Constants and Cached Values

        public const string DlcFolderName = "ViveportVRDLC";
        private const int NameMaxLength = 64;

        public const string PackageWildcard = "*" + PackageExtention;
        public const string PackageExtention = ".vive.zip";

        private static HashSet<char> _invalidFileNameChars;
        private static HashSet<char> InvalidFileNameChars { get { return _invalidFileNameChars ?? (_invalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars())); } }

        static DlcPath()
        {
            DataPath = CleanSeparators(Directory.GetCurrentDirectory());
            DlcDirectory = DataPath + "/" + DlcFolderName;
            DlcDirectoryPrefix = DlcDirectory + "/";
        }

        public static readonly string DataPath;
        public static readonly string DlcDirectory;
        public static readonly string DlcDirectoryPrefix;

        private static string _majorVersion;

        #endregion

        public static string MajorVersion
        {
            get
            {
                if (_majorVersion != null) return _majorVersion;
                
                var appVersion = Application.unityVersion;
                var sepIndex = appVersion.IndexOf('.');
                _majorVersion = appVersion.Substring(0, sepIndex);
                
                return _majorVersion;
            }
        }

        public static void Init() {} // force static constructor on main thread

        #region Name Formaters

        public static string GetSceneFileName(string appid)
        {
            return string.Format("{0}_scene_{1}", appid, MajorVersion);
        }

        public static string GetSkyboxFileName(string appid)
        {
            return string.Format("{0}_skybox_{1}", appid, MajorVersion);
        }
        
        #endregion

        #region Path Formatters

        public static string GetTargetParent(string sourcePath)
        {
            return CleanSeparators(Directory.GetParent(sourcePath).FullName);
        }

        public static string CleanSeparators(string path)
        {
            return path.Replace("\\", "/");
        }

        // ex. D:/myproject/ViveportVRDLC/100/
        public static string GetAppDlcDirectory(string appid)
        {
            return DlcDirectoryPrefix + ToValidName(appid);
        }

        public static string GetAppDlcDirectoryPrefix(string appId)
        {
            return GetAppDlcDirectory(appId) + "/";
        }

        public static string GetAppZip(string appid)
        {
            return DlcDirectoryPrefix + ToValidName(appid) + PackageExtention;
        }

        public static string GetAppScene(string appid)
        {
            return GetAppDlcDirectoryPrefix(appid) + GetSceneFileName(appid);
        }

        public static string GetAppSkybox(string appid)
        {
            return GetAppDlcDirectoryPrefix(appid) + GetSkyboxFileName(appid);
        }

        #endregion

        #region Valid Name

        public static string ToValidName(string orgStr)
        {
            if (orgStr == null) return string.Empty;

            var validStr = orgStr.Length > NameMaxLength ? orgStr.Remove(NameMaxLength, orgStr.Length - NameMaxLength) : orgStr;

            validStr = validStr.Trim().ToLower().Replace(' ', '_');

            var containsInvalidChar = false;
            var nameCharArray = validStr.ToCharArray();

            // replace all invalid character into under-line
            for (var i = nameCharArray.Length - 1; i >= 0; --i)
            {
                if (!InvalidFileNameChars.Contains(nameCharArray[i])) continue;

                containsInvalidChar = true;
                nameCharArray[i] = '_';
            }

            if (containsInvalidChar)
            {
                validStr = new string(nameCharArray);
            }

            return validStr;
        }

        public static bool IsValidName(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length > NameMaxLength) { return false; }
            
            return !str
                .Any(c => c >= 'A' && c <= 'Z'
                || c == ' '
                || InvalidFileNameChars.Contains(c));
        }

        #endregion
    }

}