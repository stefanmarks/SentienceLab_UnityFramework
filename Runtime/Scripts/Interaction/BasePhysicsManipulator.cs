#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Abstract base component for moving a physical object.
	/// When active, the script will try to maintain the relative position of the rigid body 
	/// using forces and torque applied to the centre or the grab point.
	/// </summary>
	///
	public abstract class BasePhysicsManipulator : MonoBehaviour
	{
		[Tooltip("Input action for grabbing")]
		public InputActionProperty GrabAction;

		[Tooltip("Grab PID controller")]
		public PID_Controller3D PID;

		[Tooltip("Turn off gravity for manipulated object")]
		public bool DisableGravityOnGrab = true;


		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the manipulator touches an object")]
			public UnityEvent<Rigidbody> OnTouchStart;

			[Tooltip("Event fired when the manipulator stops touching an object")]
			public UnityEvent<Rigidbody> OnTouchEnd;

			[Tooltip("Event fired when the manipulator grabs an object")]
			public UnityEvent<Rigidbody> OnGrabStart;

			[Tooltip("Event fired when the manipulator releases an object")]
			public UnityEvent<Rigidbody> OnGrabEnd;
		}

		public Events events;


		public virtual void Start()
		{
			if (GrabAction == null)
			{
				Debug.LogWarning("No action defined for grab");
				// this.enabled = false; // don't disable as this component can still be used for touch events
			}

			m_candidateBody = null;
			m_touchedBody   = null;
			m_activeBody    = null;
		}


		public void OnEnable()
		{
			if (GrabAction != null)
			{
				GrabAction.action.performed += OnGrabStart;
				GrabAction.action.canceled  += OnGrabEnd;
				GrabAction.action.Enable();
			}
		}


		public void OnDisable()
		{
			if (GrabAction != null)
			{
				GrabAction.action.performed -= OnGrabStart;
				GrabAction.action.canceled  -= OnGrabEnd;
			}
			// just in case we were manipulating or touching an object...
			EndGrab();
			SetCandidate(null, Vector3.zero);
		}


		/// <summary>
		/// Called by superclasses to set/clear a candidate and a world grab point.
		/// </summary>
		/// <param name="_candidate">potential rigidbody candidate or <c>null</c> if there is no candidate</param>
		/// <param name="_grabPoint">world coordinate of grab point</param>
		/// 
		protected void SetCandidate(Rigidbody _candidate, Vector3 _touchPoint)
		{
			if (!isActiveAndEnabled) return;

			m_candidateBody  = _candidate;
			m_candidateTouch = _touchPoint;

			// don't change candidate while manipulating an object
			if (!IsManipulatingRigidbody())
			{
				CheckCandidate();
			}
		}

		protected void CheckCandidate()
		{
			// no change > get out
			if (m_touchedBody == m_candidateBody) return;
			
			// did we touch another object before?
			if (m_touchedBody != null)
			{
				// yes > signal TouchEnd to rigidbody
				ManipulationInfo info = GetManipulationInfo(m_touchedBody);
				info.EndTouch(this);

				// fire TouchEnd for manipulator
				events?.OnTouchEnd.Invoke(m_touchedBody);
			}

			m_touchedBody = m_candidateBody;
			m_touchPoint  = m_candidateTouch;

			// are we touching an object now?
			if (m_touchedBody != null)
			{
				// yes > signal TouchStart to rigidbody
				ManipulationInfo info = GetManipulationInfo(m_touchedBody);
				info.StartTouch(this);

				// fire TouchStart for manipulator
				events?.OnTouchStart.Invoke(m_touchedBody);
			}
		}


		public Rigidbody GetActiveRigidbody()
		{
			return m_activeBody;
		}


		public Vector3 GetGrabPoint()
		{
			return m_activeBody.transform.TransformPoint(m_relTargetPoint);
		}


		public bool IsManipulatingRigidbody()
		{
			return m_activeBody != null;
		}


		protected void SetDefaultRigidbody(Rigidbody _default)
		{
			m_defaultBody = _default;
		}


		protected ManipulationInfo GetManipulationInfo(Rigidbody _rb)
		{
			if (!ms_manipulationInfo.TryGetValue(_rb, out ManipulationInfo info))
			{
				// this RB isn't being manipulated yet > add to the list
				info = new ManipulationInfo(_rb);
				ms_manipulationInfo.Add(_rb, info);
			}
			return info;
		}


		protected void OnGrabStart(InputAction.CallbackContext _)
		{
			StartGrab();
		}


		public void StartGrab()
		{
			if ((m_touchedBody != null) || (m_defaultBody != null))
			{
				if (m_touchedBody != null)
				{
					// we have touched an object > make active body
					m_activeBody = m_touchedBody;
				}
				else
				{
					// no candidate > fallback to default body:
					// need to also use current transform position as grab point
					m_activeBody = m_defaultBody;
					m_touchPoint = this.transform.position;
				}

				RigidbodyConstraints c = m_activeBody.constraints;
				if (c == RigidbodyConstraints.None)
				{
					// body can move freely - apply forces at centre
					m_relBodyPoint = Vector3.zero;
					m_relTargetPoint = transform.InverseTransformPoint(m_activeBody.transform.position);
					m_relTargetOrientation = Quaternion.Inverse(transform.rotation) * m_activeBody.transform.rotation;
				}
				else
				{
					// body is constrained - apply forces on contact point
					m_relBodyPoint = m_activeBody.transform.InverseTransformPoint(m_touchPoint);
					m_relTargetPoint = transform.InverseTransformPoint(m_touchPoint);
					m_relTargetOrientation = Quaternion.Inverse(transform.rotation) * m_activeBody.transform.rotation;
				}

				// signal GrabStart to rigidbody
				ManipulationInfo info = GetManipulationInfo(m_activeBody);
				info.StartGrab(this);

				// fire GrabStart events for manipulator
				events?.OnGrabStart.Invoke(m_activeBody);
			}
			else
			{
				m_activeBody = null;
			}
		}


		protected void OnGrabEnd(InputAction.CallbackContext _)
		{
			EndGrab();
		}


		public void EndGrab()
		{
			if (m_activeBody != null)
			{
				// signal GrabEnd to rigidbody
				ManipulationInfo info = GetManipulationInfo(m_activeBody);
				info.EndGrab(this);

				// fire GrabEnd events for manipulator
				events?.OnGrabEnd.Invoke(m_activeBody);

				m_activeBody = null;

				// in the meantime, another body might have become candidate
				CheckCandidate();
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


		//
		protected class ManipulationInfo
		{
			public ManipulationInfo(Rigidbody rigidbody) 
			{
				m_rigidbody     = rigidbody;
				m_interactiveRB = rigidbody.gameObject.GetComponent<InteractiveRigidbody>();

				m_touchingManipulators = new List<BasePhysicsManipulator>();
				m_grabbingManipulators = new List<BasePhysicsManipulator>();
			}

			public void StartTouch(BasePhysicsManipulator manipulator)
			{
				if (m_touchingManipulators.Count == 0)
				{
					// first manipulator touch > fire TouchStart event
					if (m_interactiveRB != null)
					{
						m_interactiveRB.InvokeTouchStart(manipulator.gameObject);
					}
				}

				m_touchingManipulators.Add(manipulator);
			}

			public void StartGrab(BasePhysicsManipulator manipulator)
			{
				if (m_grabbingManipulators.Count == 0)
				{
					// first manipulator grabs > save RB settings
					m_originalGravityFlag = m_rigidbody.useGravity;
					// fire GrabStart event
					if (m_interactiveRB != null)
					{
						m_interactiveRB.InvokeGrabStart(manipulator.gameObject);
					}
				}

				m_grabbingManipulators.Add(manipulator);

				if (manipulator.DisableGravityOnGrab)
				{
					m_rigidbody.useGravity = false;
				}
			}

			public void EndGrab(BasePhysicsManipulator manipulator)
			{
				m_grabbingManipulators.Remove(manipulator);

				if (m_grabbingManipulators.Count == 0)
				{
					// last manipulator lets go > restore RB settings
					m_rigidbody.useGravity = m_originalGravityFlag;
					// fire GrabEnd events
					if (m_interactiveRB != null)
					{
						m_interactiveRB.InvokeGrabEnd(manipulator.gameObject);
					}
				}
			}

			public void EndTouch(BasePhysicsManipulator manipulator)
			{
				m_touchingManipulators.Remove(manipulator);

				if (m_touchingManipulators.Count == 0)
				{
					// last touching manipulator > fire TouchEnd events
					if (m_interactiveRB != null) 
					{ 
						m_interactiveRB.InvokeTouchEnd(manipulator.gameObject); 
					}
				}
			}

			protected Rigidbody                    m_rigidbody;
			protected InteractiveRigidbody         m_interactiveRB;
			protected List<BasePhysicsManipulator> m_touchingManipulators;
			protected List<BasePhysicsManipulator> m_grabbingManipulators;

			protected bool                         m_originalGravityFlag;
		}

		static protected Dictionary<Rigidbody, ManipulationInfo> ms_manipulationInfo = new Dictionary<Rigidbody, ManipulationInfo>();

		protected Rigidbody  m_candidateBody,  m_touchedBody, m_activeBody,     m_defaultBody;
		protected Vector3    m_candidateTouch, m_touchPoint,  m_relTargetPoint, m_relBodyPoint;
		protected Quaternion m_relTargetOrientation;
	}
}
