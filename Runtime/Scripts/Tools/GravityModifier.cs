#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Script that changes the Gravity vector while active.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/Gravity Modifier")]

	public class GravityModifier : MonoBehaviour
	{
		[Tooltip("Gravity Vector to set when this component is active")]
		public Vector3 GravityVector;


		public void Start()
		{
			// nothing to do here
		}


		public void OnEnable()
		{
			m_oldGravityVector = Physics.gravity;
			Physics.gravity = GravityVector;
		}


		public void OnDisable()
		{
			Physics.gravity = m_oldGravityVector;
		}


		protected Vector3 m_oldGravityVector;
	}
}
