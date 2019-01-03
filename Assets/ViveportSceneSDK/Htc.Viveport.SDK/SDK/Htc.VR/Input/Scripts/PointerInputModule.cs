// ========================================================================== //
//
//  class PointerInputModule
//  -----
//  Purpose: A Unity input module supporting 3D pointer input
//
//  Usage: Include this component as a input module of the event system.
//      Attach Htc.VR.Input.PointerInput to any GameObject to make it
//      a 3D pointer inupt
//
//
//  Created: 2016-10-15
//  Updated: 2016-11-04
//
//  Copyright 2016 HTC America Innovation
// 
// ========================================================================== //
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Htc.Viveport.SDK;

namespace Htc.VR.Input
{
    public class PointerInputModule : BaseInputModule
    {
        #region Types and enums

        private class CanvasMemento
        {
            private Canvas canvas;
            private Camera worldCamera;

            public CanvasMemento(Canvas canvas)
            {
                this.canvas = canvas;
                worldCamera = canvas.worldCamera;
            }

            public void Restore()
            {
                if(canvas != null)
                    canvas.worldCamera = worldCamera;
            }
        }

        #endregion


        #region Unity interface

        [Header("Settings")]

        [SerializeField]
        private LayerMask layerMask = -1;

        [SerializeField]
        private float dragThreshold = 0.1f;

        #endregion


        #region Properties and fields

        private PointerInput[] pointerInputs;

        private List<CanvasMemento> canvasMementos = new List<CanvasMemento>();

        private Camera pointerCamera;

        private RaycastResult nullResult = new RaycastResult();
		
		private HighlightsFX _highlightsFX;

        #endregion

        //for fix lose focus issue at Unity5.5
        private int m_processedFrame;
#if UNITY_5_5_OR_NEWER
        private bool m_hasFocus;

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            m_hasFocus = hasFocus;
        }

        protected virtual void Update()
        {
            // EventSystem is paused when application lost focus, so force ProcessRaycast here
            if (isActiveAndEnabled && !m_hasFocus)
            {
                if (EventSystem.current.currentInputModule == this)
                {
                    ProcessRaycast();
                }
            }
        }
#endif

        public override bool IsModuleSupported()
        {
            return UnityEngine.XR.XRSettings.enabled;
        }

        public override void ActivateModule()
        {
            // Find canvases in the scene and assign our custom UICamera to them
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();

            canvasMementos.Clear();
            foreach (var canvas in canvases)
            {
                if ((1 << canvas.gameObject.layer & layerMask.value) > 0)
                {
                    canvasMementos.Add(new CanvasMemento(canvas));
                    canvas.worldCamera = pointerCamera;
                }
            }
        }

        public override void DeactivateModule()
        {
            foreach (var memento in canvasMementos)
                memento.Restore();
        }

        protected override void Awake()
        {
            base.Awake();

            // Create a new game object to host UI camera
            var camObject = new GameObject("Pointer Input Camera");
            camObject.transform.parent = gameObject.transform;

            // Create a new camera that will be used for raycasts
            pointerCamera = camObject.AddComponent<Camera>();
            pointerCamera.cullingMask = layerMask;
            pointerCamera.nearClipPlane = 0.03f;
            pointerCamera.enabled = false;

            // Create the PhysicsRaycaster
            var physicsRaycaster = camObject.AddComponent<PhysicsRaycaster>();
            physicsRaycaster.eventMask = layerMask;

            // Attach all PointerInput components to this module
            pointerInputs = Resources.FindObjectsOfTypeAll<PointerInput>();
            _highlightsFX = Transform.FindObjectOfType<HighlightsFX>();
        }


        public override void Process()
        {
            if (isActiveAndEnabled)
            {
                ProcessRaycast();
            }
        }

