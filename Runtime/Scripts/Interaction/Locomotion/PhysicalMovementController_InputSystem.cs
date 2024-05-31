#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SentienceLab
{
	/// <summary>
	/// Component to move/rotate an object forwards/sideways using input actions and physics.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Physical Movement Controller")]
	[RequireComponent(typeof(Rigidbody))]

	public class PhysicalMovementController_InputSystem : MonoBehaviour 
	{
		[Header("Translation")]

		[Tooltip("Input action for left/right movement")]
		[FormerlySerializedAs("actionMoveX")]
		public InputActionProperty ActionMoveX;
		[Tooltip("Input action for up/down movement")]
		[FormerlySerializedAs("actionMoveY")]
		public InputActionProperty ActionMoveY;
		[Tooltip("Input action for forwards/backwards movement")]
		[FormerlySerializedAs("actionMoveZ")]
		public InputActionProperty ActionMoveZ;

		[Tooltip("Translation Force")]
		public float TranslationForce = 5.0f;

		[Header("Rotation")]
	
		[Tooltip("Input action for up/down rotation")]
		[FormerlySerializedAs("actionRotateX")]
		public InputActionProperty ActionRotateX;
		[Tooltip("Input action for left/right rotation")]
		[FormerlySerializedAs("actionRotateY")]
		public InputActionProperty ActionRotateY;

		[Tooltip("Rotation Torque")]
		public float RotationTorque = 1.0f;

		[Tooltip("Moving forward/backwards ignores the up/down rotation")]
		public bool  TranslationIgnoresPitch = true;

		[Tooltip("Transform for determining movement directions (None: this object itself)")]
		public Transform RotationBasisNode;

		[Header("Jumping")]

		[Tooltip("Input action to jump")]
		[FormerlySerializedAs("actionJump")]
		public InputActionProperty ActionJump;

		[Tooltip("Impulse to apply when jumping")]
		public float JumpImpulse = 5;

		[Tooltip("Drag while being in the air")]
		public float DragInAir = 0.1f;
		
		[Tooltip("Tags of colliders to determine whether the feet are on the ground or not (for jumping)")]
		[TagSelector]
		public string[] GroundTagNames = { };

		[System.Serializable]
		public struct Events
		{
			[Tooltip("Event fired when jump has started")]
			public UnityEvent OnJumpStarted;

			[Tooltip("Event fired when losing contact with the ground")]
			public UnityEvent OnLostGroundContact;

			[Tooltip("Event fired when regaining contact with the ground")]
			public UnityEvent OnMadeGroundContact;
		}
		public Events events;


		public void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
			if (RotationBasisNode == null)
			{
				RotationBasisNode = this.transform;
			}

			m_groundColliders = new List<Collider>();
			m_onGround        = true; // let's assume we start on the ground

			if ((ActionMoveX   != null) && (ActionMoveX.action   != null)) { ActionMoveX.action.Enable(); }
			if ((ActionMoveY   != null) && (ActionMoveY.action   != null)) { ActionMoveY.action.Enable(); }
			if ((ActionMoveZ   != null) && (ActionMoveZ.action   != null)) { ActionMoveZ.action.Enable(); }
			if ((ActionRotateX != null) && (ActionRotateX.action != null)) { ActionRotateX.action.Enable(); }
			if ((ActionRotateY != null) && (ActionRotateY.action != null)) { ActionRotateY.action.Enable(); }
			if ((ActionJump    != null) && (ActionJump.action    != null)) { ActionJump.action.Enable(); ActionJump.action.performed += delegate { Jump(); };   }
		}


		public void Jump()
		{
			if (m_onGround)
			{
				m_rigidbody.AddForce(Vector3.up * JumpImpulse, ForceMode.Impulse);
				events.OnJumpStarted.Invoke();
			}
		}


		public bool IsOnGround()
		{
			return m_onGround;
		}


		public void SetTranslationForce(float _force)
		{
			TranslationForce = _force;
		}


		public void SetRotationTorque(float _torque)
		{
			RotationTorque = _torque;
		}


		public void FixedUpdate() 
		{
			// Rotation
			Vector3 vecR = Vector3.zero;
			vecR.x = ((ActionRotateX != null) && (ActionRotateX.action != null)) ? ActionRotateX.action.ReadValue<float>() : 0;
			vecR.y = ((ActionRotateY != null) && (ActionRotateY.action != null)) ? ActionRotateY.action.ReadValue<float>() : 0;
			vecR  *= RotationTorque;
			// rotate up/down (relative X axis)
			m_rigidbody.AddRelativeTorque(Vector3.right * vecR.x);
			// rotate left/right (absolute Y axis)
			m_rigidbody.AddTorque(Vector3.up * vecR.y);

			// Translation
			Vector3 vecT = Vector3.zero;
			vecT.x = ((ActionMoveX != null) && (ActionMoveX.action != null)) ? ActionMoveX.action.ReadValue<float>() : 0;
			vecT.y = ((ActionMoveY != null) && (ActionMoveY.action != null)) ? ActionMoveY.action.ReadValue<float>() : 0;
			vecT.z = ((ActionMoveZ != null) && (ActionMoveZ.action != null)) ? ActionMoveZ.action.ReadValue<float>() : 0;
			vecT *= TranslationForce;
			// translate forward (Z)
			Vector3 v = RotationBasisNode.forward;
			if (TranslationIgnoresPitch) { v.y = 0; v.Normalize(); }
			m_rigidbody.AddForce(v * vecT.z);
			
			// translate upwards (Y)
			v = TranslationIgnoresPitch ? Vector3.up : RotationBasisNode.up;
			m_rigidbody.AddForce(v * vecT.y);

			// translate sideways (X)
			v = RotationBasisNode.right;
			v.y = 0; // make sure, any roll has no effect
			v.Normalize();
			m_rigidbody.AddForce(v * vecT.x);
		}


		public void OnCollisionEnter(Collision _collision)
		{
			if (TagMatches(_collision.collider))
			{
				// check if the lowest contact point is horizontal-ish (to avoid walls being considered ground)
				for (int i=0 ; i < _collision.contactCount ; i++)
				{
					ContactPoint cp = _collision.GetContact(i);
					// Debug.Log($"Contact {cp.point}/{cp.normal}");
					if (Mathf.Abs(cp.normal.y) > 0.8f)
					{
						// a normal "vertical enough" > consider this collider ground
						m_groundColliders.Add(_collision.collider);
					}
				}
				if (!m_onGround && m_groundColliders.Count > 0)
				{
					m_onGround       = true;
					m_rigidbody.drag = m_groundDrag; // restore gound drag
					events.OnMadeGroundContact.Invoke();
				}
			}
		}


		public void OnCollisionExit(Collision _collision)
		{
			if (m_onGround && TagMatches(_collision.collider))
			{
				m_groundColliders.Remove(_collision.collider);
				if (m_groundColliders.Count == 0)
				{
					m_onGround       = false;
					m_groundDrag     = m_rigidbody.drag;
					m_rigidbody.drag = DragInAir;
					events.OnLostGroundContact.Invoke();
				}
			}
		}


		protected bool TagMatches(Collider _other)
		{
			bool matches = true;

			if ((GroundTagNames != null) && (GroundTagNames.Length > 0))
			{
				// tag names are given > check the list
				matches = false;
				foreach (var tag in GroundTagNames)
				{
					if (_other.CompareTag(tag))
					{
						matches = true;
						break;
					}
				}
			}

			return matches;
		}


		private Rigidbody m_rigidbody;
		private List<Collider> m_groundColliders;
		private bool           m_onGround;
		private float          m_groundDrag;
	}
}