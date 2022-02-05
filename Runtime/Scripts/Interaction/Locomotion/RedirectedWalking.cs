#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

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
		}


		public void Update()
		{
			// don't start warping immediately
			if (Time.frameCount > 10)
			{
				// how much did the head move
				Vector3 deltaT = HeadObject.position - m_oldPosition;

				// how much did the head rotate
				float deltaR = AngleDifference(HeadObject.rotation.eulerAngles.y, m_oldRotation);

				m_targetReached &= WarpTranslation(deltaT, deltaR);
				m_targetReached &= WarpRotation(deltaT, deltaR);
			}
			m_oldPosition = HeadObject.position;
			m_oldRotation = HeadObject.rotation.eulerAngles.y;
		}


		private bool WarpTranslation(Vector3 deltaT, float deltaR)
		{
			bool targetReached = true;
			Vector3 targetVector = TranslationTarget - TrackingSpace.position;
			if (targetVector.sqrMagnitude > 0)
			{
				targetReached = false;
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
			}
			return targetReached;
		}


		private bool WarpRotation(Vector3 deltaT, float deltaR)
		{
			bool targetReached = true;
			float targetRotation = AngleDifference(RotationTarget, TrackingSpace.rotation.eulerAngles.y);
			if (Mathf.Abs(targetRotation) > 0)
			{
				targetReached = false;
				// determine amount of influence
				float diff = Mathf.Abs(deltaR) * RotationModificationFactor;
				diff += deltaT.magnitude * Mathf.Rad2Deg * TranslationModificationFactor * TranslationRotationCrossoverFactor;
				// don't overshoot/oscillate
				diff = Mathf.Clamp(diff, 0, Mathf.Abs(targetRotation));
				// apply delta
				float offset = Mathf.Sign(targetRotation) * diff;
				TrackingSpace.RotateAround(HeadObject.position, Vector3.up, offset);
			}
			return targetReached;
		}


		float AngleDifference(float angle1, float angle2)
		{
			float diff = angle1 - angle2;
			if (diff < -180) diff += 360;
			if (diff > +180) diff -= 360;
			return diff;
		}


		public bool TargetReached { get { return m_targetReached; } private set { } }


		private Vector3 m_oldPosition;
		private float   m_oldRotation;
		private bool    m_targetReached;
	}
}
