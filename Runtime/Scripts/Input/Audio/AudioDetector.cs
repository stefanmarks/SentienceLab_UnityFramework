#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Component for recording sound from a microphone and being able to react to noise/silence.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Input/Audio/Audio Detector")]
	public class AudioDetector : MonoBehaviour
	{
		[Tooltip("Name of the microphone to use (can be regular expression. empty: use default)")]
		public string MicrophoneName = "";

		[Tooltip("Threshold at which to fire noise/silence detected events")]
		[Range(0,1)]
		public float NoiseThreshold = 0.2f;

		[Tooltip("Event fired when the detected volume level is below the noise threshold")]
		public UnityEvent SilenceDetected;

		[Tooltip("Event fired when the noise threshold is exceeded")]
		public UnityEvent NoiseDetected;


		public void Start()
		{
			SelectMicrophone();
		
			// Start recording audio in 1s clips, looping
			m_clip = Microphone.Start(MicrophoneName, true, 1, 44100);
			if (m_clip == null)
			{
				Debug.LogWarning("Could not open microphone");
				this.enabled = false;
			}
			else
			{
				Debug.LogFormat("Opened microphone '{0}'", MicrophoneName);
			}

			m_data = null;
			m_dataPos = 0;
			m_prevDataPos = 0;
			m_noiseDetected = false;
		}


		private void SelectMicrophone()
		{
			string[] microphones = Microphone.devices;
			if (microphones != null && microphones.Length > 0)
			{
				if (MicrophoneName == null || MicrophoneName.Length == 0)
				{
					// no microphone name given > use first one
					MicrophoneName = microphones[0];
				}
				else
				{
					// microphone name given > find exact match
					foreach (string mic in microphones)
					{
						if (Regex.IsMatch(mic, MicrophoneName))
						{
							MicrophoneName = mic;
							break;
						}
					}
				}
			}
		}

	
		public void Update()
		{
			// allocate audio data buffer if not already done
			if (m_data == null)
			{
				// allocate buffer if not yet done
				m_data = new float[m_clip.samples * m_clip.channels];
			}
			// how much data do we have?
			m_dataPos = Microphone.GetPosition(MicrophoneName);
			if (m_dataPos != m_prevDataPos)
			{
				m_clip.GetData(m_data, 0);

				CurrentLevelRMS  = 0;
				CurrentLevelPeak = 0;
				int   idx     = m_prevDataPos;
				int   samples = 0;
				while (idx != m_dataPos)
				{
					float sample = m_data[idx];
					CurrentLevelRMS += sample * sample;
					CurrentLevelPeak = Mathf.Max(CurrentLevelPeak, Mathf.Abs(sample));
					idx = (idx + 1) % m_data.Length;
					samples++;
				}
				CurrentLevelRMS /= samples;
				CurrentLevelRMS  = Mathf.Sqrt(CurrentLevelRMS);
				CurrentLevelDB   = 20 * Mathf.Log10(CurrentLevelRMS);

				if (CurrentLevelRMS > NoiseThreshold)
				{
					if (!m_noiseDetected)
					{
						NoiseDetected.Invoke();
						m_noiseDetected = true;
					}
				}
				else 
				{
					if (m_noiseDetected)
					{
						SilenceDetected.Invoke();
						m_noiseDetected = false;
					}
				}

				m_prevDataPos = m_dataPos;
			}
		}


		public float CurrentLevelRMS  { get; private set; }

		public float CurrentLevelDB   { get; private set; }

		public float CurrentLevelPeak { get; private set; }


		protected AudioClip m_clip;
		protected float[]   m_data;
		protected int       m_dataPos, m_prevDataPos;
		protected bool      m_noiseDetected;
	}
}