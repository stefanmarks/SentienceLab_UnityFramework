#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Component for moving a physical object by clicking and moving it.
	/// When clicked, the script will try to maintain the relative position of the rigid body using forces applied to its centre.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Physics Manipulator")]
	public class PhysicsManipulator : MonoBehaviour
	{
		[Tooltip("Input action for grabbing")]
		public InputActionProperty GrabAction;

		[Tooltip("Grab PID controller")]
		public PID_Controller3D PID;

		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the manipulator starts moving an object")]
			public UnityEvent<Rigidbody> OnManipulationStart;

			[Tooltip("Event fired when the manipulator finishes moving an object")]
			public UnityEvent<Rigidbody> OnManipulationEnd;
		}

		public Events events;


		void Start()
		{
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
			Ray tempRay = new Ray(transform.position, transform.forward);
			Physics.Raycast(tempRay, out target);

			// any rigidbody attached?
			Transform t = target.transform;
			Rigidbody rb = (t != null) ? t.GetComponentInParent<Rigidbody>() : null;
			if (rb != null)
			{
				// Yes: remember rigid body and its relative position.
				// This relative position is what the script will try to maintain while moving the object
				m_activeBody = rb;
				RigidbodyConstraints c = rb.constraints;
				if (c == RigidbodyConstraints.None)
				{
					// body can move freely - apply forces at centre
					m_relBodyPoint   = Vector3.zero;
					m_relTargetPoint = transform.InverseTransformPoint(m_activeBody.transform.position);
					m_relTargetOrientation = Quaternion.Inverse(transform.rotation) * m_activeBody.transform.rotation;
				}
				else
				{
					// body is restrained - apply forces on contact point
					m_relBodyPoint   = m_activeBody.transform.InverseTransformPoint(target.point);
					m_relTargetPoint = transform.InverseTransformPoint(target.point);
					m_relTargetOrientation = Quaternion.Inverse(transform.rotation) * m_activeBody.transform.rotation;
				}
				// make target object weightless
				m_previousGravityFlag = rb.useGravity;
				rb.useGravity = false;

				if (events != null)
				{
					events.OnManipulationStart.Invoke(m_activeBody);
				}
			}
			else
			{
				m_activeBody = null;
			}
		}


		public Rigidbody GetActiveBody()
		{
			return m_activeBody;
		}


		private void OnGrabEnd(InputAction.CallbackContext obj)
		{
			if (m_activeBody != null)
			{
				// trigger released holding a rigid body: turn gravity back on and cease control
				m_activeBody.useGravity = m_previousGravityFlag;

				if (events != null)
				{
					events.OnManipulationEnd.Invoke(m_activeBody);
				}

				m_activeBody = null;
			}
		}


		public void FixedUpdate()
		{
			// moving a rigid body: apply the right force to get that body to the new target position
			if (m_activeBody != null)
			{
				// set new target position
				PID.Setpoint    = transform.TransformPoint(m_relTargetPoint); // target point in world coordinates
				Vector3 bodyPos = m_activeBody.transform.TransformPoint(m_relBodyPoint); // body point in world coordinates
				// let PID controller work
				Vector3 force = PID.Process(bodyPos);
				m_activeBody.AddForceAtPosition(force, bodyPos, ForceMode.Force);
			}
		}


		private Rigidbody  m_activeBody;
		private bool       m_previousGravityFlag;
		private Vector3    m_relTargetPoint, m_relBodyPoint;
		private Quaternion m_relTargetOrientation;
	}
}
