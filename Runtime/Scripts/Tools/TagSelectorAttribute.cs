#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

// Attribute to make it easier to edit tags or lists of tags
// https://answers.unity.com/questions/1378822/list-of-tags-in-the-inspector.html

using UnityEngine;

public class TagSelectorAttribute : PropertyAttribute
{
	public bool UseDefaultTagFieldDrawer = false;
}

