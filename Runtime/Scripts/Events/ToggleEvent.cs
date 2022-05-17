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
	[AddComponentMenu("SentienceLab/Events/Toggle Event")]
	public class ToggleEvent : MonoBehaviour 
	{
		[Tooltip("Initial state of the toggle")]
		public bool State = false;

		[System.Serializable]
		public struct Events
		{
			[Tooltip("Event fired when toggle turns on")]
			public UnityEvent OnToggleOn;

			[Tooltip("Event fired when toggle turns off")]
			public UnityEvent OnToggleOff;
		}
		public Events events;


		public void ToggleState()
		{
			SetState(!State);
		}


		public void SetState(bool _newState)
		{
			State = _newState;

			if (State) events.OnToggleOn.Invoke();
			else       events.OnToggleOff.Invoke();
		}
	}
}
