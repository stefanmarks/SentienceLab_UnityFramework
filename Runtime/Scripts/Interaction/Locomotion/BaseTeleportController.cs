#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SentienceLab
{
	/// <summary>
	/// Base component for an object that can aim at teleport targets for teleporting.
	/// This can be a handheld controller or a gaze-controller pointer.
	/// </summary>

	public abstract class BaseTeleportController : MonoBehaviour
	{
		[Tooltip("Action to use for priming/executing a teleport")]
		public InputActionProperty TeleportAction;

		[Tooltip("How to execute aiming/teleportation")]
		public EActivationType ActivationType = EActivationType.OnRelease;

		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the teleporter starts aiming")]
			public UnityEvent OnStartAiming;

			[Tooltip("Event fired when the teleporter finishes aiming")]
			public UnityEvent OnEndAiming;

			[Tooltip("Event fired when the teleporter is activated")]
			public UnityEvent OnTeleport;
		}

		public Events events;

		public enum EActivationType
		{
			Immediately,
			OnRelease
		}


		protected struct Trajectory
		{
			public List<Vector3> points;
		}


		public void Start()
		{
			m_teleporter = GetComponentInParent<Teleporter>();
			if (m_teleporter == null)
			{
				Debug.LogWarning("A teleport controller needs a Teleporter component somewhere in its hierarchy.");
				this.enabled = false;
				return;
			}

			if (TeleportAction == null)
			{
				TeleportAction.action.performed += delegate { OnActionStart(); };
				TeleportAction.action.canceled  += delegate { OnActionEnd(); };
				TeleportAction.action.Enable();
			}

			m_currentHit = new RaycastHit();

			m_doAim      = ActivationType == EActivationType.Immediately;
			m_doTeleport = false;

			m_trajectory.points = new List<Vector3>();
			m_trajectory.points.Clear();
		}


		public void OnActionStart()
		{
			if (ActivationType == EActivationType.OnRelease)
			{
				m_doAim      = true;
				m_doTeleport = false;
			}
			else
			{
				m_doTeleport = true;
			}
		}


		public void Aim()
		{
			m_doAim = true;
		}


		public void OnActionEnd()
		{
			if (ActivationType == EActivationType.OnRelease)
			{
				m_doAim      = false;
				m_doTeleport = true;
			}
		}


		public void Teleport()
		{
			m_doTeleport = true;
		}


		public bool IsAiming
		{
			get { return m_doAim; }
		}


		public bool IsAimingAtValidTarget
		{
			get { return m_doAim && (m_currentTarget != null); }
		}


		public TeleportTarget ActiveTarget
		{
			get { return m_currentTarget; }
		}


		public RaycastHit ActivRaycastHit
		{
			get { return m_currentHit; }
		}


		protected abstract void CalculateTrajectory(ref Trajectory _trajectory);


		public void Update()
		{
			if ((m_teleporter == null) || !m_teleporter.IsReady()) return;

			CalculateTrajectory(ref m_trajectory);
			
			FindTrajectoryHit();
			TeleportTarget newTarget = m_currentTarget;

			if (m_currentHit.transform != null)
			{
				if ((m_currentTarget == null) || (m_currentHit.transform.gameObject != m_currentTarget.gameObject))
				{
					// hit an object > does it have an active TeleportTarget component?
					newTarget = m_currentHit.transform.gameObject.GetComponent<TeleportTarget>();
					if (newTarget != null && newTarget.isActiveAndEnabled)
					{
						// what does the teleport target consider as "up"?
						if (newTarget.UpVector == TeleportTarget.EUpVector.Transform)
						{
							// change hit normal to TeleportTarget's "up"
							m_currentHit.normal = newTarget.transform.up;
						}
						else
						{
							// nothing to do > hit normal is calculated from collider already
						}
					}
					else
					{
						// target is disabled > ignore
						newTarget = null; 
					}
				}
			}
			
			if (m_currentTarget != newTarget)
			{
				// teleport target has changed > invoke event handlers

				if (m_currentTarget != null)
				{
					m_currentTarget.TeleportControllerStartsAiming(this);
				}

				// Debug.LogFormat("New teleport target '{0}' > '{1}'", m_currentTarget, newTarget);
				m_currentTarget = newTarget;

				if (m_currentTarget != null)
				{
					m_currentTarget.TeleportControllerEndsAiming(this);
				}
			}

			if (m_currentTarget == null || m_currentTarget.DisableTeleporting)
			{
				m_doTeleport = false;
			}

			if (m_doTeleport)
			{
				if (m_teleporter != null)
				{
					// ...and activate teleport
					m_teleporter.TeleportPosition(m_currentHit.point);
					m_currentTarget.TeleportControllerInvokesTeleport(this);
				}
				m_doTeleport = false;
			}
		}


		protected void FindTrajectoryHit()
		{
			for (int i=1; i < m_trajectory.points.Count; i++)
			{
				// calculate ray form start/end point of trajectory
				Vector3 start   = m_trajectory.points[i-1];
				Vector3 end     = m_trajectory.points[i];
				float   maxDist = Vector3.Distance(end, start);
				Ray     tempRay = new Ray(start, end-start);
				Physics.Raycast(tempRay, out m_currentHit, maxDist);
				// first hit will stop the loop
				if ((m_currentHit.distance > 0) && (m_currentHit.transform != null)) break;
			}
		}


		private bool            m_doAim, m_doTeleport;
		private Teleporter      m_teleporter;
		private Trajectory      m_trajectory;
		private TeleportTarget  m_currentTarget;
		private RaycastHit      m_currentHit;
	}
}
