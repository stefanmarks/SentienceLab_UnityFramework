#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

// Attribute to mark inspector elements as readonly
// https://medium.com/@ArianKhatiban/simple-read-only-attribute-in-inspector-unity-editor-6562e29367c6

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
        // Disable the UI so it can't be edited
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        // Re-enable it for the next fields
        GUI.enabled = true;
    }
}
