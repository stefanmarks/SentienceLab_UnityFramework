#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SentienceLab
{
	/// <summary>
	/// Component to move/rotate an object forwards/sideways using input actions.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Movement Controller")]
	public class MovementController_InputSystem : MonoBehaviour 
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

		[Tooltip("Maximum speed for translation")]
		public float TranslationSpeed = 1.0f;

		[Tooltip("Smoothing of the translation\n(0: no smoothing, 1: intense smoothing")]
		[Range(0, 1)]
		public float TranslationSmoothing = 0.1f;

		[Header("Rotation")]

		[Tooltip("Input action for up/down rotation")]
		[FormerlySerializedAs("actionRotateX")]
		public InputActionProperty ActionRotateX;
		[Tooltip("Input action for left/right rotation")]
		[FormerlySerializedAs("actionRotateY")]
		public InputActionProperty ActionRotateY;

		[Tooltip("Maximum speed for rotation in degrees per second")]
		public float RotationSpeed = 45.0f;

		[Tooltip("Smoothing of the rotation\n(0: no smoothing, 1: intense smoothing")]
		[Range(0, 1)]
		public float RotationSmoothing = 0.1f;

		public bool      TranslationIgnoresPitch = true;
		public Transform RotationBasisNode;


		private const float SMOOTHING_FACTOR_POWER = 4;
		private const float SMOOTHING_FACTOR_MAX   = 0.9f;


		public void Start()
		{
			m_vecTranslate  = Vector3.zero;
			m_vecRotate     = Vector3.zero;
			m_inputCooldown = 10;  // don't immediately process input (e.g., initial mouse delta causing large jump)

			if (RotationBasisNode == null)
			{
				RotationBasisNode = this.transform;
			}

			if ((ActionMoveX   != null) && (ActionMoveX.action   != null)) { ActionMoveX.action.Enable();   }
			if ((ActionMoveY   != null) && (ActionMoveY.action   != null)) { ActionMoveY.action.Enable();   }
			if ((ActionMoveZ   != null) && (ActionMoveZ.action   != null)) { ActionMoveZ.action.Enable();   }
			if ((ActionRotateX != null) && (ActionRotateX.action != null)) { ActionRotateX.action.Enable(); }
			if ((ActionRotateY != null) && (ActionRotateY.action != null)) { ActionRotateY.action.Enable(); }
		}


		public void Update() 
		{
			if (m_inputCooldown > 0) { m_inputCooldown--; return; }

			// Rotation
			Vector3 vecR = Vector3.zero;
			vecR.x = ((ActionRotateX != null) && (ActionRotateX.action != null)) ? ActionRotateX.action.ReadValue<float>() : 0;
			vecR.y = ((ActionRotateY != null) && (ActionRotateY.action != null)) ? ActionRotateY.action.ReadValue<float>() : 0;
			float smoothing  = Mathf.Pow(RotationSmoothing * SMOOTHING_FACTOR_MAX, SMOOTHING_FACTOR_POWER);
			float lerpFactor = 1.0f - Mathf.Pow(smoothing, Time.deltaTime);
			m_vecRotate = Vector3.Lerp(m_vecRotate, vecR, lerpFactor);
			// rotate up/down (always absolute around X axis)
			transform.RotateAround(RotationBasisNode.position, RotationBasisNode.right, m_vecRotate.x * RotationSpeed * Time.deltaTime);
			// rotate left/right (always absolute around Y axis)
			transform.RotateAround(RotationBasisNode.position, Vector3.up, m_vecRotate.y * RotationSpeed * Time.deltaTime);

			Vector3 vecT = Vector3.zero;
			vecT.x = ((ActionMoveX != null) && (ActionMoveX.action != null)) ? ActionMoveX.action.ReadValue<float>() : 0;
			vecT.y = ((ActionMoveY != null) && (ActionMoveY.action != null)) ? ActionMoveY.action.ReadValue<float>() : 0;
			vecT.z = ((ActionMoveZ != null) && (ActionMoveZ.action != null)) ? ActionMoveZ.action.ReadValue<float>() : 0;
			smoothing  = Mathf.Pow(TranslationSmoothing * SMOOTHING_FACTOR_MAX, SMOOTHING_FACTOR_POWER);
			lerpFactor = 1.0f - Mathf.Pow(smoothing, Time.deltaTime);
			m_vecTranslate = Vector3.Lerp(m_vecTranslate, vecT, lerpFactor);

			// Translation
			// translate forward (Z)
			Vector3 v = RotationBasisNode.forward;
			if (TranslationIgnoresPitch) { v.y = 0; v.Normalize(); }
			transform.Translate(m_vecTranslate.z * Time.deltaTime * TranslationSpeed * v, Space.World);

			// translate upwards (Y)
			v = TranslationIgnoresPitch ? Vector3.up : RotationBasisNode.up;
			transform.Translate(m_vecTranslate.y * Time.deltaTime * TranslationSpeed * v, Space.World);
		
			// translate sideways (X)
			v = RotationBasisNode.right; 
			v.y = 0; // make sure, any roll has no effect
			v.Normalize();
			transform.Translate(m_vecTranslate.x * Time.deltaTime * TranslationSpeed * v, Space.World);
		}

		private Vector3 m_vecTranslate;
		private Vector3 m_vecRotate;
		private int     m_inputCooldown;
	}
}