#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;


namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input System/Switch Parameter Controller")]

	public class ParameterController_InputSystem_Switch : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		[TypeConstraint(typeof(IParameterAsBoolean))]
		public ParameterBase Parameter;

		[Tooltip("Input action that controls this parameter")]
		public InputActionProperty Action;

		public enum eMode
		{
			Momentary,
			Momentary_Inverted,
			Toggle
		}

		[Tooltip("Mode of the switch")]
		public eMode Mode;


		public void Start()
		{
			if (Parameter == null)
			{
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				m_boolean = (IParameterAsBoolean)Parameter;
				if (m_boolean == null)
				{
					Debug.LogWarningFormat("Parameter '{0}' does not provide IParameterAsBoolean interface", Parameter.Name);
					this.enabled = false;
				}
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
				this.enabled = false;
			}

			if (this.enabled)
			{
				if (Action != null)
				{
					Action.action.performed += OnActionPerformed;
					Action.action.canceled  += OnActionCanceled;
					Action.action.Enable();
				}
				else
				{
					Debug.LogWarningFormat("Action not defined for parameter '{0}'", Parameter.Name);
					this.enabled = false;
				}
			}

			m_toggled = false;
		}


		private void OnActionPerformed(InputAction.CallbackContext _ctx)
		{
			switch (Mode)
			{
				case eMode.Toggle:             if (!m_toggled) { m_boolean.SetBooleanValue(!m_boolean.GetBooleanValue()); m_toggled = true; }  break;
				case eMode.Momentary:          m_boolean.SetBooleanValue(true); break;
				case eMode.Momentary_Inverted: m_boolean.SetBooleanValue(false); break;
			}
		}


		private void OnActionCanceled(InputAction.CallbackContext _ctx)
		{
			switch (Mode)
			{
				case eMode.Toggle:             m_toggled = false; break;
				case eMode.Momentary:          m_boolean.SetBooleanValue(false); break;
				case eMode.Momentary_Inverted: m_boolean.SetBooleanValue(true); break;
			}
		}


		private void OnDestroy()
		{
			if (Action != null)
			{
				Action.action.performed -= OnActionPerformed;
				Action.action.canceled  -= OnActionCanceled;
			}
		}


		private IParameterAsBoolean m_boolean;
		private bool                m_toggled;
	}
}