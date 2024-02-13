#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for moving a physical object via a ray by clicking and moving it.
	/// When active, the script will try to maintain the relative position of the rigid body using forces applied to its centre.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Ray Physics Manipulator")]
	public class PhysicsManipulator_Ray : BasePhysicsManipulator
	{
		public void Update()
		{
			// is there any rigid body where the ray points at?
			RaycastHit target;
			Ray        tempRay = new Ray(transform.position, transform.forward);
			Physics.Raycast(tempRay, out target);

			// any rigidbody attached?
			Transform t = target.transform;
			Rigidbody rb = (t != null) ? t.GetComponentInParent<Rigidbody>() : null;
			SetCandidate(rb, target.point);
		}
	}
}
