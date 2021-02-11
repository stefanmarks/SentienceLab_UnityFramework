#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Signifies an input control by changing the material
	/// </summary>
	///
	[RequireComponent(typeof(Renderer))]
	[AddComponentMenu("Sentience Lab/Input/Input Signifier - Material")]
	public class InputSignifier_Material : MonoBehaviour
	{
		[Tooltip("The action that triggers the material change")]
		public InputActionProperty Action;

		[Tooltip("The materials to set on activation")]
		public List<Material>      SignifierMaterials;


		public void Start()
		{
			m_active = false;
			// get renderer and default materials
			m_renderer = GetComponent<Renderer>();
			m_normalMaterials = m_renderer.sharedMaterials;

			if (Action != null)
			{
				Action.action.performed += OnActionPerformed;
				Action.action.canceled  += OnActionCanceled;
				Action.action.Enable();
			}
		}

		private void OnActionPerformed(InputAction.CallbackContext _ctx)
		{
			SetActiveState(true);
		}

		private void OnActionCanceled(InputAction.CallbackContext _ctx)
		{
			SetActiveState(false);
		}

		protected void SetActiveState(bool _active)
		{
			if (_active != m_active)
			{
				if (_active)
				{
					m_renderer.materials = SignifierMaterials.ToArray();
				}
				else
				{
					m_renderer.materials = m_normalMaterials;
				}
				m_active = _active;
			}
		}


#if UNITY_EDITOR
		[ContextMenu("Test Signifier")]
		public void Test()
		{
			SentienceLab.Tools.EditorCoroutines.Execute(TestWorker());
		}


		private IEnumerator TestWorker()
		{
			Start();
			UnityEditor.Undo.RegisterCompleteObjectUndo(m_renderer, "Test Signifier");
			SetActiveState(true);
			for (int i = 0; i < 100; i++) { yield return null; }
			SetActiveState(false);
			UnityEditor.Undo.PerformUndo();
			UnityEditor.Undo.ClearUndo(m_renderer);
		}
#endif


		private bool       m_active;
		private Renderer   m_renderer;
		private Material[] m_normalMaterials;
	}
}