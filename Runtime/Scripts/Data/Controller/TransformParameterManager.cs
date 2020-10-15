#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Transform Parameter Manager")]

	/// <summary>
	/// Keeps a transform and three parameters (pos/rot/scale) in sync bidirectionally.
	/// </summary>
	/// 
	public class TransformParameterManager : MonoBehaviour
	{
		public Parameter_Vector3 positionParameter;
		public Parameter_Vector3 rotationParameter;
		public Parameter_Double  scaleParameter;


		public void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
		
			if (positionParameter != null)
			{
				positionParameter.OnValueChanged += delegate { PositionChanged(); };
				// parameter is immediately applied
				m_lastPosChange = -1;
				PositionChanged();
			}

			if (rotationParameter != null)
			{
				rotationParameter.OnValueChanged += delegate { RotationChanged(); };
				// parameter is immediately applied
				m_lastRotChange = -1;
				RotationChanged();
			}

			if (scaleParameter != null)
			{
				scaleParameter.OnValueChanged += delegate { ScaleChanged(); };
				// parameter is immediately applied
				m_lastScaleChange = -1;
				ScaleChanged();
			}
		}


		private void PositionChanged()
		{
			// avoid infinite update loops by checking the last frame a position change happened
			if ((positionParameter != null) && (m_lastPosChange < Time.frameCount))
			{
				m_lastPosChange = Time.frameCount;
				Vector3 pos = positionParameter.Value;
				transform.localPosition = pos;
				if (m_rigidbody != null)
				{
					m_rigidbody.MovePosition(transform.position);
				}
				m_lastPosition = pos;
			}
		}


		private void RotationChanged()
		{
			// avoid infinite update loops by checking the last frame a rotation change happened
			if ((rotationParameter != null) && (m_lastRotChange < Time.frameCount))
			{
				m_lastRotChange = Time.frameCount;
				Quaternion rot = Quaternion.Euler(rotationParameter.Value);
				transform.localRotation = rot;
				if (m_rigidbody != null)
				{
					m_rigidbody.MoveRotation(transform.rotation);
				}
				m_lastRotation = rot;
			}
		}


		private void ScaleChanged()
		{
			// avoid infinite update loops by checking the last frame a scale change happened
			if ((scaleParameter != null) && (m_lastScaleChange < Time.frameCount))
			{
				m_lastScaleChange = Time.frameCount;
				float scale = (float) scaleParameter.Value;
				transform.localScale = Vector3.one * scale;
				m_lastScale = scale;
			}
		}


		public void Update()
		{
			// avoid infinite update loops by checking the last frame a change happened
			if ( (positionParameter != null) && 
				 (m_lastPosChange < Time.frameCount) && 
				 (transform.localPosition != m_lastPosition))
			{
				m_lastPosChange = Time.frameCount;
				m_lastPosition  = transform.localPosition;
				positionParameter.Value = m_lastPosition;
				// copy back in case limits were met
				m_lastPosition = positionParameter.Value; 
				transform.localPosition = m_lastPosition;
			}

			if ( (rotationParameter != null) &&
				 (m_lastRotChange < Time.frameCount) &&
				 (transform.localRotation != m_lastRotation))
			{
				m_lastRotChange = Time.frameCount;
				m_lastRotation  = transform.localRotation;
				rotationParameter.Value = m_lastRotation.eulerAngles;
				// copy back in case limits were met
				m_lastRotation.eulerAngles = rotationParameter.Value; 
				transform.localRotation = m_lastRotation; 
			}

			if ( (scaleParameter != null) &&
				 (m_lastScaleChange < Time.frameCount) &&
				 (transform.localScale.x != m_lastScale))
			{
				m_lastScaleChange = Time.frameCount;
				scaleParameter.Value = m_lastScale;
				// copy back in case limits were met
				m_lastScale          = (float)scaleParameter.Value;
				transform.localScale = Vector3.one * m_lastScale;
			}
		}


		private Rigidbody  m_rigidbody;
		private Vector3    m_lastPosition;
		private Quaternion m_lastRotation;
		private float      m_lastScale;
		private long       m_lastPosChange, m_lastRotChange, m_lastScaleChange;
	}
}