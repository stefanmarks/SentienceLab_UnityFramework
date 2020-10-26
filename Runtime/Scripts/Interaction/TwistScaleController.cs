#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;

namespace SentienceLab.Physics
{
	/// <summary>
	/// Enables the user to scale an object by grabbing it and twisting the controllers.
	/// </summary>
	[AddComponentMenu("Physics/Twist Scale Controller")]
	[RequireComponent(typeof(PhysicsGrab))]
	public class TwistScaleController : MonoBehaviour
	{
		[Tooltip("Name of the input that starts the twist scale")]
		public InputActionReference TwistStartAction;

		[Tooltip("Curve for the change of the scale in units/s based on the rotation angle")]
		public AnimationCurve Curve = AnimationCurve.Constant(-180, 180, 1);


		/// <summary>
		/// Initializes component data and starts MLInput.
		/// </summary>
		void Awake()
		{
			if (TwistStartAction != null)
			{
				TwistStartAction.action.performed += OnTwistStart;
				TwistStartAction.action.canceled  += OnTwistEnd;
				TwistStartAction.action.Enable();
			}
			else
			{
				Debug.LogWarning("No action defined for twist scale");
				this.enabled = false;
			}

			m_physicsGrabScript = GetComponent<PhysicsGrab>();
			m_rotation = 0;
			m_twistActive = false;
			m_lastRotation = Vector3.zero;
		}


		private void OnTwistStart(InputAction.CallbackContext obj)
		{
			m_rotation = 0;
			m_lastRotation = transform.rotation.eulerAngles;
			m_twistActive = true;
		}


		private void OnTwistEnd(InputAction.CallbackContext obj)
		{
			m_twistActive = false;
		}
		
		
		void FixedUpdate()
		{
			if (m_twistActive)
			{
				Vector3 newRot = transform.rotation.eulerAngles;
				// find delta rotation
				float deltaRot = newRot.z - m_lastRotation.z;
				// account for 360 degree jump
				while (deltaRot < -180) deltaRot += 360;
				while (deltaRot > +180) deltaRot -= 360;
				// This should only work when controller points horizontally
				// > diminish when |Y| component of forwards vector increases
				float changeFactor = 1 - Mathf.Abs(transform.forward.y);
				deltaRot *= changeFactor;
				// accumulate change (minus: clockwise = positive number)
				m_rotation += -deltaRot;
				// constrain to control curve limits
				m_rotation = Mathf.Clamp(m_rotation,
					Curve.keys[0].time,
					Curve.keys[Curve.length - 1].time);
				// actually change parameter

				InteractiveRigidbody irb = m_physicsGrabScript.GetActiveBody();
				if ((Mathf.Abs(m_rotation) > 1) && (irb != null) && irb.CanScale)
				{
					float relScaleFactor = 1.0f + Curve.Evaluate(m_rotation) * Time.fixedDeltaTime;
					Vector3 oldScale = irb.Rigidbody.transform.localScale;
					Vector3 newScale = oldScale * relScaleFactor;
					Vector3 pivot    = m_physicsGrabScript.GetGrabPoint();
					Vector3 posDiff  = irb.Rigidbody.transform.position - pivot;
					irb.Rigidbody.MovePosition(pivot + posDiff * relScaleFactor);
					irb.Rigidbody.transform.localScale = newScale;
				}
				m_lastRotation = newRot;
			}
		}

		private PhysicsGrab  m_physicsGrabScript;
		private bool         m_twistActive;
		private float        m_rotation;
		private Vector3      m_lastRotation;
	}
}
