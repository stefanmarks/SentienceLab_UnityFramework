#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Event Parameter")]
	public class Parameter_Event : ParameterBase, IParameterAsBoolean
	{
		public int EventCounter
		{
			get { return m_eventCounter; }
			set
			{
				m_eventCounter = value;
				m_triggeredEventCounter = Mathf.Min(m_triggeredEventCounter, m_eventCounter);
				MarkModified();
			}
		}


		/// <summary>
		/// Set the event counter without triggering a lot of events afterwards.
		/// </summary>
		/// <param name="_value">the value to set the counter to</param>
		public void SetCounter(int _value)
		{
			m_eventCounter = _value;
			m_triggeredEventCounter = _value;
		}


		/// <summary>
		/// Trigger an event.
		/// </summary>
		public void TriggerEvent()
		{
			EventCounter++;
		}


		/// <summary>
		/// Creates a formatted string.
		/// The value is passed to the formatting function as parameters #0.
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			return string.Format(_formatString, EventCounter);
		}


		protected override bool CheckForChange()
		{
			bool handled = true;
			if (m_triggeredEventCounter < EventCounter)
			{
				InvokeOnValueChanged();
				m_triggeredEventCounter++;
				handled = false;
			}
			return handled;
		}


		public bool GetBooleanValue()
		{
			return false;
		}


		public void SetBooleanValue(bool _value)
		{
			if (_value) TriggerEvent();
		}


		private int m_eventCounter          = 0;
		private int m_triggeredEventCounter = 0;
	}
}
