#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// A component that enforces that the scale of a game object 
	/// stays within certain limits.
	/// </summary>
	[AddComponentMenu("SentienceLab/Constraints/Scale Limits")]
	
	public class ScaleLimits : MonoBehaviour
	{
		[Tooltip("Minimum scale")]
		public float Minimum = 0.001f;

		[Tooltip("Maximum scale")]
		public float Maximum = float.PositiveInfinity;


		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when a scale limit is enforced")]
			public UnityEvent<ScaleLimits> OnLimitsEnforced;
		}

		public Events events;


		public void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
		}


		public void Update()
		{
			if (m_rigidbody == null)
			{
				CheckLimits(transform.localScale.x, true, true);
			}
		}


		public void FixedUpdate()
		{
			if (m_rigidbody != null)
			{
				CheckLimits(transform.localScale.x, true, true);
			}
		}


		public bool CheckLimits(float scale, bool _invokeCallbacks = false)
		{
			return CheckLimits(scale, _invokeCallbacks, false);
		}


		protected bool CheckLimits(float _scale, bool _invokeCallbacks, bool _applyToTransform)
		{
			bool hitLimit = false;
			if (_scale > Maximum) { _scale = Maximum; hitLimit = true; }
			if (_scale < Minimum) { _scale = Minimum; hitLimit = true; }

			if (hitLimit)
			{
				if (_applyToTransform)
				{
					transform.localScale = Vector3.one * _scale;
				}
				if (_invokeCallbacks)
				{
					if (events != null) events.OnLimitsEnforced.Invoke(this);
				}
			}
			
			return hitLimit;
		}


		protected Rigidbody m_rigidbody;
	}
}