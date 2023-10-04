#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

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

		public InputActionProperty actionMoveX;
		public InputActionProperty actionMoveY;
		public InputActionProperty actionMoveZ;

		[Tooltip("Translation Force")]
		public float TranslationForce = 5.0f;

		[Header("Rotation")]
	
		public InputActionProperty actionRotateX;
		public InputActionProperty actionRotateY;

		[Tooltip("Rotation Torque")]
		public float RotationTorque = 1.0f;

		public bool      TranslationIgnoresPitch = true;
		public Transform RotationBasisNode;


		public void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
			if (RotationBasisNode == null)
			{
				RotationBasisNode = this.transform;
			}

			if (actionMoveX   != null) { actionMoveX.action.Enable(); }
			if (actionMoveY   != null) { actionMoveY.action.Enable(); }
			if (actionMoveZ   != null) { actionMoveZ.action.Enable(); }
			if (actionRotateX != null) { actionRotateX.action.Enable(); }
			if (actionRotateY != null) { actionRotateY.action.Enable(); }
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
			vecR.x = (actionRotateX != null) ? actionRotateX.action.ReadValue<float>() : 0;
			vecR.y = (actionRotateY != null) ? actionRotateY.action.ReadValue<float>() : 0;
			vecR  *= RotationTorque;
			// rotate up/down (relative X axis)
			m_rigidbody.AddRelativeTorque(Vector3.right * vecR.x);
			// rotate left/right (absolute Y axis)
			m_rigidbody.AddTorque(Vector3.up * vecR.y);

			// Translation
			Vector3 vecT = Vector3.zero;
			vecT.x = (actionMoveX != null) ? actionMoveX.action.ReadValue<float>() : 0;
			vecT.y = (actionMoveY != null) ? actionMoveY.action.ReadValue<float>() : 0;
			vecT.z = (actionMoveZ != null) ? actionMoveZ.action.ReadValue<float>() : 0;
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

		private Rigidbody m_rigidbody;
	}
}