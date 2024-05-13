#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	[AddComponentMenu("SentienceLab/Interaction/Interactive Rigidbody")]
	[RequireComponent(typeof(Rigidbody))]
	public class InteractiveRigidbody : MonoBehaviour
	{
		public bool  CanTranslate  = true;
		public bool  CanRotate     = true;
		public bool  CanScale      = false;


		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the object is touched or aimed at by a physics manipulator")]
			public UnityEvent<InteractiveRigidbody, GameObject> OnTouchStart;

			[Tooltip("Event fired when the object is not touched or aimed at any more by a physics manipulator")]
			public UnityEvent<InteractiveRigidbody, GameObject> OnTouchEnd;

			[Tooltip("Event fired when the object is grabbed")] 
			public UnityEvent<InteractiveRigidbody, GameObject> OnGrabStart;

			[Tooltip("Event fired when the object is released")]
			public UnityEvent<InteractiveRigidbody, GameObject> OnGrabEnd;
		}

		public Events events;

		public Rigidbody Rigidbody { get; private set; }


		public void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>();
		}

		
		public void InvokeTouchStart(GameObject _other) 
		{
			events?.OnTouchStart.Invoke(this, _other); 
		}


		public void InvokeTouchEnd(GameObject _other) 
		{ 
			events?.OnTouchEnd.Invoke(this, _other); 
		}


		public void InvokeGrabStart(GameObject _other)
		{ 
			events?.OnGrabStart.Invoke(this, _other); 
		}


		public void InvokeGrabEnd(GameObject _other) 
		{ 
			events?.OnGrabEnd.Invoke(this, _other); 
		}
	}
}