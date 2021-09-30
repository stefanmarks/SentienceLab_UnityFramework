#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Script that allows events to be sent when Colliders are entered/exited.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Interaction/Collision Event")]
	[RequireComponent(typeof(Collider))]
	public class CollisionEvent : MonoBehaviour
	{
		public UnityEvent ColliderEnter;
		public UnityEvent ColliderExit;
		
		[Tooltip("GameObject tags to react to. If empty, react to any object")]
		public string[] TagNames;


		public void Start()
		{
			// no code here, just to have the enable flag
		}

		
		public void OnCollisionEnter(Collision _collision)
		{
			if (this.isActiveAndEnabled && TagMatches(_collision.collider))
			{
				ColliderEnter.Invoke();
			}
		}

		public void OnCollisionExit(Collision _collision)
		{
			if (this.isActiveAndEnabled && TagMatches(_collision.collider))
			{
				ColliderExit.Invoke();
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
