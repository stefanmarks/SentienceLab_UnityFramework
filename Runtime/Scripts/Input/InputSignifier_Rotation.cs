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
	/// Signifies an input control by changing the rotation
	/// (e.g., rotating a trigger button by the press amount)
	/// </summary>
	///
	[AddComponentMenu("Sentience Lab/Input/Input Signifier - Rotation")]
	public class InputSignifier_Rotation : MonoBehaviour
	{
		[Tooltip("The action that triggers the rotation")]
		public InputActionProperty Action;

		[Tooltip("The axis of rotation")]
		public Vector3 Axis;

		[Tooltip("The angle of rotation [degrees]")]
		public float Angle;


		public void Start()
		{
			m_restRotation = transform.localRotation;

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
			SetRotation(Action.action.ReadValue<float>());
		}
		

		protected void SetRotation(float _amount)
		{
			Quaternion rotationAmount = Quaternion.AngleAxis(_amount * Angle, Axis);
			transform.localRotation = m_restRotation * rotationAmount;
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
				SetRotation(Mathf.Sin(f));
				yield return null;
			}
			UnityEditor.Undo.PerformUndo();
			UnityEditor.Undo.ClearUndo(this.transform);
		}
#endif


		private Quaternion m_restRotation;
	}
}