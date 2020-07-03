#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;
using UnityEditor;


#if UNITY_EDITOR
    [InitializeOnLoad] // Automatically register in editor.
#endif

[Preserve]
[DisplayStringFormat("{touchpad}+{button}")]
public class TouchpadButtonComposite : InputBindingComposite<bool>
{
    [InputControl(layout = "2DVector")]
    public int touchpadPart;

    [InputControl(layout = "Button")]
    public int buttonPart;

    public enum Direction {  North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }

    [Tooltip("Directional button to check for")]
    public Direction direction;

    [Tooltip("Tolerance (in degrees) around the cardinal button direction")]
    [Range(10, 180)]
    public float directionTolerance = 45;
    
    [Tooltip("Dead Zone in the centre of the touchpad that does not generate an event")]
    [Range(0, 1)]
    public float deadZone = 0.25f;


    public override bool ReadValue(ref InputBindingCompositeContext context)
    {
        bool value = false;

        var touchpadPartValue  = context.ReadValue<Vector2, Vector2MagnitudeComparer>(touchpadPart, _comparer);
        var buttonPartValue    = context.ReadValueAsButton(buttonPart);

        float angle = Mathf.Atan2(touchpadPartValue.y, touchpadPartValue.x) * Mathf.Rad2Deg;
        float mag   = touchpadPartValue.magnitude;

        if (buttonPartValue && (mag > deadZone))
		{
            //            +90
            //             |
            // +180/-180 --+--  0
            //             |
            //            -90
            switch (direction)
			{
                case Direction.North:     value = CheckAngle(angle,   90); break;
                case Direction.NorthEast: value = CheckAngle(angle,   45); break;
                case Direction.East:      value = CheckAngle(angle,    0); break;
                case Direction.SouthEast: value = CheckAngle(angle,  -45); break;
                case Direction.South:     value = CheckAngle(angle,  -90); break;
                case Direction.SouthWest: value = CheckAngle(angle, -135); break;
                case Direction.West:      value = CheckAngle(angle,  180); break;
                case Direction.NorthWest: value = CheckAngle(angle,  135); break;
			}
		}

        return value;
    }


    private bool CheckAngle(float angle, float cardinalAngle)
    {
        float diff = Mathf.Abs(cardinalAngle - angle);
        if (diff > 180) { diff = Mathf.Abs(diff - 360); }
        return diff < directionTolerance;
    }

    public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
    {
        return ReadValue(ref context) ? 1 : 0;
    }


    static TouchpadButtonComposite()
    {
        InputSystem.RegisterBindingComposite<TouchpadButtonComposite>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() { } // Trigger static constructor.

    private Vector2MagnitudeComparer _comparer = new Vector2MagnitudeComparer();
}
