#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace SentienceLab.Tools
{
	/// <summary>
	/// Helper class to run coroutines in the editor.
	/// </summary>
	public static class EditorCoroutines
	{
		static readonly List<IEnumerator> ms_coroutines = new List<IEnumerator>();

		public static void Execute(IEnumerator enumerator)
		{
			if (ms_coroutines.Count == 0)
			{
				EditorApplication.update += Update;
			}
			ms_coroutines.Add(enumerator);
		}

		static void Update()
		{
			for (int i = ms_coroutines.Count - 1; i >= 0; i--)
			{
				var coroutine = ms_coroutines[i];
				bool done = !coroutine.MoveNext();
				if (done)
				{
					ms_coroutines.RemoveAt(i);
				}
			}
			if (ms_coroutines.Count == 0)
			{
				EditorApplication.update -= Update;
			}
		}

		internal static void StopAll()
		{
			ms_coroutines.Clear();
			EditorApplication.update -= Update;
		}
	}
}

#endif
