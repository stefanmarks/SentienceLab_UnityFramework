#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.PostProcessing
{
	/// <summary>
	/// Component for fading-in the screen on start of the scene.
	/// </summary>

	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Effects/Fade In on Scene Load")]

	public class FadeInOnSceneLoad : MonoBehaviour
	{
		[Tooltip("The colour to fade")]
		public Color FadeColour = Color.black;

		[Tooltip("The fade-in time [s]")]
		public float FadeTime = 1;


		public void Awake()
		{
			m_fader = GetComponent<ScreenFade>();
			if (m_fader == null)
			{
				m_fader = gameObject.AddComponent<ScreenFade>();
			}
			m_fader.FadeColour = FadeColour;
			m_fader.FadeFactor = 1;
		}


		public void Update()
		{
			m_fader.FadeFactor -= Time.deltaTime / FadeTime;
			if (m_fader.FadeFactor <= 0)
			{
				Destroy(this);
			}
		}


		private ScreenFade m_fader;
	}
}
