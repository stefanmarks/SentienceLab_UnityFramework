﻿#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Component for moving a physical object by clicking and moving it.
	/// When clicked, the script will try to maintain the relative position of the rigid body using forces applied to its centre.
	/// If a sound component is attached, this will be played in loop mode.
	/// </summary>
	///
	public class PhysicsManipulator : MonoBehaviour
	{
		[Tooltip("Input action for grabbing")]
		public InputActionReference GrabAction;

		[Tooltip("Grab PID controller")]
		public PID_Controller3D PID;
		

		void Start()
		{
			m_pointerRay = GetComponentInChildren<PointerRay>();
			m_sound      = GetComponent<AudioSource>();

			if (GrabAction != null)
			{
				GrabAction.action.performed += OnGrabStart;
				GrabAction.action.canceled  += OnGrabEnd;
				GrabAction.action.Enable();
			}
		}


		private void OnGrabStart(InputAction.CallbackContext obj)
		{
			// trigger pulled: is there any rigid body where the ray points at?
			RaycastHit target;
			if (m_pointerRay != null)
			{
				// PointerRay used? result is ready
				target = m_pointerRay.GetRayTarget();
			}
			else
			{
				// no PointerRay > do a quick and simple raycast
				Ray tempRay = new Ray(transform.position, transform.forward);
				UnityEngine.Physics.Raycast(tempRay, out target);
			}

			// any rigidbody attached?
			Transform t = target.transform;
			Rigidbody r = (t != null) ? t.GetComponentInParent<Rigidbody>() : null;
			if (r != null)
			{
				// Yes: remember rigid body and its relative position.
				// This relative position is what the script will try to maintain while moving the object
				activeBody = r;
				RigidbodyConstraints c = r.constraints;
				if (c == RigidbodyConstraints.None)
				{
					// body can move freely - apply forces at centre
					relBodyPoint   = Vector3.zero;
					relTargetPoint = transform.InverseTransformPoint(activeBody.transform.position);
					//relTargetOrientation = Quaternion.Inverse(transform.rotation) * activeBody.transform.rotation;
				}
				else
				{
					// body is restrained - apply forces on contact point
					relBodyPoint   = activeBody.transform.InverseTransformPoint(target.point);
					relTargetPoint = transform.InverseTransformPoint(target.point);
					//relTargetOrientation = Quaternion.Inverse(transform.rotation) * activeBody.transform.rotation;
				}
				// make target object weightless
				previousGravityFlag = r.useGravity;
				r.useGravity = false;
			}

			if (m_sound != null)
			{
				m_sound.Play();
				m_sound.loop = true;
			}
		}


		private void OnGrabEnd(InputAction.CallbackContext obj)
		{
			if (activeBody != null)
			{
				// trigger released holding a rigid body: turn gravity back on and cease control
				activeBody.useGravity = previousGravityFlag;
				activeBody = null;
			}
			if (m_sound != null)
			{
				m_sound.Stop();
			}
		}


		public void FixedUpdate()
		{
			// moving a rigid body: apply the right force to get that body to the new target position
			if (activeBody != null)
			{
				// set new target position
				PID.Setpoint    = transform.TransformPoint(relTargetPoint); // target point in world coordinates
				Vector3 bodyPos = activeBody.transform.TransformPoint(relBodyPoint); // body point in world coordinates
				// let PID controller work
				Vector3 force = PID.Process(bodyPos);
				activeBody.AddForceAtPosition(force, bodyPos, ForceMode.Force);
			}
		}


		private PointerRay   m_pointerRay;
		private AudioSource  m_sound;
		private Rigidbody    activeBody;
		private bool         previousGravityFlag;
		private Vector3      relTargetPoint, relBodyPoint;
		//private Quaternion   relTargetOrientation;
	}
}
