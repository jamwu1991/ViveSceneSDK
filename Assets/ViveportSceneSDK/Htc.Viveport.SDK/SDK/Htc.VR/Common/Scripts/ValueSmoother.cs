// ========================================================================== //
//
//  Smooth Values
//  -----
//  Purpose: Set of classes that smooth values of basic types
//      with exponential smoothing
//
//  Usage:
//      Call Update() every frame and supply the target value
//      Use the value property to get/set the current value
//
//
//  Created: 2016-11-29
//  Updated: 2016-11-29
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;

namespace Htc.VR
{
    public class SmoothFloat
    {
        public float smoothing;

        public float value { get; set; }

        public SmoothFloat(float value, float smoothing = 0.9f)
        {
            this.value = value;
            this.smoothing = smoothing;
        }

        public void Update(float value)
        {
            this.value = this.value * smoothing + value * (1 - smoothing);
        }
    }

    public class SmoothVector2
    {
        public float smoothing;

        public Vector2 value { get; set; }

        public SmoothVector2(Vector2 value, float smoothing = 0.9f)
        {
            this.value = value;
            this.smoothing = smoothing;
        }

        public void Update(Vector2 value)
        {
            this.value = this.value * smoothing + value * (1 - smoothing);
        }
    }

    public class SmoothVector3
    {
        public float smoothing;

        public Vector3 value { get; set; }

        public SmoothVector3(Vector3 value, float smoothing = 0.9f)
        {
            this.value = value;
            this.smoothing = smoothing;
        }

        public void Update(Vector3 value)
        {
            this.value = this.value * smoothing + value * (1 - smoothing);
        }
    }
}