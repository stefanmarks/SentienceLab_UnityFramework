#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SentienceLab.PostProcessing;

namespace SentienceLab
{
	/// <summary>
	/// Class for moving a Transform from one location to another.
	/// This component is specialised for user teleportation in XR,
	/// but can also be used to move normal objects.
	/// </summary>

	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Teleporter")]
	[DisallowMultipleComponent]

	public class Teleporter : MonoBehaviour
	{
		[Tooltip("Relative position reference object (e.g., tracked camera, HMD).")]
		public Transform positionReference = null;

		[Tooltip("Type of the teleport transition")]
		public TransitionType transitionType = TransitionType.MoveLinear;

		[Tooltip("Duration of the teleport transition in seconds")]
		public float transitionTime = 0.1f;

		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the teleporter starts")]
			public UnityEvent<Transform> OnTeleportStart;

			[Tooltip("Event fired when the teleporter finishes")]
			public UnityEvent<Transform> OnTeleportEnd;
		}

		public Events events;		


		public enum TransitionType
		{
			MoveLinear,
			MoveSmooth,
			Fade
		}


		/// <summary>
		/// Enum for different ways of changing orientation during teleport
		/// </summary>
		public enum OrientationAlignmentMode
		{
			KeepOrientation,
			UseCollisionNormal,
			UseTargetOrientation,
			UseTargetOrientationAndForceDirection
		}


		public void Start()
		{
			if (positionReference == null)
			{
				positionReference = this.transform;
			}
			m_transition = null;
		}


		public void Update()
		{
			if (m_transition != null)
			{
				if (m_fireStartEvent)
				{
					events.OnTeleportStart.Invoke(transform);
					m_fireStartEvent = false;
				}

				m_transition.Update(this.transform);

				if (m_transition.IsFinished())
				{
					m_transition.Cleanup();
					m_transition = null;

					events.OnTeleportEnd.Invoke(transform);
				}
			}
		}


		/// <summary>
		/// Checks if the Teleporter is ready.
		/// </summary>
		/// <returns><c>true</c> if teleport can be triggered</returns>
		/// 
		public bool IsReady()
		{
			return m_transition == null;
		}


		/// <summary>
		/// Teleport to a new teleport target.
		/// </summary>
		/// <param name="target">the TeleportTarget to use</param>
		/// 
		public void Teleport(TeleportTarget target)
		{
			Teleport(target, target.transform.position, target.transform.up);
		}


		/// <summary>
		/// Teleport to a new teleport target.
		/// </summary>
		/// <param name="target">the TeleportTarget to use</param>
		/// <param name="desiredPosition">the desired position of the target reference point (e.g. hit point of controller)</param>
		/// <param name="desiredUp">the desired up vector for the target rotation</param>
		/// 
		public void Teleport(TeleportTarget target, Vector3 desiredPosition, Vector3 desiredUp)
		{
			switch (target.OrientationAlignmentMode)
			{
				case OrientationAlignmentMode.KeepOrientation:
					Teleport_KeepOrientation(desiredPosition);
					break;

				case OrientationAlignmentMode.UseCollisionNormal:
					Teleport_UseCollisionNormal(desiredPosition, desiredUp);
					break;

				case OrientationAlignmentMode.UseTargetOrientation:
					Teleport_UseTargetOrientation(target, desiredPosition);
					break;

				case OrientationAlignmentMode.UseTargetOrientationAndForceDirection:
					Teleport_ForceDirection(target, desiredPosition, desiredUp);
					break;
			}
		}


		protected void Teleport_KeepOrientation(Vector3 desiredPosition)
		{
			// calculate relative start and target position reference
			Vector3 relStartPos  = this.transform.InverseTransformPoint(positionReference.position);
			Vector3 relTargetPos = this.transform.InverseTransformPoint(desiredPosition);
			relStartPos.y = 0; // project reference to floor

			Vector3 targetPos = this.transform.TransformPoint(relTargetPos - relStartPos);

			DoTeleport(
				this.transform.position, this.transform.rotation,
				targetPos, this.transform.rotation
			);
		}


		protected void Teleport_UseCollisionNormal(Vector3 desiredPosition, Vector3 desiredUp)
		{
			// calculate relative start position reference
			Vector3 relStartPos  = this.transform.InverseTransformPoint(positionReference.position);
			relStartPos.y = 0; // project reference to floor

			// calculate camera angle relative to start forward
			Vector3 relFwd = this.transform.InverseTransformDirection(positionReference.forward);
			relFwd.y = 0; relFwd.Normalize();
			float fwdAngle = Mathf.Atan2(relFwd.x, relFwd.z) * Mathf.Rad2Deg;

			// calculate forward from line of sight to desired position
			Vector3    projectedForward = Vector3.ProjectOnPlane(desiredPosition - positionReference.position, desiredUp);
			Quaternion targetRot = Quaternion.LookRotation(projectedForward, desiredUp);
			// compensate for camera rotation to face the desired orientation
			targetRot *= Quaternion.AngleAxis(-fwdAngle, Vector3.up);
			// compensate target position based on calculated rotation
			Vector3 targetPos = desiredPosition - targetRot * relStartPos;
						
			DoTeleport(
				this.transform.position, this.transform.rotation,
				targetPos, targetRot
			);
		}


		protected void Teleport_UseTargetOrientation(TeleportTarget target, Vector3 desiredPosition)
		{
			// calculate relative start and target position reference
			Vector3 relStartPos  = this.transform.InverseTransformPoint(positionReference.position);
			Vector3 relTargetPos = target.transform.InverseTransformPoint(desiredPosition);
			relStartPos.y = 0; // project reference to floor

			Vector3    targetPos = target.transform.TransformPoint(relTargetPos - relStartPos);
			Quaternion targetRot = target.transform.rotation;

			DoTeleport(
				this.transform.position, this.transform.rotation,
				targetPos, targetRot
			);
		}



