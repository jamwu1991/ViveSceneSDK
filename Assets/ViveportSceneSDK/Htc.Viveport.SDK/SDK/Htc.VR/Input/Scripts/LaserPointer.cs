// ========================================================================== //
//
//  class LaserPointer
//  -----
//  Purpose: Manages the rendering of laser and reticle based on the info
//      from the specified PointerInput
//
//
//  Created: 2016-11-01
//  Updated: 2016-10-04
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;

namespace Htc.VR.Input
{
    public class LaserPointer : MonoBehaviour
    {
        #region Unity interface

        [Header("Source Pointer Input")]

        [SerializeField]
        private PointerInput pointerInput;

        [Header("Components")]

        [SerializeField]
        private LineRenderer laser;

        [SerializeField]
        private SpriteRenderer reticle;

        [Header("Laser Settings")]

        [SerializeField]
        private float width = 0.01f;

        [SerializeField]
        private float length = 30f;

        [SerializeField]
        private Color normalColor = Color.white;

        //[SerializeField]
        //private Color highlightColor = Color.white;

        [SerializeField]
        private Color pressedColor = Color.white;

        [SerializeField]
        private Color inactiveColor = Color.white;

        [SerializeField]
        [Range(0, 1)]
        private float alphaFading = 0f;

        [Header("Reticle Settings")]

        [Tooltip("The size of the reticle at 1 meter away")]
        [SerializeField]
        private float reticleSize = 0.01f;

        [SerializeField]
        [Range(0, 1)]
        private float reticleScaling = 1f;

        #endregion


        #region Private methods

        private void SetLaserColor(Color color)
        {
            var endColor = color;
            endColor.a *= 1 - alphaFading;
            laser.startColor = color;
            laser.endColor = endColor;
        }

        #endregion


        #region MonoBehaviour

        void Start()
        {
            if (pointerInput == null)
                enabled = false;

            OnValidate();
        }

        void Update()
        {
            if(!pointerInput.enabled)
            {
                if (laser != null)
                    laser.enabled = false;

                if (reticle != null)
                    reticle.enabled = false;
            }

            // Update laser pointer color
            if (pointerInput.hitObject != null)
            {
                // Select color
                Color color;
                if (pointerInput.buttonPressed)
                    color = pressedColor;
                else
                    color = normalColor;

                // Update laser
                if (laser != null)
                {
                    laser.enabled = true;
                    laser.SetPosition(1, new Vector3(0, 0, pointerInput.hitDistance));
                    SetLaserColor(color);
                }

                // Update reticle
                if (reticle != null)
                {
                    reticle.enabled = true;

                    var scale = reticleSize * (1 + (pointerInput.hitDistance - 1) * reticleScaling);

                    reticle.transform.position = pointerInput.hitPosition;
                    reticle.transform.rotation = Quaternion.FromToRotation(Vector3.forward, pointerInput.hitNormal);
                    reticle.transform.localScale = new Vector3(scale, scale, scale);

                    reticle.color = color;

                    reticle.gameObject.SetActive(true);
                }
            }
            else
            {
                // Inactive

                // Update laser
                if (laser != null)
                {
                    laser.SetPosition(1, new Vector3(0, 0, length));
                    SetLaserColor(inactiveColor);
                }

                // Update reticle
                if (reticle != null)
                {
                    reticle.gameObject.SetActive(false);
                }
            }
        }

        void OnValidate()
        {
            if (laser != null)
            {
                laser.useWorldSpace = false;
                laser.startWidth = width;
                laser.endWidth = width;
                laser.SetPosition(0, Vector3.zero);
                laser.SetPosition(1, new Vector3(0, 0, length));
                SetLaserColor(normalColor);
            }
        }

        #endregion
    }
}