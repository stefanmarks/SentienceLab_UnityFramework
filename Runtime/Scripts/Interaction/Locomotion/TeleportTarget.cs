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
		public enum EUpVector
		{
			Transform,
			ColliderNormal
		}

		[Tooltip("What defines 'up' when teleporting")]
		public EUpVector UpVector = EUpVector.Transform;

		[Tooltip("Flag to prevent teleporting")]
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


		public void Start()
		{
			// nothing to do here, but needed to allow enabling/disabling of teleport targets
		}


		public void TeleportControllerStartsAiming(BaseTeleportController _contoller)
		{
			m_controller = _contoller;
			events.OnStartAiming?.Invoke();
		}


		public void TeleportControllerEndsAiming(BaseTeleportController _contoller)
		{
			m_controller = _contoller;
			events.OnEndAiming?.Invoke();
		}


		public void TeleportControllerInvokesTeleport(BaseTeleportController _contoller)
		{
			m_controller = _contoller;
			events.OnTeleport?.Invoke();
		}


		public void OnPointerClick(PointerEventData eventData)
		{
			// Pointer Down/Up do the work.
			// This handler's presence makes sure the gaze cursor changes appearance
		}


		public void OnPointerDown(PointerEventData eventData)
		{
			if (m_controller != null)
			{
				m_controller.OnActionStart();
			}
		}


		public void OnPointerUp(PointerEventData eventData)
		{
			if (m_controller != null)
			{
				m_controller.OnActionEnd();
			}
		}


		protected BaseTeleportController m_controller;
	}
}
