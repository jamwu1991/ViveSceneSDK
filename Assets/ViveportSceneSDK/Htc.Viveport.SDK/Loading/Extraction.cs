

using System;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine.Assertions;

namespace Htc.Viveport.SDK
{
    public static class Extraction
    {
        public class Parameters
        {
            public readonly string AppId;
            public readonly string SrcPath;
            public readonly string DestPath;

            public Parameters(string appId, string srcPath, string destPath)
            {
                AppId = appId;
                SrcPath = srcPath;
                DestPath = destPath;
            }
        }

        private static void CheckForError(string err)
        {
            if(!string.IsNullOrEmpty(err)) throw new Exception(err);
        }
        
        public static IObservable<Unit> ExtractPackage(Parameters parameters)
        {
            return Observable.Start(() =>
                {
                    var src = parameters.SrcPath;
                    var dst = parameters.DestPath;

                    if (string.IsNullOrEmpty(src) || !File.Exists(src)) throw new FileNotFoundException(src);

                    if (!Directory.Exists(dst))
                        Directory.CreateDirectory(dst);

                    var error = ZipUtility.UnzipPackage(dst, src);
                    CheckForError(error);
                })
                .ObserveOnMainThread();
        }
    }
}
