#region Copyright Information
// Sentience Lab - Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

public class ApplicationQuitControl: MonoBehaviour
{
	[Tooltip("Action that quits the application (default: ESC)")]
	public InputAction QuitAction;
	
	public void Start()
	{
		// default, if not otherwise stated: ESC key
		if (QuitAction == null)
		{
			QuitAction = new InputAction(binding: "<Keyboard>/escape");
		}
		else if (QuitAction.bindings.Count == 0)
		{
			QuitAction.AddBinding("<Keyboard>/escape");
		}

		QuitAction.performed += delegate { Quit(); };
		QuitAction.Enable();
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
