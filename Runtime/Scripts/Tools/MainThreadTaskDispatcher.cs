﻿#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using System;

namespace SentienceLab
{
	public class MainThreadTaskDispatcher : MonoBehaviour
	{
		public static MainThreadTaskDispatcher Instance
		{
			get
			{
				if (ms_instance == null)
				{
					ms_instance = FindObjectOfType<MainThreadTaskDispatcher>();
				}
				return ms_instance;
			}
		}


		void Awake()
		{
			if (ms_instance == null)
			{
				// preempt search in instance getter
				ms_instance = this;
			}
		}


		public void Update()
		{
			lock (ms_actionQueue)
			{
				while (ms_actionQueue.Count > 0)
				{
					ms_actionQueue.Dequeue().Invoke();
				}
			}
		}


		/// <summary>
		/// Adds an action to the task queue
		/// </summary>
		/// <param name="_action">function to execute from the main thread</param>
		public void Add(Action _action)
		{
			lock (ms_actionQueue) { ms_actionQueue.Enqueue(_action); }
		}


		private static          MainThreadTaskDispatcher ms_instance    = null;
		private static readonly Queue<Action>            ms_actionQueue = new Queue<Action>();
	}
}