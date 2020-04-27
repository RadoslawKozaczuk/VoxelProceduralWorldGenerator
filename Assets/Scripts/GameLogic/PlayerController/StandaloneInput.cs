using System;
using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    internal class StandaloneInput : VirtualInput
    {
        internal override float GetAxis(string name, bool raw) => raw ? Input.GetAxisRaw(name) : Input.GetAxis(name);

        internal override bool GetButton(string name) => Input.GetButton(name);

        internal override bool GetButtonDown(string name) => Input.GetButtonDown(name);

        internal override bool GetButtonUp(string name) => Input.GetButtonUp(name);

        internal override void SetButtonDown(string name) 
            => throw new Exception(" This is not possible to be called for standalone input. Please check your platform and code where this is called");

        internal override void SetButtonUp(string name) 
            => throw new Exception(" This is not possible to be called for standalone input. Please check your platform and code where this is called");

        internal override void SetAxisPositive(string name) 
            => throw new Exception(" This is not possible to be called for standalone input. Please check your platform and code where this is called");

        internal override void SetAxisNegative(string name) 
            => throw new Exception(" This is not possible to be called for standalone input. Please check your platform and code where this is called");

        internal override void SetAxisZero(string name) 
            => throw new Exception(" This is not possible to be called for standalone input. Please check your platform and code where this is called");

        internal override void SetAxis(string name, float value) 
            => throw new Exception(" This is not possible to be called for standalone input. Please check your platform and code where this is called");

        internal override Vector3 MousePosition() => Input.mousePosition;
    }
}