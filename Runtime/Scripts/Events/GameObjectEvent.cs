#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Script that allows events to be sent when Triggers are entered/exited.
	/// </summary>
	///
	[AddComponentMenu("SentienceLab/Events/GameObject Event")]
	public class GameObjectEvent : MonoBehaviour
	{
		[System.Serializable]
		public struct Events
		{
			[Tooltip("Event fired when the game object is started")]
			public UnityEvent<GameObjectEvent> OnStart;

			[Tooltip("Event fired when the game object is enabled")]
			public UnityEvent<GameObjectEvent> OnEnabled;

			[Tooltip("Event fired (in the next frame!) when the game object is disabled")]
			public UnityEvent<GameObjectEvent> OnDisabled;
		}
		public Events events;


		public void Start()
		{
			// wait one more frame in case this is at the start of the game and receiving objects are not finished setting up
			StartCoroutine(OnStartCoroutine());
		}

		protected IEnumerator OnStartCoroutine()
		{
			yield return null;
			events.OnStart.Invoke(this);
		}

		public void OnEnable()
		{
			events.OnEnabled.Invoke(this);
		}

		public void OnDisable()
		{
			events.OnDisabled.Invoke(this);
		}
	}
}
