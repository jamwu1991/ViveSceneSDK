using UnityEngine;

namespace Htc.Viveport.SDK
{
    [RequireComponent(typeof(SteamVR_TrackedObject), typeof(Collider))]
    public class ControllerThrowObj : MonoBehaviour
    {
        [SerializeField] private Rigidbody attachPoint;

        private SteamVR_TrackedObject trackedObj;
        private FixedJoint joint;
        private ViveObjectProps pickupObject;

        private void Reset()
        {
            trackedObj = GetComponent<SteamVR_TrackedObject>();
            if (GetComponent<GameController>() == null)
                gameObject.AddComponent<GameController>();
        }

        private void Awake()
        {
            if (trackedObj == null) trackedObj = GetComponent<SteamVR_TrackedObject>();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!Throwable.Is(other.gameObject) || joint != null || pickupObject != null) return;
            
            pickupObject = other.gameObject.GetComponent<ViveObjectProps>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (joint != null || pickupObject == null) return;
            
            pickupObject.isBeingPickedUp = false;
            pickupObject = null;
        }


        private void FixedUpdate()
        {
            var device = SteamVR_Controller.Input((int)trackedObj.index);
            if (joint == null && pickupObject != null && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                Pickup();
            }
            else if (joint != null && device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                Throw(device);
            }
        }

        private void Pickup()
        {
            pickupObject.isBeingPickedUp = true;
            joint = pickupObject.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = attachPoint;
        }


        private void Throw(SteamVR_Controller.Device device)
        {
            var rigidbody = pickupObject.gameObject.GetComponent<Rigidbody>();
            DestroyImmediate(joint);
            joint = null;

            pickupObject.isBeingPickedUp = false;
            pickupObject = null;

            var origin = trackedObj.origin ? trackedObj.origin : trackedObj.transform.parent;
            if (origin != null)
            {
                rigidbody.velocity = origin.TransformVector(device.velocity);
                rigidbody.angularVelocity = origin.TransformVector(device.angularVelocity);
            }
            else
            {
                rigidbody.velocity = device.velocity;
                rigidbody.angularVelocity = device.angularVelocity;
            }

            rigidbody.maxAngularVelocity = rigidbody.angularVelocity.magnitude;
        }
    }
}
