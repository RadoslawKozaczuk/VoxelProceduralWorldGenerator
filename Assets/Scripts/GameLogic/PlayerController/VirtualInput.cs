using System.Collections.Generic;
using UnityEngine;

namespace Voxels.GameLogic.PlayerController
{
    internal abstract class VirtualInput
    {
        internal Vector3 virtualMousePosition { get; private set; }
        
        protected Dictionary<string, CrossPlatformInputManager.VirtualAxis> _virtualAxes =
            new Dictionary<string, CrossPlatformInputManager.VirtualAxis>();
            // Dictionary to store the name relating to the virtual axes
        protected Dictionary<string, CrossPlatformInputManager.VirtualButton> _virtualButtons =
            new Dictionary<string, CrossPlatformInputManager.VirtualButton>();
        protected List<string> m_AlwaysUseVirtual = new List<string>();
        // list of the axis and button names that have been flagged to always use a virtual axis or button

        internal bool AxisExists(string name) => _virtualAxes.ContainsKey(name);
        
        internal bool ButtonExists(string name) => _virtualButtons.ContainsKey(name);

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

                // if we dont want to match with the input manager setting then revert to always using virtual
                if (!axis.MatchWithInputManager)
                {
                    m_AlwaysUseVirtual.Add(axis.Name);
                }
            }
        }

        internal void RegisterVirtualButton(CrossPlatformInputManager.VirtualButton button)
        {
            // check if already have a buttin with that name and log an error if we do
            if (_virtualButtons.ContainsKey(button.Name))
            {
                Debug.LogError("There is already a virtual button named " + button.Name + " registered.");
            }
            else
            {
                // add any new buttons
                _virtualButtons.Add(button.Name, button);

                // if we dont want to match to the input manager then always use a virtual axis
                if (!button.MatchWithInputManager)
                {
                    m_AlwaysUseVirtual.Add(button.Name);
                }
            }
        }

        internal void UnRegisterVirtualAxis(string name)
        {
            // if we have an axis with that name then remove it from our dictionary of registered axes
            if (_virtualAxes.ContainsKey(name))
            {
                _virtualAxes.Remove(name);
            }
        }

        internal void UnRegisterVirtualButton(string name)
        {
            // if we have a button with this name then remove it from our dictionary of registered buttons
            if (_virtualButtons.ContainsKey(name))
            {
                _virtualButtons.Remove(name);
            }
        }

        // returns a reference to a named virtual axis if it exists otherwise null
        internal CrossPlatformInputManager.VirtualAxis VirtualAxisReference(string name) => _virtualAxes.ContainsKey(name) ? _virtualAxes[name] : null;

        internal void SetVirtualMousePositionX(float f) => virtualMousePosition = new Vector3(f, virtualMousePosition.y, virtualMousePosition.z);

        internal void SetVirtualMousePositionY(float f) => virtualMousePosition = new Vector3(virtualMousePosition.x, f, virtualMousePosition.z);

        internal void SetVirtualMousePositionZ(float f) => virtualMousePosition = new Vector3(virtualMousePosition.x, virtualMousePosition.y, f);

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
