#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace SentienceLab
{
	[AddComponentMenu("SentienceLab/Tools/Transform Logger")]
	public class TransformLogger : MonoBehaviour
	{
		[Tooltip("Filename of the logfile\n(The string \"{TIMESTAMP}\" will be replaced by an actual timestamp)")]
		public string LogFilename = "TransformLog_{TIMESTAMP}.csv";

		[Tooltip("Separator between values")]
		public string Separator = ",";

		[System.Flags]
		public enum EWhatToLog
		{
			Enabled = 1, Position = 2, Rotation_Quaternion = 4, Rotation_Euler = 8
		}

		public enum ECoordinateSpace
		{
			Global, Local
		}

		[System.Serializable]
		public struct TransformToLog
		{
			public string           name;
			public Transform        transform;
			public EWhatToLog       whatToLog;
			public ECoordinateSpace coordinateSpace;
		}

		[Tooltip("List of transforms to log")]
		public List<TransformToLog> Transforms;

		[System.Flags]
		public enum EWhenToLog
		{
			Update = 1, FixedUpdate = 2
		}

		[Tooltip("When to log the transforms")]
		public EWhenToLog WhenToLog;


		public void Awake()
		{
			Start();
		}


		public void Start()
		{
			// force opening of file
			OpenLogfile();
		}


		protected void OpenLogfile()
		{
			if (m_writer == null && this.enabled)
			{
				try
				{
					// if required, build timestamped filename
					string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
					LogFilename = LogFilename.Replace("{TIMESTAMP}", timestamp);

#if !UNITY_EDITOR
					LogFilename = Application.persistentDataPath + "/" + LogFilename;
#endif
					// special case for TAB as separator
					if (Separator == "\\t")
					{
						Separator = "\t";
					}

					// open logfile and append
					m_writer = new StreamWriter(LogFilename, true);

					WriteHeader();
				}
				catch (System.Exception e)
				{
					Debug.LogWarningFormat("Could not open transform logfile (Reason: {0})", e.ToString());
					this.enabled = false;
				}
			}
		}


		protected void WriteHeader()
		{
			if (m_writer != null)
			{
				                           m_writer.Write("frame");
				if (WhenToLog.HasFlag(EWhenToLog.Update))
				{
					m_writer.Write(Separator); m_writer.Write("time.unscaled");
					m_writer.Write(Separator); m_writer.Write("time.scaled");
				}
				if (WhenToLog.HasFlag(EWhenToLog.FixedUpdate))
				{
					m_writer.Write(Separator); m_writer.Write("fixedTime.unscaled");
					m_writer.Write(Separator); m_writer.Write("fixedTime.scaled");
				}
			}
			foreach (var t in Transforms)
			{
				string name = t.name.Trim();
				if (name.Length == 0) { name = t.transform.gameObject.name; }

				string fieldPrefix = Separator + name;
				if (t.whatToLog.HasFlag(EWhatToLog.Enabled))
				{
					m_writer.Write(fieldPrefix); m_writer.Write(".enabled");
				}
				if (t.whatToLog.HasFlag(EWhatToLog.Position))
				{
					string field = fieldPrefix +
						( (t.coordinateSpace == ECoordinateSpace.Local) ? 
						  ".localPos." : ".globalPos."
						);
					m_writer.Write(field); m_writer.Write("x");
					m_writer.Write(field); m_writer.Write("y");
					m_writer.Write(field); m_writer.Write("z");
				}
				if (t.whatToLog.HasFlag(EWhatToLog.Rotation_Quaternion))
				{
					string field = fieldPrefix +
						( (t.coordinateSpace == ECoordinateSpace.Local) ? 
						  ".localRot." : ".globalRot."
						);
					m_writer.Write(field); m_writer.Write("q.x");
					m_writer.Write(field); m_writer.Write("q.y");
					m_writer.Write(field); m_writer.Write("q.z");
					m_writer.Write(field); m_writer.Write("q.w");
				}
				if (t.whatToLog.HasFlag(EWhatToLog.Rotation_Euler))
				{
					string field = fieldPrefix +
						((t.coordinateSpace == ECoordinateSpace.Local) ?
						  ".localRot." : ".globalRot."
						);
					m_writer.Write(field); m_writer.Write("e.x");
					m_writer.Write(field); m_writer.Write("e.y");
					m_writer.Write(field); m_writer.Write("e.z");
				}
			}
			m_writer.WriteLine();
		}


		private void CloseLogfile()
		{
			// close logfile
			try
			{
				if (m_writer != null)
				{
					m_writer.Close();
					m_writer = null;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarningFormat("Could not close transform logfile (Reason: {0})", e.ToString());
			}
		}


		public void Update()
		{
			if (WhenToLog.HasFlag(EWhenToLog.Update)) LogTransforms();
		}


		public void FixedUpdate()
		{
			if (WhenToLog.HasFlag(EWhenToLog.FixedUpdate)) LogTransforms();
		}


		public void LogTransforms()
		{
			if (m_writer != null)
			{
				m_writer.Write(Time.frameCount);

				if (WhenToLog.HasFlag(EWhenToLog.Update))
				{
					m_writer.Write(Separator); m_writer.Write(Time.timeAsDouble.ToString("F3"));
					m_writer.Write(Separator); m_writer.Write(Time.unscaledTimeAsDouble.ToString("F3"));
				}
				if (WhenToLog.HasFlag(EWhenToLog.FixedUpdate))
				{
					m_writer.Write(Separator); m_writer.Write(Time.fixedTimeAsDouble.ToString("F3"));
					m_writer.Write(Separator); m_writer.Write(Time.fixedUnscaledTimeAsDouble.ToString("F3"));
				}

				foreach (var t in Transforms)
				{
					LogTransform(t);
				}

				m_writer.WriteLine();
				m_writer.Flush();
			}
		}


		protected void LogTransform(TransformToLog _t)
		{
			if (_t.whatToLog.HasFlag(EWhatToLog.Enabled))
			{
				m_writer.Write(Separator); m_writer.Write(_t.transform.gameObject.activeInHierarchy);
			}
			if (_t.whatToLog.HasFlag(EWhatToLog.Position))
			{
				Vector3 p = (_t.coordinateSpace == ECoordinateSpace.Local) ? 
					_t.transform.localPosition : _t.transform.position;
				m_writer.Write(Separator); m_writer.Write(p.x.ToString("F4"));
				m_writer.Write(Separator); m_writer.Write(p.y.ToString("F4"));
				m_writer.Write(Separator); m_writer.Write(p.z.ToString("F4"));
			}
			if (_t.whatToLog.HasFlag(EWhatToLog.Rotation_Quaternion))
			{
				Quaternion q = (_t.coordinateSpace == ECoordinateSpace.Local) ?
					_t.transform.localRotation : _t.transform.rotation;
				m_writer.Write(Separator); m_writer.Write(q.x.ToString("F5"));
				m_writer.Write(Separator); m_writer.Write(q.y.ToString("F5"));
				m_writer.Write(Separator); m_writer.Write(q.z.ToString("F5"));
				m_writer.Write(Separator); m_writer.Write(q.w.ToString("F5"));
			}
			if (_t.whatToLog.HasFlag(EWhatToLog.Rotation_Euler))
			{
				Quaternion q = (_t.coordinateSpace == ECoordinateSpace.Local) ?
					_t.transform.localRotation : _t.transform.rotation;
				Vector3 e = q.eulerAngles;
				m_writer.Write(Separator); m_writer.Write(e.x.ToString("F4"));
				m_writer.Write(Separator); m_writer.Write(e.y.ToString("F4"));
				m_writer.Write(Separator); m_writer.Write(e.z.ToString("F4"));
			}
		}


		public void OnApplicationQuit()
		{
			CloseLogfile();
		}


		private StreamWriter m_writer;
	}
}