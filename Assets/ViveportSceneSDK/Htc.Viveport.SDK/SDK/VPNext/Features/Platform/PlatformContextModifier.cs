using Htc.Viveport.SDK;
using UnityEngine;

namespace Htc.Viveport.Store
{
    public abstract class PlatformContextModifier : Trait<PlatformContextModifier>
    {
        public abstract void Modify(RecognizedPlatformId id);
    }
}