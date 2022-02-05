#region Copyright Information
// Sentience Lab - Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Make sure the game object is not destroyed when another scene is loaded.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/Don't Destroy on Load")]
	public class DontDestroyOnLoad : MonoBehaviour
	{
		[Tooltip("Explicit list of game objects to preserve when loading new scenes.\nIf empty, the game object that this component is attached to is preserved.")]
		public List<GameObject> ObjectsToPreserve;

		public void Awake()
		{
			if (ObjectsToPreserve.Count == 0)
			{
				ObjectsToPreserve.Add(gameObject);
			}

			foreach (var gameObject in ObjectsToPreserve)
			{
				if (gameObject != null) DontDestroyOnLoad(gameObject);
			}
		}
	}
}
