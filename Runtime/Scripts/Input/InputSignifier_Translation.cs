#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Signifies an input control by changing the transform
	/// (e.g., moving a trigger button by the press amount)
	/// </summary>
	///
	[AddComponentMenu("Sentience Lab/Input/Input Signifier - Translation")]
	public class InputSignifier_Translation : MonoBehaviour
	{
		[Tooltip("The action that triggers the translation")]
		public InputActionProperty Action;

		[Tooltip("The axis and magnitude to translate")]
		public Vector3 Axis;


		public void Start()
		{
			m_restPosition = transform.localPosition;

			if (Action != null)
			{
				Action.action.Enable();
			}
			else
			{
				this.enabled = false;
			}
		}


		public void Update()
		{
			SetTranslation(Action.action.ReadValue<float>());
		}


		protected void SetTranslation(float _amount)
		{
			transform.localPosition = m_restPosition + Axis * _amount;
		}


#if UNITY_EDITOR
		[ContextMenu("Test Signifier")]
		public void Test()
		{
			SentienceLab.Tools.EditorCoroutines.Execute(TestWorker());
		}


		private IEnumerator TestWorker()
		{
			UnityEditor.Undo.RegisterCompleteObjectUndo(this.transform, "Test Signifier");
			Start();
			for (float f = 0; f < 3.14159; f += 0.05f)
			{
				SetTranslation(Mathf.Sin(f));
				yield return null;
			}
			UnityEditor.Undo.PerformUndo();
			UnityEditor.Undo.ClearUndo(this.transform);
		}
#endif


		private Vector3 m_restPosition;
	}
}