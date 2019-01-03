using System;
using UnityEngine;
using System.Collections.Generic;

namespace Htc.Viveport.SDK
{
    [RequireComponent(typeof(Collider))]
    public class ViveObjectProps : MonoBehaviour
    {
        public Collider objectCollider;
        public Rigidbody body;

        public bool pickUpItem;
        public bool isBeingPickedUp;
        
        public AudioClipProvider touchAudioProvider;
        public AnimationClip touchAnimation;
        
        public AudioClipProvider clickAudioProvider;
        public AnimationClip clickAnimation;
        
        public AudioClipProvider enterAudioProvider;
        public AnimationClip enterAnimation;
        
        public AudioClipProvider impactAudioProvider;
        public AnimationClip impactAnim;

        public AudioClipProvider objectAudioProvider;
        public AnimationClip objectAnim;
        public ViveObjectProps objectObject;
        public bool objectIsConsumed = false;

        private HighlightsFX _highlightsFX;

        public bool HasTouchProps
        {
            get { return touchAudioProvider != null || touchAnimation != null; }
        }

        public bool HasClickProps
        {
            get { return clickAudioProvider != null || clickAnimation != null; }
        }

        public bool HasEnterProps
        {
            get { return enterAudioProvider != null || enterAnimation != null; }
        
		}
		
		public bool HasImpactProps
		{
			get { return impactAudioProvider != null || impactAnim != null; }
		}
		
        public bool HasObjectProps
        {
            get { return objectAudioProvider != null || objectAnim != null || objectObject != null; }
        }

        public bool HasAnyDataProps
        {
			get { return HasTouchProps || HasClickProps || HasEnterProps || HasImpactProps || HasObjectProps; } 
        }
        
        public void HighlightObject()
        {
            //only highlight if the user can interact with the controller
            if( !pickUpItem && !HasTouchProps && !HasClickProps && !HasEnterProps )
                return;

            //don't highlight if object is being held
            if( pickUpItem && isBeingPickedUp )
                return;

            //get all renderers under the interactive object and add them to the highlighter list
            List<Renderer> renderers = new List<Renderer>();
            gameObject.GetComponentsInChildren<Renderer>( false, renderers );
            if( renderers != null && renderers.Count > 0 )
            {
                foreach( Renderer rend in renderers )
                {
                    if
                    (
                        rend.GetType() != typeof(ParticleSystemRenderer) &&
                        rend.gameObject.activeInHierarchy &&
                        rend.enabled &&
                        _highlightsFX != null &&
                        _highlightsFX.ObjectRenderers != null &&
                        !_highlightsFX.ObjectRenderers.Contains( rend )
                    )
                    {
                        _highlightsFX.ObjectRenderers.Add( rend );
                    }
                }
            }
        }

        private void Cache()
        {
            if(body == null)
                body = GetComponent<Rigidbody>();

            if(objectCollider == null)
                objectCollider = GetComponent<Collider>();

            if(_highlightsFX == null)
                _highlightsFX = Transform.FindObjectOfType<HighlightsFX>();
        }

        private void Awake()
        {
            Cache();
        }

        /// Note: Even though I'm searching for the collider and rigid body, no properties are required.
        private void Reset()
        {
            Cache();
        }

        private void OnValidate()
        {
            Cache();
        }
    }
}


