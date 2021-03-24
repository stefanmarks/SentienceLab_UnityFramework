#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Data;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for controlling a laser-like ray to point at objects in the scene.
	/// This component can be queried as to what it is pointing at.
	/// </summary>
	///
	[AddComponentMenu("Sentience Lab/Pointer Ray")]
	[RequireComponent(typeof(LineRenderer))]
	[RequireComponent(typeof(Parameter_Boolean))]

	public class PointerRay : MonoBehaviour
	{
		[Tooltip("Parameter that activates the ray\n(None: ray is permanently enabled)")]
		public Parameter_Boolean activationParameter;

		[Tooltip("Maximum range of the ray")]
		public float rayRange = 100.0f;

		[Tooltip("List of tags that the pointer reacts to (e.g., 'floor')")]
		public string[] tagList = { };

		[Tooltip("List of colliders to check for inside-out collisions")]
		public Collider[] checkInsideColliders = { };

		[Tooltip("Object to render at the point where the ray meets another game object (optional)")]
		public Transform activeEndPoint = null;

		[Tooltip("(Optional) Parameter for relative ray direction")]
		public Parameter_Vector3 rayDirection = null;


		/// <summary>
		/// Interface to implement for objects that need to react to the pointer ray entering/exiting their colliders.
		/// </summary>
		///
		public interface IPointerRayTarget
		{
			void OnPointerEnter(PointerRay _ray);
			void OnPointerExit(PointerRay _ray);
		}


		void Start()
		{
			if (activationParameter == null)
			{
				activationParameter = GetComponent<Parameter_Boolean>();
			}
			m_rayEnabled = false;
			m_line = GetComponent<LineRenderer>();
			m_line.positionCount = 2;
			m_line.useWorldSpace = true;
			m_overrideTarget = false;
			m_activeTarget = null;
		}


		void LateUpdate()
		{
			// assume nothing is hit at first
			m_rayTarget.distance = 0;

			if (activationParameter != null)
			{
				m_rayEnabled = activationParameter.Value;
			}
			else
			{
				m_rayEnabled = true;
			}

			// change in enabled flag
			if (m_line.enabled != m_rayEnabled)
			{
				m_line.enabled = m_rayEnabled;
				if ((activeEndPoint != null) && !m_rayEnabled)
				{
					activeEndPoint.gameObject.SetActive(false);
				}
			}

			if (m_rayEnabled)
			{
				// construct ray
				Vector3 forward = (rayDirection == null) ? Vector3.forward : rayDirection.Value;
				forward = transform.TransformDirection(forward); // relative forward to "world forward"
				m_ray = new Ray(transform.position, forward);
				Vector3 end = m_ray.origin + m_ray.direction * rayRange;
				m_line.SetPosition(0, m_ray.origin);
				Debug.DrawLine(m_ray.origin, end, Color.red);

				bool hit;
				if (!m_overrideTarget)
				{
					// do raycast
					hit = UnityEngine.Physics.Raycast(m_ray, out m_rayTarget, rayRange);

					// test tags
					if (hit && (tagList.Length > 0))
					{
						hit = false;
						foreach (string tag in tagList)
						{
							if (m_rayTarget.transform.CompareTag(tag))
							{
								hit = true;
								break;
							}
						}
						if (!hit)
						{
							// tag test negative > reset raycast structure
							UnityEngine.Physics.Raycast(m_ray, out m_rayTarget, 0);
						}
					}

					// are there colliders to check for inside-collisions?
					if (checkInsideColliders.Length > 0)
					{
						// checking inside colliders: reverse ray
						Ray reverse = new Ray(m_ray.origin + m_ray.direction * rayRange, -m_ray.direction);
						float minDistance = hit ? m_rayTarget.distance : rayRange;
						foreach (Collider c in checkInsideColliders)
						{
							RaycastHit hitReverse;
							if (c.Raycast(reverse, out hitReverse, rayRange))
							{
								if (rayRange - hitReverse.distance < minDistance)
								{
									m_rayTarget = hitReverse;
									minDistance = rayRange - hitReverse.distance;
									hit = true;
								}
							}
						}
					}
				}
				else
				{
					UnityEngine.Physics.Raycast(m_ray, out m_rayTarget, 0); // reset structure
					m_rayTarget.point = m_overridePoint;        // override point
					hit = true;
				}

				if (hit)
				{
					// hit something > draw ray to there and render end point object
					m_line.SetPosition(1, m_rayTarget.point);
					if (activeEndPoint != null)
					{
						activeEndPoint.position = m_rayTarget.point;
						activeEndPoint.gameObject.SetActive(true);
					}
				}
				else
				{
					// hit nothing > draw ray to end and disable end point object
					m_line.SetPosition(1, end);
					if (activeEndPoint != null)
					{
						activeEndPoint.gameObject.SetActive(false);
					}
				}
			}

			HandleEvents();
		}


		/// <summary>
		/// Handles events like OnEnter/OnExit if the object that the ray points at
		/// has implemented the IPointerRayTarget interface
		/// </summary>
		/// 
		private void HandleEvents()
		{
			IPointerRayTarget currentTarget = null;
			if (m_rayTarget.distance > 0 && (m_rayTarget.transform != null))
			{
				currentTarget = m_rayTarget.collider.gameObject.GetComponent<IPointerRayTarget>();
			}
			if (currentTarget != m_activeTarget)
			{
				if (m_activeTarget != null) m_activeTarget.OnPointerExit(this);
				m_activeTarget = currentTarget;
				if (m_activeTarget != null) m_activeTarget.OnPointerEnter(this);
			}
		}


		/// <summary>
		/// Returns the current ray.
		/// </summary>
		/// <returns>the current ray</returns>
		/// 
		public Ray GetRay()
		{
			return m_ray;
		}


		/// <summary>
		/// Returns the current target of the ray.
		/// </summary>
		/// <returns>the last raycastHit result</returns>
		/// 
		public RaycastHit GetRayTarget()
		{
			return m_rayTarget;
		}


		/// <summary>
		/// Checks whether the ray is enabled or not.
		/// </summary>
		/// <returns><c>true</c> when the ray is enabled</returns>
		/// 
		public bool IsEnabled()
		{
			return m_rayEnabled;
		}


		/// <summary>
		/// Sets an override target of the ray.
		/// </summary>
		/// 
		public void OverrideRayTarget(Vector3 position)
		{
			m_overrideTarget = true;
			m_overridePoint  = position;
		}


		/// <summary>
		/// Resets the override target of the ray.
		/// </summary>
		/// 
		public void ResetOverrideRayTarget()
		{
			m_overrideTarget = false;
			m_overridePoint  = Vector3.zero;
		}
		

		private bool              m_rayEnabled;
		private LineRenderer      m_line;
		private Ray               m_ray;
		private RaycastHit        m_rayTarget;
		private bool              m_overrideTarget;
		private Vector3           m_overridePoint;
		private IPointerRayTarget m_activeTarget;
	}
}
