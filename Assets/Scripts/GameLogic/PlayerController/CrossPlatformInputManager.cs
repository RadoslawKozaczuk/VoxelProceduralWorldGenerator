using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    public static class CrossPlatformInputManager
    {
        static VirtualInput _activeInput;
        static VirtualInput _touchInput;
        static VirtualInput _hardwareInput;

        static CrossPlatformInputManager()
        {
            _touchInput = new MobileInput();
            _hardwareInput = new StandaloneInput();
#if MOBILE_INPUT
            _activeInput = _touchInput;
#else
            _activeInput = _hardwareInput;
#endif
        }

        internal static void RegisterVirtualAxis(VirtualAxis axis) => _activeInput.RegisterVirtualAxis(axis);

        internal static void RegisterVirtualButton(VirtualButton button) => _activeInput.RegisterVirtualButton(button);

        // returns the platform appropriate axis for the given name
        public static float GetAxis(string name) => GetAxis(name, false);

        // private function handles both types of axis (raw and not raw)
        public static float GetAxis(string name, bool raw) => _activeInput.GetAxis(name, raw);

        public static bool GetButtonDown(string name) => _activeInput.GetButtonDown(name);

        // virtual axis and button classes - applies to mobile input
        // Can be mapped to touch joysticks, tilt, gyro, etc, depending on desired implementation.
        // Could also be implemented by other input devices - kinect, electronic sensors, etc
        internal class VirtualAxis
        {
            internal string Name { get; private set; }
            internal bool MatchWithInputManager { get; private set; }

            internal VirtualAxis(string name) : this(name, true) { }

            internal VirtualAxis(string name, bool matchToInputSettings)
            {
                Name = name;
                MatchWithInputManager = matchToInputSettings;
            }

            // a controller gameobject (eg. a virtual thumbstick) should update this class
            internal void Update(float value) => GetValue = value;

            internal float GetValue { get; private set; }
        }

        // a controller gameobject (eg. a virtual GUI button) should call the
        // 'pressed' function of this class. Other objects can then read the
        // Get/Down/Up state of this button.
        internal class VirtualButton
        {
            internal string Name { get; private set; }
            internal bool MatchWithInputManager { get; private set; }

            int _lastPressedFrame = -5;
            int _releasedFrame = -5;

            internal VirtualButton(string name) : this(name, true) { }

            internal VirtualButton(string name, bool matchToInputSettings)
            {
                Name = name;
                MatchWithInputManager = matchToInputSettings;
            }

            // A controller gameobject should call this function when the button is pressed down
            internal void Pressed()
            {
                if (GetButton)
                    return;

                GetButton = true;
                _lastPressedFrame = Time.frameCount;
            }

            // A controller gameobject should call this function when the button is released
            internal void Released()
            {
                GetButton = false;
                _releasedFrame = Time.frameCount;
            }

            // these are the states of the button which can be read via the cross platform input system
            internal bool GetButton { get; private set; }

            internal bool GetButtonDown => _lastPressedFrame - Time.frameCount == -1;

            internal bool GetButtonUp => _releasedFrame == Time.frameCount - 1;
        }
    }
}
