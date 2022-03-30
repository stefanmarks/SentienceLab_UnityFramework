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
	///   > This locomotion method is based on Immersive Movement, originally created by Highsight. Thanks to KJack (author of Arm Swinger) for additional work.
	/// </remarks>
	/// <example>
	/// `VRTK/Examples/042_CameraRig_MoveInPlace` demonstrates how the user can move and traverse colliders by either swinging the controllers in a walking fashion or by running on the spot utilisng the head bob for movement.
	/// </example>
	[AddComponentMenu("SentienceLab/Interaction/Locomotion/WalkInPlace")]
	public class WalkInPlace : MonoBehaviour
	{
		/// <summary>
		/// Which tracked devices are used for determining the movement amount.
		/// </summary>
		/// <param name="HeadsetAndControllers">Track both headset and controllers for movement calculations.</param>
		/// <param name="ControllersOnly">Track only the controllers for movement calculations.</param>
		/// <param name="HeadsetOnly">Track only headset for movement caluclations.</param>
		public enum MovementSourceOption
		{
			HeadsetAndControllers,
			ControllersOnly,
			HeadsetOnly,
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

		[Tooltip("Select action to engage Move In Place.")]
		public InputActionProperty engageAction;

		[Tooltip("Select which trackables are used to determine movement.")]
		public MovementSourceOption controlOptions = MovementSourceOption.HeadsetAndControllers;

		[Tooltip("How the user's movement direction will be determined.  The Gaze method tends to lead to the least motion sickness.  Smart decoupling is still a Work In Progress.")]
		public DirectionSourceOption directionMethod = DirectionSourceOption.Gaze;

		[Header("Speed Settings")]

		[Tooltip("Factor to scale head movement by for speed calculation.")]
		public float headsetSpeedScale = 10;
		[Tooltip("Factor to scale controller movement by for speed calculation.")]
		public float controllerSpeedScale = 5;
		[Tooltip("The max speed the user can move in game units.")]
		public float maxSpeed = 4;
		[Tooltip("The speed in which the play area slows down to a complete stop when the user is no longer pressing the engage button. This deceleration effect can ease any motion sickness that may be suffered.")]
		public float deceleration = 8;
		[Tooltip("The speed in which the play area slows down to a complete stop when the user is falling.")]
		public float fallingDeceleration = 0.01f;

		[Header("Advanced Settings")]

		[Tooltip("Time period for averaging tracking samples [s]")]
		[Range(0.01f, 1)]
		public float averagingPeriod = 0.1f;

		[Tooltip("The degree threshold that all tracked objects (controllers, headset) must be within to change direction when using the Smart Decoupling Direction Method.")]
		public float smartDecoupleThreshold = 30f;
		// The cap before we stop adding the delta to the movement list. This will help regulate speed.
		[Tooltip("Movement beyond this delta will be ignored as it might be from tracking jitter.")]
		public float deltaThreshold = 0.15f;


		/// <summary>
		/// Structure for keeping movement data for each tracked object
		/// </summary>
		protected class MovementData
		{
			public Transform   trackedObject;
			public Vector3     lastPosition;
			public List<float> deltas;
			public float       scaleFactor;

			public MovementData(Transform _t, float _scaleFactor)
			{
				deltas        = new List<float>();
				trackedObject = _t;
				lastPosition  = _t.localPosition;
				scaleFactor   = _scaleFactor;
			}

			public void Update()
			{
				lastPosition = trackedObject.localPosition;
			}


			public void Reset()
			{
				deltas.Clear();
			}


			public void ProcessDelta(float deltaThreshold, int maxBufferSize)
			{
				// Get the amount of Y movement that's occured since the last update.
				float delta = Mathf.Abs(lastPosition.y - trackedObject.localPosition.y);

				// add delta unless larger than tracking jitter threshold
				if (delta < deltaThreshold)
				{
					deltas.Add(delta);
				}

				// limit buffer size
				if (deltas.Count > maxBufferSize)
				{
					deltas.RemoveRange(deltas.Count, maxBufferSize - deltas.Count);
				}
			}

			public float GetAverageDelta()
			{
				float sum = 0;
				foreach (var delta in deltas) { sum += delta; }
				float average = sum / Mathf.Max(1, deltas.Count);
				return average * scaleFactor;
			}
		}

		// Ttracked objects to use to determine amount of movement.
		protected List<MovementData> trackedObjects;
		// controller that initiated the engage action
		protected Transform engageController;
		// Used to determine the direction when using a decoupling method.
		protected Vector3 initalGaze;
		// The current move speed of the player. If Move In Place is not active, it will be set to 0.00f.
		protected float currentSpeed;
		// The current direction the player is moving. If Move In Place is not active, it will be set to Vector.zero.
		protected Vector3 direction;
		protected Vector3 previousDirection;
		// True if Move In Place is currently engaged.
		protected bool active;
		protected bool currentlyFalling;


		/// <summary>
		/// Set the control options and modify the trackables to match.
		/// </summary>
		/// <param name="givenControlOptions">The control options to set the current control options to.</param>
		public virtual void SetControlOptions(MovementSourceOption givenControlOptions)
		{
			controlOptions = givenControlOptions;
			trackedObjects.Clear();

			if (controllerLeftHand != null && controllerRightHand != null && (controlOptions.Equals(MovementSourceOption.HeadsetAndControllers) || controlOptions.Equals(MovementSourceOption.ControllersOnly)))
			{
				trackedObjects.Add(new MovementData(controllerLeftHand,  controllerSpeedScale));
				trackedObjects.Add(new MovementData(controllerRightHand, controllerSpeedScale));
			}

			if (headset != null && (controlOptions.Equals(MovementSourceOption.HeadsetAndControllers) || controlOptions.Equals(MovementSourceOption.HeadsetOnly)))
			{
				trackedObjects.Add(new MovementData(headset, headsetSpeedScale));
			}
		}

		/// <summary>
		/// The GetMovementDirection method will return the direction the player is moving.
		/// </summary>
		/// <returns>Returns a vector representing the player's current movement direction.</returns>
		public virtual Vector3 GetMovementDirection()
		{
			return direction;
		}

		/// <summary>
		/// The GetSpeed method will return the current speed the player is moving at.
		/// </summary>
		/// <returns>Returns a float representing the player's current movement speed.</returns>
		public virtual float GetSpeed()
		{
			return currentSpeed;
		}


		public void Start()
		{
			trackedObjects    = new List<MovementData>();
			initalGaze        = Vector3.zero;
			direction         = Vector3.zero;
			previousDirection = Vector3.zero;
			currentSpeed      = 0f;
			active            = false;
			engageController  = null;

			engageAction.action.performed += OnEngageActionPerformed;
			engageAction.action.canceled  += OnEngageActionCanceled;
			engageAction.action.Enable();

			SetControlOptions(controlOptions);
		}


		public void Update()
		{
			// nothing to do here
		}


		protected virtual void FixedUpdate()
		{
			HandleFalling();
			
			if (IsActive() && !currentlyFalling)
			{
				// Calculate the average movement
				currentSpeed      = Mathf.Min(maxSpeed, CalculateAverageMovement() / Time.fixedDeltaTime);
				previousDirection = direction;
				direction         = CalculateDirection();
			}
			else if (currentSpeed > 0f)
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

			UpdateTrackedObjectsState();
			MoveTrackingOffset(direction, currentSpeed);
		}


		public bool IsActive()
		{
			return active;
		}


		protected float CalculateAverageMovement()
		{
			float averageMovement      = 0;
			int   averagingBufferCount = (int) Mathf.Max(1, averagingPeriod / Time.fixedDeltaTime);

			foreach (var trackedObject in trackedObjects)
			{
				trackedObject.ProcessDelta(deltaThreshold, averagingBufferCount);
				averageMovement += trackedObject.GetAverageDelta();
			}
			averageMovement /= Mathf.Max(1, trackedObjects.Count);

			return averageMovement;
		}


		protected virtual Vector3 CalculateDirection()
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

		protected virtual Vector3 CalculateControllerRotationDirection(Vector3 calculatedControllerDirection)
		{
			return (Vector3.Angle(previousDirection, calculatedControllerDirection) <= 90f ? calculatedControllerDirection : previousDirection);
		}

		protected virtual void UpdateTrackedObjectsState()
		{
			foreach (var trackedObject in trackedObjects)
			{
				trackedObject.Update();
			}
		}

		protected virtual void MoveTrackingOffset(Vector3 moveDirection, float moveSpeed)
		{
			Vector3 movement = (moveDirection * moveSpeed) * Time.fixedDeltaTime;
			Vector3 finalPosition = new Vector3(movement.x + trackingOffset.position.x, trackingOffset.position.y, movement.z + trackingOffset.position.z);
			if (trackingOffset != null && CanMove(trackingOffset.position, finalPosition))
			{
				trackingOffset.position = finalPosition;
			}
		}

		protected virtual bool CanMove(Vector3 currentPosition, Vector3 proposedPosition)
		{
			/*if (givenBodyPhysics == null)
			{
				return true;
			}

			Vector3 proposedDirection = (proposedPosition - currentPosition).normalized;
			float distance = Vector3.Distance(currentPosition, proposedPosition);
			return !givenBodyPhysics.SweepCollision(proposedDirection, distance);*/
			return true;
		}

		protected virtual void HandleFalling()
		{
			/*if (bodyPhysics != null && bodyPhysics.IsFalling())
			{
				currentlyFalling = true;
			}

			if (bodyPhysics != null && !bodyPhysics.IsFalling() && currentlyFalling)
			{
				currentlyFalling = false;
				currentSpeed = 0f;
			}*/
		}

		protected void OnEngageActionPerformed(InputAction.CallbackContext _)
		{
			// TODO: might need to find out which controller did that
			// engageController = ...
			active = true;
		}

		protected void OnEngageActionCanceled(InputAction.CallbackContext _)
		{
			// If the button is released, clear all the lists.
			foreach (var movementData in trackedObjects)
			{
				movementData.Reset();
			}
			initalGaze = Vector3.zero;

			active = false;
			// engagedController = null;
		}

		protected virtual Quaternion DetermineAverageControllerRotation()
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