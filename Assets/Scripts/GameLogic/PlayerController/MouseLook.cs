using System;
using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    [Serializable]
    public class MouseLook
    {
        public Quaternion CharacterTargetRot;
        public Quaternion CameraTargetRot;

        readonly float _xSensitivity = 2f;
        readonly float _ySensitivity = 2f;
        readonly bool _clampVerticalRotation = true;
        readonly float _minimumX = -90F;
        readonly float _maximumX = 90F;
        readonly bool _smooth;
        readonly float _smoothTime = 5f;

        bool _lockCursor = true;
        bool _cursorIsLocked = true;

        public void Init(Transform character, Transform camera)
        {
            CharacterTargetRot = character.localRotation;
            CameraTargetRot = camera.localRotation;
        }

        public void LookRotation(Transform character, Transform camera)
        {
            float yRot = CrossPlatformInputManager.GetAxis("Mouse X") * _xSensitivity;
            float xRot = CrossPlatformInputManager.GetAxis("Mouse Y") * _ySensitivity;

            CharacterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
            CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

            if (_clampVerticalRotation)
                CameraTargetRot = ClampRotationAroundXAxis(CameraTargetRot);

            if (_smooth)
            {
                character.localRotation = Quaternion.Slerp(character.localRotation, CharacterTargetRot, _smoothTime * Time.deltaTime);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, CameraTargetRot, _smoothTime * Time.deltaTime);
            }
            else
            {
                character.localRotation = CharacterTargetRot;
                camera.localRotation = CameraTargetRot;
            }

            UpdateCursorLock();
        }

        public void SetCursorLock(bool value)
        {
            _lockCursor = value;
            if (!_lockCursor)
            {//we force unlock the cursor if the user disable the cursor locking helper
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void UpdateCursorLock()
        {
            //if the user set "lockCursor" we check & properly lock the cursor
            if (_lockCursor)
                InternalLockUpdate();
        }

        private void InternalLockUpdate()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
                _cursorIsLocked = false;
            else if (Input.GetMouseButtonUp(0))
                _cursorIsLocked = true;

            if (_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
            angleX = Mathf.Clamp(angleX, _minimumX, _maximumX);
            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }
    }
}
