using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using SentienceLab.PostProcessing;
using System.Collections.Generic;

namespace SentienceLab
{
	/// <summary>
	/// Behaviour for selecting the next/previous scene in the build settings.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/Scene Selection")]
	public class SceneSelection : MonoBehaviour
	{
		[Tooltip("Input action for tiggering the next scene")]
		public InputActionProperty NextSceneAction;

		[Tooltip("Input action for tiggering the previous scene")]
		public InputActionProperty PreviousSceneAction;

		[Tooltip("Time in seconds for the fade out and in")]
		private float fadeTime = 1.0f;


		public void Start()
		{
			// get current scene number and maximum scene index
			sceneIndex = SceneManager.GetActiveScene().buildIndex;
			currentSceneIndex = sceneIndex;
			maxSceneIndex = SceneManager.sceneCountInBuildSettings - 1;

			// fade in
			fadeLevel = 1;
			fadeTime = -1;

			// create screen faders
			faders = ScreenFade.AttachToAllCameras();

			// create action handlers for next/prev scene selection
			if (NextSceneAction != null)
			{
				NextSceneAction.action.performed += delegate { NextScene(); };
				NextSceneAction.action.Enable();
			}
			if (PreviousSceneAction != null)
			{
				PreviousSceneAction.action.performed += delegate { PreviousScene(); };
				PreviousSceneAction.action.Enable();
			}
		}


		public void Update()
		{
			if (fadeLevel > 0)
			{
				// fade and level load is in progress

				fadeLevel += fadeTime * Time.deltaTime;
				if (fadeLevel < 0)
				{
					// fade in finished
					fadeLevel = 0;
					fadeTime = 0;
				}
				else if (fadeLevel > 1)
				{
					// fade to black finished -> load level
					fadeLevel = 1;
					fadeTime = 0;
					if (sceneIndex != currentSceneIndex)
					{
						Debug.Log("Loading scene " + sceneIndex);
						SceneManager.LoadScene(sceneIndex);
						sceneIndex = currentSceneIndex;
					}
				}

				foreach (ScreenFade fade in faders)
				{
					fade.FadeFactor = fadeLevel;
				}
			}
		}


		public void NextScene()
		{
			SetScene(sceneIndex + 1);
		}


		public void PreviousScene()
		{
			SetScene(sceneIndex - 1);
		}


		public void SetScene(int _sceneIndex)
		{
			// scene index sanity check
			if (_sceneIndex < 0) { _sceneIndex = maxSceneIndex; }
			if (_sceneIndex > maxSceneIndex) { _sceneIndex = 0; }

			if (_sceneIndex != currentSceneIndex)
			{
				// new scene number > start fading
				sceneIndex = _sceneIndex;
				Debug.Log("About to load scene #" + sceneIndex);

				// start the fade
				fadeLevel = 0.01f;
				fadeTime = 1;
			}
		}


		private float fadeLevel;
		private int sceneIndex;
		private int maxSceneIndex, currentSceneIndex;
		private List<ScreenFade> faders;
	}
}