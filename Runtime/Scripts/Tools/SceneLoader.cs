#region Copyright Information
// Sentience Lab - Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.PostProcessing;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SentienceLab
{
	public class SceneLoader : MonoBehaviour
	{
		public List<string>   ScenesToLoad = null;
		public bool           LoadOnStart  = false;
		public float          FadeTime     = 0.5f;
		public Color          FadeColour   = Color.black;


		public void Start()
		{
			CheckSceneList(ScenesToLoad);

			if (LoadOnStart)
			{
				StartLoading();
			}
		}


		protected void CheckSceneList(List<string> sceneList)
		{
			if (sceneList == null) return;
#if UNITY_EDITOR
			List<string> toRemove = new List<string>();

			foreach (var asset in sceneList)
			{
				bool found = false;
				foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
				{
					if (scene.enabled && ScenesToLoad != null && scene.path.Contains(asset))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					Debug.LogWarningFormat("Could not find scene '{0}' in build list", asset);
					toRemove.Add(asset);
				}
			}

			foreach (var asset in toRemove)
			{
				sceneList.Remove(asset);
			}
#endif
		}


		public void StartLoading()
		{
			if ( (ScenesToLoad == null || ScenesToLoad.Count == 0) ) return;

			StartCoroutine(LoadSceneWorker());
		}


		public IEnumerator LoadSceneWorker()
		{
			string thisScene = SceneManager.GetActiveScene().name;
			Debug.LogFormat(
				"Scene {0} attaching ScreenFade effect to cameras",
				thisScene);
			// create fade effect
			List<ScreenFade> fadeEffects = ScreenFade.AttachToAllCameras();
			foreach (ScreenFade fade in fadeEffects)
			{
				fade.FadeColour = FadeColour;
			}

			Debug.LogFormat(
				"Scene {0} fading to black",
				thisScene);
			float fadeFactor = 0;
			while (fadeFactor < 1)
			{
				fadeFactor += Time.deltaTime / FadeTime;
				foreach (ScreenFade fade in fadeEffects)
				{
					fade.FadeFactor = fadeFactor;
				}
				yield return null;
			}

			foreach (var asset in ScenesToLoad)
			{
				Debug.LogFormat(
					"Scene {0} loading scene {1}",
					thisScene, asset);

				var loadingOperation = SceneManager.LoadSceneAsync(asset);
				while (!loadingOperation.isDone) yield return null;
			}
		}
	}
}
