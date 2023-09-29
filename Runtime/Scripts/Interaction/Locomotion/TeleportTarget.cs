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

	public class TeleportTarget : MonoBehaviour
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
			[Tooltip("Event fired when teleporting to this target")]
			public UnityEvent OnTeleport;
		}


		public void Start()
		{
			// nothing to do here, but needed to allow enabling/disabling of teleport targets
		}


		public void InvokeOnTeleport()
		{
			events.OnTeleport?.Invoke();
		}
	}
}
