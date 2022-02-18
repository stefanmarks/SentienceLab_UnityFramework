#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Controls sleep timeout and brightness for the screen.
	/// </summary>
	[AddComponentMenu("SentienceLab/Tools/Screen Control")]
	public class ScreenControl : MonoBehaviour
	{
		[Tooltip("Screen sleep timeout in seconds\n(-1: Never sleep, -2: Use system setting)")]
		public int sleepTimeout = SleepTimeout.SystemSetting;

		[Tooltip("Screen brightness")]
		[Range(0, 1)]
		public float screenBrightness = 1.0f;


		public void Start()
		{
			SetScreenSleepTimeout(sleepTimeout);
			SetScreenBrightness(screenBrightness);
		}


		public void SetScreenSleepTimeout(int _timeout)
		{
			// Brightness control is expected to work only in iOS, see:
			// https://docs.unity3d.com/ScriptReference/Screen-brightness.html.
			Screen.sleepTimeout = _timeout;
			sleepTimeout = Screen.sleepTimeout; // read back into field
		}


		public void SetScreenBrightness(float _brightness)
		{
			Screen.brightness = _brightness;
			screenBrightness = Screen.brightness; // read back into field
		} 
	}
}