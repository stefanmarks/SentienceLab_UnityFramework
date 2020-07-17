#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input System/Discrete Controller")]

	public class ParameterController_InputSystem_Discrete : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		[TypeConstraint(typeof(IParameterModify))]
		public ParameterBase Parameter;

		[Tooltip("The index of the value to change (e.g., 0: min, 1: max. Default: 0)")]
		public int ValueIndex = 0;

		[Tooltip("Action that increases this parameter")]
		public InputActionReference IncreaseAction;

		[Tooltip("Action that decreases this parameter")]
		public InputActionReference DecreaseAction;

		[Tooltip("Factor to change the parameter by per step")]
		public int Multiplier = 1;


		public void Start()
		{
			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				m_modify = (IParameterModify)Parameter;
				if (m_modify == null)
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

			if (IncreaseAction != null)
			{
				IncreaseAction.action.performed += delegate { IncreaseValue(); };
				IncreaseAction.action.Enable();
			}
			
			if (DecreaseAction != null)
			{
				DecreaseAction.action.performed += delegate { DecreaseValue(); };
				DecreaseAction.action.Enable();
			}
		}


		public void IncreaseValue()
		{
			if (m_modify != null)
			{
				m_modify.ChangeValue(Multiplier, ValueIndex);
			}
		}


		public void DecreaseValue()
		{
			if (m_modify != null)
			{
				m_modify.ChangeValue(-Multiplier, ValueIndex);
			}
		}

		private IParameterModify m_modify;
	}
}