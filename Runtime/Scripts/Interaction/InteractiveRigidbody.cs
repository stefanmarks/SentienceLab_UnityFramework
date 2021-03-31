#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	[AddComponentMenu("SentienceLab/Interaction/Interactive Rigidbody")]
	[RequireComponent(typeof(Rigidbody))]
	public class InteractiveRigidbody : MonoBehaviour
	{
		[System.Serializable]
		public class OnHoverStartEvent : UnityEvent<InteractiveRigidbody, GameObject> { }
		[System.Serializable]
		public class OnHoverEndEvent   : UnityEvent<InteractiveRigidbody, GameObject> { }
		[System.Serializable]
		public class OnGrabStartEvent  : UnityEvent<InteractiveRigidbody, GameObject> { }
		[System.Serializable]
		public class OnGrabEndEvent    : UnityEvent<InteractiveRigidbody, GameObject> { }


		public bool  CanTranslate  = true;
		public bool  CanRotate     = true;
		public bool  CanScale      = false;


		public OnHoverStartEvent OnHoverStart;
		public OnHoverEndEvent   OnHoverEnd;
		public OnGrabStartEvent  OnGrabStart;
		public OnGrabEndEvent    OnGrabEnd;


		public Rigidbody Rigidbody { get; private set; }


		public void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>();
		}


		
		public void InvokeHoverStart(GameObject _other) { OnHoverStart.Invoke(this, _other); }
		public void InvokeHoverEnd(GameObject _other) { OnHoverEnd.Invoke(this, _other); }
		public void InvokeGrabStart(GameObject _other) { OnGrabStart.Invoke(this, _other); }
		public void InvokeGrabEnd(GameObject _other) { OnGrabEnd.Invoke(this, _other); }
	}
}