		protected void Teleport_ForceDirection(TeleportTarget target, Vector3 desiredPosition, Vector3 desiredUp)
		{
			// calculate relative start and target position reference
			Vector3 relStartPos  = this.transform.InverseTransformPoint(positionReference.position);
			Vector3 relTargetPos = target.transform.InverseTransformPoint(desiredPosition);
			relStartPos.y = 0; // project reference to floor

			// calculate camera angle relative to start forward
			Vector3 relFwd = this.transform.InverseTransformDirection(positionReference.forward);
			relFwd.y = 0; relFwd.Normalize();
			float fwdAngle = Mathf.Atan2(relFwd.x, relFwd.z) * Mathf.Rad2Deg;

			Vector3 targetPos = target.transform.TransformPoint(relTargetPos - relStartPos);
			// calculate difference rotation so camera faces forwards after teleport
			Quaternion targetRot = target.transform.rotation * Quaternion.AngleAxis(-fwdAngle, Vector3.up);

			DoTeleport(
				this.transform.position, this.transform.rotation,
				targetPos, targetRot
			);
		}


		/// <summary>
		/// Starts the Teleport function to a specific point and a specific orientation.
		/// </summary>
		/// <param name="originPoint">the point to teleport from</param>
		/// <param name="originOrientation">the orientation to teleport from</param>
		/// <param name="targetPoint">the point to teleport to</param>
		/// <param name="targetOrientation">the orientation to teleport to</param>
		/// 
		protected void DoTeleport(Vector3 originPoint, Quaternion originOrientation, Vector3 targetPoint, Quaternion targetOrientation)
		{
			if (!IsReady()) return;

			// activate transition
			switch (transitionType)
			{
				case TransitionType.Fade:
					m_transition = new Transition_Fade(targetPoint, targetOrientation, transitionTime);
					break;

				case TransitionType.MoveLinear:
					m_transition = new Transition_Move(originPoint, originOrientation, targetPoint, targetOrientation, transitionTime, false);
					break;

				case TransitionType.MoveSmooth:
					m_transition = new Transition_Move(originPoint, originOrientation, targetPoint, targetOrientation, transitionTime, true);
					break;
			}

			m_fireStartEvent = true;
		}


		private ITransition m_transition;
		private bool        m_fireStartEvent;


		private interface ITransition
		{
			void Update(Transform offsetObject);
			bool IsFinished();
			void Cleanup();
		}


		private class Transition_Fade : ITransition
		{
			public Transition_Fade(Vector3 endPoint, Quaternion endRot, float duration)
			{
				this.m_endPoint    = endPoint;
				this.m_endRotation = endRot;
				this.m_duration    = duration;

				m_progress = 0;
				m_moved = false;

				// create fade effect
				m_fadeEffects = ScreenFade.AttachToAllCameras();
				foreach (ScreenFade fade in m_fadeEffects)
				{
					fade.FadeColour = Color.black;
				}
			}

			public void Update(Transform offsetObject)
			{
				// move immediately to B when fade is half way (all black)
				m_progress += Time.deltaTime / m_duration;
				m_progress  = Math.Min(1, m_progress);
				if ((m_progress >= 0.5f) && !m_moved)
				{
					offsetObject.position = m_endPoint;
					offsetObject.rotation = m_endRotation;
					m_moved = true; // only move once
				}

				float fadeFactor = 1.0f - Math.Abs(m_progress * 2 - 1); // from [0....1....0]
				foreach (ScreenFade fade in m_fadeEffects)
				{
					fade.FadeFactor = fadeFactor;
				}
			}

			public bool IsFinished()
			{
				return m_progress >= 1; // movement has finished
			}

			public void Cleanup()
			{
				foreach (ScreenFade fade in m_fadeEffects)
				{
					GameObject.Destroy(fade);
				}
			}


			private Vector3          m_endPoint;
			private Quaternion       m_endRotation;
			private float            m_duration, m_progress;
			private bool             m_moved;
			private List<ScreenFade> m_fadeEffects;
		}


		private class Transition_Move : ITransition
		{
			public Transition_Move(
				Vector3 startPoint, Quaternion startRot, 
				Vector3 endPoint,   Quaternion endRot, 
				float   duration, 
				bool    smooth)
			{
				this.m_startPoint    = startPoint;
				this.m_startRotation = startRot;
				this.m_endPoint      = endPoint;
				this.m_endRotation   = endRot;
				this.m_duration      = duration;
				this.m_smooth        = smooth;

				m_progress = 0;
			}

			public void Update(Transform offsetObject)
			{
				// move from A to B
				m_progress += Time.deltaTime / m_duration;
				m_progress = Math.Min(1, m_progress);
				// linear: lerpFactor = progress. smooth: lerpFactor = sin(progress * PI/2) ^ 2
				float lerpFactor = m_smooth ? (float)Math.Pow(Math.Sin(m_progress * Math.PI / 2), 2) : m_progress;
				offsetObject.position = Vector3.Lerp(m_startPoint, m_endPoint, lerpFactor);
				offsetObject.rotation = Quaternion.Slerp(m_startRotation, m_endRotation, lerpFactor);
			}

			public bool IsFinished()
			{
				return m_progress >= 1; // movement has finished
			}

			public void Cleanup()
			{
				// nothing to do
			}

			private Vector3    m_startPoint, m_endPoint;
			private Quaternion m_startRotation, m_endRotation;
			private float      m_duration, m_progress;
			private bool       m_smooth;
		}
	}
}
