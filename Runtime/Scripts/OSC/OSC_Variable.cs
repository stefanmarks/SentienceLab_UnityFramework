#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityOSC;

namespace SentienceLab.OSC
{
	public interface IOSCVariableContainer
	{
		List<OSC_Variable> GetOSC_Variables();
	}


	public abstract class OSC_Variable
	{
		public string Name;

		/// <summary>
		/// Class for delegates that are notified when an OSC variable has received new data.
		/// </summary>
		/// <param name="var">the variable that has received data</param>
		/// 
		public delegate void DataReceived(OSC_Variable var);
		public event DataReceived OnDataReceived;


		public OSC_Variable(string _name = "")
		{
			Name      = _name;
			m_manager = null;
		}


		public void SetManager(OSC_Manager _manager)
		{
			m_manager = _manager;
		}


		public bool CanAccept(OSCPacket _packet)
		{
			return _packet.Address.CompareTo(Name) == 0;
		}


		public void Accept(OSCPacket _packet)
		{
			Unpack(_packet);
			m_packetReceived = true;
		}


		public abstract void Unpack(OSCPacket _packet);


		public void SendUpdate()
		{
			if (m_manager == null)
			{
				// see if the manager instance exists
				m_manager = OSC_Manager.Instance;
			}

			if (m_manager != null)
			{
				OSCPacket packet = new OSCMessage(Name);
				Pack(packet);
				m_manager.SendPacket(packet);
			}
		}


		public abstract void Pack(OSCPacket _packet);


		public void Update()
		{
			// notify listener within the main Unity update loop
			if (m_packetReceived)
			{
				if (OnDataReceived != null) OnDataReceived.Invoke(this);
				m_packetReceived = false;
			}
		}


		private OSC_Manager m_manager;
		private bool        m_packetReceived;
	}


	public class OSC_BoolVariable : OSC_Variable
	{
		public bool Value;

		public OSC_BoolVariable(string _name = "") : base(_name)
		{
			Value = false;
		}


		public override void Unpack(OSCPacket _packet)
		{
			object obj = _packet.Data[0];
			System.Type type = obj.GetType();
			if      (type == typeof(byte)  ) { Value = ((byte)  obj) > 0; }
			else if (type == typeof(int)   ) { Value = ((int)   obj) > 0; }
			else if (type == typeof(long)  ) { Value = ((long)  obj) > 0; }
			else if (type == typeof(float) ) { Value = ((float) obj) > 0; }
			else if (type == typeof(double)) { Value = ((double)obj) > 0; }
		}


		public override void Pack(OSCPacket _packet)
		{
			_packet.Append<int>(Value ? 1 : 0);
		}
	}


	public class OSC_IntVariable : OSC_Variable
	{
		public int Value;
		public int Min, Max;


		public OSC_IntVariable(string _name = "", int _min = int.MinValue, int _max = int.MaxValue) : base(_name)
		{
			Value = 0;
			Min = _min;
			Max = _max;
		}


		public override void Unpack(OSCPacket _packet)
		{
			object obj = _packet.Data[0];
			System.Type type = obj.GetType();
			if      (type == typeof(byte)  ) { Value = (byte)         obj ; }
			else if (type == typeof(int)   ) { Value = (int)          obj ; }
			else if (type == typeof(long)  ) { Value = (int)((long)   obj); }
			else if (type == typeof(float) ) { Value = (int)((float)  obj); }
			else if (type == typeof(double)) { Value = (int)((double) obj); }

			if (Value > Max) { Value = Max; }
			if (Value < Min) { Value = Min; }
		}


		public override void Pack(OSCPacket _packet)
		{
			_packet.Append<int>(Value);
		}
	}


	public class OSC_FloatVariable : OSC_Variable
	{
		public float Value;
		public float Min, Max;


		public OSC_FloatVariable(string _name = "", float _min = 0, float _max = 1) : base(_name)
		{
			Value = 0;
			Min = _min;
			Max = _max;
		}


		public override void Unpack(OSCPacket _packet)
		{
			object obj = _packet.Data[0];
			System.Type type = obj.GetType();
			if      (type == typeof(byte)  ) { Value = (byte)  obj; }
			else if (type == typeof(int)   ) { Value = (int)   obj; }
			else if (type == typeof(long)  ) { Value = (long)  obj; }
			else if (type == typeof(float) ) { Value = (float) obj; }
			else if (type == typeof(double)) { Value = (float)((double) obj); }

			if (Value > Max) { Value = Max; }
			if (Value < Min) { Value = Min; }
		}


