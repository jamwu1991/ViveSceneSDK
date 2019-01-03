using Htc.VR.Input;
using UnityEngine;
using Valve.VR;

namespace Htc.Viveport.Store
{
    [RequireComponent(typeof(Controller))]
    public class TeleportInputVpNext : TeleportInput
    {
        [SerializeField] private Controller _controller;

        public RecognizedControllerKind ControllerKind = RecognizedControllerKind.ViveController;

        private void Awake()
        {
            Cache();
        }

        public override void GetSelectionStatus(out bool shouldTeleport, out bool shouldCancel)
        {
            switch (ControllerKind)
            {
                case RecognizedControllerKind.ViveController:
                    shouldTeleport = _controller.padTouchUp;
                    shouldCancel = _controller.gripUp;
                    break;
                case RecognizedControllerKind.OculusTouch:
                    
                    var axisSector = _controller.padAxisSector;
                    var isTeleportSector = axisSector != Controller.AxisSector.Unknown;
                    
                    shouldTeleport = _controller.padDown || !_controller.padTouched;
                    shouldCancel = !shouldTeleport && !isTeleportSector;
                    
                    break;
                default:
                    goto case RecognizedControllerKind.ViveController;
            }
        }

        public override bool TryEnterSelectionMode(out Transform trackedParent)
        {
            var pressDown = false;
            switch (ControllerKind)
            {
                case RecognizedControllerKind.ViveController:
                    pressDown = _controller.padDown;
                    break;
                case RecognizedControllerKind.OculusTouch:
                    var axisSector = _controller.padAxisSector;
                    var isTeleportSector = axisSector != Controller.AxisSector.Unknown;
                    
                    pressDown = _controller.padTouched && isTeleportSector;
                    break;
                default:
                    goto case RecognizedControllerKind.ViveController;
            }
            
            trackedParent = pressDown ? _controller.transform : null;
            return pressDown;
            
        }

        public override void ExitSelectionMode()
        {
            // no-op
        }

        public override void TriggerHapticPulse(ushort durationMicroSec = 500, EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0)
        {
            _controller.HapticPulse(durationMicroSec);
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
}