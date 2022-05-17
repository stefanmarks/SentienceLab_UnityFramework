#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Text;

namespace SentienceLab
{
	/// <summary>
	/// Component for debugging input action values.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/InputAction Debugger")]
	public class InputActionDebugger : MonoBehaviour
	{
		public List<InputActionProperty> actionsToDebug;

		[Tooltip("Interval in seconds between debugging values")]
		public float printInterval = 1.0f;


		public void Start()
		{
			m_string = new StringBuilder();
			foreach (var action in actionsToDebug) { action.action.Enable(); }
			m_timer = 0;
		}

		
		public void Update()
		{
			if (m_timer <= 0)
			{
				m_string.Clear();
				foreach (var action in actionsToDebug)
				{
					if ((action != null) && (action.action != null))
					{
						DebugAction(action.action);
					}
				}
				Debug.Log(m_string);
				m_timer = printInterval;
			}
			else
			{
				m_timer -= Time.unscaledDeltaTime;
			}
		}


		protected void DebugAction(InputAction action)
		{
			m_string.Append(action.name);
			if (action.controls.Count > 0)
			{
				System.Type valueType = action.controls[0].valueType;
				m_string.Append(" (").Append(valueType.ToString()).Append(") = ");
				switch (valueType)
				{
					case System.Type _ when valueType == typeof(float):
						m_string.Append(action.ReadValue<float>());
						break;
					case System.Type _ when valueType == typeof(bool):
						m_string.Append(action.ReadValue<bool>());
						break;
					case System.Type _ when valueType == typeof(Vector2):
						m_string.Append(action.ReadValue<Vector2>());
						break;
					case System.Type _ when valueType == typeof(Vector3):
						m_string.Append(action.ReadValue<Vector3>());
						break;
					case System.Type _ when valueType == typeof(Quaternion):
						m_string.Append(action.ReadValue<Quaternion>());
						break;
				}
			}
			m_string.Append(" -> ").Append(action.phase);
			m_string.Append('\n');
		}

		protected StringBuilder m_string;
		protected float         m_timer;
	}
}
