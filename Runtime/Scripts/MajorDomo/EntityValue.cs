#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System;
using UnityEngine;

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for managing a MajorDomo entity with methods for synchronising 
	/// from and to MajorDomo data structures and UnityEngien transformations.
	/// </summary>
	/// 
	public class EntityValue
	{
		/// <summary> Name of the entity value </summary>
		public string Name  { get; private set; }

		/// <summary> Index of the value in the list of values </summary>
		public byte Index { get; private set; } 

		/// <summary> Type of the entity value </summary>
		public AUT_WH.MajorDomoProtocol.EntityValueType Type { get; private set; }


		/// <summary> Value name for the "enabled" state </summary>
		public static readonly string ENABLED  = "enabled";
		/// <summary> Value name for the position vector </summary>
		public static readonly string POSITION = "pos";
		/// <summary> Value name for the linear velocity vector </summary>
		public static readonly string VELOCITY = "vel";
		/// <summary> Value name for the rotation quaternion </summary>
		public static readonly string ROTATION = "rot";
		/// <summary> Value name for the rotational velocity vector</summary>
		public static readonly string VELOCITY_ROTATION = "velr";
		/// <summary> Value name for the scale vector </summary>
		public static readonly string SCALE    = "scale";
		/// <summary> Value name for the template name </summary>
		public static readonly string TEMPLATE = "template";



		public EntityValue(string _name, AUT_WH.MajorDomoProtocol.EntityValueType _type, byte _index, ushort _size)
		{
			Name  = _name;
			Type  = _type;
			Index = _index;
			m_data   = new byte[_size];
			m_buffer = new FlatBuffers.ByteBuffer(m_data);
			m_modified = false;
			m_updated  = false;
			m_fixedSize = (_size > 0); // size > 0 indicates a fixed size buffer, size 0 as length indicates a variable length
		}


		public FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityValueInformation> WriteEntityValueInformation(FlatBuffers.FlatBufferBuilder _builder)
		{
			return AUT_WH.MajorDomoProtocol.EntityValueInformation.CreateEntityValueInformation(_builder,
				_builder.CreateString(Name),
				Type,
				AUT_WH.MajorDomoProtocol.EntityValue.CreateValueVector(_builder, (sbyte[])(Array)m_data));
		}


		public FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityValue> WriteEntityValue(FlatBuffers.FlatBufferBuilder _builder)
		{
			return AUT_WH.MajorDomoProtocol.EntityValue.CreateEntityValue(_builder, 
				(sbyte) Index, 
				Type,
				AUT_WH.MajorDomoProtocol.EntityValue.CreateValueVector(_builder, (sbyte[])(Array)m_data));
		}


		public void ReadEntityValue(AUT_WH.MajorDomoProtocol.EntityValueInformation _information)
		{
			int dataSize = _information.ValueLength;

			// is this a variable size value?
			if ((dataSize != m_data.Length) && !m_fixedSize)
			{
				// yes: adapt buffer
				m_data = new byte[dataSize];
				m_buffer = new FlatBuffers.ByteBuffer(m_data);
			}
			else
			{
				// no: limit to data size
				dataSize = Math.Min(dataSize, m_data.Length);
			}

			for (int i = 0; i < dataSize; i++)
			{
				m_data[i] = (byte)_information.Value(i);
			}

			m_updated = true;
		}


		public bool ReadEntityValue(AUT_WH.MajorDomoProtocol.EntityValue _value)
		{
			bool modified = false;
			int dataSize = _value.ValueLength;

			// is this a variable size value?
			if ((dataSize != m_data.Length) && !m_fixedSize)
			{
				// yes: adapt buffer
				m_data = new byte[dataSize];
				m_buffer = new FlatBuffers.ByteBuffer(m_data);
				modified = true;
			}
			else
			{
				// no: limit to data size
				dataSize = Math.Min(dataSize, m_data.Length);
			}

			// copy over the bytes, checking for changes
			for (int i = 0 ; i < dataSize; i++)
			{
				byte b = (byte)_value.Value(i);
				modified |= (m_data[i] != b);
				m_data[i] = b;
			}
			m_updated |= modified;
			return modified;
		}


		public bool IsModified()
		{
			return m_modified;
		}


		public void ResetModified()
		{
			m_modified = false;
		}


		public bool IsUpdated()
		{
			return m_updated;
		}


		public void ResetUpdated()
		{
			m_updated = false;
		}
	

		public bool Equals(EntityValue _other)
		{
			return (Name == _other.Name) && (Type == _other.Type);
		}


		override public string ToString()
		{
			return Index + ":" + Name + ":" + Type.ToString();
		}


		public static EntityValue GenerateEntityValueInstance(string _name, byte _index, AUT_WH.MajorDomoProtocol.EntityValueType _type)
		{
			switch (_type)
			{
				case AUT_WH.MajorDomoProtocol.EntityValueType.Boolean:    return new EntityValue_Boolean(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Byte:       return new EntityValue_Byte(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Int16:      return new EntityValue_Int16(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Int32:      return new EntityValue_Int32(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Int64:      return new EntityValue_Int64(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Float32:    return new EntityValue_Float32(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Float64:    return new EntityValue_Float64(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.String:     return new EntityValue_String(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Color:      return new EntityValue_Color(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Vector3D:   return new EntityValue_Vector3D(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.Quaternion: return new EntityValue_Quaternion(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.ByteArray:  return new EntityValue_ByteArray(_name, _index);
				case AUT_WH.MajorDomoProtocol.EntityValueType.FloatArray: return new EntityValue_FloatArray(_name, _index);
			}
			return null;
		}


		/// <summary>Flag for modification by a client</summary>
		protected bool m_modified;
	
		/// <summary>Flag for update by the server</summary>
		protected bool m_updated;

		/// <summary> \c true when the size of the value never changes </summary>
		protected bool m_fixedSize;

		/// <summary> Byte buffer for value </summary>
		protected byte[] m_data;
	
		/// <summary> Byte buffer encapsulating reading/writing of the buffer </summary>
		protected FlatBuffers.ByteBuffer m_buffer;
	}


	public class EntityValue_Boolean : EntityValue
	{
		public EntityValue_Boolean(string _name, byte _index, bool _value = false) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Boolean, _index, sizeof(byte)) { Value = _value; }
		public bool Value { get { return m_buffer.Get(0) != 0; } set { m_buffer.PutByte(0, (byte) (value ? 1 : 0)); } }
		public void Modify(bool _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_Byte : EntityValue
	{
		public EntityValue_Byte(string _name, byte _index, byte _value = 0) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Byte, _index, sizeof(byte)) { Value = _value; }
		public byte Value { get { return m_buffer.Get(0); } set { m_buffer.PutByte(0, value); } }
		public void Modify(Byte _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_Int16 : EntityValue
	{
		public EntityValue_Int16(string _name, byte _index, short _value = 0) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int16, _index, sizeof(short)) { Value = _value; }
		public short Value { get { return m_buffer.GetShort(0); } set { m_buffer.PutShort(0, value); } }
		public void Modify(short _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_Int32 : EntityValue
	{
		public EntityValue_Int32(string _name, byte _index, int _value = 0) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int32, _index, sizeof(int)) { Value = _value; }
		public int Value { get { return m_buffer.GetInt(0); } set { m_buffer.PutInt(0, value); } }
		public void Modify(int _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_Int64 : EntityValue
	{
		public EntityValue_Int64(string _name, byte _index, long _value = 0) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int64, _index, sizeof(long)) { Value = _value; }
		public long Value { get { return m_buffer.GetLong(0); } set { m_buffer.PutLong(0, value); } }
		public void Modify(long _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_Float32 : EntityValue
	{
		public EntityValue_Float32(string _name, byte _index, float _value = 0) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Float32, _index, sizeof(float)) { Value = _value; }
		public float Value { get { return m_buffer.GetFloat(0); } set { m_buffer.PutFloat(0, value); } }
		public void Modify(float _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_Float64 : EntityValue
	{
		public EntityValue_Float64(string _name, byte _index, double _value = 0) : base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Float64, _index, sizeof(double)) { Value = _value; }
		public double Value { get { return m_buffer.GetDouble(0); } set { m_buffer.PutDouble(0, value); } }
		public void Modify(double _value) { if (Value != _value) { Value = _value; m_modified = true; } }
		override public string ToString() { return base.ToString() + "=" + Value; }
	}


	public class EntityValue_String : EntityValue
	{
		public EntityValue_String(string _name, byte _index, string _value = "") :
			base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.String, _index, 0)
		{
			Value = _value;
		}

		public string Value
		{
			get
			{
				return System.Text.Encoding.UTF8.GetString(m_data);
			}
			set
			{
				m_data = System.Text.Encoding.UTF8.GetBytes(value);
			}
		}

		public void Modify(string _value)
		{
			if (Value != _value)
			{
				Value = _value;
				m_modified = true;
			}
		}

		override public string ToString() { return base.ToString() + "='" + Value + "'"; }
	}


	public class EntityValue_Color : EntityValue
	{
		public EntityValue_Color(string _name, byte _index, Color32 _value) :
			base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Color, _index, 4)
		{
			Value = _value;
		}

		public EntityValue_Color(string _name, byte _index) :
			this(_name, _index, Color.black)
		{ }

		public Color32 Value
		{
			get
			{
				// order: ARGB
				m_tmpColor.a = m_data[0];
				m_tmpColor.r = m_data[1];
				m_tmpColor.g = m_data[2];
				m_tmpColor.b = m_data[3];
				return m_tmpColor;
			}
			set
			{
				m_data[0] = value.a;
				m_data[1] = value.r;
				m_data[2] = value.g;
				m_data[3] = value.b;
			}
		}

		public void Modify(Color32 _value)
		{
			Value = _value;
			m_modified = true;
		}

		override public string ToString() { return base.ToString() + "=(" + Value.ToString() + ")"; }

		private Color32 m_tmpColor = Color.black;
	}


	public class EntityValue_Vector3D : EntityValue
	{
		public EntityValue_Vector3D(string _name, byte _index, Vector3 _value) :
			base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Vector3D, _index, 3 * sizeof(float))
		{
			Value = _value;
		}

		public EntityValue_Vector3D(string _name, byte _index) : 
			this(_name, _index, Vector3.zero) { }

		public Vector3 Value
		{
			get {
				tempVec.Set(m_buffer.GetFloat(0), m_buffer.GetFloat(4), m_buffer.GetFloat(8));
				return tempVec;
			}
			set {
				m_buffer.PutFloat(0, value.x);
				m_buffer.PutFloat(4, value.y);
				m_buffer.PutFloat(8, value.z);
			}
		}

		public void Modify(Vector3 _value)
		{
			if (Value != _value)
			{
				Value = _value;
				m_modified = true;
			}
		}

		override public string ToString() { return base.ToString() + "=" + Value.ToString(); }

		private Vector3 tempVec = Vector3.zero;
	}


	public class EntityValue_Quaternion : EntityValue
	{
		public EntityValue_Quaternion(string _name, byte _index, Quaternion _value) :
			base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Quaternion, _index, 4 * sizeof(float))
		{
			Value = _value;
		}

		public EntityValue_Quaternion(string _name, byte _index) :
			this(_name, _index, Quaternion.identity)
		{ }

		public Quaternion Value
		{
			get
			{
				m_tmpQuaternion.Set(m_buffer.GetFloat(0), m_buffer.GetFloat(4), m_buffer.GetFloat(8), m_buffer.GetFloat(12));
				return m_tmpQuaternion;
			}
			set
			{
				m_buffer.PutFloat( 0, value.x);
				m_buffer.PutFloat( 4, value.y);
				m_buffer.PutFloat( 8, value.z);
				m_buffer.PutFloat(12, value.w);
			}
		}

		public void Modify(Quaternion _value)
		{
			if (Value != _value)
			{
				Value = _value;
				m_modified = true;
			}
		}

		override public string ToString() { return base.ToString() + "=" + Value.ToString(); }

		private Quaternion m_tmpQuaternion = Quaternion.identity;
	}


	public class EntityValue_ByteArray : EntityValue
	{
		public EntityValue_ByteArray(string _name, byte _index, byte[] _value) :
			base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.ByteArray, _index, 0)
		{
			Value = _value;
		}

		public EntityValue_ByteArray(string _name, byte _index) :
			this(_name, _index, new byte[] { })
		{ }

		public byte[] Value
		{
			get
			{
				return m_data;
			}
			set
			{
				if (value == null) return;
				if (m_data.Length != value.Length)
				{
					// array size has changed
					m_data = new byte[value.Length];
				}
				for (int idx = 0; idx < value.Length; idx++)
				{
					Array.Copy(value, m_data, m_data.Length);
				}
			}
		}

		public void Modify(byte[] _value)
		{
			Value = _value; // don't bother comparing for difference
			m_modified = true;
		}

		override public string ToString() { return base.ToString() + "=byte[" + m_data.Length + "]"; }
	}


	public class EntityValue_FloatArray : EntityValue
	{
		public EntityValue_FloatArray(string _name, byte _index, float[] _value) :
			base(_name, AUT_WH.MajorDomoProtocol.EntityValueType.FloatArray, _index, 0)
		{
			Value = _value;
		}

		public EntityValue_FloatArray(string _name, byte _index) :
			this(_name, _index, new float[] { })
		{ }

		public float[] Value
		{
			get
			{
				if (m_tmpArray == null)
				{
					m_tmpArray = new float[m_data.Length / sizeof(float)];
				}
				for (int idx = 0; idx < m_tmpArray.Length; idx++)
				{
					m_tmpArray[idx] = m_buffer.GetFloat(idx * sizeof(float));
				}
				return m_tmpArray;
			}
			set
			{
				if (value == null) return;
				if (m_data.Length != (value.Length * sizeof(float)))
				{
					// array size has changed
					m_data     = new byte[value.Length * sizeof(float)];
					m_tmpArray = new float[value.Length];
					m_buffer   = new FlatBuffers.ByteBuffer(m_data);
				}
				for (int idx = 0; idx < value.Length; idx++)
				{
					m_buffer.PutFloat(idx * sizeof(float), value[idx]);
				}
			}
		}

		public void Modify(float[] _value)
		{
			Value = _value; // don't bother comparing for difference
			m_modified = true;
		}

		override public string ToString() { return base.ToString() + "=[" + m_data.Length / sizeof(float) + "]"; }

		private float[] m_tmpArray = null;
	}
}
