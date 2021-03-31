#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for changing the appearance of an object on hover/select.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Interactivity Signifier")]
	[RequireComponent(typeof(InteractiveRigidbody))]
	public class InteractivitySignifier : MonoBehaviour
	{
		public List<MeshRenderer> Renderers;

		public Material HoverMaterial;
		public Material GrabMaterial;

		public void Awake()
		{
			InteractiveRigidbody irb = GetComponent<InteractiveRigidbody>();

			irb.OnHoverStart.AddListener(OnHoverStart);
			irb.OnHoverEnd.AddListener(OnHoverEnd);
			irb.OnGrabStart.AddListener(OnGrabStart);
			irb.OnGrabEnd.AddListener(OnGrabEnd);

			m_hover = m_grab = false;

			UpdateMaterials();
		}


		private void OnHoverStart(InteractiveRigidbody _rb, GameObject _other) { m_hover = true; UpdateMaterials(); }
		private void OnHoverEnd(InteractiveRigidbody _rb, GameObject _other) { m_hover = false; UpdateMaterials(); }
		private void OnGrabStart(InteractiveRigidbody _rb, GameObject _other) { m_grab = true; UpdateMaterials(); }
		private void OnGrabEnd(InteractiveRigidbody _rb, GameObject _other) { m_grab = false; UpdateMaterials(); }

		private void UpdateMaterials()
		{
			Material m = null;
			if      (m_grab ) { m = GrabMaterial;  }
			else if (m_hover) { m = HoverMaterial; }

			foreach (var r in Renderers)
			{
				if (m == null)
				{
					r.gameObject.SetActive(false);
				}
				else
				{
					r.gameObject.SetActive(true);
					r.material = m;
				}
			}
		}

		private bool m_hover, m_grab;
	}
}