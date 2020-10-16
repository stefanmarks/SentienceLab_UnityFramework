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
    public int TouchpadPart;

    [InputControl(layout = "Button")]
    public int ButtonPart;

    [Tooltip("Direction (in degrees) of the button on the touchpad (0=N, 90=E, 180=S, 270=W)")]
    [Range(0,360)]
    public float Direction;

    [Tooltip("Tolerance (in degrees) around the cardinal button direction")]
    [Range(10, 180)]
    public float DirectionTolerance = 45;
    
    [Tooltip("Dead Zone in the centre of the touchpad that does not generate an event")]
    [Range(0, 1)]
    public float DeadZone = 0.25f;


    public override bool ReadValue(ref InputBindingCompositeContext context)
    {
        bool value = false;

        var touchpadPartValue  = context.ReadValue<Vector2, Vector2MagnitudeComparer>(TouchpadPart, _comparer);
        var buttonPartValue    = context.ReadValueAsButton(ButtonPart);

        float mag = touchpadPartValue.magnitude;

        if (buttonPartValue && (mag > DeadZone))
		{
            float angle = Mathf.Atan2(touchpadPartValue.y, touchpadPartValue.x) * Mathf.Rad2Deg;
            // convert from atan angle to "compass angle" (0=N, E=90, S=180, W=270)
            angle = 90 - angle;
            if (angle <   0) { angle += 360; }
            if (angle > 360) { angle -= 360; }
            value = CheckAngle(angle,  Direction);
		}

        return value;
    }


    private bool CheckAngle(float angle, float cardinalAngle)
    {
        float diff = Mathf.Abs(cardinalAngle - angle);
        if (diff > 180) { diff = Mathf.Abs(diff - 360); }
        return diff < DirectionTolerance;
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
