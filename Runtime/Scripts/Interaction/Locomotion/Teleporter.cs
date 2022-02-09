#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;
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
			transition = null;
		}


		public void Update()
		{
			if (transition != null)
			{
				transition.Update(this.transform);
				if (transition.IsFinished())
				{
					transition.Cleanup();
					transition = null;
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
			return transition == null;
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
		/// <param name="desiredPosition">the desire position of the target reference point (e.g. hit point of controller)</param>
		/// 
		public void Teleport(TeleportTarget target, Vector3 desiredPosition, Vector3 desiredUp)
		{
			// calculate relative start and target position reference
			Vector3 relStartPos  = this.transform.InverseTransformPoint(positionReference.position);
			Vector3 relTargetPos = target.transform.InverseTransformPoint(desiredPosition);
			relStartPos.y  = 0;
			relTargetPos.y = 0;
			
			// calculate camera angle relative to start forward
			Vector3 relFwd = this.transform.InverseTransformDirection(positionReference.forward);
			relFwd.y = 0; relFwd.Normalize();
			float fwdAngle = Mathf.Atan2(relFwd.x, relFwd.z) * Mathf.Rad2Deg;

			Vector3    targetPos = Vector3.zero;
			Quaternion targetRot = Quaternion.identity;
			switch (target.OrientationAlignmentMode)
			{
				case OrientationAlignmentMode.KeepOrientation:
					{
						targetPos = this.transform.TransformPoint(relTargetPos - relStartPos);
						targetRot = this.transform.rotation;
						break;
					}
				case OrientationAlignmentMode.UseCollisionNormal:
					{
						// calculate forward from line of sight to desired position
						Vector3 projectedForward = Vector3.ProjectOnPlane(desiredPosition - positionReference.position, desiredUp);
						targetRot  = Quaternion.LookRotation(projectedForward, desiredUp);
						// compensate for camera rotation to face the desired orientation
						targetRot  = targetRot * Quaternion.AngleAxis(-fwdAngle, Vector3.up);
						// compensate target position based on calculated rotation
						targetPos = desiredPosition - targetRot * relStartPos;
						break;
					}
				case OrientationAlignmentMode.UseTargetOrientation:
					{
						targetPos = target.transform.TransformPoint(relTargetPos - relStartPos);
						targetRot = target.transform.rotation;
						break;
					}
				case OrientationAlignmentMode.UseTargetOrientationAndForceDirection:
					{
						targetPos = target.transform.TransformPoint(relTargetPos - relStartPos);
						// calculate difference rotation so camera faces forwards after teleport
						targetRot = target.transform.rotation * Quaternion.AngleAxis(-fwdAngle, Vector3.up);
						break;
					}
			}

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
					transition = new Transition_Fade(targetPoint, targetOrientation, transitionTime, this.gameObject);
					break;

				case TransitionType.MoveLinear:
					transition = new Transition_Move(originPoint, originOrientation, targetPoint, targetOrientation, transitionTime, false);
					break;

				case TransitionType.MoveSmooth:
					transition = new Transition_Move(originPoint, originOrientation, targetPoint, targetOrientation, transitionTime, true);
					break;
			}
		}


		private ITransition transition;


		private interface ITransition
		{
			void Update(Transform offsetObject);
			bool IsFinished();
			void Cleanup();
		}


		private class Transition_Fade : ITransition
		{
			public Transition_Fade(Vector3 endPoint, Quaternion endRot, float duration, GameObject parent)
			{
				this.endPoint    = endPoint;
				this.endRotation = endRot;
				this.duration    = duration;

				progress = 0;
				moved = false;

				// create fade effect
				fadeEffects = ScreenFade.AttachToAllCameras();
				foreach (ScreenFade fade in fadeEffects)
				{
					fade.FadeColour = Color.black;
				}
			}

			public void Update(Transform offsetObject)
			{
				// move immediately to B when fade is half way (all black)
				progress += Time.deltaTime / duration;
				progress  = Math.Min(1, progress);
				if ((progress >= 0.5f) && !moved)
				{
					offsetObject.position = endPoint;
					offsetObject.rotation = endRotation;
					moved = true; // only move once
				}

				float fadeFactor = 1.0f - Math.Abs(progress * 2 - 1); // from [0....1....0]
				foreach (ScreenFade fade in fadeEffects)
				{
					fade.FadeFactor = fadeFactor;
				}
			}

			public bool IsFinished()
			{
				return progress >= 1; // movement has finished
			}

			public void Cleanup()
			{
				foreach (ScreenFade fade in fadeEffects)
				{
					GameObject.Destroy(fade);
				}
			}


			private Vector3    endPoint;
			private Quaternion endRotation;
			private float      duration, progress;
			private bool       moved;
			private List<ScreenFade> fadeEffects;
		}


		private class Transition_Move : ITransition
		{
			public Transition_Move(
				Vector3 startPoint, Quaternion startRot, 
				Vector3 endPoint,   Quaternion endRot, 
				float   duration, 
				bool    smooth)
			{
				this.startPoint    = startPoint;
				this.startRotation = startRot;
				this.endPoint      = endPoint;
				this.endRotation   = endRot;
				this.duration      = duration;
				this.smooth        = smooth;

				progress = 0;
			}

			public void Update(Transform offsetObject)
			{
				// move from A to B
				progress += Time.deltaTime / duration;
				progress = Math.Min(1, progress);
				// linear: lerpFactor = progress. smooth: lerpFactor = sin(progress * PI/2) ^ 2
				float lerpFactor = smooth ? (float)Math.Pow(Math.Sin(progress * Math.PI / 2), 2) : progress;
				offsetObject.position = Vector3.Lerp(startPoint, endPoint, lerpFactor);
				offsetObject.rotation = Quaternion.Slerp(startRotation, endRotation, lerpFactor);
			}

			public bool IsFinished()
			{
				return progress >= 1; // movement has finished
			}

			public void Cleanup()
			{
				// nothing to do
			}

			private Vector3    startPoint, endPoint;
			private Quaternion startRotation, endRotation;
			private float      duration, progress;
			private bool       smooth;
		}
	}
}
