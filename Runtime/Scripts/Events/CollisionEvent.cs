#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Script that allows events to be sent when Colliders are entered/exited.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Events/Collision Event")]
	[RequireComponent(typeof(Collider))]
	public class CollisionEvent : MonoBehaviour
	{
		[Tooltip("GameObject tags to react to. If empty, react to any object")]
		[TagSelector]
		public string[] TagNames;

		[System.Serializable]
		public struct Events 
		{
			[Tooltip("Event fired when collider is entered")]
			public UnityEvent<Collision> OnColliderEnter;

			[Tooltip("Event fired when collider is exited")]
			public UnityEvent<Collision> OnColliderExit;
		}
		public Events events;


		public void Start()
		{
			// no code here, just to have the "enable" flag
		}


		public void OnCollisionEnter(Collision _collision)
		{
			if (this.isActiveAndEnabled && TagMatches(_collision.collider))
			{
				events.OnColliderEnter.Invoke(_collision);
			}
		}


		public void OnCollisionExit(Collision _collision)
		{
			if (this.isActiveAndEnabled && TagMatches(_collision.collider))
			{
				events.OnColliderExit.Invoke(_collision);
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
