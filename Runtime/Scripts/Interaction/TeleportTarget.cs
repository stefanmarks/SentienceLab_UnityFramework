#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.EventSystems;

namespace SentienceLab
{
	/// <summary>
	/// Component for an object that can be aimed at for teleporting.
	/// This component uses the event system.
	/// </summary>

	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Teleport Target")]
	[DisallowMultipleComponent]

	public class TeleportTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		[Tooltip("Transform to position at the location and rotation of the potential teleport target")]
		public Transform groundMarker;


		public void Start()
		{
			raycaster     = null;
			raycastResult = new RaycastResult();
			teleporter    = null;
		}


		public void Update()
		{
			bool enableTeleport = false;

			if (raycaster != null && teleporter != null)
			{
				enableTeleport = teleporter.IsReady();

				// If this object is still "hit" by the raycast source, update ground marker position and orientation
				raycastResult.Clear();
				BaseInputModule bim = EventSystem.current.currentInputModule;
				if (bim is GazeInputModule)
				{
					GazeInputModule gim = (GazeInputModule) bim;
					raycastResult = gim.GetPointerData().pointerCurrentRaycast;
				}

				if (enableTeleport && raycastResult.gameObject != null)
				{
					Transform hit = raycastResult.gameObject.transform;

					if ((hit.transform == this.transform) || (hit.parent == this.transform))
					{
						float yaw = raycaster.rotation.eulerAngles.y;
						groundMarker.position = raycastResult.worldPosition;
						groundMarker.localRotation = Quaternion.Euler(0, yaw, 0);
					}
				}
			}

			groundMarker.gameObject.SetActive(enableTeleport);
		}


		public void OnPointerClick(PointerEventData eventData)
		{
			if (teleporter != null)
			{
				groundMarker.gameObject.SetActive(false);
				teleporter.TeleportPosition(
					eventData.pointerPressRaycast.worldPosition
				);
			}
		}


		public void OnPointerEnter(PointerEventData eventData)
		{
			// get the raycaster and the (hopefully) attached Teleporter component
			raycaster  = eventData.enterEventCamera.transform;
			teleporter = raycaster.GetComponentInParent<Teleporter>();
		}


		public void OnPointerExit(PointerEventData eventData)
		{
			raycaster = null;
		}


		private Transform     raycaster;
		private RaycastResult raycastResult;
		private Teleporter    teleporter;
	}
}
