#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MajorDomo
{
	[AddComponentMenu("MajorDomo/Synchronised Transform")]
	public class SynchronisedTransform : AbstractSynchronisedComponent
	{
		[Tooltip("Which components of this game object's transform are to be synchronised")]
		public ETransformComponents TransformComponents = ETransformComponents.TranslationRotation;

		[Tooltip("Transform that this object's transform is based on\n(None: World coordinate system)")]
		public Transform ReferenceTransform = null;

		[Tooltip("How much translation can happen before synchronisation is requested")]
		public float MovementThreshold = 0.001f;

		[Tooltip("How much rotation (degrees) can happen before synchronisation is requested")]
		public float RotationThreshold = 0.1f;

		/// <summary>
		/// What components of this game object's transform are synchronised.
		/// </summary>
		public enum ETransformComponents
		{
			TranslationRotationScale,
			TranslationRotation,
			Translation,
			Rotation
		}


		private bool DoTrans()
		{
			return
				(TransformComponents == ETransformComponents.Translation) ||
				(TransformComponents == ETransformComponents.TranslationRotation) ||
				(TransformComponents == ETransformComponents.TranslationRotationScale)
			;
		}


		private bool DoRot()
		{
			return
				(TransformComponents == ETransformComponents.Rotation) ||
				(TransformComponents == ETransformComponents.TranslationRotation) ||
				(TransformComponents == ETransformComponents.TranslationRotationScale)
			;
		}


		private bool DoScale()
		{
			return (TransformComponents == ETransformComponents.TranslationRotationScale);
		}


		public override void CreateEntityVariables(EntityData _entity)
		{
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


		public override void FindEntityVariables(EntityData _entity)
		{
			// transform variables
			m_valPosition = DoTrans() ? _entity.GetValue_Vector3D(EntityValue.POSITION) : null;
			m_valRotation = DoRot()   ? _entity.GetValue_Quaternion(EntityValue.ROTATION) : null;
			m_valScale    = DoScale() ? _entity.GetValue_Vector3D(EntityValue.SCALE) : null;
		}


		public override void DestroyEntityVariables()
		{
			// transform variables
			m_valPosition = null;
			m_valRotation = null;
			m_valScale    = null;
		}


		public override void SynchroniseFromEntity(bool _initialise)
		{
			if (m_valPosition != null)
			{
				Vector3 pos = m_valPosition.Value;
				if (ReferenceTransform != null)
				{
					pos = ReferenceTransform.TransformPoint(pos);
				}
				transform.position = pos;
			}

			if (m_valRotation != null)
			{
				Quaternion rot = m_valRotation.Value;
				if (ReferenceTransform != null)
				{
					rot = ReferenceTransform.rotation * rot;
				}
				transform.rotation = rot;
			}

			if (m_valScale != null)
			{
				Vector3 scl = m_valScale.Value;
				// TODO: Consider global scale?
				// Since there is no absolute "global" scale, let's just use localScale for now
				// if (ReferenceTransform != null) { ReferenceTransform.lossyScale.Scale(scl); }
				transform.localScale = scl;
			}
		}


		public override bool IsModified()
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


		public override void SynchroniseToEntity()
		{
			if (m_valPosition != null)
			{
				Vector3 pos = transform.position;
				if (ReferenceTransform != null)
				{
					pos = ReferenceTransform.InverseTransformPoint(pos);
				}
				m_valPosition.Modify(pos);
			}

			if (m_valRotation != null)
			{
				Quaternion rot = transform.rotation;
				if (ReferenceTransform != null) 
				{ 
					rot = Quaternion.Inverse(ReferenceTransform.rotation) * rot; 
				}
				m_valRotation.Modify(rot);
			}

			if (m_valScale != null)
			{
				// TODO: Consider global scale?
				Vector3 scl = transform.localScale;
				// if (ReferenceTransform != null) { ...??? }
				m_valScale.Modify(scl);
			}
		}


		public override void ResetModified()
		{
			m_modified = false;
		}



		private EntityValue_Vector3D   m_valPosition;
		private EntityValue_Quaternion m_valRotation;
		private EntityValue_Vector3D   m_valScale;

		private Vector3    m_oldPosition;
		private Quaternion m_oldRotation;
		private Vector3    m_oldScale;
		private bool       m_modified;
	}
}
