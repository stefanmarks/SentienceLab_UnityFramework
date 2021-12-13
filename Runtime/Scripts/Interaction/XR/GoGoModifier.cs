#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for scaling MoCap positional data based on the distance to another object.
	/// Applied to a hand controller, this is also known asn the Go-Go technique.
	/// 
	/// Poupyrev, Ivan, Mark Billinghurst, Suzanne Weghorst, and Tadao Ichikawa.
	/// "The Go-Go Interaction Technique: Non-Linear Mapping for Direct Manipulation in VR." 
	/// In Proceedings of the 9th Annual ACM Symposium on User Interface Software and Technology, p79–80. 
	/// UIST '96. New York, NY, USA: ACM, 1996. https://doi.org/10.1145/237091.237102.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("SentienceLab/Interaction/XR/Go-Go Modifier")]

	public class GoGoModifier : MonoBehaviour
	{
		[Tooltip("Transform to measure the relative distance to")]
		public Transform centreObject;

		[Tooltip("Y-Offset to apply to the centre object (distance between head and arms)")]
		public float centreObjectY_Offset = 0.3f;

		[Tooltip("Scale factor curve based on distance of MoCap object to the centre object.")]
		public AnimationCurve curve = AnimationCurve.Linear(0, 1, 2, 1);

		[Tooltip("Factor to scale the Y axis influence (0=none)")]
		[Range(0,1)]
		public float yAxisInfluence = 1.0f;


		public void Start()
		{
			curve.preWrapMode  = WrapMode.Clamp;
			curve.postWrapMode = WrapMode.Clamp;
		}


		public void LateUpdate()
		{
			if (!enabled) return;

			// get parent position
			Vector3 pos = transform.parent.position;

			// build relative distance to centre object (- head/arm offset)
			Vector3 offset = centreObject.position;
			offset.y -= centreObjectY_Offset;
			pos -= offset;

			// calculate distance and GoGo-factor
			Vector3 distVec = pos;
			float scaleFactor = curve.Evaluate(distVec.magnitude);

			// scale object position
			pos.x *= scaleFactor;
			pos.y *= 1 + ((scaleFactor-1) * yAxisInfluence);
			pos.z *= scaleFactor;

			// turn back to absolute coordinate and apply
			pos += offset;
			transform.position = pos;
		}
	}
}
