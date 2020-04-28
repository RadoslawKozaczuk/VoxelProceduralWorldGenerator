using UnityEngine;
using Voxels.GameLogic.PlayerController;

namespace Voxels.GameLogic
{
    internal class HeadBob : MonoBehaviour
    {
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        internal Camera Camera;
        internal RigidbodyFirstPersonController RigidbodyFirstPersonController;
        internal float StrideInterval;
        [Range(0f, 1f)]
        internal float RunningStrideLengthen;
#pragma warning restore CS0649

        internal CurveControlledBob MotionBob = new CurveControlledBob();
        internal LerpControlledBob JumpAndLandingBob = new LerpControlledBob();

        bool _previouslyGrounded;
        Vector3 _originalCameraPosition;

        void Start()
        {
            MotionBob.Setup(Camera, StrideInterval);
            _originalCameraPosition = Camera.transform.localPosition;
        }

        void Update()
        {
            Vector3 newCameraPosition;
            if (RigidbodyFirstPersonController.Velocity.magnitude > 0 && RigidbodyFirstPersonController.Grounded)
            {
                Camera.transform.localPosition = MotionBob.DoHeadBob(RigidbodyFirstPersonController.Velocity.magnitude * (RigidbodyFirstPersonController.Running ? RunningStrideLengthen : 1f));
                newCameraPosition = Camera.transform.localPosition;
                newCameraPosition.y = Camera.transform.localPosition.y - JumpAndLandingBob.Offset();
            }
            else
            {
                newCameraPosition = Camera.transform.localPosition;
                newCameraPosition.y = _originalCameraPosition.y - JumpAndLandingBob.Offset();
            }

            Camera.transform.localPosition = newCameraPosition;

            if (!_previouslyGrounded && RigidbodyFirstPersonController.Grounded)
                StartCoroutine(JumpAndLandingBob.DoBobCycle());

            _previouslyGrounded = RigidbodyFirstPersonController.Grounded;
        }
    }
}
