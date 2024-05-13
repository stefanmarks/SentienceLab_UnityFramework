#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Component for moving a physical object via mouse or finger by clicking and moving it.
	/// When clicked, the component will try to maintain the relative position of the rigid body using forces.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Pointer Physics Manipulator")]
	[RequireComponent(typeof(Camera))]
	public class PhysicsManipulator_Pointer : BasePhysicsManipulator
	{
		[Tooltip("Pointer Position")]
		public InputActionProperty PointerPosition;

		[Tooltip("Maximum range of the pointer manipulator")]
		public float Range = float.PositiveInfinity;

		
		public override void Start()
		{
			base.Start();

			m_camera     = GetComponent<Camera>();
			m_isGrabbing = false;

			if (PointerPosition != null) { PointerPosition.action.Enable(); }
		}


		public void Update()
		{
			// construct ray through pointer
			Vector3 pointerPos = Vector3.zero;
			pointerPos.x = PointerPosition.action.ReadValue<Vector2>().x;
			pointerPos.y = PointerPosition.action.ReadValue<Vector2>().y;

			RaycastHit target;
			Ray        tempRay = m_camera.ScreenPointToRay(pointerPos);

			// is there any rigidbody where the ray points at?
			Physics.Raycast(tempRay, out target, Range);
			Debug.Log(tempRay);

			// any rigidbody attached?
			Transform t  = target.transform;
			Rigidbody rb = (t != null) ? t.GetComponentInParent<Rigidbody>() : null;
			SetCandidate(rb, target.point);

			if (!m_isGrabbing && IsManipulatingRigidbody())
			{
				// grab started: get distance
				m_grabDistance = (transform.position - GetGrabPoint()).magnitude;
				m_isGrabbing   = true;
			}
			
			if (m_isGrabbing)
			{
				if (IsManipulatingRigidbody())
				{
					// move object to pointer position at start grab distance
					SetGrabPoint(transform.position + m_grabDistance * tempRay.direction);
				}
				else
				{
					// let go of object
					m_isGrabbing = false;
				}
			}
		}

		protected Camera m_camera;
		protected bool   m_isGrabbing;
		protected float  m_grabDistance;
	}
}
