#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// This script can be attached to any game object with a camera component.
	/// When you click the mouse on the camera's render window,
	/// it will apply a configurable force to any rigidbody being clicked.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Apply Force at Cursor")]
	[RequireComponent(typeof(Camera))]
	public class ApplyForceAtCursor : MonoBehaviour
	{
		public InputActionProperty ApplyPushForce;
		public InputActionProperty ApplyPullForce;
		public InputActionProperty ForceScreenPosition;

		[Tooltip("How much force is applied at the clicked point")]
		public float maxForce = 1;


		public void Start()
		{
			// what camera am I attached to?
			m_camera = GetComponent<Camera>();

			if ((ApplyPushForce != null) && (ApplyPushForce.action != null))
			{
				ApplyPushForce.action.performed += delegate { m_forceDirection += 1; };
				ApplyPushForce.action.canceled += delegate { m_forceDirection -= 1; };
				ApplyPushForce.action.Enable();
			}
			if ((ApplyPullForce != null) && (ApplyPullForce.action != null))
			{
				ApplyPullForce.action.performed += delegate { m_forceDirection -= 1; };
				ApplyPullForce.action.canceled += delegate { m_forceDirection += 1; };
				ApplyPullForce.action.Enable();
			}
			if ((ForceScreenPosition != null) && (ForceScreenPosition.action != null))
			{
				ForceScreenPosition.action.Enable();
			}
		}

		public void FixedUpdate()
		{
			// is the push/pull action active?
			if (m_forceDirection != 0)
			{
				// default click position: center of screen
				Vector2 screenPosition = new Vector2(0.5f, 0.5f);
				if (ForceScreenPosition != null)
				{
					screenPosition = ForceScreenPosition.action.ReadValue<Vector2>();
				}
				// construct a ray through the camera centre and the clicked point
				Ray ray = m_camera.ScreenPointToRay(screenPosition);
				// shoot that ray through the point on the camera window into the scene
				bool hitSomething = Physics.Raycast(
					ray,
					out RaycastHit hit,
					float.PositiveInfinity,
					Physics.DefaultRaycastLayers,
					QueryTriggerInteraction.Ignore);
				// did we hit something, and is it a physical body?
				if (hitSomething && (hit.rigidbody != null))
				{
					// apply a force to the rigibody
					hit.rigidbody.AddForceAtPosition(ray.direction * maxForce * m_forceDirection, hit.point);
				}
			}
		}

		private Camera m_camera;
		private float  m_forceDirection;
	}
}