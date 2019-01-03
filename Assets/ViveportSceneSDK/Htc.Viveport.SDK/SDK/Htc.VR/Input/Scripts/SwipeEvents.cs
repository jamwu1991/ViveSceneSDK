// ========================================================================== //
//
//  class SwipeEvents
//  -----
//  Purpose: Emits Events based on the results from a SwipeDetector
//
//
//  Created: 2017-07-12
//
//  Copyright 2017 Thomas Key
// 
// ========================================================================== //

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Htc.VR.Input
{
    public class SwipeEvents : MonoBehaviour
    {
        #region Editable Variables

        [SerializeField] private SwipeDetector _swipeDetector;

        [SerializeField] private UnityEvent _swipeUpEvent = new UnityEvent();
        [SerializeField] private UnityEvent _swipeDownEvent = new UnityEvent();
        [SerializeField] private UnityEvent _swipeLeftEvent = new UnityEvent();
        [SerializeField] private UnityEvent _swipeRightEvent = new UnityEvent();

        #endregion

        #region Unity API

        private void Update()
        {
            if (!_swipeDetector.Swiped) return;

            var quadrant = _swipeDetector.SwipeQuadrant;
            switch (quadrant)
            {
                case SwipeDetector.Quadrant.Unknown:
                    break;
                case SwipeDetector.Quadrant.Top:
                    _swipeUpEvent.Invoke();
                    break;
                case SwipeDetector.Quadrant.Bottom:
                    _swipeDownEvent.Invoke();
                    break;
                case SwipeDetector.Quadrant.Left:
                    _swipeLeftEvent.Invoke();
                    break;
                case SwipeDetector.Quadrant.Right:
                    _swipeRightEvent.Invoke();
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unknown quadrant: {0}", quadrant));
            }
        }

        #endregion

        #region Runtime Listener Functions

        public void AddListener(SwipeDetector.Quadrant quadrant, UnityAction listener)
        {
            ChangeAction(quadrant, (evt) => evt.AddListener(listener));
        }

        public void RemoveListener(SwipeDetector.Quadrant quadrant, UnityAction listener)
        {
            ChangeAction(quadrant, evt => evt.RemoveListener(listener));
        }

        private void ChangeAction(SwipeDetector.Quadrant quadrant, Action<UnityEvent> change)
        {
            UnityEvent evt = null;
            switch (quadrant)
            {
                case SwipeDetector.Quadrant.Unknown:
                    break;
                case SwipeDetector.Quadrant.Top:
                    evt = _swipeUpEvent;
                    break;
                case SwipeDetector.Quadrant.Bottom:
                    evt = _swipeDownEvent;
                    break;
                case SwipeDetector.Quadrant.Left:
                    evt = _swipeLeftEvent;
                    break;
                case SwipeDetector.Quadrant.Right:
                    evt = _swipeRightEvent;
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unknown quadrant: {0}", quadrant));
            }

            if (evt != null)
                change(evt);
        }

        #endregion
    }
}
