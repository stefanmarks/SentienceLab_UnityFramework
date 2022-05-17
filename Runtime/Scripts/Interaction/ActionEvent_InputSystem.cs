#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Component to trigger Unity events on input actions.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Action Event")]
	public class ActionEvent_InputSystem : MonoBehaviour 
	{
		[Tooltip("Action that fires the event")]
		public InputActionProperty action;

		[Tooltip("Event fired when action is performed")]
		public UnityEvent OnActionPerformed;


		public void Start()
		{
			if (action != null) 
			{ 
				action.action.Enable();
				action.action.performed += ActionPerformed;
			}
		}

		private void ActionPerformed(InputAction.CallbackContext obj)
		{
			PerformAction();
		}


		public void PerformAction()
		{
			OnActionPerformed.Invoke();
		}
	}
}