		public override void Pack(OSCPacket _packet)
		{
			_packet.Append<float>(Value);
		}
	}


	public class OSC_Vector2Variable : OSC_Variable
	{
		public Vector2 Value;


		public OSC_Vector2Variable(string _name = "") : base(_name)
		{
			Value = Vector2.zero;
		}


		public override void Unpack(OSCPacket _packet)
		{
			for (int idx = 0; idx < _packet.Data.Count; idx++)
			{
				object obj = _packet.Data[idx];
				System.Type type = obj.GetType();
				float value = 0;
				if      (type == typeof(byte)  ) { value = (byte)  obj; }
				else if (type == typeof(int)   ) { value = (int)   obj; }
				else if (type == typeof(long)  ) { value = (long)  obj; }
				else if (type == typeof(float) ) { value = (float) obj; }
				else if (type == typeof(double)) { value = (float)((double)obj); }

				switch (idx)
				{
					case 0: Value.x = value; break;
					case 1: Value.y = value; break;
					default: break;
				}
			}
		}

		public override void Pack(OSCPacket _packet)
		{
			_packet.Append<float>(Value.x);
			_packet.Append<float>(Value.y);
		}
	}


	public class OSC_Vector3Variable : OSC_Variable
	{
		public Vector3 Value;


		public OSC_Vector3Variable(string _name = "") : base(_name)
		{
			Value = Vector3.zero;
		}


		public override void Unpack(OSCPacket _packet)
		{
			for (int idx = 0; idx < _packet.Data.Count; idx++)
			{
				object obj = _packet.Data[idx];
				System.Type type = obj.GetType();
				float value = 0;
				if      (type == typeof(byte)  ) { value = (byte)  obj; }
				else if (type == typeof(int)   ) { value = (int)   obj; }
				else if (type == typeof(long)  ) { value = (long)  obj; }
				else if (type == typeof(float) ) { value = (float) obj; }
				else if (type == typeof(double)) { value = (float)((double)obj); }

				switch (idx)
				{
					case 0: Value.x = value; break;
					case 1: Value.y = value; break;
					case 2: Value.z = value; break;
					default: break;
				}
			}
		}


		public override void Pack(OSCPacket _packet)
		{
			_packet.Append<float>(Value.x);
			_packet.Append<float>(Value.y);
			_packet.Append<float>(Value.z);
		}
	}


	public class OSC_6DofPoseVariable : OSC_Variable
	{
		public    Vector3    Position;
		public    Quaternion Rotation;
		
		public enum EDataFormat
		{
			Pos_RotEuler, Pos_RotQuat, RotEuler_Pos, RotQuat_Pos
		}

		protected EDataFormat DataFormat;

		public OSC_6DofPoseVariable(string _name = "", bool _posFirst = true) : base(_name)
		{
			Position   = Vector3.zero;
			Rotation   = Quaternion.identity;
			DataFormat = _posFirst ? EDataFormat.Pos_RotEuler : EDataFormat.RotEuler_Pos;
		}


		public OSC_6DofPoseVariable(string _name, EDataFormat _dataFormat) : base(_name)
		{
			Position   = Vector3.zero;
			Rotation   = Quaternion.identity;
			DataFormat = _dataFormat;
		}


		public override void Unpack(OSCPacket _packet)
		{
			Vector3 euler = Rotation.eulerAngles;
			for (int idx = 0; idx < _packet.Data.Count; idx++)
			{
				object obj = _packet.Data[idx];
				System.Type type = obj.GetType();
				float value = 0;
				if      (type == typeof(byte)  ) { value = (byte)  obj; }
				else if (type == typeof(int)   ) { value = (int)   obj; }
				else if (type == typeof(long)  ) { value = (long)  obj; }
				else if (type == typeof(float) ) { value = (float) obj; }
				else if (type == typeof(double)) { value = (float)((double)obj); }
				
				switch (idx)
				{
					case 0: 
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotEuler: Position.x = value; break;
							case EDataFormat.Pos_RotQuat:  Position.x = value; break;
							case EDataFormat.RotEuler_Pos: euler.x    = value; break;
							case EDataFormat.RotQuat_Pos:  Rotation.w = value; break;
						}
						break;

					case 1:
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotEuler: Position.y = value; break;
							case EDataFormat.Pos_RotQuat:  Position.y = value; break;
							case EDataFormat.RotEuler_Pos: euler.y    = value; break;
							case EDataFormat.RotQuat_Pos:  Rotation.x = value; break;
						}
						break;

					case 2:
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotEuler: Position.z = value; break;
							case EDataFormat.Pos_RotQuat:  Position.z = value; break;
							case EDataFormat.RotEuler_Pos: euler.z    = value; break;
							case EDataFormat.RotQuat_Pos:  Rotation.y = value; break;
						}
						break;

