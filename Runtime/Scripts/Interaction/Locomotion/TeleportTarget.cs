#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SentienceLab
{
	/// <summary>
	/// Component for an object that can be aimed at for teleporting.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Teleport Target")]
	[DisallowMultipleComponent]

	public class TeleportTarget : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
	{
		[Tooltip("How is the orientation and view direction determined after teleport")]
		public Teleporter.OrientationAlignmentMode OrientationAlignmentMode = Teleporter.OrientationAlignmentMode.KeepOrientation;

		[Tooltip("Flag to prevent teleporting to this target")]
		public bool DisableTeleporting = false;

		public Events events;

		// own class to allow grouping in inspector
		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when a teleport controller starts aiming at this teleporter")]
			public UnityEvent OnStartAiming;

			[Tooltip("Event fired when a teleport controller stops aiming at this teleporter")]
			public UnityEvent OnEndAiming;

			[Tooltip("Event fired when teleporting to this target")]
			public UnityEvent OnTeleport;
		}

		// active teleport controller aiming at this target
		public BaseTeleportController AimingController { get; protected set; }


		public void Start()
		{
			// nothing to do here, but needed to allow enabling/disabling of teleport targets
		}


		public void TeleportControllerStartsAiming(BaseTeleportController _controller)
		{
			AimingController = _controller;
			events.OnStartAiming?.Invoke();
		}


		public void TeleportControllerEndsAiming(BaseTeleportController _)
		{
			events.OnEndAiming?.Invoke();
			AimingController = null;
		}


		public void TeleportControllerInvokesTeleport(BaseTeleportController _)
		{
			events.OnTeleport?.Invoke();
		}


		public void OnPointerClick(PointerEventData eventData)
		{
			// Pointer Down/Up do the work.
			// This handler's presence makes sure the gaze cursor changes appearance
		}


		public void OnPointerDown(PointerEventData eventData)
		{
			if (AimingController != null)
			{
				AimingController.OnActionStart();
			}
		}


		public void OnPointerUp(PointerEventData eventData)
		{
			if (AimingController != null)
			{
				AimingController.OnActionEnd();
			}
		}
	}
}
