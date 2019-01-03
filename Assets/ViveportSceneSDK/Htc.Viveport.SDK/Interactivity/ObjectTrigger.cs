using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace Htc.Viveport.SDK
{
    [DisallowMultipleComponent]
    public class ObjectTrigger : MonoBehaviour
    {
        private TriggerData _touchData;
        private TriggerData _clickData;
        private TriggerData _hoverData;
		private TriggerData _impactData;
        private TriggerData _objectData;        

        private PlayObjectAnimations _playObject;
        private AudioSource _audioSource;
        private ViveObjectProps _objectProps;

        #region Methods

        #region Set Data Functions

        public void SetTouchData(AnimationClip anim, AudioClipProvider audio)
        {
            SetData(out _touchData, anim, audio);
        }

        public void SetClickData(AnimationClip anim, AudioClipProvider audio)
        {
            SetData(out _clickData, anim, audio);
        }

        public void SetHoverData(AnimationClip anim, AudioClipProvider audio)
        {
            SetData(out _hoverData, anim, audio);
        }
		
		public void SetImpactData(AnimationClip anim, AudioClipProvider audio)
		{
			SetData (out _impactData, anim, audio);
		}

		public void SetObjectData(AnimationClip anim, AudioClipProvider audio, ViveObjectProps objectProps, bool objectIsConsumed)
        {
            SetData( out _objectData, anim, audio, objectProps, objectIsConsumed );
        }

        private void SetData(out TriggerData data, AnimationClip anim, AudioClipProvider audio, ViveObjectProps objectProps = null, bool objectIsConsumed = false)
        {
            data = new TriggerData(anim, audio, objectProps, objectIsConsumed);
            if (anim != null)
            {
                this.SafeGetComponent(ref _playObject);
                
                data.Setup(_playObject);
            }

            if (audio != null)
            {
                this.SafeGetComponent(ref _audioSource);
            }

            if(objectProps != null)
            {
                this.SafeGetComponent( ref _objectProps );
            }
        }
        

        #endregion

        #region Trigger Functions

        public void Click(BaseEventData bed)
        {
            Play(ref _clickData);
        }

        public void Hover(BaseEventData bed)
        {
            Play(ref _hoverData);
        }

        private void OnTriggerEnter( Collider other )
        {
            TriggerHandler( other );
        }

        public void TriggerHandler(Collider other)
        { 
            if (!GameController.Is(other.gameObject)) return;

            Play(ref _touchData);
        }

        private void Play(ref TriggerData data)
        {
            if( data != null )
                data.Play(_playObject, _audioSource);
        }

		
		private void OnCollisionEnter(Collision col)
		{
            CollisionHandler( col );
		}
		
        public void CollisionHandler( Collision col )
        {
            if( GameController.Is( col.gameObject ) ) return;

            if( _impactData != null )
            {
                //Debug.Log( "velocity: " + col.relativeVelocity.magnitude );
                //Debug.Log( "impulse: " + col.impulse.magnitude );

                //var relativeVelocity = col.relativeVelocity.sqrMagnitude;
                var relativeVelocity = col.impulse.magnitude; //col.relativeVelocity was not working for objects that were held by the user, so switched to impulse - Corey Paganucci

                var velocityProvider = _impactData.Audio as AudioClipImpactVelocityProvider;
                if( velocityProvider != null )
                {
                    velocityProvider.ContactVelocity = relativeVelocity;
                }

                Play( ref _impactData );
            }
            else if( _objectData != null && col.gameObject == _objectData.ObjectProps.gameObject )
            {
                if( _objectData.ObjectIsConsumed )
                    Destroy( _objectData.ObjectProps.gameObject );

                Play( ref _objectData );
            }
        }


        #endregion

        #endregion

        #region Helper Types

        [Serializable]
        private class TriggerData
        {
            public AnimationClip Anim;
            public AudioClipProvider Audio;
            public ViveObjectProps ObjectProps;
            public bool ObjectIsConsumed;

            public TriggerData(AnimationClip anim, AudioClipProvider audio, ViveObjectProps objectProps = null, bool objectIsConsumed = false)
            {
                Anim = anim;
                Audio = audio;
                ObjectProps = objectProps;
                ObjectIsConsumed = objectIsConsumed;
            }

            public void Play(PlayObjectAnimations playObject, AudioSource source)
            {
                if (Anim == null && Audio == null) return;

                Play(playObject);
                Play(source);
            }

            private void Play(AudioSource source)
            {
                if (Audio == null || Audio.Count <= 0) return;

                var next = Audio.Next;
                var clip = next.Clip;
                var pitch = UnityEngine.Random.Range(next.MinPitch, next.MaxPitch);
                source.clip = clip;
                source.pitch = pitch;
                source.Play();
            }

            private void Play(PlayObjectAnimations playObject)
            {
                if(Anim == null) return;
                
                playObject.Play(Anim);
            }

            public void Setup(PlayObjectAnimations playObject)
            {
                if (Anim == null) return;

                playObject.AddClip(Anim);
            }
        }

        #endregion
    }
}