					case 3:
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotEuler: euler.x    = value; break;
							case EDataFormat.Pos_RotQuat:  Rotation.w = value; break;
							case EDataFormat.RotEuler_Pos: Position.x = value; break;
							case EDataFormat.RotQuat_Pos:  Rotation.z = value; break;
						}
						break;

					case 4:
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotEuler: euler.y    = value; break;
							case EDataFormat.Pos_RotQuat:  Rotation.x = value; break;
							case EDataFormat.RotEuler_Pos: Position.y = value; break;
							case EDataFormat.RotQuat_Pos:  Position.x = value; break;
						}
						break;

					case 5:
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotEuler: euler.z    = value; break;
							case EDataFormat.Pos_RotQuat:  Rotation.y = value; break;
							case EDataFormat.RotEuler_Pos: Position.z = value; break;
							case EDataFormat.RotQuat_Pos:  Position.y = value; break;
						}
						break;

					case 6:
						switch (DataFormat)
						{
							case EDataFormat.Pos_RotQuat: Rotation.z = value; break;
							case EDataFormat.RotQuat_Pos: Position.z = value; break;
						}
						break;

					default: break;
				}
			}
			if ((DataFormat == EDataFormat.Pos_RotEuler) || (DataFormat == EDataFormat.RotEuler_Pos))
			{
				Rotation.eulerAngles = euler;
			}
		}


		public override void Pack(OSCPacket _packet)
		{
			switch (DataFormat)
			{
				case EDataFormat.Pos_RotEuler:
					{
						Vector3 euler = Rotation.eulerAngles;
						_packet.Append<float>(Position.x);
						_packet.Append<float>(Position.y);
						_packet.Append<float>(Position.z);
						_packet.Append<float>(euler.x);
						_packet.Append<float>(euler.y);
						_packet.Append<float>(euler.z);
						break;
					}

				case EDataFormat.RotEuler_Pos:
					{
						Vector3 euler = Rotation.eulerAngles;
						_packet.Append<float>(euler.x);
						_packet.Append<float>(euler.y);
						_packet.Append<float>(euler.z);
						_packet.Append<float>(Position.x);
						_packet.Append<float>(Position.y);
						_packet.Append<float>(Position.z);
						break;
					}

				case EDataFormat.Pos_RotQuat:
					{
						_packet.Append<float>(Position.x);
						_packet.Append<float>(Position.y);
						_packet.Append<float>(Position.z);
						_packet.Append<float>(Rotation.w);
						_packet.Append<float>(Rotation.x);
						_packet.Append<float>(Rotation.y);
						_packet.Append<float>(Rotation.z);
						break;
					}

				case EDataFormat.RotQuat_Pos:
					{
						_packet.Append<float>(Rotation.w);
						_packet.Append<float>(Rotation.x);
						_packet.Append<float>(Rotation.y);
						_packet.Append<float>(Rotation.z);
						_packet.Append<float>(Position.x);
						_packet.Append<float>(Position.y);
						_packet.Append<float>(Position.z);
						break;
					}

				default: break;
			}
		}
	}
	
	
	public class OSC_StringVariable : OSC_Variable
	{
		public string Value;


		public OSC_StringVariable(string _name = "") : base(_name)
		{
			Value = "";
		}


		public override void Unpack(OSCPacket _packet)
		{
			object obj = _packet.Data[0];
			System.Type type = obj.GetType();
			if      (type == typeof(string)) { Value = (string) obj; }
			/*
			else if (type == typeof(byte)  ) { value = (byte)obj; }
			else if (type == typeof(int)   ) { value = (int)obj; }
			else if (type == typeof(long)  ) { value = (int) ((long)obj); }
			else if (type == typeof(float) ) { value = (int) ((float)obj); }
			else if (type == typeof(double)) { value = (int) ((double)obj); }
			*/
		}


		public override void Pack(OSCPacket _packet)
		{
			_packet.Append<string>(Value);
		}
	}


	public class OSC_VariableComparer : IComparer<OSC_Variable>
	{
		public int Compare(OSC_Variable x, OSC_Variable y)
		{
			return string.Compare(x.Name, y.Name);
		}

		public static readonly OSC_VariableComparer Instance = new OSC_VariableComparer();
	}

}
