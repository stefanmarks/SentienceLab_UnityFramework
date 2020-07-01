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
			TeleportAction.action.performed += OnTeleportStart;
			TeleportAction.action.canceled  += OnTeleportStop;
			
			ray = GetComponentInChildren<PointerRay>();
			if (ray == null)
			{
				// activate and release doesn't make much sense without the ray
				activationType  = ActivationType.OnTrigger;
				rayAlwaysActive = false;
			}
			else
			{
				rayAlwaysActive = ray.rayEnabled;
			}

			doAim      = false;
			doTeleport = false;
			teleporter = GameObject.FindObjectOfType<Teleporter>();
		}


		private void OnTeleportStart(InputAction.CallbackContext obj)
		{
			if (activationType == ActivationType.OnTrigger)
			{
				doAim      = true;
				doTeleport = true;
			}
			else
			{
				doAim = true;
			}
		}

		private void OnTeleportStop(InputAction.CallbackContext obj)
		{
			if (activationType == ActivationType.ActivateAndRelease)
			{
				doAim      = false;
				doTeleport = true;
			}
		}


		void Update()
		{
			if ((teleporter == null) || !teleporter.IsReady()) return;

			ray.rayEnabled = (doAim || rayAlwaysActive);

			RaycastHit hit;
			if (ray != null)
			{
				hit = ray.GetRayTarget();
			}
			else
			{
				// no ray component > do a basic raycast here
				Ray tempRay = new Ray(transform.position, transform.forward);
				UnityEngine.Physics.Raycast(tempRay, out hit);
			}

			if ((hit.distance > 0) && (hit.transform != null) && hit.transform.gameObject.tag.Equals(groundTag))
			{
				if (doTeleport)
				{
					if (teleporter != null)
					{
						// here we go: hide marker...
						targetMarker.gameObject.SetActive(false);
						// ...and activate teleport
						teleporter.Activate(cameraNode.transform.position, hit.point);
					}
					doTeleport = false;
				}
				else
				{
					if ((targetMarker != null) && doAim)
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
			}
		}


		private PointerRay  ray;
		private bool        rayAlwaysActive;
		private bool        doAim, doTeleport;
		private Teleporter  teleporter;
	}
}
