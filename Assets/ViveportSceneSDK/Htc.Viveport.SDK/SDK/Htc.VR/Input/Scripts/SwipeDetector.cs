// ========================================================================== //
//
//  class SwipeDetector
//  -----
//  Summary: Detects controller trackpad swipe actions
//
//  Description: This component tracks the touch movement on the attached
//      controller reference and sets the Swiped property to true on the frame
//      the swipe action is detected. When Swiped is true, SwipeQuadrant
//      may be called to obtain the direction of the swipe. A swipe action is
//      determined at TouchUp event when a minimal movement speed is achieved.
//
//  Updated: 2017-07-12
//  Updated by: Thomas Key (thomas_key@htc.com)
//  Updated due to migration to newer input framework.
// 
// ========================================================================== //

using UnityEngine;
using System.Collections;

namespace Htc.VR.Input
{
    public class SwipeDetector : MonoBehaviour
    {
        public enum Quadrant
        {
            Unknown,
            Top,
            Bottom,
            Left,
            Right
        }

        // Reference to the controller in question
        [SerializeField] private Controller _controller;


        // Public properties
        public bool Touched { get; private set; }                                   // Returns whether the trackpad is being touched
        public bool Swiped { get; private set; }                                    // Returns whether a swipe action occured
        public Vector2 TouchPosition { get { return currPos; } }                    // The touched position in absolute coordinates
        public Vector2 RelativeTouchPosition { get { return currPos - initPos; } }  // The touched position relative to the initial touched position
        public Vector2 TouchVelocity { get { return velocity; } }                   // Current velocity of the touch
        
        // Returns which quadrant the swipe is toward
        public Quadrant SwipeQuadrant
        {
            get
            {
                if (Swiped)
                    return GetQuadrant(velocity.normalized);
                else
                    return Quadrant.Unknown;
            }
        }

        // Internal variables
        private Vector2 initPos;
        private Vector2 currPos;
        private Vector2 velocity;

        // Exponential smoothing factor for velocity, (0, 1)
        // The larger the value, the sooner older values are "forgotten"
        const float ExpSmoothFactor = 0.1f;

        // Minimal end velocity for a movement to be qualified as a swipe
        const float MinSwipeVelocity = 2f;

        void Update()
        {
            // Reset swipe status
            Swiped = false;

            // Bypass if no active controller is attached
            if (_controller == null || !_controller.gameObject.activeInHierarchy)
                return;

            // Interrupt the touch if the trackpad button is pressed during the process
            if (_controller.padDown)
            {
                if (Touched)
                    OnTouchInterrupted();

                return;
            }

            // Handle touchpad movement
            if (_controller.padTouchDown)
                OnTouchDown();
            else if (_controller.padTouched)
                OnTouchMove();
            else if (_controller.padTouchUp)
                OnTouchUp();
        }

        void OnTouchDown()
        {
            // Set initial touch position
            initPos = _controller.padAxis;
            currPos = initPos;
            velocity = Vector2.zero;

            Touched = true;
        }

        void OnTouchMove()
        {
            if (!Touched)
            {
                OnTouchDown();
                return;
            }

            // Update position and instantaneous velocity
            var prvsPos = currPos;
            currPos = _controller.padAxis;
            var vel = (currPos - prvsPos) / Time.deltaTime;

            // Apply exponential smoothing to velocity
            velocity = velocity * (1 - ExpSmoothFactor) + vel * ExpSmoothFactor;
        }

        void OnTouchUp()
        {
            // Test if thie movement qualify as a swipe
            if (velocity.magnitude > MinSwipeVelocity)
                Swiped = true;

            Touched = false;
        }

        void OnTouchInterrupted()
        {
            Touched = false;
        }

        // Determines what quadrant (top, bottom, left, right) the input vector lies in
        public static Quadrant GetQuadrant(Vector2 vec)
        {
            // First make sure this is a vector with a valid direction
            var minval = 0.001f;
            if (Mathf.Abs(vec.x) < minval && Mathf.Abs(vec.y) < minval)
                return Quadrant.Unknown;

            // Calculate the angle and offset it to [0, 2*pi)
            var angle = Mathf.Atan2(vec.y, vec.x) - Mathf.PI / 4;
            if (angle < 0)
                angle += Mathf.PI * 2;

            // Determine quadrant
            if (angle < Mathf.PI / 2)
                return Quadrant.Top;
            else if (angle < Mathf.PI)
                return Quadrant.Left;
            else if (angle < Mathf.PI * 3 / 2)
                return Quadrant.Bottom;
            else if (angle < Mathf.PI * 2)
                return Quadrant.Right;

            return Quadrant.Unknown;
        }

        // Legacy function support
        // Consider using Swiped and SwipeQuadrant properties instead
        public Vector2 GetSwipeDirection()
        {
            switch (SwipeQuadrant)
            {
                case Quadrant.Top:
                    return Vector2.up;
                case Quadrant.Bottom:
                    return Vector2.down;
                case Quadrant.Left:
                    return Vector2.left;
                case Quadrant.Right:
                    return Vector2.right;
            }

            return Vector2.zero;
        }

        // Legacy function
        // Consider using RelativeTouchPosition.magnitude instead
        public float GetSwipeDistance()
        {
            return RelativeTouchPosition.magnitude;
        }
    }
}
