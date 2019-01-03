using UnityEngine;
using UnityEngine.Assertions;
using Valve.VR;

public class DefaultTeleportInput : TeleportInput
{
    /// SteamVR controllers that should be polled.
    [Tooltip("Array of SteamVR controllers that may used to select a teleport destination.")]
    public SteamVR_TrackedObject[] Controllers;
    private SteamVR_TrackedObject ActiveController;
    private SteamVR_Controller.Device ControllerDevice;
    
    public override void GetSelectionStatus(out bool shouldTeleport, out bool shouldCancel)
    {
        Assert.IsNotNull(ActiveController);
        
        // Here, there is an active controller - that is, the user is holding down on the trackpad.
        // Poll controller for pertinent button data
        var index = (int)ActiveController.index;
        var device = SteamVR_Controller.Input(index);
        shouldTeleport = device.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad);
        shouldCancel = device.GetPressUp(SteamVR_Controller.ButtonMask.Grip);
    }

    public override bool TryEnterSelectionMode(out Transform trackedParent)
    {
        // At this point the user is not holding down on the touchpad at all or has canceled a teleport and hasn't
        // let go of the touchpad.  So we wait for the user to press the touchpad and enable visual indicators
        // if necessary.
        foreach (var obj in Controllers)
        {
            var index = (int)obj.index;
            if (index == -1) continue;

            var device = SteamVR_Controller.Input(index);
            if (!device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad)) continue;

            // Set active controller to this controller, and enable the parabolic pointer and visual indicators
            // that the user can use to determine where they are able to teleport.
            ActiveController = obj;
            ControllerDevice = device;
            trackedParent = obj.transform;
            return true;

        }

        trackedParent = null;
        return false;
    }
    
    public override void ExitSelectionMode()
    {
        // Reset active controller, disable pointer, disable visual indicators
        ActiveController = null;
        ControllerDevice = null;
    }

    public override void TriggerHapticPulse(ushort durationMicroSec = 500, EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0)
    {
        Assert.IsNotNull(ControllerDevice);
        
        ControllerDevice.TriggerHapticPulse(durationMicroSec, buttonId);
    }
}