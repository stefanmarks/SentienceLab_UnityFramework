﻿#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MajorDomo
{
	[DisallowMultipleComponent]
	[AddComponentMenu("MajorDomo/Synchronised Transform")]
	public class SynchronisedTransform : SynchronisedEntityBase
	{
		/// <summary>
		/// What components of this transform are synchronised.
		/// </summary>
		public enum ETransformComponents
		{
			TranslationRotationScale,
			TranslationRotation,
			Translation,
			Rotation
		}

		/// <summary>
		/// Possible actions for when the synchronisation is lost.
		/// </summary>
		public enum ESyncLostBehaviour
		{
			/// <summary>
			/// Freeze the object at the last synchronised position/orientation.
			/// </summary>
			Freeze,

			/// <summary>
			/// Disable the game object and re-enable when tracking continues.
			/// </summary>
			Disable
		};

		[Tooltip("Transform that this object's transform is based on (empty=World)")]
		public Transform ReferenceTransform = null;

		[Tooltip("Transform that this object's transform is aimed at (empty=this Game Object)")]
		public Transform TargetTransform = null;

		[Tooltip("Which components of this game object's transform are to be synchronised")]
		public ETransformComponents TransformComponents = ETransformComponents.TranslationRotation;

		[Tooltip("What to do when the synchronisation is lost")]
		public ESyncLostBehaviour SyncLostBehaviour = ESyncLostBehaviour.Disable;

		[Tooltip("How much translation can happen before synchronisation is requested")]
		public float MovementThreshold = 0.001f;

		[Tooltip("How much rotation (degrees) can happen before synchronisation is requested")]
		public float RotationThreshold = 0.1f;


		protected override void Initialise()
		{
			if (TargetTransform == null)
			{
				TargetTransform = this.transform;
			}
		}


		private bool DoTrans()
		{
			return
				TransformComponents == ETransformComponents.Translation ||
				TransformComponents == ETransformComponents.TranslationRotation ||
				TransformComponents == ETransformComponents.TranslationRotationScale;
		}


		private bool DoRot()
		{
			return
				TransformComponents == ETransformComponents.Rotation ||
				TransformComponents == ETransformComponents.TranslationRotation ||
				TransformComponents == ETransformComponents.TranslationRotationScale;
		}


		private bool DoScale()
		{
			return
				TransformComponents == ETransformComponents.TranslationRotationScale;
		}


		protected override void CreateVariables(EntityData _entity)
		{
			_entity.AddValue_Boolean(EntityValue.ENABLED, gameObject.activeSelf);

			if (DoTrans())
			{
				_entity.AddValue_Vector3D(EntityValue.POSITION, transform.localPosition);
			}

			if (DoRot())
			{
				_entity.AddValue_Quaternion(EntityValue.ROTATION, transform.localRotation);
			}

			if (DoScale() && GetComponent<Camera>() == null)
			{
				// no scale in cameras
				_entity.AddValue_Vector3D(EntityValue.SCALE, transform.localScale);
			}
		}


		protected override void FindVariables()
		{
			m_valEnabled  = Entity.GetValue_Boolean(EntityValue.ENABLED);
			m_valPosition = DoTrans() ? Entity.GetValue_Vector3D(EntityValue.POSITION)   : null;
			m_valRotation = DoRot()   ? Entity.GetValue_Quaternion(EntityValue.ROTATION) : null;
			m_valScale    = DoScale() ? Entity.GetValue_Vector3D(EntityValue.SCALE)      : null;
		}


		protected override void DestroyVariables()
		{
			m_valEnabled  = null;
			m_valPosition = null;
			m_valRotation = null;
			m_valScale    = null;
		}


		protected override void SynchroniseFromEntity()
		{
			if (m_valEnabled != null && CanDisableGameObject())
			{
				TargetTransform.gameObject.SetActive(m_valEnabled.Value);
			}
			else
			{
				TargetTransform.gameObject.SetActive(true);
			}

			if (m_valPosition != null)
			{
				Vector3 pos = m_valPosition.Value;
				if (ReferenceTransform != null) { pos = ReferenceTransform.TransformPoint(pos); }
				TargetTransform.position = pos;
			}

			if (m_valRotation != null)
			{
				Quaternion rot = m_valRotation.Value;
				if (ReferenceTransform != null) { rot = ReferenceTransform.rotation * rot; }
				TargetTransform.rotation = rot;
			}

			if (m_valScale != null)
			{
				Vector3 scl = m_valScale.Value;
				// TODO: Consider global scale?
				// Since there is no absolute "global" scale, let's just use localScale for now
				// if (ReferenceTransform != null) { ReferenceTransform.lossyScale.Scale(scl); }
				TargetTransform.localScale = scl;
			}
		}


		protected override bool IsModified()
		{
			if (!m_modified)
			{
				if (DoTrans())
				{
					if ((m_oldPosition - this.transform.position).magnitude > MovementThreshold)
					{
						m_modified = true;
						m_oldPosition = this.transform.position;
					}
				}

				if (DoRot())
				{
					float angle = Quaternion.Angle(this.transform.rotation, m_oldRotation);
					if (angle > RotationThreshold)
					{
						m_modified = true;
						m_oldRotation = this.transform.rotation;
					}
				}

				if (DoScale())
				{
					if ((m_oldScale - this.transform.localScale).sqrMagnitude > 0)
					{
						m_modified = true;
						m_oldScale = this.transform.localScale;
					}
				}
			}
			return m_modified;
		}


		protected override void SynchroniseToEntity()
		{
			if (m_valEnabled != null)
			{
				m_valEnabled.Modify(TargetTransform.gameObject.activeSelf);
			}

			if (m_valPosition != null)
			{
				Vector3 pos = TargetTransform.position;
				if (ReferenceTransform != null) pos = ReferenceTransform.InverseTransformPoint(pos);
				m_valPosition.Modify(pos);
			}

			if (m_valRotation != null)
			{
				Quaternion rot = TargetTransform.rotation;
				if (ReferenceTransform != null) { rot = Quaternion.Inverse(ReferenceTransform.rotation) * rot; }
				m_valRotation.Modify(rot);
			}

			if (m_valScale != null)
			{
				// TODO: Consider global scale?
				Vector3 scl = TargetTransform.localScale;
				// if (ReferenceTransform != null) { ...??? }
				m_valScale.Modify(scl);
			}
		}


		protected override void ResetModified()
		{
			m_modified = false;
		}


		protected override bool CanDisableGameObject()
		{
			return SyncLostBehaviour == ESyncLostBehaviour.Disable;
		}


		protected override string GetEntityTypeName()
		{
			return "Transform";
		}


		private EntityValue_Boolean    m_valEnabled;
		private EntityValue_Vector3D   m_valPosition;
		private EntityValue_Quaternion m_valRotation;
		private EntityValue_Vector3D   m_valScale;

		private Vector3    m_oldPosition;
		private Quaternion m_oldRotation;
		private Vector3    m_oldScale;
		private bool       m_modified;
	}
}