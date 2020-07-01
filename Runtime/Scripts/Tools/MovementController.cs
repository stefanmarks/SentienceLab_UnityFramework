#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script to move an object forwards/sideways/etc.
/// </summary>
public class MovementController : MonoBehaviour 
{
	public InputActionReference MoveX;
	public InputActionReference MoveY;
	public InputActionReference MoveZ;
	public float  maxTranslationSpeed = 1.0f;
	public float  translationLerp     = 1.0f;

	public InputActionReference RotateX;
	public InputActionReference RotateY;
	public float  maxRotationSpeed    = 45.0f;
	public float  rotationLerp        = 1.0f;

	public bool      translationIgnoresPitch = true;
	public Transform rotationBasisNode;


	void Start()
	{
		vecTranslate = new Vector3();
		vecRotate    = new Vector3();
		vec          = new Vector3();

		if (MoveX   != null) MoveX.action.Enable();
		if (MoveY   != null) MoveY.action.Enable();
		if (MoveZ   != null) MoveZ.action.Enable();
		if (RotateX != null) RotateX.action.Enable();
		if (RotateY != null) RotateY.action.Enable();

		if (rotationBasisNode == null)
		{
			rotationBasisNode = this.transform;
		}
	}


	void Update() 
	{
		Vector3 vecR = Vector3.zero;
		vecR.x = (RotateX != null) ? RotateX.action.ReadValue<float>() : 0;
		vecR.y = (RotateY != null) ? RotateY.action.ReadValue<float>() : 0;
		vecRotate = Vector3.Lerp(vecRotate, vecR, rotationLerp);
		// rotate up/down (always absolute around X axis)
		transform.RotateAround(rotationBasisNode.position, rotationBasisNode.right, vecRotate.x * maxRotationSpeed * Time.deltaTime);
		// rotate left/right (always absolute around Y axis)
		transform.RotateAround(rotationBasisNode.position, Vector3.up, vecRotate.y * maxRotationSpeed * Time.deltaTime);

		Vector3 vecT = Vector3.zero;
		vecT.x = (MoveX != null) ? MoveX.action.ReadValue<float>() : 0;
		vecT.y = (MoveY != null) ? MoveY.action.ReadValue<float>() : 0;
		vecT.z = (MoveZ != null) ? MoveZ.action.ReadValue<float>() : 0;
		vecTranslate = Vector3.Lerp(vecTranslate, vecT, translationLerp);

		// calculate forward (Z) direction of camera
		vec = rotationBasisNode.forward;
		if (translationIgnoresPitch) { vec.y = 0; }
		vec.Normalize();
		// translate forward
		transform.Translate(vec * vecTranslate.z * maxTranslationSpeed * Time.deltaTime, Space.World);
		// calculate upwards (Y) direction of camera
		vec = rotationBasisNode.up; vec.Normalize();
		// translate upwards
		transform.Translate(vec * vecTranslate.y * maxTranslationSpeed * Time.deltaTime, Space.World);
		// calculate level sideways (X) direction of camera
		vec = rotationBasisNode.right; vec.y = 0; vec.Normalize();
		// translate forward
		transform.Translate(vec * vecTranslate.x * maxTranslationSpeed * Time.deltaTime, Space.World);
	}

	private Vector3  vecTranslate, vecRotate, vec;
}
