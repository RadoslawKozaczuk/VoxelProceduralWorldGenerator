using System;
using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    internal class RigidbodyFirstPersonController : MonoBehaviour
    {
        [Serializable]
        internal class MovementSettings
        {
            internal float ForwardSpeed = 8.0f;   // Speed when walking forward
            internal float BackwardSpeed = 4.0f;  // Speed when walking backwards
            internal float StrafeSpeed = 4.0f;    // Speed when walking sideways
            internal float RunMultiplier = 2.0f;   // Speed when sprinting
            internal KeyCode RunKey = KeyCode.LeftShift;
            internal float JumpForce = 30f;
            internal AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
            [HideInInspector] internal float CurrentTargetSpeed = 8f;

#if !MOBILE_INPUT
#endif

            internal void UpdateDesiredTargetSpeed(Vector2 input)
            {
                if (input == Vector2.zero) return;

                //strafe
                if (input.x > 0 || input.x < 0)
                    CurrentTargetSpeed = StrafeSpeed;

                //backwards
                if (input.y < 0)
                    CurrentTargetSpeed = BackwardSpeed;

                //forwards
                //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                if (input.y > 0)
                    CurrentTargetSpeed = ForwardSpeed;

#if !MOBILE_INPUT
                if (Input.GetKey(RunKey))
                {
                    CurrentTargetSpeed *= RunMultiplier;
                    Running = true;
                }
                else
                {
                    Running = false;
                }
#endif
            }

#if !MOBILE_INPUT
            internal bool Running { get; private set; }
#endif
        }

        [Serializable]
        internal class AdvancedSettings
        {
            internal float GroundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            internal float StickToGroundHelperDistance = 0.5f; // stops the character
            internal float SlowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
            internal bool AirControl; // can the user control the direction that is being moved in the air
            [Tooltip("set it to 0.1 or more if you get stuck in wall")]
            internal float ShellOffset; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
#pragma warning restore CS0649
        }

#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        internal Camera Camera;
#pragma warning restore CS0649

        internal MovementSettings MovementSetting = new MovementSettings();
        internal MouseLook MouseLook = new MouseLook();
        internal AdvancedSettings AdvancedSetting = new AdvancedSettings();

        Rigidbody _rigidBody;
        CapsuleCollider _capsule;
        Vector3 _groundContactNormal;
        bool _jump, _previouslyGrounded;

        internal Vector3 Velocity => _rigidBody.velocity;

        internal bool Grounded { get; private set; }

        internal bool Jumping { get; private set; }

        internal bool Running
        {
            get
            {
#if !MOBILE_INPUT
                return MovementSetting.Running;
#else
	            return false;
#endif
            }
        }

        void Start()
        {
            _rigidBody = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            MouseLook.Init(transform, Camera.transform);
        }

        void Update()
        {
            RotateView();

            if (CrossPlatformInputManager.GetButtonDown("Jump") && !_jump)
                _jump = true;
        }

        void FixedUpdate()
        {
            GroundCheck();
            Vector2 input = GetInput();

            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (AdvancedSetting.AirControl || Grounded))
            {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = Camera.transform.forward * input.y + Camera.transform.right * input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, _groundContactNormal).normalized;
                desiredMove.x *= MovementSetting.CurrentTargetSpeed;
                desiredMove.z *= MovementSetting.CurrentTargetSpeed;
                desiredMove.y *= MovementSetting.CurrentTargetSpeed;

                if (_rigidBody.velocity.sqrMagnitude < (MovementSetting.CurrentTargetSpeed * MovementSetting.CurrentTargetSpeed))
                    _rigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
            }

            if (Grounded)
            {
                _rigidBody.drag = 5f;

                if (_jump)
                {
                    _rigidBody.drag = 0f;
                    _rigidBody.velocity = new Vector3(_rigidBody.velocity.x, 0f, _rigidBody.velocity.z);
                    _rigidBody.AddForce(new Vector3(0f, MovementSetting.JumpForce, 0f), ForceMode.Impulse);
                    Jumping = true;
                }

                if (!Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && _rigidBody.velocity.magnitude < 1f)
                    _rigidBody.Sleep();
            }
            else
            {
                _rigidBody.drag = 0f;
                if (_previouslyGrounded && !Jumping)
                    StickToGroundHelper();
            }
            _jump = false;
        }

        float SlopeMultiplier()
        {
            float angle = Vector3.Angle(_groundContactNormal, Vector3.up);
            return MovementSetting.SlopeCurveModifier.Evaluate(angle);
        }

        void StickToGroundHelper()
        {
            if (Physics.SphereCast(
                origin: transform.position,
                radius: _capsule.radius * (1.0f - AdvancedSetting.ShellOffset),
                direction: Vector3.down,
                hitInfo: out RaycastHit hitInfo,
                maxDistance: (_capsule.height / 2f) - _capsule.radius + AdvancedSetting.StickToGroundHelperDistance,
                layerMask: Physics.AllLayers,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore))
            {
                if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
                    _rigidBody.velocity = Vector3.ProjectOnPlane(_rigidBody.velocity, hitInfo.normal);
            }
        }

        Vector2 GetInput()
        {
            var input = new Vector2
            {
                x = CrossPlatformInputManager.GetAxis("Horizontal"),
                y = CrossPlatformInputManager.GetAxis("Vertical")
            };

            MovementSetting.UpdateDesiredTargetSpeed(input);
            return input;
        }

        void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            MouseLook.LookRotation(transform, Camera.transform);

            if (Grounded || AdvancedSetting.AirControl)
            {
                // Rotate the rigidbody velocity to match the new direction that the character is looking
                Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
                _rigidBody.velocity = velRotation * _rigidBody.velocity;
            }
        }

        /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        void GroundCheck()
        {
            _previouslyGrounded = Grounded;
            if (Physics.SphereCast(
                origin: transform.position,
                radius: _capsule.radius * (1.0f - AdvancedSetting.ShellOffset),
                direction: Vector3.down,
                hitInfo: out RaycastHit hitInfo,
                maxDistance: (_capsule.height / 2f) - _capsule.radius + AdvancedSetting.GroundCheckDistance,
                layerMask: Physics.AllLayers,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore))
            {
                Grounded = true;
                _groundContactNormal = hitInfo.normal;
            }
            else
            {
                Grounded = false;
                _groundContactNormal = Vector3.up;
            }

            if (!_previouslyGrounded && Grounded && Jumping)
                Jumping = false;
        }
    }
}
