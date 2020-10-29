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
                UnbindTrackedState();
                m_trackedStateAction = value;
                BindTrackedState();
            }
        }

        bool m_trackedStateBound = false;


        void BindTrackedState()
        {
            if (!m_trackedStateBound && m_trackedStateAction != null)
            {
                m_trackedStateAction.Rename($"{gameObject.name} - TSE - Tracked State");
                m_trackedStateAction.performed += OnTrackedStateUpdate;
                m_trackedStateBound = true;
                m_trackedStateAction.Enable();
            }
        }

        void UnbindTrackedState()
        {
            if (m_trackedStateAction != null && m_trackedStateBound)
            {
                m_trackedStateAction.Disable();
                m_trackedStateAction.performed -= OnTrackedStateUpdate;
                m_trackedStateBound = false;
            }
        }


        void OnTrackedStateUpdate(InputAction.CallbackContext context)
        {
            Debug.Assert(m_trackedStateBound);
            this.gameObject.SetActive(context.ReadValueAsButton());
        }


        protected void OnEnable()
        {
            BindTrackedState();
        }


        public void Update()
        {
            if (m_trackedStateAction != null)
            {
                this.gameObject.SetActive(m_trackedStateAction.ReadValue<float>() > 0);
            }
        }
    }
}
