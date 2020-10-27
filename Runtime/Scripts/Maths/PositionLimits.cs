#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// A component that enforces that the position of a game object/rigidbody 
	/// stays within certain limits.
	/// </summary>
	[AddComponentMenu("Constraints/Position Limits")]
	
	public class PositionLimits : MonoBehaviour
	{
		[Tooltip("Minimum position limit for each axis")]
		public Vector3 Minimum = Vector3.one * float.NegativeInfinity;

		[Tooltip("Maximum position limit for each axis")]
		public Vector3 Maximum = Vector3.one * float.PositiveInfinity;

		public enum EPositionReference { Global, Local };
		[Tooltip("What position reference to use")]
		public EPositionReference Reference = EPositionReference.Global;


		[System.Serializable]
		public class OnLimitsEnforcedEvent : UnityEvent<PositionLimits> { }

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
			Vector3 pos = (m_rigidbody != null) ? m_rigidbody.position : this.transform.position;
			Vector3 vel = (m_rigidbody != null) ? m_rigidbody.velocity : Vector3.zero;
			
			if (Reference == EPositionReference.Local)
			{
				// convert from global to local
				pos = transform.InverseTransformPoint(pos);
				vel = transform.InverseTransformDirection(vel);
			}
			
			// enforce limits
			bool hitLimit = false;
			if (pos.x < Minimum.x) { pos.x = Minimum.x; vel.x = 0; hitLimit = true; }
			if (pos.y < Minimum.y) { pos.y = Minimum.y; vel.y = 0; hitLimit = true; }
			if (pos.z < Minimum.z) { pos.z = Minimum.z; vel.z = 0; hitLimit = true; }
			if (pos.x > Maximum.x) { pos.x = Maximum.x; vel.x = 0; hitLimit = true; }
			if (pos.y > Maximum.y) { pos.y = Maximum.y; vel.y = 0; hitLimit = true; }
			if (pos.z > Maximum.z) { pos.z = Maximum.z; vel.z = 0; hitLimit = true; }
			
			if (hitLimit)
			{
				if (Reference == EPositionReference.Local)
				{
					// convert from local to global
					pos = transform.TransformPoint(pos);
					vel = transform.TransformDirection(vel);
				}

				if (m_rigidbody != null)
				{
					m_rigidbody.MovePosition(pos);
					m_rigidbody.velocity = vel;
				}
				else
				{
					transform.position = pos;
				}

				OnLimitsEnforced.Invoke(this);
			}
		}


		protected Rigidbody m_rigidbody;
	}
}