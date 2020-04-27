using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    internal class MobileInput : VirtualInput
    {
        // we have not registered this button yet so add it, happens in the constructor
        void AddButton(string name) => CrossPlatformInputManager.RegisterVirtualButton(new CrossPlatformInputManager.VirtualButton(name));

        // we have not registered this button yet so add it, happens in the constructor
        void AddAxes(string name) => CrossPlatformInputManager.RegisterVirtualAxis(new CrossPlatformInputManager.VirtualAxis(name));

        internal override float GetAxis(string name, bool raw)
        {
            if (!_virtualAxes.ContainsKey(name))
                AddAxes(name);

            return _virtualAxes[name].GetValue;
        }

        internal override void SetButtonDown(string name)
        {
            if (!_virtualButtons.ContainsKey(name))
                AddButton(name);

            _virtualButtons[name].Pressed();
        }

        internal override void SetButtonUp(string name)
        {
            if (!_virtualButtons.ContainsKey(name))
                AddButton(name);

            _virtualButtons[name].Released();
        }

        internal override void SetAxisPositive(string name)
        {
            if (!_virtualAxes.ContainsKey(name))
                AddAxes(name);

            _virtualAxes[name].Update(1f);
        }

        internal override void SetAxisNegative(string name)
        {
            if (!_virtualAxes.ContainsKey(name))
                AddAxes(name);

            _virtualAxes[name].Update(-1f);
        }

        internal override void SetAxisZero(string name)
        {
            if (!_virtualAxes.ContainsKey(name))
                AddAxes(name);

            _virtualAxes[name].Update(0f);
        }

        internal override void SetAxis(string name, float value)
        {
            if (!_virtualAxes.ContainsKey(name))
                AddAxes(name);

            _virtualAxes[name].Update(value);
        }

        internal override bool GetButtonDown(string name)
        {
            if (_virtualButtons.ContainsKey(name))
                return _virtualButtons[name].GetButtonDown;

            AddButton(name);
            return _virtualButtons[name].GetButtonDown;
        }

        internal override bool GetButtonUp(string name)
        {
            if (_virtualButtons.ContainsKey(name))
                return _virtualButtons[name].GetButtonUp;

            AddButton(name);
            return _virtualButtons[name].GetButtonUp;
        }

        internal override bool GetButton(string name)
        {
            if (_virtualButtons.ContainsKey(name))
                return _virtualButtons[name].GetButton;

            AddButton(name);
            return _virtualButtons[name].GetButton;
        }

        internal override Vector3 MousePosition() => VirtualMousePosition;
    }
}
