#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

/// <summary>
/// PID controller.
/// </summary>
/// 
namespace SentienceLab
{
	[System.Serializable]
	public class PID_Controller
	{
		[Tooltip("PID proportional control value")]
		public float P = 0f;

		[Tooltip("PID integral control value")]
		public float I = 0f;

		[Tooltip("PID derivative control value")]
		public float D = 0f;

		[Tooltip("Maximum output signal from the controller")]
		public float MaxOutput = float.PositiveInfinity;

		[Tooltip("Maximum error sum allowed")]
		public float MaxErrorSum = float.PositiveInfinity;


		public float Setpoint { get; set; }


		public float Process(float _inputValue, float _deltaT = -1)
		{
			m_in = _inputValue;

			if (_deltaT < 0) { _deltaT = Time.fixedDeltaTime; }

			if (_deltaT > 0)
			{
				float error = Setpoint - m_in;
			
				// Proportional part
				m_out = P * error;

				// Integral part
				m_errorSum += _deltaT * error;
				m_errorSum  = Mathf.Clamp(m_errorSum, -MaxErrorSum, MaxErrorSum);
				m_out += I * m_errorSum;

				// Derivative part
				float d_dt_error = (error - m_errorOld) / _deltaT;
				m_out += D * d_dt_error;

				// keep track of error for next timestep
				m_errorOld = error;

				// clamp to maximum output
				m_out = Mathf.Clamp(m_out, -MaxOutput, MaxOutput);
			}

			return m_out;
		} 
		
		private float m_errorOld = 0f;
		private float m_errorSum = 0f;	
		private float m_in, m_out;
	}


	[System.Serializable]
	public class PID_Controller3D
	{
		[Tooltip("PID proportional control value")]
		public float P = 0f;

		[Tooltip("PID integral control value")]
		public float I = 0f;

		[Tooltip("PID derivative control value")]
		public float D = 0f;

		[Tooltip("Maximum output signal from the controller")]
		public float MaxOutput = float.PositiveInfinity;

		[Tooltip("Maximum error sum allowed")]
		public float MaxErrorSum = float.PositiveInfinity;


		public Vector3 Setpoint { get; set; }


		public Vector3 Process(Vector3 _inputValue, float _deltaT = -1)
		{
			m_in = _inputValue;

			if (_deltaT < 0) { _deltaT = Time.fixedDeltaTime; }

			if (_deltaT > 0)
			{
				Vector3 error = Setpoint - m_in;

				// Proportional part
				m_out = P * error;

				// Integral part
				m_errorSum += _deltaT * error;
				m_errorSum = Vector3.ClampMagnitude(m_errorSum, MaxErrorSum);
				m_out += I * m_errorSum;

				// Derivative part
				Vector3 d_dt_error = (error - m_errorOld) / _deltaT;
				m_out += D * d_dt_error;

				// keep track of error for next timestep
				m_errorOld = error;

				// clamp to maximum output
				m_out = Vector3.ClampMagnitude(m_out, MaxOutput);

			}

			return m_out;
		}

		private Vector3 m_errorOld = Vector3.zero;
		private Vector3 m_errorSum = Vector3.zero;
		private Vector3 m_in, m_out;
	}
}
