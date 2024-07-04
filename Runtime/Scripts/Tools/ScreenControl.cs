#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Controls the cursor, sleep timeout, and brightness for the screen.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/Screen Control")]
	public class ScreenControl : MonoBehaviour
	{
		[Tooltip("Cursor mode")]
		public enum ECursorMode { Visible, Hidden };
		public ECursorMode CursorMode = ECursorMode.Visible;

		[Tooltip("Cursor lock mode")]
		public CursorLockMode CursorLockMode = CursorLockMode.None;

		[Tooltip("Screen sleep timeout in seconds\n(-1: Never sleep, -2: Use system setting)")]
		public int sleepTimeout = SleepTimeout.SystemSetting;

		[Tooltip("Screen brightness (-1: don't change)")]
		[Range(-1, 1)]
		public float screenBrightness = 1.0f;


		public void Start()
		{
			SetScreenSleepTimeout(sleepTimeout);
			SetScreenBrightness(screenBrightness);
			Cursor.visible   = CursorMode == ECursorMode.Visible;
			Cursor.lockState = CursorLockMode;
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
			if (_brightness >= 0)
			{
				Screen.brightness = _brightness;
				screenBrightness = Screen.brightness; // read back into field
			}
		}


		public void OnApplicationFocus(bool _focus)
		{
            if (_focus)
            {
				Cursor.visible   = CursorMode == ECursorMode.Visible;
				Cursor.lockState = CursorLockMode;
			}
			else
			{
				Cursor.visible   = true;
				Cursor.lockState = CursorLockMode.None;
			}
        }
	}
}