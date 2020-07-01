#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab.Physics
{
	[AddComponentMenu("Physics/Controller Grab")]
	[RequireComponent(typeof(Collider))]
	public class PhysicsGrab : MonoBehaviour
	{
		[Tooltip("Input action for grabbing")]
		public InputActionReference GrabAction;

		[Tooltip("Grab PID controller")]
		public PID_Controller3D PID;

		[Tooltip("Default rigidbody that can be grabbed without having a collider (e.g., the only main object in the scene)")]
		public InteractiveRigidbody DefaultRigidBody = null;


		public void Start()
		{
			if (GrabAction != null)
			{
				GrabAction.action.performed += OnGrabStart;
				GrabAction.action.canceled  += OnGrabEnd;
				GrabAction.action.Enable();
			}
			else
			{
				Debug.LogWarning("No action defined for grab");
				this.enabled = false;
			}
			m_candidate = DefaultRigidBody;
		}


		private void OnGrabStart(InputAction.CallbackContext obj)
		{
			m_activeBody = m_candidate != null ? m_candidate : DefaultRigidBody;
			if (m_activeBody != null)
			{
				m_activeBody.InvokeGrabStart(this.gameObject);
				m_localGrabPoint = m_activeBody.transform.InverseTransformPoint(this.transform.position);
			}
		}


		private void OnGrabEnd(InputAction.CallbackContext obj)
		{
			if (m_activeBody != null)
			{
				m_activeBody.InvokeGrabEnd(this.gameObject);
				m_activeBody = null;
			}
		}


		public void FixedUpdate()
		{
			if (m_activeBody != null)
			{
				// set new target position
				PID.Setpoint = transform.position;
				// let PID controller work
				Vector3 grabPoint = GetGrabPoint();
				Vector3 force = PID.Process(grabPoint);
				m_activeBody.Rigidbody.AddForceAtPosition(force, grabPoint, ForceMode.Force);
			}
		}


		public void OnTriggerEnter(Collider other)
		{
			m_candidate = other.GetComponentInParent<InteractiveRigidbody>();
			m_candidate?.InvokeHoverStart(this.gameObject);
		}


		public void OnTriggerExit(Collider other)
		{
			m_candidate?.InvokeHoverEnd(this.gameObject);
			m_candidate = null;
		}


		public Vector3 GetGrabPoint()
		{
			return m_activeBody.transform.TransformPoint(m_localGrabPoint);
		}


		public InteractiveRigidbody GetActiveBody()
		{
			return m_activeBody;
		}


		private Vector3              m_localGrabPoint;
		private InteractiveRigidbody m_candidate, m_activeBody;
	}
}