#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Component to trigger Unity events via timing.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Timer Event")]
	public class TimerEvent : MonoBehaviour 
	{
		[Tooltip("Timer runtime in seconds")]
		public float      Duration;
		
		public UnityEvent TimerExpired;


		public void Start()
		{
			StopTimer();
		}


		public void StartTimer()
		{
			m_fTime = 0;
		}


		public void StopTimer()
		{
			m_fTime = -1;
		}


		public void Update()
		{
			if (m_fTime >= 0)
			{
				m_fTime += Time.deltaTime;

				if (m_fTime > Duration) 
				{
					StopTimer();
					TimerExpired.Invoke();
				}
			}
		}


		private float m_fTime;
	}
}
