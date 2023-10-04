#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Class for fading the screen using postprocessing.
	/// </summary>
	///
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("SentienceLab/Effects/Screen Fade")]

	public class ScreenFade : MonoBehaviour
	{
		[Tooltip("The colour to fade to")]
		public Color FadeColour = Color.black;

		[Tooltip("The fade factor (0: no fade, 1: completely faded)")]
		[Range(0, 1)]
		public float FadeFactor = 0;


		public void Awake()
		{
			Shader shader = Shader.Find("FX/Screen Fade");
			if (shader != null)
			{
				m_FadeMaterial = new Material(shader);
			}
			else
			{
				Debug.LogWarning("Shader 'FX/Screen Fade' not found. Is it included in the list of preloaded shaders or in a 'Resources' folder?");
			}
		}


		public virtual void OnPostRender()
		{ 
			if (FadeFactor > 0 && m_FadeMaterial != null)
			{
				m_FadeMaterial.color = new Color(FadeColour.r, FadeColour.g, FadeColour.b, FadeColour.a * FadeFactor);
				m_FadeMaterial.SetPass(0);
				GL.PushMatrix();
					GL.LoadOrtho();
					GL.Color(m_FadeMaterial.color);
					GL.Begin(GL.QUADS);
						GL.Vertex3(0f, 0f, 1f);
						GL.Vertex3(0f, 1f, 1f);
						GL.Vertex3(1f, 1f, 1f);
						GL.Vertex3(1f, 0f, 1f);
					GL.End();
				GL.PopMatrix();
			}
		}

		/// <summary>
		/// Attached the fade effect to all cameras and returns a list to the scripts.
		/// </summary>
		/// <returns>the list of scripts attached to cameras</returns>
		/// 
		public static List<ScreenFade> AttachToAllCameras()
		{
			// get all cameras
			Camera[] cameras = new Camera[Camera.allCamerasCount];
			Camera.GetAllCameras(cameras);

			// find or add fade behaviour
			List<ScreenFade> faders = new List<ScreenFade>();
			foreach(Camera cam in cameras)
			{
				if (cam != null)
				{
					ScreenFade fade = cam.gameObject.GetComponent<ScreenFade>();
					if (fade == null)
					{
						fade = cam.gameObject.AddComponent<ScreenFade>();
					}
					faders.Add(fade);
				}
			}

			return faders;
		}


		private Material m_FadeMaterial;
	}
}
