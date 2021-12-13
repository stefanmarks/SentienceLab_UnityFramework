﻿#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	[AddComponentMenu("SentienceLab/Tools/Application Quit Manager")]
	public class ApplicationQuitManager : MonoBehaviour
	{
		[Tooltip("Action that quits the application (default: ESC)")]
		public InputActionProperty QuitAction;

		public void Start()
		{
			// default, if not otherwise stated: ESC key
			if (QuitAction == null)
			{
				QuitAction = new InputActionProperty();
			}
			else if (QuitAction.action.bindings.Count == 0)
			{
				QuitAction.action.AddBinding("<Keyboard>/escape");
			}

			QuitAction.action.performed += delegate { Quit(); };
			QuitAction.action.Enable();
		}


		public void Quit()
		{
			Debug.Log("Quitting application");
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}