#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Physics
{
	[AddComponentMenu("Physics/Controller Grab")]
	[RequireComponent(typeof(Collider))]
	public class PhysicsGrab : MonoBehaviour
	{
		[Tooltip("Name of the input that starts the grab action")]
		public string InputName;

		[Tooltip("Grab PID controller")]
		public PID_Controller3D PID;

		[Tooltip("Default rigidbody that can be grabbed without having a collider (e.g., the only main object in the scene)")]
		public InteractiveRigidbody DefaultRigidBody = null;


		public void Start()
		{
			m_handlerActive = InputHandler.Find(InputName);
			if (m_handlerActive == null)
			{
				Debug.LogWarning("Could not find input handler for '" + InputName + "'");
				this.enabled = false;
			}
			m_candidate = DefaultRigidBody;
		}


		public void Update()
		{
			if (m_handlerActive.IsActivated())
			{
				m_activeBody = m_candidate != null ? m_candidate : DefaultRigidBody;
				if (m_activeBody != null)
				{
					m_activeBody.InvokeGrabStart(this.gameObject);
					m_localGrabPoint = m_activeBody.transform.InverseTransformPoint(this.transform.position);
				}
			}
			else if (m_handlerActive.IsDeactivated())
			{
				if (m_activeBody != null)
				{
					m_activeBody.InvokeGrabEnd(this.gameObject);
					m_activeBody = null;
				}
			}
		}


		public void FixedUpdate()
		{
			if (m_handlerActive.IsActive() && (m_activeBody != null))
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
			if (m_candidate != null)
			{
				m_candidate.InvokeHoverStart(this.gameObject);
			}
		}


		public void OnTriggerExit(Collider other)
		{
			if (m_candidate != null)
			{
				m_candidate.InvokeHoverEnd(this.gameObject);
				m_candidate = null;
			}
		}


		public Vector3 GetGrabPoint()
		{
			return m_activeBody.transform.TransformPoint(m_localGrabPoint);
		}


		public InteractiveRigidbody GetActiveBody()
		{
			return m_activeBody;
		}


		private InputHandler         m_handlerActive;
		private Vector3              m_localGrabPoint;
		private InteractiveRigidbody m_candidate, m_activeBody;
	}
}