// ========================================================================== //
//
//  class TrackpadButtons
//  -----
//  Purpose: Extend SteamVR's trackpad as soft buttons
//
//
//  Created: 2016-10-12
//  Updated: 2016-11-07
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

namespace Htc.VR.Input
{
    public class TrackpadButtons : MonoBehaviour
    {
        #region Types and enums

        const int InvalidRegionId = -1;

        #endregion


        #region Unity interface

        [Header("SteamVR Controller")]
        [SerializeField]
        private Controller controller;

        [Header("Buttons")]

        [SerializeField]
        private Button centerButton;

        [SerializeField]
        private Button[] peripheralButtons;

        [Header("Layout")]

        [SerializeField]
        private float centerRadius = 0.5f;

        [Tooltip("Angle in degrees of the first periphral button. North = 0, rotating clockwise.")]
        [SerializeField]
        private float peripheralStartAngle = 0;

        [Header("Debug")]

        // Debug
        public Vector2 padAxis;
        public float angle;

        #endregion


        #region Properties and fields

        private int curRegionId = InvalidRegionId;
        private int clickedRegionId = InvalidRegionId;

        #endregion


        #region Public methods

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion


        #region Private methods

        /// <summary>
        /// Gets the id of the touched region
        /// </summary>
        /// <param name="vec"></param>
        /// <returns>
        /// 0 for the center region.
        /// 1 ~ periphralCount for periphral regions.
        /// -1 if error.
        /// </returns>
        private int GetRegionId(Vector2 vec)
        {
            if (centerButton != null && vec.magnitude < centerRadius)
                return 0;

            if (peripheralButtons.Length <= 0)
                return InvalidRegionId;

            // First make sure this is a vector with a valid direction
            var minval = 0.001f;
            if (Mathf.Abs(vec.x) < minval && Mathf.Abs(vec.y) < minval)
                return InvalidRegionId;

            // The angle offset for the touch area
            var areaOffset = 360f / peripheralButtons.Length / 2;

            var angle = (Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg - peripheralStartAngle + areaOffset) / 360f;

            while (angle >= 1)
                angle--;

            while (angle < 0)
                angle++;

            return Mathf.FloorToInt(angle * peripheralButtons.Length) + 1;
        }

        private void OnPadDown(Controller controller)
        {
            Button button = GetButton(curRegionId);
            if (button != null)
            {
                clickedRegionId = curRegionId;

                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);

                //Debug.Log("<color=cyan> pad down executed.</color>");
            }

            //Debug.Log("<color=green>Pad button down!</color>");
        }
        
        private void OnPadUp(Controller controller)
        {
            Button button = GetButton(curRegionId);
            if (button != null)
            {
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                button.OnDeselect(pointer);

                //Debug.Log("<color=cyan> pad up executed.</color>");

                if (curRegionId == clickedRegionId)
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            }

            //Debug.Log("<color=green>Pad Button up!</color>");

            clickedRegionId = InvalidRegionId;
        }

        private void OnPadTouchDown(Controller controller)
        {
            Button button = GetButton(curRegionId);
            if (button != null)
            {
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);

                //Debug.Log("<color=cyan> pad touch down executed.</color>");
            }

            //Debug.Log("<color=green>Pad Touch down!</color>");
        }

        private void OnPadTouchUp(Controller controller)
        {
            Button button = GetButton(curRegionId);
            if (button != null)
            {
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);

                //Debug.Log("<color=cyan> pad touch up executed.</color>；");
            }

            //Debug.Log("<color=green>Pad Touch up!</color>");
            //OnPadUp(controller);
        }

        private void UpdateRegion(int regionId)
        {
            if (curRegionId == regionId)
                return;

            var pointer = new PointerEventData(EventSystem.current);

            Button curButton = GetButton(curRegionId);
            Button newButton = GetButton(regionId);

            if (curButton != null)
            {
                if (controller.padPressed)
                {
                    //Debug.Log("<color=cyan>Leaving old button.</color>");
                    ExecuteEvents.Execute(curButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                }
                else if(!controller.padPressed && regionId == -1)
                {
                    //Debug.Log("<color=cyan>Leaving old button.</color>");
                    ExecuteEvents.Execute(curButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                }

                ExecuteEvents.Execute(curButton.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            }

            if (newButton != null)
            {
                ExecuteEvents.Execute(newButton.gameObject, pointer, ExecuteEvents.pointerEnterHandler);

                if (controller.padPressed)
                {
                    //Debug.Log("<color=cyan>Entering new button.</color>");
                    ExecuteEvents.Execute(newButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                }
                    
            }

            //Debug.LogFormat("<color=#ffff00ff> Changing region {0} {1}</color>", curRegionId, regionId);

            curRegionId = regionId;
            
            controller.HapticPulse();
        }

        private Button GetButton(int regionId)
        {
            if (regionId == 0)
                return centerButton;
            else if (regionId > 0 && regionId <= peripheralButtons.Length)
                return peripheralButtons[regionId - 1];

            return null;
        }

        #endregion


        #region MonoBehaviour

        void Update()
        {
            if (controller.padTouched)
            {
                padAxis = controller.padAxis;

                var regionId = GetRegionId(padAxis);

                UpdateRegion(regionId);
            }
            else
            {
                UpdateRegion(InvalidRegionId);
            }
        }

        void OnEnable()
        {
            if (controller == null)
                controller = GetComponentInParent<Controller>();

            if (controller != null)
            {
                // Add event listeners
                controller.onPadTouchDown += OnPadTouchDown;
                controller.onPadTouchUp += OnPadTouchUp;
                controller.onPadDown += OnPadDown;
                controller.onPadUp += OnPadUp;
            }
            else
            {
                enabled = false;
            }
        }

        void OnDisable()
        {
            if(controller != null)
            {
                // Remove event listeners
                controller.onPadTouchDown -= OnPadTouchDown;
                controller.onPadTouchUp -= OnPadTouchUp;
                controller.onPadDown -= OnPadDown;
                controller.onPadUp -= OnPadUp;
            }
        }

        #endregion
    }
}