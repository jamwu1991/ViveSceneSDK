using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Htc.Viveport.Store
{
    public class PlatformLaserPointerModifier : PlatformControllerModifier
    {
        private const string RenderModelLoaded = "render_model_loaded";
        [SerializeField] private SteamVR_RenderModel _sourceModel;

        [SerializeField] private Vector3 _oculusOffset;
        private Vector3 _currentOffset;

        private Transform _selfTransform;
        private Transform _origin;

        private UnityAction<SteamVR_RenderModel, bool> _cachedOnRenderModelLoaded;
        
        private void Awake()
        {
            _cachedOnRenderModelLoaded = OnRenderModelLoaded;
            _selfTransform = _origin = GetComponent<Transform>();
            
            SteamVR_Events.RenderModelLoaded.Listen(_cachedOnRenderModelLoaded);
        }

        private void OnDestroy()
        {
            SteamVR_Events.RenderModelLoaded.Remove(_cachedOnRenderModelLoaded);
        }

        private void OnRenderModelLoaded(SteamVR_RenderModel renderModel, bool success)
        {
            Assert.IsNotNull(renderModel);
            if(renderModel != _sourceModel)
            {
                Restore();
                return;
            }
            
            if(!success)
            {
                Restore();
                return;
            }

            var tip = _sourceModel.FindComponent("tip");
            if (tip != null)
            {
                Assert.IsTrue(tip.childCount >= 1);

                var attach = tip.GetChild(0);
                Assert.IsNotNull(attach);
                
                _origin = attach;
                _selfTransform.position = _origin.position + _currentOffset;
                _selfTransform.rotation = _origin.rotation;
            }
            else
            {
                Restore();
            }
                
        }

        private void Restore()
        {
            _origin = _selfTransform;
            _selfTransform.localPosition = Vector3.zero;
            _selfTransform.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if(_origin == _selfTransform) return;

            _selfTransform.position = _origin.position + _currentOffset;
            _selfTransform.rotation = _origin.rotation;
        }

        public override void Modify(RecognizedControllerKind kind)
        {
            switch (kind)
            {
                case RecognizedControllerKind.ViveController:
                    _currentOffset = new Vector3();
                    break;
                case RecognizedControllerKind.OculusTouch:
                    _currentOffset = _oculusOffset;
                    break;
                default:
                    goto case RecognizedControllerKind.ViveController;
            }
        }
    }
}