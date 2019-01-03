// ========================================================================== //
//
//  clsss Controller
//  -----
//  Purpose: Wrapper for a Vive Controller
//
//
//  Created: 2016-11-27
//  Updated: 2016-11-27
//
//  Copyright 2016 Yu-hsien Chang
// 
// ========================================================================== //
using UnityEngine;

namespace Htc.VR.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SteamVR_TrackedObject))]
    public class Controller : MonoBehaviour
    {
        #region Types and enums

        public enum AxisSector
        {
            Unknown,
            Top,
            Bottom,
            Left,
            Right
        }

        public enum AxisSectorMode
        {
            TopBottom,
            LeftRight,
            Quadrant,
        }

        public delegate void ControllerEvent(Controller controller);

        #endregion


        #region Unity interface

        #endregion


        #region Properties and fields

        private SteamVR_TrackedObject _trackedObject;
        private SteamVR_TrackedObject trackedObject { get { if (_trackedObject == null) _trackedObject = GetComponent<SteamVR_TrackedObject>(); return _trackedObject; } }

        private ControllerPointerInput _pointerInput;
        public ControllerPointerInput pointerInput
        {
            get
            {
                if (_pointerInput == null)
                {
                    _pointerInput = GetComponent<ControllerPointerInput>();
                    if (_pointerInput == null)
                        _pointerInput = gameObject.AddComponent<ControllerPointerInput>();
                }

                return _pointerInput;
            }
        }

        private SteamVR_TrackedObject.EIndex index { get { return trackedObject.index; } }

        public SteamVR_Controller.Device device
        {
            get
            {
                if (index >= 0)
                    return SteamVR_Controller.Input((int)index);
                else
                    return null;
            }
        }

        public bool triggerDown { get { return GetButtonDown(SteamVR_Controller.ButtonMask.Trigger); } }
        public bool triggerPressed { get { return GetButtonPressed(SteamVR_Controller.ButtonMask.Trigger); } }
        public bool triggerUp { get { return GetButtonUp(SteamVR_Controller.ButtonMask.Trigger); } }

        public bool menuDown { get { return GetButtonDown(SteamVR_Controller.ButtonMask.ApplicationMenu); } }
        public bool menuPressed { get { return GetButtonPressed(SteamVR_Controller.ButtonMask.ApplicationMenu); } }
        public bool menuUp { get { return GetButtonUp(SteamVR_Controller.ButtonMask.ApplicationMenu); } }

        public bool padDown { get { return GetButtonDown(SteamVR_Controller.ButtonMask.Touchpad); } }
        public bool padPressed { get { return GetButtonPressed(SteamVR_Controller.ButtonMask.Touchpad); } }
        public bool padUp { get { return GetButtonUp(SteamVR_Controller.ButtonMask.Touchpad); } }

        public bool padTouchDown { get { return GetTouchDown(SteamVR_Controller.ButtonMask.Touchpad); } }
        public bool padTouched { get { return GetTouch(SteamVR_Controller.ButtonMask.Touchpad); } }
        public bool padTouchUp { get { return GetTouchUp(SteamVR_Controller.ButtonMask.Touchpad); } }

        public bool gripDown { get { return GetButtonDown(SteamVR_Controller.ButtonMask.Grip); } }
        public bool gripPressed { get { return GetButtonPressed(SteamVR_Controller.ButtonMask.Grip); } }
        public bool gripUp { get { return GetButtonUp(SteamVR_Controller.ButtonMask.Grip); } }
        
        public Vector2 padAxis
        {
            get
            {
                if (device != null)
                    return device.GetAxis();
                else
                    return Vector2.zero;
            }
        }

        public AxisSector padAxisSector { get { return GetAxisSector(padAxis); } }

        public Vector2 padAxisDelta { get; private set; }

        public event ControllerEvent onTriggerDown;
        public event ControllerEvent onTriggerPressed;
        public event ControllerEvent onTriggerUp;

        public event ControllerEvent onMenuDown;
        public event ControllerEvent onMenuPressed;
        public event ControllerEvent onMenuUp;

        public event ControllerEvent onPadDown;
        public event ControllerEvent onPadPressed;
        public event ControllerEvent onPadUp;

        public event ControllerEvent onPadTouchDown;
        public event ControllerEvent onPadTouched;
        public event ControllerEvent onPadTouchUp;

        public event ControllerEvent onGripDown;
        public event ControllerEvent onGripPressed;
        public event ControllerEvent onGripUp;

        private Vector2 previousPadAxis;

        #endregion


        #region Public methods

        public void HapticPulse()
        {
            if (device != null)
                device.TriggerHapticPulse();
        }
        
        public void HapticPulse(ushort durationMicroSecs)
        {
            if(device != null)
                device.TriggerHapticPulse(durationMicroSecs);
        }

        #endregion


        #region Static methods

        public static AxisSector GetAxisSector(Vector2 vec, AxisSectorMode mode = AxisSectorMode.Quadrant)
        {
            if (Mathf.Approximately(vec.magnitude, 0))
                return AxisSector.Unknown;

            switch (mode)
            {
                case AxisSectorMode.TopBottom:
                    {
                        if (vec.Angle() < 0)
                            return AxisSector.Bottom;
                        else
                            return AxisSector.Top;
                    }

                case AxisSectorMode.LeftRight:
                    {
                        float angle = Math.Angle(Vector2.up, vec);
                        if (angle < 0)
                            return AxisSector.Right;
                        else
                            return AxisSector.Left;
                    }

                case AxisSectorMode.Quadrant:
                    {
                        float angle = Math.Angle(new Vector2(0.5f, 0.5f), vec);

                        if (angle < -90)
                            return AxisSector.Bottom;
                        else if (angle < 0)
                            return AxisSector.Right;
                        else if (angle < 90)
                            return AxisSector.Top;
                        else
                            return AxisSector.Left;
                    }
            }

            return AxisSector.Unknown;
        }

        #endregion


        #region Private methods

        public bool GetButtonDown(ulong buttonMask)
        {
            if (device != null)
                return device.GetPressDown(buttonMask);
            else
                return false;
        }

        public bool GetButtonPressed(ulong buttonMask)
        {
            if (device != null)
                return device.GetPress(buttonMask);
            else
                return false;
        }

        public bool GetButtonUp(ulong buttonMask)
        {
            if (device != null)
                return device.GetPressUp(buttonMask);
            else
                return false;
        }

        public bool GetTouchDown(ulong buttonMask)
        {
            if (device != null)
                return device.GetTouchDown(buttonMask);
            else
                return false;
        }

        public bool GetTouch(ulong buttonMask)
        {
            if (device != null)
                return device.GetTouch(buttonMask);
            else
                return false;
        }

        public bool GetTouchUp(ulong buttonMask)
        {
            if (device != null)
                return device.GetTouchUp(buttonMask);
            else
                return false;
        }

        public void InvokeIfNotNull(ControllerEvent evt)
        {
            if (evt != null)
                evt(this);
        }

        #endregion


        #region MonoBehaviour

        void Update()
        {
            if (device == null)
                return;

            // Trigger
            if (triggerDown)
                InvokeIfNotNull(onTriggerDown);
            else if (triggerPressed)
                InvokeIfNotNull(onTriggerPressed);
            else if (triggerUp)
                InvokeIfNotNull(onTriggerUp);

            // Menu
            if (menuDown)
                InvokeIfNotNull(onMenuDown);
            else if (menuPressed)
                InvokeIfNotNull(onMenuPressed);
            else if (menuUp)
                InvokeIfNotNull(onMenuUp);

            // Touchpad
            if (padDown)
                InvokeIfNotNull(onPadDown);
            else if (padPressed)
                InvokeIfNotNull(onPadPressed);
            else if (padUp)
                InvokeIfNotNull(onPadUp);

            if (padTouchDown)
                InvokeIfNotNull(onPadTouchDown);
            else if (padTouched)
                InvokeIfNotNull(onPadTouched);
            else if (padTouchUp)
                InvokeIfNotNull(onPadTouchUp);

            // Grip
            if (gripDown)
                InvokeIfNotNull(onGripDown);
            else if (gripPressed)
                InvokeIfNotNull(onGripPressed);
            else if (gripUp)
                InvokeIfNotNull(onGripUp);

            // Pad axis
            if (padTouchDown)
            {
                padAxisDelta = Vector2.zero;
                previousPadAxis = padAxis;
            }
            else if (padTouched)
            {
                padAxisDelta = padAxis - previousPadAxis;
                previousPadAxis = padAxis;
            }
            else
            {
                padAxisDelta = Vector2.zero;
            }
        }

        #endregion
    }
}