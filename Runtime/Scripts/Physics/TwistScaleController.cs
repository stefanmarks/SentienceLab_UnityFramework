using SentienceLab.Input;
using UnityEngine;

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
		public string InputName;

		[Tooltip("Curve for the change of the scale in units/s based on the rotation angle")]
		public AnimationCurve Curve = AnimationCurve.Constant(-180, 180, 1);


		/// <summary>
		/// Initializes component data and starts MLInput.
		/// </summary>
		void Awake()
		{
			m_handlerActive     = InputHandler.Find(InputName);
			m_physicsGrabScript = GetComponent<PhysicsGrab>();
			m_rotation = 0;
			m_lastRotation = Vector3.zero;
		}


		/// <summary>
		/// Update mesh polling center position to camera.
		/// </summary>
		void Update()
		{
			if (m_handlerActive != null)
			{
				if (m_handlerActive.IsActivated())
				{
					m_rotation = 0;
					m_lastRotation = transform.rotation.eulerAngles;
				}
				else if (m_handlerActive.IsActive())
				{
					InteractiveRigidbody irb = m_physicsGrabScript.GetActiveBody();
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
		
					if (Mathf.Abs(m_rotation) > 1)
					{
						// scale with centre point 1m in front of the observer
						float relScaleFactor = 1.0f + Curve.Evaluate(m_rotation) * Time.deltaTime;
						Vector3 oldScale = transform.localScale;
						Vector3 newScale = oldScale * relScaleFactor;
						Vector3 pivot    = this.transform.position;
						if (m_physicsGrabScript != null) pivot = m_physicsGrabScript.GetGrabPoint();
						Vector3 posDiff  = transform.position - pivot;
						transform.position   = pivot + posDiff * relScaleFactor;
						transform.localScale = newScale;
					}
					m_lastRotation = newRot;
				}
			}
		}

		private InputHandler m_handlerActive;
		private PhysicsGrab  m_physicsGrabScript;
		private float        m_rotation;
		private Vector3      m_lastRotation;
	}
}
