#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class for managing a help UI that fades in when the specific object 
/// is held for a certain time within a collider in front of the camera.
/// </summary>
/// 
public class HelpUI : MonoBehaviour
{
	[Tooltip("Time in seconds before the help UI is shown")]
	public float  activateTime   = 1;

	[Tooltip("Time in seconds before the help UI is hidden")]
	public float  deactivateTime = 1;

	[Tooltip("Tag of colliders that trigger the UI")]
	public string triggerTag     = "help";

	[Tooltip("List of input actions that hide the UI when performed")]
	public List<InputActionReference> hideInputActions;


	void Start()
	{
		m_Canvas              = GetComponentInChildren<Canvas>();
		m_Canvas.enabled      = false;
		m_time                = deactivateTime;
		m_isWithinTrigger     = false;
		m_hideActionPerformed = false;
		foreach (var actionRef in hideInputActions)
		{
			if (actionRef != null)
			{
				actionRef.action.performed += delegate { m_hideActionPerformed = true; };
				actionRef.action.Enable();
			}
		}
	}


	void Update()
	{
		// hide UI when specific actions are active
		if (m_hideActionPerformed)
		{
			m_isWithinTrigger = false;
			m_time = float.Epsilon;
		}

		// check timing and show/hide UI accordingly
		if (m_isWithinTrigger && (m_time < activateTime))
		{
			m_time += Time.deltaTime;
			if (m_time >= activateTime)
			{
				m_Canvas.enabled = true;
			}
		}
		else if (!m_isWithinTrigger && (m_time > 0))
		{
			m_time -= Time.deltaTime;
			if (m_time < 0)
			{
				m_Canvas.enabled = false;
			}
		}
	}


	void OnTriggerEnter(Collider other)
	{
		if (other != null && other.CompareTag(triggerTag))
		{
			m_isWithinTrigger = true;
			m_time = 0;
		}
	}


	void OnTriggerExit(Collider other)
	{
		if (other != null && other.CompareTag(triggerTag))
		{
			m_isWithinTrigger = false;
			m_time = deactivateTime;
			m_hideActionPerformed = false;
		}
	}


	private Canvas m_Canvas;
	private bool   m_isWithinTrigger;
	private float  m_time;
	private bool   m_hideActionPerformed;
}
