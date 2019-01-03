using UnityEngine;

namespace Htc.Viveport.Store
{
    [RequireComponent(typeof(TeleportInputVpNext))]
    public class TeleportInputVpNextModifier : PlatformControllerModifier
    {
        [SerializeField] private TeleportInputVpNext _teleportInput;

        private void Awake()
        {
            Cache();
        }
        
        public override void Modify(RecognizedControllerKind kind)
        {
            _teleportInput.ControllerKind = kind;
        }

        private void Cache()
        {
            if (_teleportInput == null)
                _teleportInput = GetComponent<TeleportInputVpNext>();
        }
        
        #if UNITY_EDITOR

        private void Reset()
        {
            Cache();
        }

        private void OnValidate()
        {
            Cache();
        }

#endif
    }
}