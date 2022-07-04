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

		[Tooltip("Layer mask of objects to consider for manipulation")]
		public LayerMask LayerMask = Physics.AllLayers;


		public override void Start()
		{
			base.Start();
			SetDefaultRigidbody(DefaultRigidBody);
		}


		public void OnTriggerEnter(Collider _other)
		{
			if (LayerMaskMatches(_other))
			{
				var rb = _other.GetComponentInParent<Rigidbody>();
				SetCandidate(rb, this.transform.position);
			}
		}


		public void OnTriggerStay(Collider _other)
		{
			if (LayerMaskMatches(_other))
			{
				var rb = _other.GetComponentInParent<Rigidbody>();
				SetCandidate(rb, this.transform.position);
			}
		}


		public void OnTriggerExit(Collider _other)
		{
			if (LayerMaskMatches(_other))
			{
				SetCandidate(null, Vector3.zero);
			}
		}
		
		
		protected bool LayerMaskMatches(Collider _other)
		{
			return (1 << (_other.gameObject.layer) & LayerMask.value) != 0;
		}
	}
}