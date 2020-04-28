using UnityEngine;
using Voxels.GameLogic.PlayerController;

namespace Voxels.GameLogic
{
    internal class HeadBob : MonoBehaviour
    {
        internal Camera Camera;
        internal CurveControlledBob MotionBob = new CurveControlledBob();
        internal LerpControlledBob JumpAndLandingBob = new LerpControlledBob();
        internal RigidbodyFirstPersonController rigidbodyFirstPersonController;
        internal float StrideInterval;
        [Range(0f, 1f)]
        internal float RunningStrideLengthen;

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
            if (rigidbodyFirstPersonController.Velocity.magnitude > 0 && rigidbodyFirstPersonController.Grounded)
            {
                Camera.transform.localPosition = MotionBob.DoHeadBob(rigidbodyFirstPersonController.Velocity.magnitude * (rigidbodyFirstPersonController.Running ? RunningStrideLengthen : 1f));
                newCameraPosition = Camera.transform.localPosition;
                newCameraPosition.y = Camera.transform.localPosition.y - JumpAndLandingBob.Offset();
            }
            else
            {
                newCameraPosition = Camera.transform.localPosition;
                newCameraPosition.y = _originalCameraPosition.y - JumpAndLandingBob.Offset();
            }

            Camera.transform.localPosition = newCameraPosition;

            if (!_previouslyGrounded && rigidbodyFirstPersonController.Grounded)
                StartCoroutine(JumpAndLandingBob.DoBobCycle());

            _previouslyGrounded = rigidbodyFirstPersonController.Grounded;
        }
    }
}
