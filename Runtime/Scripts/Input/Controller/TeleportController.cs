#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Component for an object that can be aimed at for teleporting.
	/// This component does NOT use the event system.
	/// </summary>

	[AddComponentMenu("Locomotion/Teleport Controller")]
	[DisallowMultipleComponent]

	public class TeleportController : MonoBehaviour
	{
		public InputActionReference TeleportAction;
		public string               groundTag      = "floor";
		public Transform            cameraNode     = null;
		public Transform            targetMarker   = null;

		public ActivationType       activationType = ActivationType.OnTrigger;


		public enum ActivationType
		{
			OnTrigger,
			ActivateAndRelease
		}



		void Start()
		{
			if (TeleportAction == null)
			{
				Debug.LogWarning("Teleport action not defined");
				this.enabled = false;
				return;
			}
			
			TeleportAction.action.performed += OnTeleportStart;
			TeleportAction.action.canceled  += OnTeleportStop;
			TeleportAction.action.Enable();
			
			m_pointerRay = GetComponentInChildren<PointerRay>();
			if (m_pointerRay == null)
			{
				// activate and release doesn't make much sense without the ray
				activationType  = ActivationType.OnTrigger;
				m_rayAlwaysActive = false;
			}
			else
			{
				m_rayAlwaysActive = (m_pointerRay.activationParameter != null) &&
				                  (m_pointerRay.activationParameter.Value == true);
			}

			m_doAim      = false;
			m_doTeleport = false;
			m_teleporter = GameObject.FindObjectOfType<Teleporter>();
		}


		private void OnTeleportStart(InputAction.CallbackContext obj)
		{
			if (activationType == ActivationType.OnTrigger)
			{
				m_doAim      = true;
				m_doTeleport = true;
			}
			else
			{
				m_doAim = true;
			}
		}


		private void OnTeleportStop(InputAction.CallbackContext obj)
		{
			if (activationType == ActivationType.ActivateAndRelease)
			{
				m_doAim      = false;
				m_doTeleport = true;
			}
		}


		void Update()
		{
			if ((m_teleporter == null) || !m_teleporter.IsReady()) return;

			m_pointerRay.activationParameter.Value = (m_doAim || m_rayAlwaysActive);

			RaycastHit hit;
			if (m_pointerRay != null)
			{
				hit = m_pointerRay.GetRayTarget();
			}
			else
			{
				// no ray component > do a basic raycast here
				Ray tempRay = new Ray(transform.position, transform.forward);
				UnityEngine.Physics.Raycast(tempRay, out hit);
			}

			if ((hit.distance > 0) && (hit.transform != null) && hit.transform.gameObject.CompareTag(groundTag))
			{
				if (m_doTeleport)
				{
					if (m_teleporter != null)
					{
						// here we go: hide marker...
						targetMarker.gameObject.SetActive(false);
						// ...and activate teleport
						m_teleporter.Activate(cameraNode.transform.position, hit.point);
					}
					m_doTeleport = false;
				}
				else
				{
					if ((targetMarker != null) && m_doAim)
					{
						targetMarker.gameObject.SetActive(true);
						float yaw = cameraNode.transform.rotation.eulerAngles.y;
						targetMarker.position = hit.point;
						targetMarker.localRotation = Quaternion.Euler(0, yaw, 0);
					}
				}
			}
			else
			{
				if (targetMarker != null)
				{
					targetMarker.gameObject.SetActive(false);
				}
				m_doTeleport = false;
			}
		}


		private PointerRay  m_pointerRay;
		private bool        m_rayAlwaysActive;
		private bool        m_doAim, m_doTeleport;
		private Teleporter  m_teleporter;
	}
}
