#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace SentienceLab
{
	/// <summary>
	/// Component for managing and sending haptic feedback impulses.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Haptic Feedback Manager")]
	public class HapticFeedbackManager : MonoBehaviour
	{
		[Tooltip("Input action for haptic feedback")]
		public InputActionProperty HapticFeedbackAction;

		[System.Serializable]
		public struct HapticImpulse
		{
			public float Amplitude;
			public float Duration;

			public HapticImpulse(float _amplitude, float _duration)
			{
				Amplitude = Mathf.Clamp01(_amplitude);
				Duration = Mathf.Clamp(_duration, 0, 10); // 10s max
			}
		}

		[Tooltip("List with different kinds of impulses")]
		public List<HapticImpulse> ImpulseList;


		public virtual void Start()
		{
			if (HapticFeedbackAction != null)
			{
				HapticFeedbackAction.action.Enable();
			}
			else
			{
				Debug.LogWarning("No action defined for haptic feedback");
				this.enabled = false;
			}

			if (ImpulseList == null)
			{
				ImpulseList = new List<HapticImpulse>();
			}
			if (ImpulseList.Count == 0)
			{
				// have at least a default impulse
				ImpulseList.Add(new HapticImpulse(1, 0.1f));
			}
		}


		public void SendHapticFeedback(int _id)
		{
			_id = Mathf.Clamp(_id, 0, ImpulseList.Count - 1);

			var controls = HapticFeedbackAction.action?.controls;
			if (controls != null)
			{
				foreach (var control in controls)
				{
					var device = control.device;
					if (device is XRControllerWithRumble rumbleController)
					{
						HapticImpulse impulse = ImpulseList[_id];
						rumbleController.SendImpulse(impulse.Amplitude, impulse.Duration);
					}
				}
			}
		}
	}
}