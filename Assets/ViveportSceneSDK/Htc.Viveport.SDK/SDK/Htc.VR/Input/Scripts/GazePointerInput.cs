// ========================================================================== //
//
//  class GazePointerInput
//  -----
//  Purpose: Enables gaze feature for a camera
//
//  Usage: Attach to the main camera. Set the key variable to bind a key
//      from the keyboard as the primary button
//
//
//  Created: 2016-10-15
//  Updated: 2016-10-15
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;

namespace Htc.VR.Input
{
    [DisallowMultipleComponent]
    public class GazePointerInput : PointerInput
    {
        #region Unity interface

        [SerializeField]
        private string key = "space";

        #endregion


        #region PointerInput overrides

        public override bool buttonDown { get { return UnityEngine.Input.GetKeyDown(key); } }
        public override bool buttonUp { get { return UnityEngine.Input.GetKeyUp(key); } }
        public override bool buttonClicked { get { return UnityEngine.Input.GetKeyUp(key); } }

        #endregion
    }
}