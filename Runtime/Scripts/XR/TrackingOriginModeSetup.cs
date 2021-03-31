#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SentienceLab
{
	/// <summary>
	/// Script for globally changing the tracking space reference (e.g., floor, head)
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/XR/Tracking Origin Mode Setup")]
	public class TrackingOriginModeSetup : MonoBehaviour
	{
		public TrackingOriginModeFlags TrackingOriginMode = TrackingOriginModeFlags.Floor;


		public void Start()
		{
			SetTrackingOriginMode(TrackingOriginMode);
		}
		
		
		public void SetTrackingOriginMode(TrackingOriginModeFlags _mode)
		{
			List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
			foreach (var subsystem in inputSubsystems)
			{
				if (!subsystem.TrySetTrackingOriginMode(_mode))
				{
					Debug.LogWarningFormat("Could not set tracking origin mode '{0}' for device '{1}'",
						_mode.ToString(), subsystem.ToString());
				}
			}
		}
	}
}