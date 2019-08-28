#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

/// <summary>
/// PID controller.
/// </summary>
/// 
namespace SentienceLab
{
	public class PID_Controller
	{
		[Tooltip("PID proportional control value")]
		public float P = 0f;

		[Tooltip("PID integral control value")]
		public float I = 0f;

		[Tooltip("PID derivative control value")]
		public float D = 0f;
		
		[Tooltip("Setpoint of the controller")]
		public float Setpoint = 0;

		[Tooltip("Maximum error sum allowed")]
		public float MaxError = 20f;


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
				m_sumErr += _deltaT * error;
				m_sumErr  = Mathf.Clamp(m_sumErr, -MaxError, MaxError);
				m_out += I * m_sumErr;

				// Derivative part
				float d_dt_error = (error - m_oldErr) / _deltaT;
				m_out += D * d_dt_error;

				// keep track of error for next timestep
				m_oldErr = error;
			}

			return m_out;
		} 
		
		private float m_oldErr = 0f;
		private float m_sumErr = 0f;	
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

		[Tooltip("Maximum error sum allowed")]
		public float MaxError = 20f;


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
				m_sumErr += _deltaT * error;
				m_sumErr = Vector3.ClampMagnitude(m_sumErr, MaxError);
				m_out += I * m_sumErr;

				// Derivative part
				Vector3 d_dt_error = (error - m_oldErr) / _deltaT;
				m_out += D * d_dt_error;

				// keep track of error for next timestep
				m_oldErr = error;
			}

			return m_out;
		}

		private Vector3 m_oldErr = Vector3.zero;
		private Vector3 m_sumErr = Vector3.zero;
		private Vector3 m_in, m_out;
	}
}
