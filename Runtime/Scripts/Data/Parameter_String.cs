#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/String")]
	public class Parameter_String : ParameterBase
	{
		[SerializeField]
		private String value;


		public new void Start()
		{
			base.Start();
		}


		/// <summary>
		/// The actual value.
		/// </summary>
		///
		public String Value
		{
			get { return value; }
			set
			{
				this.value = value;
				m_checkForChange = true;
			}
		}


		/// <summary>
		/// Creates a formatted string.
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			return string.Format(_formatString, value);
		}


		/// <summary>
		/// Check for changes to limits of the value and call event handlers accordingly.
		/// </summary>
		/// 
		protected override void CheckForChange()
		{
			if (m_oldValue != value)
			{
				InvokeOnValueChanged();
				m_oldValue = value;
			}
		}


		public override string ToString()
		{
			return Name + ":String:'" + value + "'";
		}


		protected String m_oldValue;
	}
}