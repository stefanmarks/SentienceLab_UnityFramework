#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Script that allows events to be sent when Triggers are entered/exited.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Interaction/Trigger Event")]
	[RequireComponent(typeof(Collider))]
	public class TriggerEvent : MonoBehaviour
	{
		public UnityEvent TriggerEnter;
		public UnityEvent TriggerExit;

		[Tooltip("GameObject tags to react to. If empty, react to any object")]
		public string[] TagNames;


		public void Start()
		{
			// no code here, just to have the enable flag
		}

		
		public void OnTriggerEnter(Collider _other)
		{
			if (this.isActiveAndEnabled && TagMatches(_other))
			{
				TriggerEnter.Invoke();
			}
		}

		public void OnTriggerExit(Collider _other)
		{
			if (this.isActiveAndEnabled && TagMatches(_other))
			{
				TriggerExit.Invoke();
			}
		}

		
		private bool TagMatches(Collider _other)
		{
			bool matches = true;

			if ((TagNames != null) && (TagNames.Length > 0))
			{
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
