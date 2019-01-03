using UnityEngine;
using Valve.VR;

public abstract class TeleportInput : MonoBehaviour
{
    public abstract void GetSelectionStatus(out bool shouldTeleport, out bool shouldCancel);

    public abstract bool TryEnterSelectionMode(out Transform trackedParent);

    public abstract void ExitSelectionMode();

    public abstract void TriggerHapticPulse(ushort durationMicroSec = 500,
        EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad);
}