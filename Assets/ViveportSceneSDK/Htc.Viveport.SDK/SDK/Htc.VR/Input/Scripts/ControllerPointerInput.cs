// ========================================================================== //
//
//  class ControllerPointerInput
//  -----
//  Purpose: Enable the attached SteamVR Controller as a PointerInput
//
//  Usage: Attach to the root of a SteamVR Controller GameObject
//
//  Created: 2016-10-12
//  Updated: 2016-10-12
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;

namespace Htc.VR.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Controller))]
    public class ControllerPointerInput : PointerInput
    {
        #region Unity interface

        [SerializeField]
        private bool _enableScroll = true;
        public bool enableScroll
        {
            get { return _enableScroll; }
            set { _enableScroll = value; }
        }

        [SerializeField]
        private float _scrollSpeed = 1f;
        public float scrollSpeed
        {
            get { return _scrollSpeed; }
            set { _scrollSpeed = value; }
        }

        [SerializeField]
        //[Range(0, 1)]
        private float _scrollInertia = 0.5f;
        public float scrollInertia
        {
            get { return _scrollInertia; }
            set { _scrollInertia = value; }
        }

        [Tooltip("Reverse scrolling direction to mimic trackpad scrolling behaviour")]
        [SerializeField]
        private bool reverseScrollingDirection = true;

        #endregion


        #region PointerInput overrides

        public override int id
        {
            get
            {
                if (controller.device != null)
                    return (int)controller.device.index;
                else
                    return base.id;
            }
        }

        public override bool buttonDown { get { return controller.triggerDown; } }
        public override bool buttonPressed { get { return controller.triggerPressed; } }
        public override bool buttonUp { get { return controller.triggerUp; } }

        public override bool isScrolling { get { return enableScroll && (controller.padTouched || controller.padTouchUp || isScrollInertiaInEffect); } }
        public override bool isScrollInertiaInEffect { get; set; }

        public override Vector2 scrollDelta
        {
            get
            {
                var delta = padTouchDelta + padPressDelta;

                // HACKHACK: force horizontal scroll delta to zero
                delta.x = 0;

                return delta;
            }
        }

        #endregion


        #region Fields and properties

        private Controller _controller;
        private Controller controller { get { if (_controller == null) _controller = GetComponent<Controller>(); return _controller; } }

        private SmoothVector2 _padTouchDelta = new SmoothVector2(Vector2.zero, 0.5f);
        private Vector2 _inertiaDelta;
        private Vector2 padTouchDelta
        {
            get
            {
                if (controller.padPressed || controller.padTouchDown)
                {
                    _padTouchDelta.value = Vector2.zero;
                    return Vector2.zero;
                }
                if (controller.padTouched)
                {
                    var delta = controller.padAxisDelta / Time.deltaTime * scrollSpeed * (reverseScrollingDirection ? -1 : 1);

                    isScrollInertiaInEffect = false;
                    _padTouchDelta.Update(delta);

                    return _padTouchDelta.value;
                }
                else if (controller.padTouchUp)
                {
                    isScrollInertiaInEffect = true;
                    _inertiaDelta = _padTouchDelta.value;
                }

                if (isScrollInertiaInEffect)
                {
                    _inertiaDelta -= _inertiaDelta * Time.deltaTime * scrollInertia;

                    if (_inertiaDelta.magnitude < 0.001f)
                    {
                        _inertiaDelta = Vector2.zero;
                        isScrollInertiaInEffect = false;
                    }

                    return _inertiaDelta;
                }

                return Vector2.zero;
            }
        }

        private Vector2 padPressDelta
        {
            get
            {
                if (controller.padPressed)
                {
                    switch (controller.padAxisSector)
                    {
                        case Controller.AxisSector.Top:
                            return Vector2.up;
                        case Controller.AxisSector.Bottom:
                            return Vector2.down;
                        case Controller.AxisSector.Left:
                            return Vector2.left;
                        case Controller.AxisSector.Right:
                            return Vector2.right;
                    }
                }

                return Vector2.zero;
            }
        }

        #endregion


        #region Private methods

        #endregion


        #region MonoBehaviour

        void Start()
        {
            if (controller.device == null)
                enabled = false;
        }

        #endregion
    }
}