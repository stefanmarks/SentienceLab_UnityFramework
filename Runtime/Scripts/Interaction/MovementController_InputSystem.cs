#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// Script to move an object forwards/sideways
/// </summary>
public class MovementController_InputSystem : MonoBehaviour 
{
	[Header("Translation")]

	public InputActionProperty actionMoveX;
	public InputActionProperty actionMoveY;
	public InputActionProperty actionMoveZ;

	[Tooltip("Maximum speed for translation")]
	public float TranslationSpeed = 1.0f;

	[Tooltip("Smoothing of the translation\n(0: no smoothing, 1: intense smoothing")]
	[Range(0, 1)]
	public float TranslationSmoothing = 0.1f;

	[Header("Rotation")]
	
	public InputActionProperty actionRotateX;
	public InputActionProperty actionRotateY;

	[Tooltip("Maximum speed for rotation in degrees per second")]
	public float RotationSpeed = 45.0f;

	[Tooltip("Smoothing of the rotation\n(0: no smoothing, 1: intense smoothing")]
	[Range(0, 1)]
	public float RotationSmoothing = 0.1f;

	public bool      TranslationIgnoresPitch = true;
	public Transform RotationBasisNode;


	private const float SMOOTHING_FACTOR_POWER = 4;
	private const float SMOOTHING_FACTOR_MAX   = 0.9f;


	void Start()
	{
		m_vecTranslate = Vector3.zero;
		m_vecRotate = Vector3.zero;

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


	void Update() 
	{
		// Rotation
		Vector3 vecR = Vector3.zero;
		vecR.x = (actionRotateX != null) ? actionRotateX.action.ReadValue<float>() : 0;
		vecR.y = (actionRotateY != null) ? actionRotateY.action.ReadValue<float>() : 0;
		float smoothing  = Mathf.Pow(RotationSmoothing * SMOOTHING_FACTOR_MAX, SMOOTHING_FACTOR_POWER);
		float lerpFactor = 1.0f - Mathf.Pow(smoothing, Time.deltaTime);
		m_vecRotate = Vector3.Lerp(m_vecRotate, vecR, lerpFactor);
		// rotate up/down (always absolute around X axis)
		transform.RotateAround(RotationBasisNode.position, RotationBasisNode.right, m_vecRotate.x * RotationSpeed * Time.deltaTime);
		// rotate left/right (always absolute around Y axis)
		transform.RotateAround(RotationBasisNode.position, Vector3.up, m_vecRotate.y * RotationSpeed * Time.deltaTime);

		Vector3 vecT = Vector3.zero;
		vecT.x = (actionMoveX != null) ? actionMoveX.action.ReadValue<float>() : 0;
		vecT.y = (actionMoveY != null) ? actionMoveY.action.ReadValue<float>() : 0;
		vecT.z = (actionMoveZ != null) ? actionMoveZ.action.ReadValue<float>() : 0;
		smoothing  = Mathf.Pow(TranslationSmoothing * SMOOTHING_FACTOR_MAX, SMOOTHING_FACTOR_POWER);
		lerpFactor = 1.0f - Mathf.Pow(smoothing, Time.deltaTime);
		m_vecTranslate = Vector3.Lerp(m_vecTranslate, vecT, lerpFactor);

		// Translation
		// translate forward (Z)
		Vector3 v = RotationBasisNode.forward;
		if (TranslationIgnoresPitch) { v.y = 0; }
		v.Normalize();
		transform.Translate(v * m_vecTranslate.z * TranslationSpeed * Time.deltaTime, Space.World);
		
		// translate upwards (Y)
		v = RotationBasisNode.up; 
		v.Normalize();
		transform.Translate(v * m_vecTranslate.y * TranslationSpeed * Time.deltaTime, Space.World);
		
		// translate sideways (X)
		v = RotationBasisNode.right; 
		v.y = 0; // make sure, any roll has no effect
		v.Normalize();
		transform.Translate(v * m_vecTranslate.x * TranslationSpeed * Time.deltaTime, Space.World);
	}

	private Vector3 m_vecTranslate;
	private Vector3 m_vecRotate;
}
