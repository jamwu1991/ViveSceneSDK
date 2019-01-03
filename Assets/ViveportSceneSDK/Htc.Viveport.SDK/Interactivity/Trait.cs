
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
// ReSharper disable StaticMemberInGenericType

namespace Htc.Viveport.SDK
{
    using InstanceSet = HashSet<int>;

    public interface ITrait { }

    public abstract class Trait<T> : MonoBehaviour, ITrait
        where T : MonoBehaviour, ITrait
    {
        private static readonly InstanceSet GoIds = new InstanceSet();
        private static readonly Dictionary<int, T> Traits = new Dictionary<int, T>();

        public static IEnumerable<T> All
        {
            get { return Traits.Values; }
        }

        protected virtual void DidAwake()
        {
            
        }

        protected virtual void WillBeDestroyed()
        {
            
        }

        private void Awake()
        {
            var id = gameObject.GetInstanceID();

            Assert.IsTrue(this is T);
            
            GoIds.Add(id);
            Traits.Add(id, this as T);
            
            DidAwake();
        }

        private void OnDestroy()
        {
            WillBeDestroyed();
            
            var id = gameObject.GetInstanceID();

            Traits.Remove(id);
            GoIds.Remove(id);
        }

        public static bool Is(GameObject go)
        {
            Assert.IsNotNull(go);

            return go.activeInHierarchy && GoIds.Contains(go.GetInstanceID());
        }

        public static bool Is(Component comp)
        {
            Assert.IsNotNull(comp);
            return Is(comp.gameObject);
        }

        public static T Get(GameObject go)
        {
            Assert.IsNotNull(go);
            var id = go.GetInstanceID();

            T trait;
            if (!Traits.TryGetValue(id, out trait)) return null;

            return trait as T;
        }

        public static T Get(Component comp)
        {
            Assert.IsNotNull(comp);
            return Get(comp.gameObject);
        }

    }
}