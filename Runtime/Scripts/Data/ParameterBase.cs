#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SentienceLab.Data
{
	/// <summary>
	/// Abstract base class for parameters.
	/// </summary>
	/// 
	public abstract class ParameterBase : MonoBehaviour
	{
		public string Name;

		public delegate void ValueChanged(ParameterBase _parameter);
		public event ValueChanged OnValueChanged;


		/// <summary>
		/// Called at beginning. Resets variables.
		/// </summary>
		/// 
		public void Start()
		{
			// make sure name is defined
			Assert.IsNotNull(Name);
			Assert.IsTrue(Name.Length > 0);

			m_checkForChange = false;
		}


		/// <summary>
		/// Called every frame.
		/// Checks for updates and will call event handlers accordingly.
		/// </summary>
		/// 
		public void Update()
		{
			if (m_checkForChange)
			{
				bool changeHandled = CheckForChange();
				m_checkForChange = !changeHandled;
			}
		}


		/// <summary>
		/// Marks the parameter as modified and will trigger a check how to sync with the entity.
		/// </summary>
		/// 
		public void MarkModified()
		{
			m_checkForChange = true;
		}


		/// <summary>
		/// Any changes in the editor should cause events.
		/// </summary>
		/// 
		public void OnValidate()
		{
			MarkModified();
		}


		/// <summary>
		/// Override in derived classes to create a formatted string.
		/// </summary>
		/// 
		public abstract string ToFormattedString(string _formatString);


		/// <summary>
		/// Override in derived classes to check for any changes to the parameter.
		/// </summary>
		/// <returns><c>true</c> when change has been dealt with, <c>false</c> when to check again in the next Update</returns>
		/// 
		protected abstract bool CheckForChange();


		/// <summary>
		/// Accessor to call change event handlers.
		/// </summary>
		/// 
		protected void InvokeOnValueChanged()
		{
			if (OnValueChanged != null) OnValueChanged.Invoke(this);
		}


		private bool m_checkForChange;
	}


	/// <summary>
	/// Interface for a parameter that can be treated as a boolean.
	/// </summary>
	/// 
	public interface IParameterAsBoolean
	{
		bool GetBooleanValue();
		void SetBooleanValue(bool _value);
	}


	/// <summary>
	/// Interface for a parameter that can be modified with a delta value.
	/// </summary>
	/// 
	public interface IParameterModify
	{
		/// <summary>
		/// Changes the value or a parameter
		/// </summary>
		/// <param name="_delta">the value to change by</param>
		/// <param name="_idx">the index of the value to change (e.g., 0: min, 1: max) </param>
		void ChangeValue(float _delta, int _idx = 0);
	}
}
