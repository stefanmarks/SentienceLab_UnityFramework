#region Copyright Information
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
		public enum eTransformComponents
		{
			TranslationRotationScale,
			TranslationRotation,
			Translation,
			Rotation
		}

		[Tooltip("Transform that this object's transform is based on (empty=World)")]
		public Transform ReferenceTransform = null;

		[Tooltip("Transform that this object's transform is aimed at (empty=this Game Object)")]
		public Transform TargetTransform = null;

		[Tooltip("Which components of this game object's transform are to be synchronised")]
		public eTransformComponents TransformComponents;


		public new void Awake()
		{
			base.m_entityType = "Transform";
			base.Awake();
			
			if (ReferenceTransform == null)
			{
				m_reference = transform.root;
			}

			if (TargetTransform == null)
			{
				TargetTransform = this.transform;
			}
		}


		private bool DoTrans()
		{
			return
				TransformComponents == eTransformComponents.Translation ||
				TransformComponents == eTransformComponents.TranslationRotation ||
				TransformComponents == eTransformComponents.TranslationRotationScale;
		}


		private bool DoRot()
		{
			return
				TransformComponents == eTransformComponents.TranslationRotation ||
				TransformComponents == eTransformComponents.TranslationRotationScale;
		}


		private bool DoScale()
		{
			return
				TransformComponents == eTransformComponents.TranslationRotationScale;
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
			m_valEnabled  = m_entity.GetValue_Boolean(EntityValue.ENABLED);
			m_valPosition = DoTrans() ? m_entity.GetValue_Vector3D(EntityValue.POSITION)   : null;
			m_valRotation = DoRot()   ? m_entity.GetValue_Quaternion(EntityValue.ROTATION) : null;
			m_valScale    = DoScale() ? m_entity.GetValue_Vector3D(EntityValue.SCALE)      : null;
			Debug.Log(m_valPosition);
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
			if (m_valEnabled != null)
			{
				TargetTransform.gameObject.SetActive(m_valEnabled.Value);
			}
			else
			{
				TargetTransform.gameObject.SetActive(true);
			}

			if (m_valPosition != null) TargetTransform.position   = m_valPosition.Value;
			if (m_valRotation != null) TargetTransform.rotation   = m_valRotation.Value;
			if (m_valScale    != null) TargetTransform.localScale = m_valScale.Value;
		}


		protected override void SynchroniseToEntity()
		{
			if (m_valEnabled  != null) m_valEnabled.Modify(TargetTransform.gameObject.activeSelf);
			if (m_valPosition != null) m_valPosition.Modify(TargetTransform.position);
			if (m_valRotation != null) m_valRotation.Modify(TargetTransform.rotation);
			if (m_valScale    != null) m_valScale.Modify(TargetTransform.localScale);
		}


		protected override bool CanDisableGameObject()
		{
			return true;
		}


		private EntityValue_Boolean    m_valEnabled;
		private EntityValue_Vector3D   m_valPosition;
		private EntityValue_Quaternion m_valRotation;
		private EntityValue_Vector3D   m_valScale;
		private EntityValue_Colour     m_valueColour;

		private Transform              m_reference;
	}
}