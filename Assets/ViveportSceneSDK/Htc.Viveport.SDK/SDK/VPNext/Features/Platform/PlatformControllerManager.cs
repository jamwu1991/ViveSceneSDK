using System.Collections;
using System.Text;
using Htc.VR.Input;
using UnityEngine;
using Valve.VR;

namespace Htc.Viveport.Store
{
    public class PlatformControllerManager : MonoBehaviour
    {
        [SerializeField] private Controller _controller;

        private IEnumerator Start()
        {
            if (_controller == null)
            {
                Modify(RecognizedControllerKind.Invalid);
                yield break;
            }
            
            while (_controller.device == null)
            {
                yield return null;
            }
            
            var deviceIndex = _controller.device.index;

            var err = ETrackedPropertyError.TrackedProp_Success;
            var result = new StringBuilder(64);
            OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex,
                ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref err);

            if (err != ETrackedPropertyError.TrackedProp_Success)
            {
                Modify(RecognizedControllerKind.Invalid);
                Debug.LogErrorFormat("<color=blue>{0} Error!</color", result);
                yield break;
            }

            var resultAsLowercase = result.ToString().ToLower();

            var id = RecognizedControllerKind.Unknown;

            if (resultAsLowercase.Contains("vive"))
                id = RecognizedControllerKind.ViveController;
            else if (resultAsLowercase.Contains("oculus"))
                id = RecognizedControllerKind.OculusTouch;
            else if (resultAsLowercase.Contains("knuckles"))
                id = RecognizedControllerKind.Knuckles;
            
            Modify(id);
        }

        private void Modify(RecognizedControllerKind kind)
        {
            foreach (var modifier in GetComponentsInChildren<PlatformControllerModifier>(true))
            {
                modifier.Modify(kind);
            }
        }

        private void Cache()
        {
            if (_controller == null)
                _controller = GetComponent<Controller>();
        }
        
        #if UNITY_EDITOR

        private void Reset()
        {
            Cache();
        }

        private void OnValidate()
        {
            Cache();
        }

        #endif
    }

    public enum RecognizedControllerKind
    {
        Invalid = -1,
        Unknown = 0,
        ViveController,
        OculusTouch,
        Knuckles,
    }
}