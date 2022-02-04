#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab
{
	/// <summary>
	/// Component for changing a parameter in GUI materials to make them appear always on top of geometry.
	/// </summary>
	[AddComponentMenu("SentienceLab/Tools/Always Render Canvas On Top ")]
	public class AlwaysRenderOnTopCanvas : MonoBehaviour
	{
		public void Start()
		{
			if (!m_changesApplied)
			{
				m_changesApplied = true;
				Graphic[] graphicElements = GetComponentsInChildren<Graphic>();
				foreach (var ge in graphicElements)
				{
					Material originalMaterial = ge.materialForRendering;
					Material updatedMaterial = new Material(originalMaterial);
					updatedMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
					ge.material = updatedMaterial;
				}
			}
		}

		private bool m_changesApplied = false;
	}
}
