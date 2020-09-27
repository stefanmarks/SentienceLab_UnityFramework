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

		[Tooltip("How is the translation/rotation interpolated")]
		public ETransformInterpolation InterpolationMode = ETransformInterpolation.None;

		[Tooltip("How much translation can happen before synchronisation is requested")]
		public float MovementThreshold = 0.001f;

		[Tooltip("How much rotation (degrees) can happen before synchronisation is requested")]
		public float RotationThreshold = 0.1f;

		// vector magnitude threshold for treating it as zero
		private float VECTOR_ZERO_EPSILON = 0.00001f; 

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

		/// <summary>
		/// How is translation/rotation interpolated
		/// </summary>
		public enum ETransformInterpolation
		{
			None,
			Linear
		}


		public override void Initialise()
		{
			m_rigidbody = GetComponent<Rigidbody>();
		}


		private bool DoTrans()
		{
			return
				(TransformComponents == ETransformComponents.Translation) ||
				(TransformComponents == ETransformComponents.TranslationRotation) ||
				(TransformComponents == ETransformComponents.TranslationRotationScale)
			;
		}


		private bool DoTransVel()
		{
			return DoTrans() && (InterpolationMode != ETransformInterpolation.None);
		}


		private bool DoRot()
		{
			return
				(TransformComponents == ETransformComponents.Rotation) ||
				(TransformComponents == ETransformComponents.TranslationRotation) ||
				(TransformComponents == ETransformComponents.TranslationRotationScale)
			;
		}


		private bool DoRotVel()
		{
			return DoRot() && (InterpolationMode != ETransformInterpolation.None);
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
			if (DoTransVel())
			{
				_entity.AddValue_Vector3D(EntityValue.VELOCITY, Vector3.zero);
			}

			if (DoRot())
			{
				_entity.AddValue_Quaternion(EntityValue.ROTATION, transform.localRotation);
			}
			if (DoRotVel())
			{
				_entity.AddValue_Vector3D(EntityValue.VELOCITY_ROTATION, Vector3.zero);
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
			m_valPosition    = DoTrans()    ? _entity.GetValue_Vector3D(  EntityValue.POSITION) : null;
			m_valVelocityPos = DoTransVel() ? _entity.GetValue_Vector3D(  EntityValue.VELOCITY) : null;
			m_valRotation    = DoRot()      ? _entity.GetValue_Quaternion(EntityValue.ROTATION) : null;
			m_valVelocityRot = DoRotVel()   ? _entity.GetValue_Vector3D(  EntityValue.VELOCITY_ROTATION) : null;
			m_valScale       = DoScale()    ? _entity.GetValue_Vector3D(  EntityValue.SCALE) : null;
		}


		public override void DestroyEntityVariables()
		{
			// transform variables
			m_valPosition    = null;
			m_valVelocityPos = null;
			m_valRotation    = null;
			m_valVelocityRot = null;
			m_valScale       = null;
		}


		public override void SynchroniseFromEntity(bool _initialise)
		{
			m_lastUpdateFrame = Time.frameCount;

			if (m_valPosition != null && m_valVelocityPos == null) // set position without interpolation
			{
				Vector3 pos = m_valPosition.Value;
				if (ReferenceTransform != null)
				{
					pos = ReferenceTransform.TransformPoint(pos);
				}
				transform.position = pos;
			}

			if (m_valRotation != null && m_valVelocityRot == null) // set rotation without interpolation
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


		/// <summary>
		/// Fuidly animate the game object in case interpolation mode is not "None"
		/// </summary>
		/// 
		public override void OnUpdate(bool _controlledByServer)
		{
			if (_controlledByServer && InterpolationMode != ETransformInterpolation.None)
			{
				float deltaT = (Time.frameCount - m_lastUpdateFrame) * Time.deltaTime;

				if (m_valPosition != null && m_valVelocityPos != null) // set position with interpolation
				{
					Vector3 pos = m_valPosition.Value;
					Vector3 vel = m_valVelocityPos.Value;
					if (ReferenceTransform != null)
					{
						pos = ReferenceTransform.TransformPoint(pos);
						vel = ReferenceTransform.TransformDirection(vel);
					}
					pos += vel * deltaT;
					transform.position = pos;
				}

				if (m_valRotation != null && m_valVelocityRot != null) // set rotation with interpolation
				{
					Quaternion rot = m_valRotation.Value;
					Vector3    vel = m_valVelocityRot.Value;
					if (ReferenceTransform != null)
					{
						rot = ReferenceTransform.rotation * rot;
						vel = ReferenceTransform.rotation * vel;
					}
					rot = IntegrateAngularVelocity(rot, vel, deltaT);
					transform.rotation = rot;
				}
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
					}
				}
				if (DoTransVel())
				{
					// non-zero velocity forces update
					if ((m_valVelocityPos != null) && (m_valVelocityPos.Value.magnitude > 0))
					{
						m_modified = true;
					}
				}	
				if (DoRot())
				{
					float angle = Quaternion.Angle(this.transform.rotation, m_oldRotation);
					if (angle > RotationThreshold)
					{
						m_modified = true;
					}
				}
				if (DoRotVel())
				{
					// non-zero velocity forces update
					if ((m_valVelocityRot != null) && (m_valVelocityRot.Value.magnitude > 0))
					{
						m_modified = true;
					}
				}
				if (DoScale())
				{
					if ((m_oldScale - this.transform.localScale).sqrMagnitude > 0)
					{
						m_modified = true;
					}
				}
			}

			return m_modified;
		}


		public override void SynchroniseToEntity(bool _firstTime)
		{
			float deltaT = (Time.frameCount - m_lastUpdateFrame) * Time.deltaTime;
			
			if (m_valPosition != null)
			{
				Vector3 pos = transform.position;
				if (_firstTime) 
				{ 
					m_oldPosition = pos; // don't start with a "jump"
				}
				// calculate or get velocity
				Vector3 vel;
				if (m_rigidbody != null)
				{
					if (m_rigidbody.IsSleeping())
					{
						vel = Vector3.zero;
					}
					else
					{ 
						vel = m_rigidbody.velocity; 
					}
				}
				else
				{
					vel = (pos - m_oldPosition) / deltaT;
				}
				// cut off very low values
				if (vel.magnitude < VECTOR_ZERO_EPSILON)
				{
					vel = Vector3.zero;
				}
				m_oldPosition = pos;
				// make relative to reference tansform (if given)
				if (ReferenceTransform != null)
				{
					pos = ReferenceTransform.InverseTransformPoint(pos);
					vel = ReferenceTransform.InverseTransformDirection(vel);
				}
				// send updated values
				m_valPosition.Modify(pos);
				if (m_valVelocityPos != null) m_valVelocityPos.Modify(vel);
			}

			if (m_valRotation != null)
			{
				Quaternion rot = transform.rotation;
				if (_firstTime)
				{
					m_oldRotation = rot; // don't start with a "jump"
				}
				// caluclate or get velocity
				Vector3 vel;
				if (m_rigidbody != null)
				{
					if (m_rigidbody.IsSleeping())
					{
						vel = Vector3.zero;
					}
					else
					{
						vel = m_rigidbody.angularVelocity;
					}
				}
				else
				{
					vel = CalculateAngularVelocity(rot, m_oldRotation, deltaT);
				}
				// cut off very low values
				if (vel.magnitude < VECTOR_ZERO_EPSILON)
				{
					vel = Vector3.zero;
				}
				m_oldRotation = rot;
				// make relative to reference tansform (if given)
				if (ReferenceTransform != null) 
				{ 
					rot = Quaternion.Inverse(ReferenceTransform.rotation) * rot;
					vel = Quaternion.Inverse(ReferenceTransform.rotation) * vel;
				}
				// send updated values
				m_valRotation.Modify(rot);
				if (m_valVelocityRot != null) m_valVelocityRot.Modify(vel);
			}

			if (m_valScale != null)
			{
				// TODO: Consider global scale?
				Vector3 scl = transform.localScale;
				// if (ReferenceTransform != null) { ...??? }
				m_valScale.Modify(scl);
				m_oldScale = scl;
			}

			m_lastUpdateFrame = Time.frameCount;
		}


		public override void ResetModified()
		{
			m_modified = false;
		}


		/// <summary>
		/// Calculate rotation velocity from two orientations (current and previous frame) and a delta time.
		/// </summary>
		/// <param name="rotNow">Rotation now</param>
		/// <param name="rotPrev">Rotation in previous frame</param>
		/// <param name="deltaTime">time between rotations in seconds</param>
		/// <returns>Rotation axis scaled by speed in radians/s</returns>
		/// 
		protected Vector3 CalculateAngularVelocity(Quaternion rotNow, Quaternion rotPrev, float deltaTime)
		{
			// calculate difference rotation
			Quaternion rotDelta = rotNow * Quaternion.Inverse(rotPrev);
			rotDelta.ToAngleAxis(out float angle, out Vector3 axis);
			angle *= Mathf.Deg2Rad;
			angle /= deltaTime;
			Vector3 vel = axis * angle;
			return vel;
			/*
		    // Source: https://forum.unity.com/threads/manually-calculate-angular-velocity-of-gameobject.289462/
			Vector3 velocity = Vector3.zero;
			Quaternion rotDelta = rotNow * Quaternion.Inverse(rotPrev);
			// Is there any "significant" rotation?
			if (Mathf.Abs(rotDelta.w) < 0.9995f)
			{
				float magnitude;
				// handle negatives, we could just flip it but this is faster
				if (rotDelta.w < 0.0f)
				{
					float angle = Mathf.Acos(-rotDelta.w);
					magnitude = -2.0f * angle / (Mathf.Sin(angle) * deltaTime);
				}
				else
				{
					float angle = Mathf.Acos(rotDelta.w);
					magnitude = 2.0f * angle / (Mathf.Sin(angle) * deltaTime);
				}
				velocity.Set(rotDelta.x * magnitude, rotDelta.y * magnitude, rotDelta.z * magnitude);
			}
			return velocity;
			*/
		}


		/// <summary>
		/// Calculate an orientation based on a start orientation, a rotation velocity, and a delta time.
		/// </summary>
		/// <param name="rotNow">Rotation now</param>
		/// <param name="velocity">Rotation axis scaled by speed in radians/s</param>
		/// <param name="deltaTime">Time interval to integrate the rotation velocity over</param>
		/// <returns>Integrated orientation</returns>
		/// 
		protected Quaternion IntegrateAngularVelocity(Quaternion rotNow, Vector3 velocity, float deltaTime)
		{
			float angle = velocity.magnitude;
			angle *= Mathf.Rad2Deg * deltaTime;
			Quaternion qDelta = Quaternion.AngleAxis(angle, velocity);
			return rotNow * qDelta;
		}


		private EntityValue_Vector3D   m_valPosition;
		private EntityValue_Vector3D   m_valVelocityPos;
		private EntityValue_Quaternion m_valRotation;
		private EntityValue_Vector3D   m_valVelocityRot;
		private EntityValue_Vector3D   m_valScale;

		private Rigidbody  m_rigidbody;
		private Vector3    m_oldPosition;
		private Quaternion m_oldRotation;
		private Vector3    m_oldScale;
		private int        m_lastUpdateFrame;
		private bool       m_modified;
	}
}
