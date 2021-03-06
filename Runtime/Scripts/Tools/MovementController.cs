﻿#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using UnityEngine;

/// <summary>
/// Script to move an object forwards/sideways
/// </summary>
public class MovementController : MonoBehaviour 
{
	public string actionMoveX         = "moveX";
	public string actionMoveY         = "moveY";
	public string actionMoveZ         = "moveZ";
	public float  maxTranslationSpeed = 1.0f;
	public float  translationLerp     = 1.0f;

	public string actionRotateX       = "rotateX";
	public string actionRotateY       = "rotateY";
	public float  maxRotationSpeed    = 45.0f;
	public float  rotationLerp        = 1.0f;

	public bool      translationIgnoresPitch = true;
	public Transform rotationBasisNode;


	void Start()
	{
		handlerMoveX   = InputHandler.Find(actionMoveX);
		handlerMoveY   = InputHandler.Find(actionMoveY);
		handlerMoveZ   = InputHandler.Find(actionMoveZ);
		handlerRotateX = InputHandler.Find(actionRotateX);
		handlerRotateY = InputHandler.Find(actionRotateY);
		vecTranslate   = new Vector3();
		vecRotate      = new Vector3();
		vec            = new Vector3();

		if (rotationBasisNode == null)
		{
			rotationBasisNode = this.transform;
		}
	}


	void Update() 
	{
		Vector3 vecR = Vector3.zero;
		vecR.x = (handlerRotateX != null) ? handlerRotateX.GetValue() : 0;
		vecR.y = (handlerRotateY != null) ? handlerRotateY.GetValue() : 0;
		vecRotate = Vector3.Lerp(vecRotate, vecR, rotationLerp);
		// rotate up/down (always absolute around X axis)
		transform.RotateAround(rotationBasisNode.position, rotationBasisNode.right, vecRotate.x * maxRotationSpeed * Time.deltaTime);
		// rotate left/right (always absolute around Y axis)
		transform.RotateAround(rotationBasisNode.position, Vector3.up, vecRotate.y * maxRotationSpeed * Time.deltaTime);

		Vector3 vecT = Vector3.zero;
		vecT.x = (handlerMoveX != null) ? handlerMoveX.GetValue() : 0;
		vecT.y = (handlerMoveY != null) ? handlerMoveY.GetValue() : 0;
		vecT.z = (handlerMoveZ != null) ? handlerMoveZ.GetValue() : 0;
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

	private InputHandler handlerMoveX, handlerMoveY, handlerMoveZ;  // input handlers for moving
	private InputHandler handlerRotateX, handlerRotateY;            // input handlers for rotating
	private Vector3      vecTranslate, vecRotate, vec;
}
