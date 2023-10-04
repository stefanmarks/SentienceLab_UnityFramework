#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component to trigger Unity events on input actions.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Effects/Footstep Controller")]

	public class FootstepController : MonoBehaviour
	{
		public AudioSource LeftFoot;
		public AudioSource RightFoot;

		[Tooltip("Distance in units for each step")]
		public float StepDistance = 0.8f;

		[Range(0, 20)]
		[Tooltip("Percentage of pitch variation of the sounds")]
		public float PitchVariation = 0;

		public List<AudioClip> FootstepAudioL;
		public List<AudioClip> FootstepAudioR;


		public void Start()
		{
			m_lastFootstepLocation = transform.position;
			m_leftStep = false; // start on the right foot

			if (LeftFoot == null && RightFoot == null)
			{
				this.enabled = false;
			}

			if (LeftFoot  == null) { LeftFoot  = RightFoot; }
			if (RightFoot == null) { RightFoot = LeftFoot; }
		}

		// Update is called once per frame
		public void Update()
		{
			float distanceWalked = Vector3.Distance(m_lastFootstepLocation, transform.position);
			if (distanceWalked > StepDistance)
			{
				AudioSource src = m_leftStep ? LeftFoot : RightFoot;
				src.clip  = ChooseFromSounds(m_leftStep ? FootstepAudioL : FootstepAudioR);
				src.pitch = Random.Range(1 - PitchVariation / 100.0f, 1 + PitchVariation / 100.0f); ;
				src.Play();
			
				m_leftStep = !m_leftStep;
				m_lastFootstepLocation = transform.position;
			}
		}


		private AudioClip ChooseFromSounds(List<AudioClip> _sounds)
		{
			AudioClip clip = null;
			if (_sounds.Count > 0)
			{
				clip = _sounds[(int)Random.Range(0, _sounds.Count)];
			}
			return clip;
		}


		private Vector3 m_lastFootstepLocation;
		private bool    m_leftStep;
	}
}