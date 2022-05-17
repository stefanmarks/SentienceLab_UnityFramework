#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Component to toggle a state and send events accordingly, 
	/// e.g, to turn lights on and off.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Toggle Event")]
	public class ToggleEvent : MonoBehaviour 
	{
		[Tooltip("Initial state of the toggle")]
		public bool       ToggleState = false; 

		[Tooltip("Event to fire when toggle turns on")]
		public UnityEvent OnToggleOn;

		[Tooltip("Event to fire when toggle turns off")]
		public UnityEvent OnToggleOff;


		public void Start()
		{
			// nothing to do here
		}


		public void Toggle()
		{
			SetState(!ToggleState);
		}


		public void SetState(bool _newState)
		{
			ToggleState = _newState;

			if (ToggleState) OnToggleOn.Invoke();
			else             OnToggleOff.Invoke();
		}
	}
}
