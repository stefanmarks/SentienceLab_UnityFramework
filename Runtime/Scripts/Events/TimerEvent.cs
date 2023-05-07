#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Component to trigger Unity events via timing.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Events/Timer Event")]
	public class TimerEvent : MonoBehaviour 
	{
		[Tooltip("Timer runtime in seconds")]
		public float Duration = 0;

		[Tooltip("Timer mode")]
		public enum EMode { Countdown, Timer };
		public EMode Mode = EMode.Countdown;

		[Tooltip("Format string for the OnTimerTextChanged event\nUses DateTime.toString() syntax (e.g. \"hh\\:mm\\:ss\\.f\")")]
		public string TextFormatString = "";

		[System.Serializable]
		public struct Events
		{
			[Tooltip("Event fired when timer is reset")]
			public UnityEvent<TimerEvent> OnTimerReset;

			[Tooltip("Event fired when timer starts")]
			public UnityEvent<TimerEvent> OnTimerStarted;

			[Tooltip("Event fired when the timer changes")]
			public UnityEvent<float> OnTimerChanged;

			[Tooltip("Event fired when the formatted timer text changes")]
			public UnityEvent<string> OnTimerTextChanged;

			[Tooltip("Event fired when timer expires")]
			public UnityEvent<TimerEvent> OnTimerExpired;
		}
		public Events events;


		public void Start()
		{
			m_running             = false;
			m_paused              = false;
			m_formatWarningIssued = false;
			ResetTimer();
		}


		public void StartTimer()
		{
			m_fTime   = (Mode == EMode.Countdown) ? Duration : 0f;
			m_running = true;
			events.OnTimerStarted.Invoke(this);
		}


		public void StartTimer(float timeInSeconds)
		{
			Duration = timeInSeconds;
			StartTimer();
		}


		public void ToggleTimerPaused()
		{
			m_paused = !m_paused;
		}


		public void SetTimerPaused(bool _paused)
		{
			m_paused = _paused;
		}


		public void ResetTimer()
		{
			m_fTime   = (Mode == EMode.Countdown) ? Duration : 0f;
			m_running = false;
			events.OnTimerReset.Invoke(this);
			InvokeChangeEvents();
		}


		public void Update()
		{
			if (!m_paused && m_running)
			{
				// update timer
				m_fTime += (Mode == EMode.Countdown) ? -Time.deltaTime : +Time.deltaTime;
				m_fTime = Mathf.Clamp(m_fTime, 0, Duration);

				InvokeChangeEvents();

				// are we there yet?
				if ( ((Mode == EMode.Timer    ) && (m_fTime >= Duration)) ||
				     ((Mode == EMode.Countdown) && (m_fTime <= 0       )) )
				{
					m_running = false; // stop, but don't invoke reset
					events.OnTimerExpired.Invoke(this);
				}
			}
		}


		protected void InvokeChangeEvents()
		{
			// fire change events
			events.OnTimerChanged.Invoke(m_fTime);

			if (TextFormatString.Length > 0)
			{
				System.TimeSpan ts = System.TimeSpan.FromSeconds(m_fTime);
				try
				{
					string text = ts.ToString(TextFormatString);
					if (!text.Equals(m_oldText))
					{
						// only fire text change event when text actually changes
						events.OnTimerTextChanged.Invoke(text);
						m_oldText = text;
						m_formatWarningIssued = false;
					}
				}
				catch (System.FormatException)
				{
					if (!m_formatWarningIssued)
					{
						Debug.LogWarningFormat("Timer Text Format String '{0}' not valid", TextFormatString);
						m_formatWarningIssued = true;
					}
				}
			}
		}


		public void OnValidate()
		{
			m_formatWarningIssued = false;
			if (!TextFormatString.Equals(m_oldText))
			{
				// format string has changed > verify
				System.TimeSpan ts = System.TimeSpan.FromSeconds(m_fTime);
				try
				{
					ts.ToString(TextFormatString);
				}
				catch (System.FormatException)
				{
					Debug.LogWarningFormat("Timer Text Format String '{0}' not valid", TextFormatString);
				}
				m_oldText = TextFormatString;
			}
		}

		private float  m_fTime;
		private bool   m_running, m_paused;
		private string m_oldText;
		private bool   m_formatWarningIssued;
	}
}
