#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// A component that enforces that the rotation of a game object/rigidbody 
	/// stays within certain limits.
	/// </summary>
	[AddComponentMenu("Constraints/Rotation Limits")]
	
	public class RotationLimits : MonoBehaviour
	{
		[Tooltip("Minimum Euler angles in degrees for each axis")]
		public Vector3 Minimum = Vector3.one * -360;

		[Tooltip("Maximum Euler angles in degrees for each axis")] 
		public Vector3 Maximum = Vector3.one * +360;

		public enum ERotationReference { Global, Local };
		[Tooltip("What rotation reference to use")]
		public ERotationReference Reference = ERotationReference.Global;

		[System.Serializable]
		public class OnLimitsEnforcedEvent : UnityEvent<RotationLimits> { }

		[Tooltip("Event fired when a position limit is enforced")]
		public OnLimitsEnforcedEvent OnLimitsEnforced;


		public void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
		}


		public void Update()
		{
			if (m_rigidbody == null) CheckLimits();
		}


		public void FixedUpdate()
		{
			if (m_rigidbody != null) CheckLimits();
		}


		public void CheckLimits()
		{
			Quaternion qrot = (m_rigidbody != null) ? m_rigidbody.rotation : this.transform.rotation;
			Vector3    velR = (m_rigidbody != null) ? m_rigidbody.angularVelocity : Vector3.zero;
			
			if (Reference == ERotationReference.Local)
			{
				// convert from global to local
				qrot = Quaternion.Inverse(transform.rotation) * qrot;
				velR = Quaternion.Inverse(transform.rotation) * velR;
			}

			// enforce limits
			bool hitLimit = false;
			Vector3 rot = qrot.eulerAngles;
			if (rot.x < Minimum.x) { rot.x = Minimum.x; velR.x = 0; hitLimit = true; }
			if (rot.y < Minimum.y) { rot.y = Minimum.y; velR.y = 0; hitLimit = true; }
			if (rot.z < Minimum.z) { rot.z = Minimum.z; velR.z = 0; hitLimit = true; }
			if (rot.x > Maximum.x) { rot.x = Maximum.x; velR.x = 0; hitLimit = true; }
			if (rot.y > Maximum.y) { rot.y = Maximum.y; velR.y = 0; hitLimit = true; }
			if (rot.z > Maximum.z) { rot.z = Maximum.z; velR.z = 0; hitLimit = true; }
			
			if (hitLimit)
			{
				qrot = Quaternion.Euler(rot);
				if (Reference == ERotationReference.Local)
				{
					// convert from local to global
					qrot = transform.rotation * qrot;
					velR = transform.rotation * velR;
				}

				if (m_rigidbody != null)
				{
					m_rigidbody.MoveRotation(qrot);
					m_rigidbody.velocity = velR;
				}
				else
				{
					transform.rotation = qrot;
				}

				OnLimitsEnforced.Invoke(this);
			}
		}


		protected Rigidbody m_rigidbody;
	}
}