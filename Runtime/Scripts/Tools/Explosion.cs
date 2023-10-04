using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Script that applies an impulse to objects within a certain radius.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Effects/Explosion")]

	public class Explosion : MonoBehaviour 
	{
		[Tooltip("Radius of objects to apply the impulse to")]
		public float Radius  = 10;

		[Tooltip("Impulse force to apply within the given radius")]
		public float Impulse = 100;


		public void TriggerExplosion()
		{
			// find all objects in the radius
			var otherObjects = Physics.OverlapSphere(this.transform.position, Radius);
			foreach (var collider in otherObjects)
			{
				Rigidbody rb = collider.GetComponent<Rigidbody>();
				if (rb != null)
				{
					// where and how far away is the other object?
					Vector3 delta    = rb.position - this.transform.position;
					float   distance = delta.magnitude;
					// scale impulse magnitude accordingly
					float   relativeImpulseMagnitude = Mathf.Lerp(Impulse, 0, Mathf.Min(1, distance / Radius));
					Vector3 relativeImpulse = relativeImpulseMagnitude * delta.normalized;
					// apply impulse
					rb.AddForce(relativeImpulse, ForceMode.Impulse);
				}
			}
		}
	}
}