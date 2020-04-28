using System.Collections.Generic;
using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    internal abstract class VirtualInput
    {
        internal Vector3 VirtualMousePosition { get; private set; }

        protected Dictionary<string, CrossPlatformInputManager.VirtualAxis> _virtualAxes = new Dictionary<string, CrossPlatformInputManager.VirtualAxis>();
        // Dictionary to store the name relating to the virtual axes
        protected Dictionary<string, CrossPlatformInputManager.VirtualButton> _virtualButtons = new Dictionary<string, CrossPlatformInputManager.VirtualButton>();

        protected List<string> _alwaysUseVirtual = new List<string>();
        // list of the axis and button names that have been flagged to always use a virtual axis or button

        internal void RegisterVirtualAxis(CrossPlatformInputManager.VirtualAxis axis)
        {
            // check if we already have an axis with that name and log and error if we do
            if (_virtualAxes.ContainsKey(axis.Name))
            {
                Debug.LogError("There is already a virtual axis named " + axis.Name + " registered.");
            }
            else
            {
                // add any new axes
                _virtualAxes.Add(axis.Name, axis);

                // if we don't want to match with the input manager setting then revert to always using virtual
                if (!axis.MatchWithInputManager)
                    _alwaysUseVirtual.Add(axis.Name);
            }
        }

        internal void RegisterVirtualButton(CrossPlatformInputManager.VirtualButton button)
        {
            // check if already have a button with that name and log an error if we do
            if (_virtualButtons.ContainsKey(button.Name))
            {
                Debug.LogError("There is already a virtual button named " + button.Name + " registered.");
            }
            else
            {
                // add any new buttons
                _virtualButtons.Add(button.Name, button);

                // if we don't want to match to the input manager then always use a virtual axis
                if (!button.MatchWithInputManager)
                    _alwaysUseVirtual.Add(button.Name);
            }
        }

        internal abstract float GetAxis(string name, bool raw);

        internal abstract bool GetButton(string name);

        internal abstract bool GetButtonDown(string name);

        internal abstract bool GetButtonUp(string name);

        internal abstract void SetButtonDown(string name);

        internal abstract void SetButtonUp(string name);

        internal abstract void SetAxisPositive(string name);

        internal abstract void SetAxisNegative(string name);

        internal abstract void SetAxisZero(string name);

        internal abstract void SetAxis(string name, float value);

        internal abstract Vector3 MousePosition();
    }
}