        public void ProcessRaycast()
        {
            if (m_processedFrame == Time.frameCount) { return; }
            m_processedFrame = Time.frameCount;

            foreach (var input in pointerInputs)
            {
                if (!input.isActiveAndEnabled)
                    continue;

                // Get the current event data
                if (input.eventData == null)
                {
                    input.eventData = new PointerEventData(eventSystem);
                    input.eventData.pointerId = input.id;
                    input.eventData.useDragThreshold = true;
                }
                
                var orientation = input.Orientation;

                // Set origin of the raycast
                pointerCamera.transform.position = orientation.position;
                pointerCamera.transform.rotation = orientation.rotation;

                // Setup event data
                input.eventData.delta = Vector2.zero;
                input.eventData.position = new Vector2(pointerCamera.pixelWidth * 0.5f, pointerCamera.pixelHeight * 0.5f);

                if (input.isScrolling)
                    input.eventData.scrollDelta = input.scrollDelta;

                
                eventSystem.RaycastAll(input.eventData, m_RaycastResultCache);
                
                // Save the hit position and normal for reticle
                if (m_RaycastResultCache.Count > 0)
                {
                    // Raycast and update selected object
                    float fDist = 500;

                    for (int i = 0, imax = m_RaycastResultCache.Count; i < imax; ++i)
                    {
                        var resRaycast = m_RaycastResultCache[i];
                        if (!resRaycast.isValid) { continue; }
                        // selected object should be the shortest distance of raycast
                        if (resRaycast.distance < (fDist-0.005))
                        {
                            input.eventData.pointerCurrentRaycast = resRaycast;
                            fDist = (float)(System.Math.Floor(resRaycast.distance * 100) / 100);
                        }
                    }

                    if (input.eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
                    {
                        input.hitPosition = input.eventData.pointerCurrentRaycast.worldPosition;
                        input.hitNormal = input.eventData.pointerCurrentRaycast.worldNormal;
                        input.hitDistance = input.eventData.pointerCurrentRaycast.distance;
                    }
                    else
                    {
                        //var hitPosUpdated = false;

                        // Workaround: look through raycast heirarchy to find valid world position and normal
                        // This workaround is specifically made for CurvedUI, where the pointerCurrentRaycast.distance reported
                        // when hitting CurvedUI's children components is often incorrect
                        // Comment out this next section if you are not using CurvedUI
                        //if (input.eventData.pointerCurrentRaycast.gameObject.GetComponent<CurvedUI.CurvedUIVertexEffect>() != null)
                        //{
                        //    foreach (var raycast in m_RaycastResultCache)
                        //    {
                        //        if (raycast.worldPosition != Vector3.zero)
                        //        {
                        //            input.hitPosition = raycast.worldPosition;
                        //            input.hitNormal = raycast.worldNormal;
                        //            input.hitDistance = raycast.distance;
                        //            hitPosUpdated = true;
                        //            break;
                        //        }
                        //    }
                        //}

                        // If nothing else works, manually calculate the values
                        //if (!hitPosUpdated)
                        //{
                            input.hitPosition = pointerCamera.transform.position + pointerCamera.transform.forward * input.eventData.pointerCurrentRaycast.distance;
                            if(input.eventData.pointerCurrentRaycast.gameObject != null)
                                input.hitNormal = input.eventData.pointerCurrentRaycast.gameObject.transform.forward;
                            input.hitDistance = input.eventData.pointerCurrentRaycast.distance;
                        //}
                    }
                }
                else
                {
                    input.eventData.pointerCurrentRaycast = nullResult;
                }

                // Clear raycast results
                m_RaycastResultCache.Clear();

                // Process pointer events
                ProcessEnterExit(input);
                ProcessPress(input);
                ProcessDrag(input);
                ProcessOthers(input);
            }

            // Unselect any selected objects
            if (eventSystem.currentSelectedGameObject != null)
                eventSystem.SetSelectedGameObject(null);
        }

        void ProcessEnterExit(PointerInput input)
        {
            var eventData = input.eventData;
            var pointerHit = input.eventData.pointerCurrentRaycast.gameObject;
            //we are pointing at a new object (or nothing), so clear the current highlight list
            ClearHighlights();
            //if we are pointing at an object, try adding an object highlight
            if( pointerHit )
            {
                HighlightObject( pointerHit );
            }

            // Handle hover enter / exit when the pointerHit differs from previously hovered object
            if (input.hitObject != pointerHit)
            {
                var oldHovered = eventData.hovered;
                var newHovered = new List<GameObject>();

                // Handle pointer enter
                if (pointerHit != null)
                {
                    // Get the first event handler and set it as pointerEnter
                    var pointerEnterHandler = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(pointerHit);
                    eventData.pointerEnter = pointerEnterHandler;

                    // Loop through object hierarchy to find all hovered objects with a pointer enter handler
                    while (pointerEnterHandler != null)
                    {
                        // Add handler to the new hovered list
                        newHovered.Add(pointerEnterHandler);

                        // Remove the item from the old list so the pointerExit is not called on it
                        // Invoke pointer enter if haven't done so before
                        if (oldHovered.Exists(x => x.Equals(pointerEnterHandler)))
                            oldHovered.Remove(pointerEnterHandler);
                        else
                            ExecuteEvents.Execute(pointerEnterHandler, eventData, ExecuteEvents.pointerEnterHandler);

                        // Break if this is the root object
                        var parent = pointerEnterHandler.transform.parent;
                        if (parent == null)
                            break;

                        pointerEnterHandler = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(parent.gameObject);
                    }
                }

                // Update the hover stack
                eventData.hovered = newHovered;

                // Invoke pointer exit on all old objects
                foreach (var hovered in oldHovered)
                {
                    ExecuteEvents.Execute(hovered, eventData, ExecuteEvents.pointerExitHandler);
                }

                // In the future, selecting object will require passing an availability test
                // to make sure no other pointer is interacting with the object
                input.hitObject = pointerHit;
            }
        }
        private void HighlightObject( GameObject go )
        {
            //only highlight if any object in the hierarchy contains an interactive component
            ViveObjectProps props = go.GetComponentInParent<ViveObjectProps>();
            if( props == null )
            {
                props = go.GetComponentInChildren<ViveObjectProps>();
                if( props == null )
                    return;
            }
            props.HighlightObject();
        }
        private void ClearHighlights()
        {
            if( _highlightsFX != null && _highlightsFX.ObjectRenderers != null )
                _highlightsFX.ObjectRenderers.Clear();
        }

        void ProcessPress(PointerInput input)
        {
            var eventData = input.eventData;

            // Handle pointer down when there is a hovered object
            if (input.buttonDown && input.hitObject != null)
            {
                InvokePointerDown(input);
            }

            // Handle pointer up when there is a pressed object
            if (input.buttonUp)
            {
                if (eventData.pointerPress != null)
                {
                    InvokePointerUp(input);

                    // Invoke click event if the raw pointer down target matches pointer up target (currently selected)
                    if (input.hitObject == eventData.rawPointerPress && !eventData.dragging)
                        ExecuteEvents.ExecuteHierarchy(eventData.rawPointerPress, eventData, ExecuteEvents.pointerClickHandler);
                }

                eventData.rawPointerPress = null;
                eventData.pointerPressRaycast.Clear();
            }

            // Handle when pointer leaves the pressed object
            if (eventData.pointerPress != null && input.hitObject != eventData.rawPointerPress && !eventData.dragging)
            {
                // Invoke pointer up
                // eventData.rawPointerPress is not cleared and is used for pointer return detection
                InvokePointerUp(input);
            }

            // Handles when pointer returns to previously pressed raw object
            if (eventData.pointerPress == null && eventData.rawPointerPress != null && input.hitObject == eventData.rawPointerPress)
            {
                InvokePointerDown(input);
            }
        }

        void ProcessDrag(PointerInput input)
        {
            var eventData = input.eventData;

            // Begin dragging if pointer down on an object that has drag handler
            if (input.buttonDown && input.hitObject != null && eventData.pointerDrag == null)
            {
                // Obtain the drag handler if available
                eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(input.hitObject);
            }

            // Begin drag?
            if (eventData.pointerDrag != null && !eventData.dragging && ShouldBeginDrag(input))
            {
                eventData.dragging = true;
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
            }

            // End drag on pointer up
            if (input.buttonUp && eventData.pointerDrag != null)
            {
                if (eventData.dragging)
                {
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);
                    eventData.dragging = false;
                }

                eventData.pointerDrag = null;
            }

            // Send drag event
            if (eventData.dragging)
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
            }
        }

        void ProcessOthers(PointerInput input)
        {
            // Process scrolling
            if (input.isScrolling && !input.isScrollInertiaInEffect)
            {
                if (input.scrollObject == null)
                {
                    input.scrollObject = ExecuteEvents.ExecuteHierarchy(input.hitObject, input.eventData, ExecuteEvents.scrollHandler);
                    if(input.scrollObject != null)
                        input.scrollRect = input.scrollObject.GetComponent<UnityEngine.UI.ScrollRect>();
                }
                else
                    ExecuteEvents.Execute(input.scrollObject, input.eventData, ExecuteEvents.scrollHandler);
            }
            else if(input.isScrollInertiaInEffect && input.scrollObject != null)
            {
                if(input.scrollRect != null)
                {
                    var vPos = input.scrollRect.verticalNormalizedPosition;
                    if (vPos < 0 || vPos > 1 || Mathf.Approximately(vPos, 0) || Mathf.Approximately(vPos, 1))
                    {
                        input.isScrollInertiaInEffect = false;
                        input.scrollObject = null;
                        return;
                    }
                }

                ExecuteEvents.Execute(input.scrollObject, input.eventData, ExecuteEvents.scrollHandler);
            }
            else
            {
                input.scrollObject = null;
            }
        }

        void InvokePointerDown(PointerInput input)
        {
            var eventData = input.eventData;

            // BE WARE!! this overrides eventData.selectedObject to the handler object
            eventData.pointerPress = ExecuteEvents.ExecuteHierarchy(input.hitObject, eventData, ExecuteEvents.pointerDownHandler);

            if (eventData.pointerPress != null)
            {
                eventData.pressPosition = eventData.position;
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                eventData.rawPointerPress = input.hitObject;
                input.pressedPosition = input.transform.TransformPoint(Vector3.forward * eventData.pointerPressRaycast.distance);
            }
            else
            {
                eventData.rawPointerPress = null;
            }
        }

        void InvokePointerUp(PointerInput input)
        {
            ExecuteEvents.Execute(input.eventData.pointerPress, input.eventData, ExecuteEvents.pointerUpHandler);
            input.eventData.pointerPress = null;
        }

        bool ShouldBeginDrag(PointerInput input)
        {
            if (input.hitObject == null)
                return false;

            if (!input.eventData.useDragThreshold)
                return true;

            var curDist = input.eventData.pointerCurrentRaycast.distance;
            var curPos = input.transform.TransformPoint(Vector3.forward * curDist);

            var dist = (curPos - input.pressedPosition).magnitude / curDist;

            return dist > dragThreshold;
        }
    }
}