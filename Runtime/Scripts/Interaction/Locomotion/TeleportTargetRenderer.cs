#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for rendering a potential teleportation target.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Teleport Target Renderer")]
	[RequireComponent(typeof(BaseTeleportController))]
	[DisallowMultipleComponent]

	public class TeleportTargetRenderer : MonoBehaviour
	{
		[Tooltip("The game object to use as an teleport target indicator")]
		public Transform ValidTargetIndicator;

		[Tooltip("Optional: The game object to use as a forbidden teleport target indicator")]
		public Transform InvalidTargetIndicator = null;


		public void Start()
		{
			if (ValidTargetIndicator == null)
			{
				Debug.LogWarning("TeleportTargetRenderer needs a game object or prefab to display as a target");
				this.enabled = false;
				return;
			}

			m_controller = GetComponent<BaseTeleportController>();

			if (InvalidTargetIndicator == null)
			{
				InvalidTargetIndicator = ValidTargetIndicator;
			}

			// if indicators are prefabs, instantiate them
			if (ValidTargetIndicator.gameObject.scene.name == null)
			{
				ValidTargetIndicator = Instantiate(ValidTargetIndicator);
			}
			if (InvalidTargetIndicator.gameObject.scene.name == null)
			{
				InvalidTargetIndicator = Instantiate(InvalidTargetIndicator);
			}

			// hide indicators for now
			ValidTargetIndicator.gameObject.SetActive(false);
			InvalidTargetIndicator.gameObject.SetActive(false);
		}


		public void Update()
		{
			if (m_controller.IsAimingAtValidTarget)
			{
				Transform indicator;
				if (m_controller.ActiveTarget.DisableTeleporting)
				{
					ValidTargetIndicator.gameObject.SetActive(false);
					InvalidTargetIndicator.gameObject.SetActive(true);
					indicator = InvalidTargetIndicator;
				}
				else
				{
					InvalidTargetIndicator.gameObject.SetActive(false);
					ValidTargetIndicator.gameObject.SetActive(true);
					indicator = ValidTargetIndicator;
				}

				// calculate orientation of marker
				Vector3 pos = m_controller.ActivRaycastHit.point;
				Vector3 up  = Vector3.up;
				Vector3 fwd = Vector3.forward;
				switch (m_controller.ActiveTarget.OrientationAlignmentMode)
				{
					case Teleporter.OrientationAlignmentMode.KeepOrientation:
						{
							up  = m_controller.Teleporter.transform.up;
							fwd = m_controller.Teleporter.positionReference.forward;
							break;
						}
					case Teleporter.OrientationAlignmentMode.UseCollisionNormal:
						{
							up  = m_controller.ActivRaycastHit.normal;
							fwd = m_controller.Teleporter.positionReference.forward;
							break;
						}
					case Teleporter.OrientationAlignmentMode.UseTargetOrientation:
						{
							up  = m_controller.ActiveTarget.transform.up;
							Vector3 relFwd = m_controller.Teleporter.transform.InverseTransformDirection(m_controller.Teleporter.positionReference.forward);
							fwd = m_controller.ActiveTarget.transform.TransformDirection(relFwd);
							break;
						}
					case Teleporter.OrientationAlignmentMode.UseTargetOrientationAndForceDirection:
						{
							up  = m_controller.ActiveTarget.transform.up;
							fwd = m_controller.ActiveTarget.transform.forward;
							break;
						}
				}
				Vector3 fwdProj = Vector3.ProjectOnPlane(fwd, up).normalized;
				
				Quaternion rot = indicator.rotation; // fallback, in case fwdProj is 0
				if (fwdProj.sqrMagnitude > 0) rot = Quaternion.LookRotation(fwdProj, up);
				
				indicator.SetPositionAndRotation(pos, rot);
			}
			else
			{
				ValidTargetIndicator.gameObject.SetActive(false);
				InvalidTargetIndicator.gameObject.SetActive(false);
			}
		}

		BaseTeleportController m_controller;
	}
}
