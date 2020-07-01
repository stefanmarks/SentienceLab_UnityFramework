#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;


namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input System/Continuous")]

	public class ParameterController_InputSystem_Continuous : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		[TypeConstraint(typeof(IParameterModify))]
		public ParameterBase Parameter;

		[Tooltip("The index of the value to change (e.g., 0: min, 1: max. Default: 0)")]
		public int ValueIndex = 0;

		[Tooltip("Name of the input that controls this parameter")]
		public InputActionReference Action;

		[Tooltip("Factor to change the parameter by per second")]
		public float Multiplier = 1.0f;


		public void Start()
		{
			if(Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				if (Parameter is IParameterModify)
				{
					m_modify = (IParameterModify)Parameter;
				}
				else
				{
					Debug.LogWarning("Parameter can't be modified");
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
					Action.action.Enable();
				}
				else
				{
					Debug.LogWarningFormat("Action not defined for parameter '{0}'", Parameter.Name);
					this.enabled = false;
				}
			}
		}


		public void Update()
		{
			if (m_modify != null)
			{
				float value = Action.action.ReadValue<float>();
				m_modify.ChangeValue(value * Multiplier * Time.deltaTime, ValueIndex);
			}
		}


		protected IParameterModify m_modify;
	}
}