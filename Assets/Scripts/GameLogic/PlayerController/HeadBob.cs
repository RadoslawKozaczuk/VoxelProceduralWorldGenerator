using UnityEngine;
using Voxels.GameLogic.PlayerController;

namespace Voxels.GameLogic
{
    internal class HeadBob : MonoBehaviour
    {
        internal Camera Camera;
        internal CurveControlledBob motionBob = new CurveControlledBob();
        internal LerpControlledBob jumpAndLandingBob = new LerpControlledBob();
        internal RigidbodyFirstPersonController rigidbodyFirstPersonController;
        internal float StrideInterval;
        [Range(0f, 1f)]
        internal float RunningStrideLengthen;

        bool _previouslyGrounded;
        Vector3 _originalCameraPosition;

        void Start()
        {
            motionBob.Setup(Camera, StrideInterval);
            _originalCameraPosition = Camera.transform.localPosition;
        }

        void Update()
        {
            Vector3 newCameraPosition;
            if (rigidbodyFirstPersonController.Velocity.magnitude > 0 && rigidbodyFirstPersonController.Grounded)
            {
                Camera.transform.localPosition = motionBob.DoHeadBob(rigidbodyFirstPersonController.Velocity.magnitude * (rigidbodyFirstPersonController.Running ? RunningStrideLengthen : 1f));
                newCameraPosition = Camera.transform.localPosition;
                newCameraPosition.y = Camera.transform.localPosition.y - jumpAndLandingBob.Offset();
            }
            else
            {
                newCameraPosition = Camera.transform.localPosition;
                newCameraPosition.y = _originalCameraPosition.y - jumpAndLandingBob.Offset();
            }

            Camera.transform.localPosition = newCameraPosition;

            if (!_previouslyGrounded && rigidbodyFirstPersonController.Grounded)
                StartCoroutine(jumpAndLandingBob.DoBobCycle());

            _previouslyGrounded = rigidbodyFirstPersonController.Grounded;
        }
    }
}
