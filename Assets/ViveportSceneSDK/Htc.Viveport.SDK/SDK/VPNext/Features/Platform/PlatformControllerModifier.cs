using UnityEngine;

namespace Htc.Viveport.Store
{
    public abstract class PlatformControllerModifier : MonoBehaviour
    {
        public abstract void Modify(RecognizedControllerKind kind);
    }
}