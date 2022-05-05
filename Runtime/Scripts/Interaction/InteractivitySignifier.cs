#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for changing the appearance of an object on grab/hover.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Interactivity Signifier")]
	[RequireComponent(typeof(InteractiveRigidbody))]
	public class InteractivitySignifier : MonoBehaviour
	{
		public List<MeshRenderer> Renderers;

		public Material TouchMaterial;
		public Material GrabMaterial;


		public void Awake()
		{
			InteractiveRigidbody irb = GetComponent<InteractiveRigidbody>();

			irb.events.OnTouchStart.AddListener(OnTouchStart);
			irb.events.OnTouchEnd.AddListener(  OnTouchEnd);
			irb.events.OnGrabStart.AddListener( OnGrabStart);
			irb.events.OnGrabEnd.AddListener(   OnGrabEnd);

			m_touch = m_grab = false;

			UpdateMaterials();
		}


		private void OnTouchStart(InteractiveRigidbody _rb, GameObject _other) { m_touch = true; UpdateMaterials(); }
		private void OnTouchEnd(InteractiveRigidbody _rb, GameObject _other) { m_touch = false; UpdateMaterials(); }
		private void OnGrabStart(InteractiveRigidbody _rb, GameObject _other) { m_grab = true; UpdateMaterials(); }
		private void OnGrabEnd(InteractiveRigidbody _rb, GameObject _other) { m_grab = false; UpdateMaterials(); }

		private void UpdateMaterials()
		{
			Material m = null;
			if      (m_grab ) { m = GrabMaterial;  }
			else if (m_touch) { m = TouchMaterial; }

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

		private bool m_touch, m_grab;
	}
}