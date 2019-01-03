using UnityEngine;

namespace Htc.Viveport.SDK
{
    public class CollisionForwarder : MonoBehaviour
    {
        public ObjectTrigger Trigger;

        void OnCollisionEnter( Collision col )
        {
            Trigger.CollisionHandler( col );
        }

        void OnTriggerEnter( Collider other )
        {
            Trigger.TriggerHandler( other );
        }
    }
}