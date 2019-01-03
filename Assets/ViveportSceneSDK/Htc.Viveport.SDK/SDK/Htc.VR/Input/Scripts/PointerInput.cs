// ========================================================================== //
//
//  class PointerInput
//  -----
//  Purpose: The base class of all pointer inputs
//
//  Usage: Attach to any GameObject to make it a 3D pointer input
//
//  Notes: Override this class to create specific 3D pointer inputs
//
//
//  Created: 2016-10-15
//  Updated: 2016-10-15
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Htc.VR.Input
{
    public class PointerInput : MonoBehaviour
    {
        #region Types and enums

        public enum ScrollDir
        {
            None,
            Horizontal,
            Vertical,
            Both
        }

        #endregion
        
        [SerializeField] private Transform _orientation;

        private void Awake()
        {
            if (_orientation == null)
                _orientation = GetComponent<Transform>();
        }

        // Used by PointerInputModule
        public PointerEventData eventData { get; set; }
        public GameObject hitObject { get; set; }
        public float hitDistance { get; set; }
        public Vector3 hitPosition { get; set; }
        public Vector3 hitNormal { get; set; }
        public Vector3 pressedPosition { get; set; }
        public GameObject scrollObject { get; set; }
        public ScrollRect scrollRect { get; set; }
        public ScrollDir scrollDir { get; set; }
        
        public Transform Orientation
        {
            get { return _orientation; }
        }

        public virtual int id { get { return -1; } }

        // Overried these functions to change the button behavior
        public virtual bool buttonDown { get { return false; } }
        public virtual bool buttonPressed { get { return false; } }
        public virtual bool buttonUp { get { return false; } }
        public virtual bool buttonClicked { get { return false; } }

        // Scrolling
        public virtual bool isScrolling { get { return false; } }
        public virtual bool isScrollInertiaInEffect { get { return false; } set { } }
        public virtual Vector2 scrollDelta { get { return Vector2.zero; } }

        // Static functions
        public static PointerInput current { get; set; }

        public static bool GetButtonDown()
        {
            if (current != null)
                return current.buttonDown;
            else
                return UnityEngine.Input.GetMouseButtonDown(0);
        }

        public static bool GetButtonPressed()
        {
            if (current != null)
                return current.buttonPressed;
            else
                return UnityEngine.Input.GetMouseButton(0);
        }

        public static bool GetButtonUp()
        {
            if (current != null)
                return current.buttonUp;
            else
                return UnityEngine.Input.GetMouseButtonUp(0);
        }
    }
}