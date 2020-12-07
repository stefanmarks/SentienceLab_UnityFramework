using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab.Input
{
    /// <summary>
    /// The TrackingStateEnabler component applies the "tracked" state of a device to the game object's "enable" status.
    /// </summary>
    [Serializable]
    [AddComponentMenu("XR/Tracking State Enabler (New Input System)")]
    public class TrackingStateEnabler : MonoBehaviour
    {
        [SerializeField]
        InputAction m_trackedStateAction;
        public InputAction trackedStateAction
        {
            get { return m_trackedStateAction; }
            set
            {
                UnbindActions();
                m_trackedStateAction = value;
                BindActions();
            }
        }

        private bool m_actionsBound = false;
        private bool m_trackedState = false;


        public void Start()
        {
            InputSystem.onAfterUpdate += UpdateCallback;
            BindActions();
        }


        public void OnDestroy()
        {
            UnbindActions();
            InputSystem.onAfterUpdate -= UpdateCallback;
        }


        protected void BindActions()
        {
            if (!m_actionsBound && m_trackedStateAction != null)
            {
                m_trackedStateAction.Rename($"{gameObject.name} - TSE - Tracked State");
                m_actionsBound = true;
                m_trackedStateAction.Enable();
            }
        }

        protected void UnbindActions()
        {
            if (m_trackedStateAction != null && m_actionsBound)
            {
                m_trackedStateAction.Disable();
                m_actionsBound = false;
            }
        }


        protected void UpdateCallback()
        {
            m_trackedState = m_trackedStateAction.ReadValue<float>() > 0;
            OnUpdate();
        }


        protected virtual void OnUpdate()
        {
            this.gameObject.SetActive(m_trackedState);
        }
    }
}
