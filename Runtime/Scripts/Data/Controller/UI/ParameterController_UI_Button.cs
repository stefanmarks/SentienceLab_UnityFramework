#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.Data
{
	[RequireComponent(typeof(Button))]
	[AddComponentMenu("Parameter/Controller/UI/Button Parameter Controller")]

	public class ParameterController_UI_Button : MonoBehaviour
	{
		public Parameter_Boolean Parameter;


		public void Start()
		{
			m_button = GetComponent<Button>();

			m_button.onClick.AddListener(delegate { ButtonClicked(); });

			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<Parameter_Boolean>();
			}
			if (Parameter != null)
			{
				Parameter.OnValueChanged += ValueChanged;
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
			}

			m_updating = false;
		}


		private void ButtonClicked()
		{
			if (!m_updating && (Parameter != null))
			{
				m_updating = true;
				// toggle variable
				Parameter.Value = !Parameter.Value;
				m_updating = false;
			}
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			if (!m_updating)
			{
				m_updating = true;
				// any visible changes here?
				m_updating = false;
			}
		}


		private bool   m_updating;
		private Button m_button;
	}
}