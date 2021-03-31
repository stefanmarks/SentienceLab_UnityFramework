#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component to modify gaze fuse times and gaze ranges.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Gaze/Gaze Behaviour Modifier")]
	public class GazeBehaviourModifier : MonoBehaviour
	{
		public float fuseTimeOverride         = 0;

		public float minimumGazeRangeOverride = 0;

		public float maximumGazeRangeOverride = 0;
	}
}