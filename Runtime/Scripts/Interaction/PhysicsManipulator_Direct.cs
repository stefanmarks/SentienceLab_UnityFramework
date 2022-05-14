#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for moving a physical object directly by clicking and moving it.
	/// When clicked, the script will try to maintain the relative position of the rigid body using forces applied to its centre.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Direct Physics Manipulator")]
	[RequireComponent(typeof(Collider))]
	public class PhysicsManipulator_Direct : BasePhysicsManipulator
	{
		[Tooltip("Default rigidbody that is manipulated when not touching any object (e.g., the only main object in the scene)")]
		public Rigidbody DefaultRigidBody = null;

		public override void Start()
		{
			base.Start();
			SetDefaultRigidbody(DefaultRigidBody);
		}

		public void OnTriggerEnter(Collider other)
		{
			var rb = other.GetComponentInParent<Rigidbody>();
			SetCandidate(rb, other.transform.position);
		}


		public void OnTriggerExit(Collider other)
		{
			SetCandidate(null, Vector3.zero);
		}
	}
}