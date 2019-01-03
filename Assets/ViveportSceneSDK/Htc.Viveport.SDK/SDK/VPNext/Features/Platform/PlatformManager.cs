using System.Collections;
using Htc.Viveport.SDK;
using UnityEngine;

namespace Htc.Viveport.Store
{
    public sealed class PlatformManager : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            
            var instance = SteamVR.instance;
//            Debug.Log(string.Format("{0} {1} {2} ", instance.hmd_TrackingSystemName, instance.hmd_SerialNumber, instance.hmd_ModelNumber));

            var id = RecognizedPlatformId.Invalid;
            var modelAsLowerCase = instance.hmd_ModelNumber.ToLower();

            if (modelAsLowerCase.Contains("vive"))
                id = RecognizedPlatformId.VivePC;
            else if (modelAsLowerCase.Contains("oculus") || modelAsLowerCase.Contains("rift"))
                id = RecognizedPlatformId.OculusPC;
            else if (!modelAsLowerCase.Contains("Null"))
                id = RecognizedPlatformId.Unknown;
            
            var contextModifiers = Trait<PlatformContextModifier>.All;
            foreach (var modifier in contextModifiers)
            {
                modifier.Modify(id);
            }
        }
    }
    
    public enum RecognizedPlatformId
    {
        Invalid = -1,
        Unknown = 0,
        VivePC,
        OculusPC,
    }
}