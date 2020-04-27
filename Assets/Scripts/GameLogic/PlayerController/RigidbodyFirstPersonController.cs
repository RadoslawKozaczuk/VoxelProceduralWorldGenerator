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
                if (input.x > 0 || input.x < 0)
                {
                    //strafe
                    CurrentTargetSpeed = StrafeSpeed;
                }
                if (input.y < 0)
                {
                    //backwards
                    CurrentTargetSpeed = BackwardSpeed;
                }
                if (input.y > 0)
                {
                    //forwards
                    //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                    CurrentTargetSpeed = ForwardSpeed;
                }
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
            internal float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            internal float stickToGroundHelperDistance = 0.5f; // stops the character
            internal float slowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
            internal bool airControl; // can the user control the direction that is being moved in the air
            [Tooltip("set it to 0.1 or more if you get stuck in wall")]
            internal float shellOffset; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
        }

        internal Camera cam;
        internal MovementSettings movementSettings = new MovementSettings();
        internal MouseLook mouseLook = new MouseLook();
        internal AdvancedSettings advancedSettings = new AdvancedSettings();

        Rigidbody _rigidBody;
        CapsuleCollider m_Capsule;
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
                return movementSettings.Running;
#else
	            return false;
#endif
            }
        }


        void Start()
        {
            _rigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init(transform, cam.transform);
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

            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (advancedSettings.airControl || Grounded))
            {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, _groundContactNormal).normalized;
                desiredMove.x *= movementSettings.CurrentTargetSpeed;
                desiredMove.z *= movementSettings.CurrentTargetSpeed;
                desiredMove.y *= movementSettings.CurrentTargetSpeed;

                if (_rigidBody.velocity.sqrMagnitude < (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed))
                    _rigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
            }

            if (Grounded)
            {
                _rigidBody.drag = 5f;

                if (_jump)
                {
                    _rigidBody.drag = 0f;
                    _rigidBody.velocity = new Vector3(_rigidBody.velocity.x, 0f, _rigidBody.velocity.z);
                    _rigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
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
            return movementSettings.SlopeCurveModifier.Evaluate(angle);
        }

        void StickToGroundHelper()
        {
            if (Physics.SphereCast(
                origin: transform.position,
                radius: m_Capsule.radius * (1.0f - advancedSettings.shellOffset),
                direction: Vector3.down,
                hitInfo: out RaycastHit hitInfo,
                maxDistance: (m_Capsule.height / 2f) - m_Capsule.radius + advancedSettings.stickToGroundHelperDistance,
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

            movementSettings.UpdateDesiredTargetSpeed(input);
            return input;
        }

        void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation(transform, cam.transform);

            if (Grounded || advancedSettings.airControl)
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
                radius: m_Capsule.radius * (1.0f - advancedSettings.shellOffset),
                direction: Vector3.down,
                hitInfo: out RaycastHit hitInfo,
                maxDistance: (m_Capsule.height / 2f) - m_Capsule.radius + advancedSettings.groundCheckDistance,
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
