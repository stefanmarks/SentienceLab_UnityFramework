#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace SentienceLab
{
	/// <summary>
	/// Move In Place allows the user to move the play area by calculating the y-movement of the user's headset and/or controllers. The user is propelled forward the more they are moving. This simulates moving in game by moving in real life.
	/// </summary>
	/// <remarks>
	/// This locomotion method is based on Immersive Movement, originally created by Highsight. 
	/// Thanks to KJack (author of Arm Swinger) for additional work.
	/// </remarks>
	/// <example>
	/// `VRTK/Examples/042_CameraRig_MoveInPlace` demonstrates how the user can move and traverse colliders by either swinging the controllers in a walking fashion or by running on the spot utilisng the head bob for movement.
	/// </example>
	[AddComponentMenu("SentienceLab/Interaction/Locomotion/Walk-In-Place")]
	public class WalkInPlace : MonoBehaviour
	{
		/// <summary>
		/// Which tracked devices are used for determining the movement amount.
		/// </summary>
		/// <param name="HeadsetAndControllers">Track both headset and controllers for movement calculations.</param>
		/// <param name="ControllersOnly">Track only the controllers for movement calculations.</param>
		/// <param name="HeadsetOnly">Track only headset for movement caluclations.</param>
		[System.Flags]
		public enum MovementSourceOption
		{
			None = 0,
			Headset = 1,
			Controllers = 2,
			Accelerometer = 4
		}

		/// <summary>
		/// Options for which source is used to determine player direction while moving.
		/// </summary>
		/// <param name="Gaze">Player will always move in the direction they are currently looking.</param>
		/// <param name="ControllerRotation">Player will move in the direction that the controllers are pointing (averaged).</param>
		/// <param name="DumbDecoupling">Player will move in the direction they were first looking when they engaged Move In Place.</param>
		/// <param name="SmartDecoupling">Player will move in the direction they are looking only if their headset point the same direction as their controllers.</param>
		/// <param name="EngageControllerRotationOnly">Player will move in the direction that the controller with the engage button pressed is pointing.</param>
		/// <param name="LeftControllerRotationOnly">Player will move in the direction that the left controller is pointing.</param>
		/// <param name="RightControllerRotationOnly">Player will move in the direction that the right controller is pointing.</param>
		public enum DirectionSourceOption
		{
			Gaze,
			ControllerRotation,
			DumbDecoupling,
			SmartDecoupling,
			EngageControllerRotationOnly,
			LeftControllerRotationOnly,
			RightControllerRotationOnly
		}

		[Header("Control Settings")]

		[Tooltip("Transform with the tracking offset")]
		public Transform trackingOffset;

		[Tooltip("Transform for the left hand controller")]
		public Transform controllerLeftHand;

		[Tooltip("Transform for the right hand controller")]
		public Transform controllerRightHand;

		[Tooltip("Transform for the user's headset")]
		public Transform headset;

		[Tooltip("Action to engage Walk-In-Place")]
		public InputActionProperty engageAction;

		[Tooltip("Which trackables are used to determine movement")]
		public MovementSourceOption movementSources = MovementSourceOption.Headset | MovementSourceOption.Controllers;

		[Tooltip("How the user's movement direction will be determined.  The Gaze method tends to lead to the least motion sickness.  Smart decoupling is still a Work In Progress.")]
		public DirectionSourceOption directionMethod = DirectionSourceOption.Gaze;

		[Header("Speed Settings")]

		[Tooltip("Factor to scale head movement by for speed calculation")]
		public float headsetSpeedScale = 1;
		
		[Tooltip("Factor to scale controller movement by for speed calculation")]
		public float controllerSpeedScale = 0.5f;
		
		[Tooltip("The maximum speed the user can move in game units")]
		public float maxSpeed = 4;
		
		[Tooltip("The speed in which the play area slows down to a complete stop when the user is no longer pressing the engage button. This deceleration effect can ease any motion sickness that may be suffered.")]
		public float deceleration = 8;
		
		[Tooltip("The speed in which the play area slows down to a complete stop when the user is falling")]
		public float fallingDeceleration = 0.01f;

		[Header("Advanced Settings")]

		[Tooltip("Time period for averaging tracking samples [s]")]
		[Range(0.01f, 1)]
		public float averagingPeriod = 0.5f;

		[Tooltip("The degree threshold that all tracked objects (controllers, headset) must be within to change direction when using the Smart Decoupling Direction Method.")]
		public float smartDecoupleThreshold = 30f;
		
		[Tooltip("Movement below this delta will be ignored as it might be from tracking jitter.")]
		public float deltaThreshold = 0.01f;


		/// <summary>
		/// Abstract base class for movement sources.
		/// </summary>
		protected abstract class AbstractMovementSource
		{
			public List<float> velocityBuffer;
			public int         bufferSize;
			public float       velocityScale;

			public AbstractMovementSource(float _velocityScale)
			{
				velocityBuffer = new List<float>();
				velocityScale  = _velocityScale;
			}

			public void Update()
			{
				UpdateState();

				// calculate delta and append to buffer (if valid)
				float velocity = CalculateVelocity();
				if (velocity >= 0)
				{
					velocityBuffer.Add(velocity);
				}

				// limit buffer size by removing elements from the front
				if (velocityBuffer.Count > bufferSize)
				{
					velocityBuffer.RemoveRange(0, velocityBuffer.Count - bufferSize);
				}
			}

			protected abstract void UpdateState();

			protected abstract float CalculateVelocity();

			public float GetAverageVelocity()
			{
				float sum = 0;
				foreach (var delta in velocityBuffer) { sum += delta; }
				float average = sum / Mathf.Max(1, velocityBuffer.Count);
				average *= velocityScale;
				return average;
			}

			public void Reset()
			{
				velocityBuffer.Clear();
			}

			public void SetBufferSize(int _newSize)
			{
				bufferSize = Mathf.Max(1, _newSize);
			}
		}


		/// <summary>
		/// Movement source from a transform.
		/// </summary>
		protected class TransformMovementSource : AbstractMovementSource
		{
			public Transform trackedObject;
			public Vector3   currentPosition, lastPosition;
			public float     deltaThreshold;

			public TransformMovementSource(Transform _t, float _velocityScale, float _deltaThreshold) : base(_velocityScale)
			{
				trackedObject   = _t;
				currentPosition = _t.localPosition;
				deltaThreshold  = _deltaThreshold;
			}

			protected override void UpdateState()
			{
				lastPosition    = currentPosition;
				currentPosition = trackedObject.localPosition;
			}

			protected override float CalculateVelocity()
			{
				float delta = Mathf.Abs(currentPosition.y - lastPosition.y);
				delta /= Time.deltaTime;
				return (delta > deltaThreshold) ? delta : 0.0f;
			}
		}


		/// <summary>
		/// Movement source from the accelerometer.
		/// </summary>
		protected class AccelerometerMovementSource : AbstractMovementSource
		{
			protected LinearAccelerationSensor sensor;
			protected Vector3                  currentVelocity;
			protected float                    deltaThreshold;

			public AccelerometerMovementSource(float _velocityScale, float _deltaThreshold) : base(_velocityScale)
			{
				sensor = LinearAccelerationSensor.current;
				if (sensor != null)
				{
					InputSystem.EnableDevice(sensor);
				}
				currentVelocity = Vector3.zero;
				deltaThreshold  = _deltaThreshold;
			}

			protected override void UpdateState()
			{
				if (sensor != null)
				{
					currentVelocity *= 0.9f; // avoid drift
					currentVelocity += sensor.acceleration.ReadValue();
				}
			}

			protected override float CalculateVelocity()
			{
				float delta = Mathf.Abs(currentVelocity.y);
				return (delta > deltaThreshold) ? delta : 0;
			}
		}


		// tracked objects to use to determine amount of movement.
		protected List<AbstractMovementSource> m_movementSourceObjects;
		// controller that initiated the engage action
		protected Transform engageController;
		// Used to determine the direction when using a decoupling method.
		protected Vector3 initalGaze;
		// The current move speed of the player. If Move In Place is not active, it will be set to 0.00f.
		protected float currentSpeed;
		// The current direction the player is moving. If Move In Place is not active, it will be set to Vector.zero.
		protected Vector3 direction;
		protected Vector3 previousDirection;
		// is the controller active?
		protected bool m_isActive;
		protected bool currentlyFalling;


		/// <summary>
		/// Set the control options and modify the trackables to match.
		/// </summary>
		protected void UpdateMovementSourceObjectList()
		{
			m_movementSourceObjects.Clear();

			if (movementSources.HasFlag(MovementSourceOption.Controllers))
			{
				if (controllerLeftHand  != null) m_movementSourceObjects.Add(new TransformMovementSource(controllerLeftHand,  controllerSpeedScale, deltaThreshold));
				if (controllerRightHand != null) m_movementSourceObjects.Add(new TransformMovementSource(controllerRightHand, controllerSpeedScale, deltaThreshold));
			}

			if (movementSources.HasFlag(MovementSourceOption.Headset))
			{
				if (headset != null) m_movementSourceObjects.Add(new TransformMovementSource(headset, headsetSpeedScale, deltaThreshold));
			}

			if (movementSources.HasFlag(MovementSourceOption.Accelerometer))
			{
				if (headset != null) m_movementSourceObjects.Add(new AccelerometerMovementSource(headsetSpeedScale, deltaThreshold));
			}

			// set the buffer sizes
			int averagingBufferCount = (int)Mathf.Max(1, averagingPeriod / Time.deltaTime);
			foreach (var source in m_movementSourceObjects) { source.SetBufferSize(averagingBufferCount); }
		}

		/// <summary>
		/// The GetMovementDirection method will return the direction the player is moving.
		/// </summary>
		/// <returns>Returns a vector representing the player's current movement direction.</returns>
		public Vector3 GetMovementDirection()
		{
			return direction;
		}


		/// <summary>
		/// The GetSpeed method will return the current speed the player is moving at.
		/// </summary>
		/// <returns>Returns a float representing the player's current movement speed.</returns>
		public float GetSpeed()
		{
			return currentSpeed;
		}


		public void Start()
		{
			m_movementSourceObjects = new List<AbstractMovementSource>();
			UpdateMovementSourceObjectList();

			initalGaze = Vector3.zero;
			direction         = Vector3.zero;
			previousDirection = Vector3.zero;
			currentSpeed      = 0;
			m_isActive        = false;
			engageController  = null;

			engageAction.action.performed += OnEngageActionPerformed;
			engageAction.action.canceled  += OnEngageActionCanceled;
			engageAction.action.Enable();
		}


		public void Update()
		{
			// nothing to do here
		}


		protected void FixedUpdate()
		{
			UpdateMovementSources();

			if (m_isActive && !currentlyFalling)
			{
				// Calculate the average movement
				currentSpeed      = Mathf.Min(maxSpeed, CalculateAverageMovement() / Time.fixedDeltaTime);
				previousDirection = direction;
				direction         = CalculateDirection();
			}
			else if (currentSpeed > 0)
			{
				float dec = currentlyFalling ? fallingDeceleration : deceleration;
				currentSpeed -= dec * Time.fixedDeltaTime;
			}
			else
			{
				currentSpeed      = 0f;
				direction         = Vector3.zero;
				previousDirection = Vector3.zero;
			}

			MoveTrackingOffset(direction, currentSpeed);
		}


		protected void UpdateMovementSources()
		{
			foreach (var source in m_movementSourceObjects) { source.Update(); }
		}
		
		
		protected float CalculateAverageMovement()
		{
			float averageMovement = 0;

			foreach (var source in m_movementSourceObjects)
			{
				averageMovement += source.GetAverageVelocity();
			}
			averageMovement /= Mathf.Max(1, m_movementSourceObjects.Count);

			return averageMovement;
		}


		protected Vector3 CalculateDirection()
		{
			Vector3 returnDirection = Vector3.zero;

			// If we're doing a decoupling method...
			if (directionMethod == DirectionSourceOption.SmartDecoupling || directionMethod == DirectionSourceOption.DumbDecoupling)
			{
				// If we haven't set an inital gaze yet, set it now.
				// If we're doing dumb decoupling, this is what we'll be sticking with.
				if (initalGaze.Equals(Vector3.zero))
				{
					initalGaze = new Vector3(headset.forward.x, 0, headset.forward.z);
				}

				// If we're doing smart decoupling, check to see if we want to reset our distance.
				if (directionMethod == DirectionSourceOption.SmartDecoupling)
				{
					bool closeEnough = true;
					float curXDir = headset.rotation.eulerAngles.y;
					if (curXDir <= smartDecoupleThreshold)
					{
						curXDir += 360;
					}

					closeEnough = closeEnough && (Mathf.Abs(curXDir - controllerLeftHand.transform.rotation.eulerAngles.y) <= smartDecoupleThreshold);
					closeEnough = closeEnough && (Mathf.Abs(curXDir - controllerRightHand.transform.rotation.eulerAngles.y) <= smartDecoupleThreshold);

					// If the controllers and the headset are pointing the same direction (within the threshold) reset the direction the player's moving.
					if (closeEnough)
					{
						initalGaze = new Vector3(headset.forward.x, 0, headset.forward.z);
					}
				}
				returnDirection = initalGaze;
			}
			// if we're doing controller rotation movement
			else if (directionMethod.Equals(DirectionSourceOption.ControllerRotation))
			{
				Vector3 calculatedControllerDirection = DetermineAverageControllerRotation() * Vector3.forward;
				returnDirection = CalculateControllerRotationDirection(calculatedControllerDirection);
			}
			// if we're doing left controller only rotation movement
			else if (directionMethod.Equals(DirectionSourceOption.LeftControllerRotationOnly))
			{
				Vector3 calculatedControllerDirection = (controllerLeftHand != null ? controllerLeftHand.transform.rotation : Quaternion.identity) * Vector3.forward;
				returnDirection = CalculateControllerRotationDirection(calculatedControllerDirection);
			}
			// if we're doing right controller only rotation movement
			else if (directionMethod.Equals(DirectionSourceOption.RightControllerRotationOnly))
			{
				Vector3 calculatedControllerDirection = (controllerRightHand != null ? controllerRightHand.transform.rotation : Quaternion.identity) * Vector3.forward;
				returnDirection = CalculateControllerRotationDirection(calculatedControllerDirection);
			}
			// if we're doing engaged controller only rotation movement
			else if (directionMethod.Equals(DirectionSourceOption.EngageControllerRotationOnly))
			{
				Vector3 calculatedControllerDirection = (engageController != null ? engageController.rotation : Quaternion.identity) * Vector3.forward;
				returnDirection = CalculateControllerRotationDirection(calculatedControllerDirection);
			}
			// Otherwise if we're just doing Gaze movement, always set the direction to where we're looking.
			else if (directionMethod.Equals(DirectionSourceOption.Gaze))
			{
				returnDirection = (new Vector3(headset.forward.x, 0, headset.forward.z));
			}

			return returnDirection;
		}


		protected Vector3 CalculateControllerRotationDirection(Vector3 calculatedControllerDirection)
		{
			return (Vector3.Angle(previousDirection, calculatedControllerDirection) <= 90f ? calculatedControllerDirection : previousDirection);
		}


		protected void MoveTrackingOffset(Vector3 moveDirection, float moveSpeed)
		{
			Vector3 movement = (moveDirection * moveSpeed) * Time.fixedDeltaTime;
			Vector3 finalPosition = new Vector3(movement.x + trackingOffset.position.x, trackingOffset.position.y, movement.z + trackingOffset.position.z);
			if (trackingOffset != null)
			{
				trackingOffset.position = finalPosition;
			}
		}


		protected void OnEngageActionPerformed(InputAction.CallbackContext _)
		{
			// TODO: might need to find out which controller did that
			// engageController = ...
			m_isActive = true;
		}


		protected void OnEngageActionCanceled(InputAction.CallbackContext _)
		{
			// If the button is released, clear all the lists.
			foreach (var movementData in m_movementSourceObjects)
			{
				movementData.Reset();
			}
			initalGaze = Vector3.zero;

			m_isActive = false;
			// engagedController = null;
		}


		protected Quaternion DetermineAverageControllerRotation()
		{
			// Build the average rotation of the controller(s)
			Quaternion newRotation;

			// Both controllers are present
			if (controllerLeftHand != null && controllerRightHand != null)
			{
				newRotation = Quaternion.Slerp(controllerLeftHand.transform.rotation, controllerRightHand.transform.rotation, 0.5f);
			}
			// Left controller only
			else if (controllerLeftHand != null && controllerRightHand == null)
			{
				newRotation = controllerLeftHand.transform.rotation;
			}
			// Right controller only
			else if (controllerRightHand != null && controllerLeftHand == null)
			{
				newRotation = controllerRightHand.transform.rotation;
			}
			// No controllers!
			else
			{
				newRotation = Quaternion.identity;
			}

			return newRotation;
		}
	}
}