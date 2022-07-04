#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Script that allows events to be sent when Triggers are entered/exited.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Events/Trigger Event")]
	[RequireComponent(typeof(Collider))]
	public class TriggerEvent : MonoBehaviour
	{
		[Tooltip("Layer mask for objects eligible to trigger")]
		public LayerMask LayerMask = Physics.AllLayers;

		[Tooltip("GameObject tags to react to. If empty, react to any object")]
		[TagSelector]
		public string[] TagNames = { };

		[System.Serializable]
		public struct Events
		{
			[Tooltip("Event fired when trigger is entered")]
			public UnityEvent<Collider> OnTriggerEnter;

			[Tooltip("Event fired when trigger is exited")]
			public UnityEvent<Collider> OnTriggerExit;
		}
		public Events events;


		public void Start()
		{
			// no code here, just to have the "enable" flag
		}

		
		public void OnTriggerEnter(Collider _other)
		{
			if (this.isActiveAndEnabled && LayerMaskAndTagMatches(_other) )
			{
				events.OnTriggerEnter.Invoke(_other);
			}
		}

		public void OnTriggerExit(Collider _other)
		{
			if (this.isActiveAndEnabled && LayerMaskAndTagMatches(_other))
			{
				events.OnTriggerExit.Invoke(_other);
			}
		}

		
		protected bool LayerMaskAndTagMatches(Collider _other)
		{
			bool matches = true;

			if ((1 << (_other.gameObject.layer) & LayerMask.value) == 0)
			{
				// wrong layer
				matches = false;
			}

			if (matches && (TagNames != null) && (TagNames.Length > 0))
			{
				// tag names are given > check the list
				matches = false;
				foreach (var tag in TagNames)
				{
					if (_other.CompareTag(tag))
					{
						matches = true;
						break;
					}
				}
			}

			return matches;
		}
	}
}
