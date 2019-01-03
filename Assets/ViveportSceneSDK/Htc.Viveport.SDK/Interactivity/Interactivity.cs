

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Htc.Viveport.Store;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Htc.Viveport.SDK
{
    public static class Interactivity
    {
        public interface ITeleportProvider
        {
            ViveNavMesh NavMesh { get; }
            Animator NavMeshAnimator { get; }
            TeleportVive Teleporter { get; }
            ParabolicPointer Pointer { get; }
            Transform CameraRig { get; }

            Vector3 OriginPosition { get; }
            Quaternion OriginRotation { get; }
        }
        
        public struct Parameters
        {
            public readonly Scene Scene;
            public readonly ITeleportProvider TeleportProvider;
            public readonly Func<AudioMixerGroupBindings, AudioMixerGroup> GetAudioGroup;

            public Parameters(Scene scene, ITeleportProvider teleport, Func<AudioMixerGroupBindings, AudioMixerGroup> getAudioGroup)
            {
                Scene = scene;
                TeleportProvider = teleport;
                GetAudioGroup = getAudioGroup;
            }
        }

        public static void Setup(Parameters parameters)
        {
            var scene = parameters.Scene;
            var rootObjs = scene.GetRootGameObjects();

            SetupProps(rootObjs);
            SetupTeleport(parameters.TeleportProvider, rootObjs);
            SetupAudioContexts(parameters.GetAudioGroup, rootObjs);
        }

        private static void SetupAudioContexts(Func<AudioMixerGroupBindings, AudioMixerGroup> getMixerGroup, GameObject[] rootObjs)
        {
            // get top-level PreviewAudioContexts, anwhere they may be in the heirarchy
            var topAudioContexts = rootObjs
                .SelectMany(go => go.GetComponentsInChildren<PreviewAudioContext>(true))
                .Where(c => !c.GetComponentsInParent<PreviewAudioContext>(true).Except(new[] { c }).Any())
                .ToArray();

            for (var idx = 0; idx < topAudioContexts.Length; ++idx)
            {
                topAudioContexts[idx].Bind(getMixerGroup);
            }
        }

        private static void SetupProps(GameObject[] rootObjs)
        {
            var propsList = rootObjs
                .SelectMany(g => g
                .GetComponentsInChildren<ViveObjectProps>(true))
                .Where(vop => vop != null)
                .ToArray();

            foreach (var prop in propsList)
            {
                if (prop.objectCollider == null) return;

                var go = prop.gameObject;

                if( prop.pickUpItem )
                {
                    go.AddComponent<Throwable>();
                }

                if (!prop.HasAnyDataProps) continue;

                var objectTrigger = go.AddComponent<ObjectTrigger>();

                bool hasLocalCollider = go.GetComponent<Collider>() == prop.objectCollider;
                if(!hasLocalCollider)
                {
                    CollisionForwarder cf = prop.objectCollider.gameObject.AddComponent<CollisionForwarder>();
                    cf.Trigger = objectTrigger;
                }

                TryAddTouch(objectTrigger, prop);

                var hasEventProps = prop.HasClickProps || prop.HasEnterProps || prop.HasImpactProps || prop.HasObjectProps;
                if (!hasEventProps) continue;

                TryAddClick(objectTrigger, prop);
                TryAddHover(objectTrigger, prop);
				TryAddImpact(objectTrigger, prop);
                TryAddObject( objectTrigger, prop );               
            }
        }

        private static void SetupTeleport(ITeleportProvider provider, GameObject[] rootObjs)
        {
            var viveNavMesh = rootObjs.SelectMany(g => g.GetComponentsInChildren<ViveNavMesh>(true)).FirstOrDefault(vnm => vnm != null);

            Assert.IsNotNull(provider);

            var teleportVive = provider.Teleporter;
            var cameraRig = provider.CameraRig;
            var parabolicPointer = provider.Pointer;
            var navMesh = provider.NavMesh;
            var navMeshAnimator = provider.NavMeshAnimator;

            teleportVive.CancelTeleport();

            cameraRig.position = provider.OriginPosition;
            cameraRig.rotation = provider.OriginRotation;

            if (viveNavMesh != null)
            {
                parabolicPointer.NavMesh = viveNavMesh;
                teleportVive.NavAnimator = viveNavMesh.GetComponent<Animator>();
                navMesh.gameObject.SetActive(false);
            }
            else
            {
                teleportVive.CancelTeleport();
                parabolicPointer.NavMesh = navMesh;
                teleportVive.NavAnimator = navMeshAnimator;
                navMesh.gameObject.SetActive(true);
            }
        }

        public static void ResetTeleport(ITeleportProvider provider)
        {
            var teleportVive = provider.Teleporter;
            var cameraRig = provider.CameraRig;
            var parabolicPointer = provider.Pointer;
            var navMesh = provider.NavMesh;
            var navMeshAnimator = provider.NavMeshAnimator;

            teleportVive.CancelTeleport();
            navMesh.gameObject.SetActive(true);
            parabolicPointer.NavMesh = navMesh;
            teleportVive.NavAnimator = navMeshAnimator;

            cameraRig.position = provider.OriginPosition;
            cameraRig.rotation = provider.OriginRotation;
        }

        public static void InterruptTeleport(ITeleportProvider provider)
        {
            var teleportVive = provider.Teleporter;
            var cameraRig = provider.CameraRig;
            var parabolicPointer = provider.Pointer;
            var navMesh = provider.NavMesh;
            var navMeshAnimator = provider.NavMeshAnimator;
            
            teleportVive.CancelTeleport();
            navMesh.gameObject.SetActive(true);
            parabolicPointer.NavMesh = navMesh;
            teleportVive.NavAnimator = navMeshAnimator;
            teleportVive.ManualTeleport(provider.OriginPosition);
            cameraRig.rotation = provider.OriginRotation;
        }

        private static void TryAddTouch(ObjectTrigger objectTrigger, ViveObjectProps prop)
        {
            if (prop.touchAudioProvider == null && prop.touchAnimation == null) return;
            
            objectTrigger.SetTouchData(prop.touchAnimation, prop.touchAudioProvider);
        }

        private static void TryAddClick(ObjectTrigger objectTrigger, ViveObjectProps prop)
        {
            TryAddEventTrigger(
                objectTrigger, 
                prop, 
                EventTriggerType.PointerClick, 
                v => v.clickAnimation, 
                v => v.clickAudioProvider,
                ot => ot.Click,
                (ot,anim,audio) => ot.SetClickData(anim, audio));
        }

		private static void TryAddImpact(ObjectTrigger objectTrigger, ViveObjectProps prop)
		{
			if (prop.impactAudioProvider == null && prop.impactAnim == null) return;

			objectTrigger.SetImpactData(prop.impactAnim, prop.impactAudioProvider);
		}

        private static void TryAddObject( ObjectTrigger objectTrigger, ViveObjectProps prop )
        {
            if( prop.objectAudioProvider == null && prop.objectAnim == null ) return;

            objectTrigger.SetObjectData( prop.objectAnim, prop.objectAudioProvider, prop.objectObject, prop.objectIsConsumed );
        }

        private static void TryAddHover(ObjectTrigger objectTrigger, ViveObjectProps prop)
        {
            TryAddEventTrigger(
                objectTrigger, 
                prop, 
                EventTriggerType.PointerEnter, 
                v => v.enterAnimation, 
                v => v.enterAudioProvider,
                ot => ot.Hover,
                (ot, anim, audio) => ot.SetHoverData(anim, audio));
        }

        private static void TryAddEventTrigger(ObjectTrigger objectTrigger, ViveObjectProps prop, EventTriggerType triggerType, 
            Func<ViveObjectProps, AnimationClip> getAnim, Func<ViveObjectProps, AudioClipProvider> getAudio,
            Func<ObjectTrigger, UnityAction<BaseEventData>> getCallback, Action<ObjectTrigger, AnimationClip, AudioClipProvider> set)
        {
            Assert.IsNotNull(objectTrigger);
            Assert.IsNotNull(prop);
            Assert.IsNotNull(getAnim);
            Assert.IsNotNull(getAudio);
            Assert.IsNotNull(getCallback);
            Assert.IsNotNull(set);

            var anim = getAnim(prop);
            var audio = getAudio(prop);

            if(anim == null && audio == null) return;

            var go = objectTrigger.gameObject;

            //if collider is on a different object than the vive object props game object, add the event trigger to the object that has the collider
            bool hasLocalCollider = go.GetComponent<Collider>() == prop.objectCollider;
            if( !hasLocalCollider )
            {
                go = prop.objectCollider.gameObject;
            }

            var eventTrigger = go.SafeGetComponent<EventTrigger>();

            var entry = new EventTrigger.Entry {eventID = triggerType};
            entry.callback.AddListener(getCallback(objectTrigger));
            eventTrigger.triggers.Add(entry);

            set(objectTrigger, anim, audio);
        }

        public static T SafeGetComponent<T>(this GameObject go)
            where T : Component
        {
            var comp = go.GetComponent<T>();

            if (comp == null)
                comp = go.AddComponent<T>();

            return comp;
        }

        public static void SafeGetComponent<T>(this MonoBehaviour behaviour, ref T cachedRef)
            where T : Component
        {
            if (cachedRef != null) return;

            cachedRef = behaviour.GetComponent<T>();
            if (cachedRef != null) return;

            cachedRef = behaviour.gameObject.AddComponent<T>();
        }
    }
}
