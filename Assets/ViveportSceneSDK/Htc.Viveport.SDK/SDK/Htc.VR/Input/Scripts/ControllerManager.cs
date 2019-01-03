// ========================================================================== //
//
//  class ControllerManager
//  -----
//  Purpose: Manages the Controllers in the scene 
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
    [RequireComponent(typeof(SteamVR_ControllerManager))]
    public class ControllerManager : MonoBehaviour
    {
        #region Types and enums

        enum ControllerSide
        {
            Left,
            Right
        }

        public delegate void ControllerRoleChangedEvent();

        #endregion


        #region Unity interface

        [SerializeField]
        private static ControllerSide primaryControllerSide = ControllerSide.Right;

        #endregion


        #region Properties and fields

        // Required Component getters
        private SteamVR_ControllerManager _steamvrControllerManager;
        private SteamVR_ControllerManager steamvrControllerManager { get { if (_steamvrControllerManager == null) _steamvrControllerManager = GetComponent<SteamVR_ControllerManager>(); return _steamvrControllerManager; } }

        public static Controller primaryController { get { return (primaryControllerSide == ControllerSide.Left) ? leftController : rightController; } }
        public static Controller secondaryController { get { return (primaryControllerSide == ControllerSide.Left) ? rightController : leftController; } }

        public static Controller leftController { get; private set; }
        public static Controller rightController { get; private set; }

        #endregion


        #region Public methods


        #endregion


        #region Private methods

        private Controller SetupController(GameObject obj)
        {
            if (obj == null)
                return null;

            var controller = obj.GetComponent<Controller>();
            if(controller == null)
                controller = obj.AddComponent<Controller>();

            return controller;
        }

        #endregion


        #region MonoBehaviour

        void Awake()
        {
            // Connect non-required external reference
            leftController = SetupController(steamvrControllerManager.left);
            rightController = SetupController(steamvrControllerManager.right);

            // Set primary controller
            PointerInput.current = primaryController.pointerInput;
        }

        #endregion
    }
}