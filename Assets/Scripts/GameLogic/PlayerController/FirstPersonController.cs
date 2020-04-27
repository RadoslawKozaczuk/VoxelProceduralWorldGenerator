using UnityEngine;
using Random = UnityEngine.Random;

namespace Voxels.GameLogic.PlayerController
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        public MouseLook MouseLook;

#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [Range(0f, 1f)]
        [SerializeField] float _runstepLenghten;
        [SerializeField] bool _isWalking;
        [SerializeField] float _walkSpeed;
        [SerializeField] float _runSpeed;
        [SerializeField] float _jumpSpeed;
        [SerializeField] float _stickToGroundForce;
        [SerializeField] float _gravityMultiplier;
        [SerializeField] bool _useFovKick;
        [SerializeField] FOVKick _fovKick = new FOVKick();
        [SerializeField] bool _useHeadBob;
        [SerializeField] CurveControlledBob _headBob = new CurveControlledBob();
        [SerializeField] LerpControlledBob _jumpBob = new LerpControlledBob();
        [SerializeField] float _stepInterval;
        [SerializeField] AudioClip[] _footstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] AudioClip _jumpSound;           // the sound played when character leaves the ground.
        [SerializeField] AudioClip _landSound;           // the sound played when character touches back on ground.
#pragma warning restore CS0649

        Camera _camera;
        bool _jump;
        Vector2 _input;
        Vector3 _moveDir = Vector3.zero;
        CharacterController _characterController;
        CollisionFlags _collisionFlags;
        bool _previouslyGrounded;
        Vector3 _originalCameraPosition;
        float _stepCycle;
        float _nextStep;
        bool _jumping;
        AudioSource _audioSource;

        void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _camera = Camera.allCameras[1];
            _originalCameraPosition = _camera.transform.localPosition;
            _fovKick.Setup(_camera);
            _headBob.Setup(_camera, _stepInterval);
            _stepCycle = 0f;
            _nextStep = _stepCycle / 2f;
            _jumping = false;
            _audioSource = GetComponent<AudioSource>();
            MouseLook.Init(transform, _camera.transform);
        }

        void Update()
        {
            RotateView();

            // the jump state needs to read here to make sure it is not missed
            if (!_jump)
                _jump = CrossPlatformInputManager.GetButtonDown("Jump");

            if (!_previouslyGrounded && _characterController.isGrounded)
            {
                StartCoroutine(_jumpBob.DoBobCycle());
                PlayLandingSound();
                _moveDir.y = 0f;
                _jumping = false;
            }

            if (!_characterController.isGrounded && !_jumping && _previouslyGrounded)
                _moveDir.y = 0f;

            _previouslyGrounded = _characterController.isGrounded;
        }

        void FixedUpdate()
        {
            GetInput(out float speed);

            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * _input.y + transform.right * _input.x;

            // get a normal for the surface that is being touched to move along it
            Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out RaycastHit hitInfo,
                _characterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            _moveDir.x = desiredMove.x * speed;
            _moveDir.z = desiredMove.z * speed;

            if (_characterController.isGrounded)
            {
                _moveDir.y = -_stickToGroundForce;

                if (_jump)
                {
                    _moveDir.y = _jumpSpeed;
                    PlayJumpSound();
                    _jump = false;
                    _jumping = true;
                }
            }
            else
            {
                _moveDir += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
            }
            _collisionFlags = _characterController.Move(_moveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            MouseLook.UpdateCursorLock();
        }

        void PlayLandingSound()
        {
            _audioSource.clip = _landSound;
            _audioSource.Play();
            _nextStep = _stepCycle + .5f;
        }

        void PlayJumpSound()
        {
            _audioSource.clip = _jumpSound;
            _audioSource.Play();
        }

        void ProgressStepCycle(float speed)
        {
            if (_characterController.velocity.sqrMagnitude > 0 && (_input.x != 0 || _input.y != 0))
                _stepCycle += (_characterController.velocity.magnitude + (speed * (_isWalking ? 1f : _runstepLenghten)))
                    * Time.fixedDeltaTime;

            if (!(_stepCycle > _nextStep))
                return;

            _nextStep = _stepCycle + _stepInterval;

            PlayFootStepAudio();
        }

        void PlayFootStepAudio()
        {
            if (!_characterController.isGrounded)
                return;

            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, _footstepSounds.Length);
            _audioSource.clip = _footstepSounds[n];
            _audioSource.PlayOneShot(_audioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            _footstepSounds[n] = _footstepSounds[0];
            _footstepSounds[0] = _audioSource.clip;
        }

        void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!_useHeadBob)
                return;

            if (_characterController.velocity.magnitude > 0 && _characterController.isGrounded)
            {
                _camera.transform.localPosition =
                    _headBob.DoHeadBob(_characterController.velocity.magnitude
                    + (speed * (_isWalking ? 1f : _runstepLenghten)));
                newCameraPosition = _camera.transform.localPosition;
                newCameraPosition.y = _camera.transform.localPosition.y - _jumpBob.Offset();
            }
            else
            {
                newCameraPosition = _camera.transform.localPosition;
                newCameraPosition.y = _originalCameraPosition.y - _jumpBob.Offset();
            }
            _camera.transform.localPosition = newCameraPosition;
        }

        void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool wasWalking = _isWalking;

            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            _isWalking = !Input.GetKey(KeyCode.LeftShift);

            // set the desired speed to be walking or running
            speed = _isWalking ? _walkSpeed : _runSpeed;
            _input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (_input.sqrMagnitude > 1)
                _input.Normalize();

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (_isWalking != wasWalking && _useFovKick && _characterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!_isWalking ? _fovKick.FOVKickUp() : _fovKick.FOVKickDown());
            }
        }

        void RotateView() => MouseLook.LookRotation(transform, _camera.transform);

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            // don't move the rigidbody if the character is on top of it
            if (_collisionFlags == CollisionFlags.Below)
                return;

            if (body == null || body.isKinematic)
                return;

            body.AddForceAtPosition(_characterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }
}