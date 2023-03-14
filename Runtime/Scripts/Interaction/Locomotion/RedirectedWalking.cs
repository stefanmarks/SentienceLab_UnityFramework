#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Script for slowly applying rotation and/or translation offsets to the tracking space
	/// while the user is moving.
	/// The modification factors should be less than 10% according to
	/// F. Steinicke, G. Bruder, J. Jerald, H. Frenz and M. Lappe, 
	/// "Estimation of Detection Thresholds for Redirected Walking Techniques," 
	/// in IEEE Transactions on Visualization and Computer Graphics, vol. 16, no. 1, pp. 17-27, Jan.-Feb. 2010
	/// https://ieeexplore.ieee.org/document/5072212/
	/// </summary>
	/// 

	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Redirected Walking")]
	public class RedirectedWalking : MonoBehaviour
	{
		[Tooltip("Activate redirecting")]
		public bool Active;

		[Tooltip("The node that represents the user's head")]
		public Transform HeadObject;

		[Tooltip("The node that represents hte tracking space to manipulate")]
		public Transform TrackingSpace;

		[Tooltip("Target position")]
		public Vector3 TranslationTarget = Vector3.zero;

		[Tooltip("Amount of translation modulation (should be < 0.1)")]
		public float TranslationModificationFactor = 0.05f;

		[Tooltip("Target rotation")]
		public float RotationTarget = 0;

		[Tooltip("Amount of rotation modulation (should be < 0.1)")]
		public float RotationModificationFactor = 0.05f;

		[Tooltip("How much does translation affect rotation and vice versa")]
		public float TranslationRotationCrossoverFactor = 0.2f;

		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the redirecting starts")]
			public UnityEvent OnRedirectingStart;

			[Tooltip("Event fired during redirecting")]
			public UnityEvent<float> OnRedirectingProgress;
			
			[Tooltip("Event fired when the redirecting finishes")]
			public UnityEvent OnRedirectingEnd;
		}

		public Events events;


		public void StartRedirecting()
		{
			Active = true;
		}

		public void EndRedirecting()
		{
			Active = false;
		}

		public void Start()
		{
			if (HeadObject == null)
			{
				// fallback: find main camera automatically
				HeadObject = Camera.main.transform;
			}

			if (TrackingSpace == null)
			{
				// fallback: this object
				TrackingSpace = this.transform;
			}

			m_active = false;
		}


		public void Update()
		{
			// avoid working at all at the very beginning to avoid sudden jumps
			if (Time.frameCount < 10) return;

			if (Active)
			{
				if (!m_active)
				{
					// just activated > send event
					events.OnRedirectingStart.Invoke();
					m_active = true;
					m_trackingSpaceStartPosition = TrackingSpace.transform.position;
					m_trackingSpaceStartRotation = TrackingSpace.transform.rotation.eulerAngles.y;
				}

				// how much did the head move
				Vector3 deltaT = HeadObject.position - m_oldHeadPosition;

				// how much did the head rotate
				float deltaR = AngleDifference(HeadObject.rotation.eulerAngles.y, m_oldHeadRotation);

				// warp
				float progressT = WarpTranslation(deltaT, deltaR);
				float progressR = WarpRotation(deltaT, deltaR);

				// signal progress
				float progress = Mathf.Min(progressT, progressR);
				events.OnRedirectingProgress.Invoke(progress);
				if (progress >= 1.0f)
				{
					// reached goal > deactivate (which will send event next frame)
					Active = false;
				}
			}
			else // not active
			{
				if (m_active)
				{
					// just deactivated > send event
					events.OnRedirectingEnd.Invoke();
					m_active = false;
				}
			}

			// remember current head position for next delta calculation
			m_oldHeadPosition = HeadObject.position;
			m_oldHeadRotation = HeadObject.rotation.eulerAngles.y;
		}


		private float WarpTranslation(Vector3 deltaT, float deltaR)
		{
			float progress = 1.0f;
			Vector3 targetVector = TranslationTarget - TrackingSpace.position;
			if (targetVector.sqrMagnitude > 0)
			{
				// only find the bit in the actual correction direction (project onto direction vector)
				deltaT = Vector3.Project(deltaT, targetVector.normalized);
				// determine amount of influence
				float diff = deltaT.magnitude * TranslationModificationFactor;
				diff += Mathf.Abs(deltaR) * Mathf.Deg2Rad * RotationModificationFactor * TranslationRotationCrossoverFactor;
				// don't overshoot/oscillate
				diff = Mathf.Clamp(diff, 0, targetVector.magnitude);
				// apply delta
				Vector3 offset = targetVector.normalized * diff;
				TrackingSpace.position += offset;
				// calculate progress
				float overall = (TranslationTarget - m_trackingSpaceStartPosition).magnitude;
				float current = (TranslationTarget - TrackingSpace.position      ).magnitude;
				progress = 1.0f - (current / overall);
			}
			return progress;
		}


		private float WarpRotation(Vector3 deltaT, float deltaR)
		{
			float progress = 1.0f;
			float targetRotation = AngleDifference(RotationTarget, TrackingSpace.rotation.eulerAngles.y);
			if (Mathf.Abs(targetRotation) > 0)
			{
				// determine amount of influence
				float diff = Mathf.Abs(deltaR) * RotationModificationFactor;
				diff += deltaT.magnitude * Mathf.Rad2Deg * TranslationModificationFactor * TranslationRotationCrossoverFactor;
				// don't overshoot/oscillate
				diff = Mathf.Clamp(diff, 0, Mathf.Abs(targetRotation));
				// apply delta
				float offset = Mathf.Sign(targetRotation) * diff;
				TrackingSpace.RotateAround(HeadObject.position, Vector3.up, offset);
				// calculate progress
				float overall = AngleDifference(RotationTarget, m_trackingSpaceStartRotation);
				float current = AngleDifference(RotationTarget, TrackingSpace.rotation.eulerAngles.y);
				progress = 1.0f - (current / overall);
			}
			return progress;
		}


		float AngleDifference(float angle1, float angle2)
		{
			float diff = angle1 - angle2;
			if (diff < -180) diff += 360;
			if (diff > +180) diff -= 360;
			return diff;
		}


		private Vector3 m_trackingSpaceStartPosition;
		private float   m_trackingSpaceStartRotation;
		private Vector3 m_oldHeadPosition;
		private float   m_oldHeadRotation;
		private bool    m_active;
	}
}
