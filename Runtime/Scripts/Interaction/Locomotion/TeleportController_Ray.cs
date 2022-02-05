#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for a teleport controller using a straight ray.
	/// </summary>

	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Ray Teleport Controller")]
	[DisallowMultipleComponent]

	public class TeleportController_Ray : BaseTeleportController
	{
		[Tooltip("Minimum range of the ray")]
		public float MinimumRange = 0.0f;

		[Tooltip("Maximum range of the ray")]
		public float MaximumRange = 1000.0f;

		protected override void CalculateTrajectory(ref Trajectory _trajectory)
		{
			_trajectory.points.Clear();
			_trajectory.points.Add(this.transform.position + this.transform.forward * MinimumRange);
			_trajectory.points.Add(this.transform.position + this.transform.forward * MaximumRange);
		}
	}
}
