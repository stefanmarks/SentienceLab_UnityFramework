#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Helper component for a connect/disconnect button UI
	/// </summary>
	/// 
	[DisallowMultipleComponent]
	[AddComponentMenu("MajorDomo/MajorDomo Manager UI")]
	public class MajorDomoManager_UI : MonoBehaviour
	{
		[Tooltip("Where to put the connect/disconnect button")]
		public Rect ConnectButtonDimensions = new Rect(10, 10, 100, 30);


		public void OnGUI()
		{
			if (!MajorDomoManager.Instance.IsConnected())
			{
				if (GUI.Button(ConnectButtonDimensions, "Connect"))
				{
					MajorDomoManager.Instance.Connect();
				}
			}
			else 
			{
				if (GUI.Button(ConnectButtonDimensions, "Disconnect"))
				{
					MajorDomoManager.Instance.Disconnect();
				}
			}
		}
	}
}