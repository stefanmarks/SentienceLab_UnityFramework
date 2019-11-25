#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.Physics
{
	[AddComponentMenu("Physics/Interactive Rigidbody")]
	[RequireComponent(typeof(Rigidbody))]
	public class InteractiveRigidbody : MonoBehaviour
	{
		public bool CanTranslate = true;
		public bool CanRotate    = true;
		public bool CanScale     = false;

		public Rigidbody Rigidbody { get; private set; }

		public void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>();
		}
	}
}