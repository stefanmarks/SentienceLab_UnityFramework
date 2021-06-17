#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for managing a MajorDomo entity with methods for synchronising 
	/// from and to MajorDomo data structures and UnityEngine transformations.
	/// </summary>
	/// 
	public class EntityData
	{
		/// <summary> Name of the entity </summary>
		public string Name { get; private set; }

		/// <summary> Server assigned entity UID </summary>
		public uint EntityUID { get; private set; }

		/// <summary> UID of the client associated with the entity </summary>
		public uint ClientUID { get; private set; }

		public enum EntityState { Created, Registered, Revoked }

		/// <summary> State of the entity </summary>
		public EntityState State { get; private set; }

		/// <summary> Entity UID while it is not registered with the server </summary>
		public static readonly uint UID_UNASSIGNED = 0;

		/// <summary> Maximum number of values of the entity </summary>
		public static readonly byte MAX_VALUE_COUNT = 250;


		public delegate void EntityModified(EntityData _entity);
		public event EntityModified OnEntityModified;

		public delegate void EntityUpdated(EntityData _entity);
		public event EntityUpdated OnEntityUpdated;

		public delegate void ControlChanged(EntityData _entity, uint _oldClientUID);
		public event ControlChanged OnControlChanged;


		public EntityData(AUT_WH.MajorDomoProtocol.EntityInformation _information)
		{
			Name      = _information.Name;
			ClientUID = _information.ClientUID;
			EntityUID = _information.EntityUID;
			State     = EntityState.Created;

			m_properties = _information.Properties;
			m_values     = new List<EntityValue>();
			m_updated    = false;

			ReadEntityValues(_information);

			CheckRegistered();
		}


		public EntityData(string _name)
		{
			Name      = _name;
			EntityUID = EntityData.UID_UNASSIGNED;
			ClientUID = ClientData.UID_UNASSIGNED;
			State     = EntityState.Created;

			m_properties = 0;
			m_values  = new List<EntityValue>();
			m_updated = false;
		}


		public void SetEntityUID(uint _entityUID)
		{
			EntityUID = _entityUID;
			CheckRegistered();
		}


		private void CheckRegistered()
		{
			// valid UID and client makes this entity registered
			if ((State == EntityState.Created) && (ClientUID != ClientData.UID_UNASSIGNED) && (EntityUID != UID_UNASSIGNED))
			{
				State = EntityState.Registered;
			}
		}


		public void SetRevoked()
		{
			State = EntityState.Revoked;
		}


		public bool IsControlledByClient(uint _clientUID)
		{
			return ClientUID == _clientUID;
		}


		public bool IsControlledByThisClient()
		{
			return IsControlledByClient(MajorDomoManager.Instance.ClientUID);
		}


		public void SetClientUID(uint _clientUID)
		{
			ClientUID = _clientUID;
			CheckRegistered();
		}


		public bool ChangeClientUID(uint _clientUID)
		{
			bool controlChanged = false;
			uint oldClientUID   = ClientUID;

			SetClientUID(_clientUID);
			
			if (ClientUID != oldClientUID)
			{
				// control effectively changed > call event handlers
				controlChanged = true;
				OnControlChanged?.Invoke(this, oldClientUID);
			}

			return controlChanged;
		}


		public byte GetValueCount()
		{
			return (byte)m_values.Count;
		}


		public EntityValue GetValue(byte _idx)
		{
			return (_idx < m_values.Count) ? m_values[_idx] : null;
		}


		public EntityValue GetValue(string _name)
		{
			foreach (EntityValue value in m_values)
			{
				if (value.Name == _name) return value;
			}
			return null;
		}


		public EntityValue GetValue(string _name, AUT_WH.MajorDomoProtocol.EntityValueType _type)
		{
			foreach (EntityValue value in m_values)
			{
				if ((value.Name == _name) && (value.Type == _type)) return value;
			}
			return null;
		}


		public EntityValue_Boolean GetValue_Boolean(string _name)
		{
			return (EntityValue_Boolean)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Boolean);
		}


		public EntityValue_Byte GetValue_Byte(string _name)
		{
			return (EntityValue_Byte)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Byte);
		}


		public EntityValue_Int16 GetValue_Int16(string _name)
		{
			return (EntityValue_Int16)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int16);
		}


		public EntityValue_Int32 GetValue_Int32(string _name)
		{
			return (EntityValue_Int32)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int32);
		}


		public EntityValue_Int64 GetValue_Int64(string _name)
		{
			return (EntityValue_Int64)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int64);
		}


		public EntityValue_Float32 GetValue_Float32(string _name)
		{
			return (EntityValue_Float32)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Float32);
		}


		public EntityValue_Float64 GetValue_Float64(string _name)
		{
			return (EntityValue_Float64)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Float64);
		}


		public EntityValue_String GetValue_String(string _name)
		{
			return (EntityValue_String)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.String);
		}


		public EntityValue_Color GetValue_Color(string _name)
		{
			return (EntityValue_Color)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Color);
		}


		public EntityValue_Vector3D GetValue_Vector3D(string _name)
		{
			return (EntityValue_Vector3D)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Vector3D);
		}


		public EntityValue_Quaternion GetValue_Quaternion(string _name)
		{
			return (EntityValue_Quaternion)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Quaternion);
		}


		public EntityValue_ByteArray GetValue_ByteArray(string _name)
		{
			return (EntityValue_ByteArray)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.ByteArray);
		}


		public EntityValue_FloatArray GetValue_FloatArray(string _name)
		{
			return (EntityValue_FloatArray)GetValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.FloatArray);
		}

		protected EntityValue AddEntityValue(string _name, AUT_WH.MajorDomoProtocol.EntityValueType _type)
		{
			// does the value already exist?
			EntityValue value = GetValue(_name, _type);
			if (value == null)
			{
				// not yet
				// is this entity already registered? If so, then no more values can be added
				if (State == EntityState.Created)
				{
					// is there room for one more?
					byte index = (byte)m_values.Count;
					if (index < MAX_VALUE_COUNT)
					{
						value = EntityValue.GenerateEntityValueInstance(_name, index, _type);
						m_values.Add(value);
						// Debug.Log("Entity value '" + _name + "' added to entity '" + Name + "'");
					}
					else
					{
						Debug.LogWarning("Trying to add more than " + MAX_VALUE_COUNT + " values to entity '" + Name + "'");
					}
				}
				else
				{
					Debug.LogWarning("Trying to add a value to registered entity '" + Name + "'");
				}
			}
			else if (value.Type != _type)
			{
				Debug.LogWarning("Entity value '" + _name + "' already added to entity '" + Name + "' with different type");
				value = null;
			}
			return value;
		}


		public EntityValue_Boolean AddValue_Boolean(string _name, bool _value = false)
		{
			EntityValue_Boolean value = (EntityValue_Boolean)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Boolean);
			if (value != null) value.Value = _value;
			return value;
		}


		public EntityValue_Byte AddValue_Byte(string _name, byte _value = 0)
		{
			EntityValue_Byte value = (EntityValue_Byte)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Byte);
			if (value != null) value.Value = _value;
			return value;
		}


		public EntityValue_Int16 AddValue_Int16(string _name, short _value = 0)
		{
			EntityValue_Int16 value = (EntityValue_Int16)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int16);
			if (value != null) value.Value= _value;
			return value;
		}


		public EntityValue_Int32 AddValue_Int32(string _name, int _value = 0)
		{
			EntityValue_Int32 value = (EntityValue_Int32)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int32);
			if (value != null) value.Value = _value;
			return value;
		}


		public EntityValue_Int64 AddValue_Int64(string _name, long _value = 0)
		{
			EntityValue_Int64 value = (EntityValue_Int64)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Int64);
			if (value != null) value.Value = _value;
			return value;
		}


		public EntityValue_Float32 AddValue_Float32(string _name, float _value = 0)
		{
			EntityValue_Float32 value = (EntityValue_Float32)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Float32);
			if (value != null) value.Value = _value;
			return value;
		}


		public EntityValue_Float64 AddValue_Float64(string _name, double _value = 0)
		{
			EntityValue_Float64 value = (EntityValue_Float64)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Float64);
			if (value != null) value.Value = _value;
			return value;
		}


		public EntityValue_String AddValue_String(string _name, string _value)
		{
			EntityValue_String value = (EntityValue_String)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.String);
			value.Value = _value;
			return value;
		}


		public EntityValue_Color AddValue_Color(string _name, Color _value)
		{
			EntityValue_Color value = (EntityValue_Color)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Color);
			value.Value = _value;
			return value;
		}


		public EntityValue_Vector3D AddValue_Vector3D(string _name, Vector3 _value)
		{
			EntityValue_Vector3D value = (EntityValue_Vector3D)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Vector3D);
			value.Value = _value;
			return value;
		}


		public EntityValue_Quaternion AddValue_Quaternion(string _name, Quaternion _value)
		{
			EntityValue_Quaternion value = (EntityValue_Quaternion)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.Quaternion);
			value.Value = _value;
			return value;
		}


		public EntityValue_ByteArray AddValue_ByteArray(string _name, byte[] _value)
		{
			EntityValue_ByteArray value = (EntityValue_ByteArray)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.ByteArray);
			value.Value = _value;
			return value;
		}


		public EntityValue_FloatArray AddValue_FloatArray(string _name, float[] _value)
		{
			EntityValue_FloatArray value = (EntityValue_FloatArray)AddEntityValue(_name, AUT_WH.MajorDomoProtocol.EntityValueType.FloatArray);
			value.Value = _value;
			return value;
		}


		public void ReadEntityValues(AUT_WH.MajorDomoProtocol.EntityInformation _information)
		{
			// go through the update list
			for (int valueIdx = 0; valueIdx < _information.ValuesLength; valueIdx++)
			{
				// search by value index
				var valueInformation = _information.Values(valueIdx).Value;
				EntityValue value = EntityValue.GenerateEntityValueInstance(valueInformation.Name, (byte)valueIdx, valueInformation.Type);
				if (value != null) // TODO: This could cause index mismatch
				{
					m_values.Add(value);
					value.ReadEntityValue(valueInformation);
					m_updated = true;
				}
				else
				{
					Debug.LogWarning("Could not create entity value '" + valueInformation.Name + "' for " + ToString());
				}
			}
		}


		public void ReadEntityUpdate(AUT_WH.MajorDomoProtocol.EntityUpdate _update)
		{
			// go through the update list
			for (int valueIdx = 0; valueIdx < _update.ValuesLength; valueIdx++)
			{
				// search by value index
				var valueUpdate = _update.Values(valueIdx).Value;
				EntityValue value = GetValue((byte)valueUpdate.Index);
				if (value != null)
				{
					// is this the correct type?
					if (value.Type == valueUpdate.Type)
					{
						// read value and remember if this changed it
						m_updated |= value.ReadEntityValue(valueUpdate);
						// Debug.Log("updated " + Name + "/" + value.Name + ":" + updated);
					}
				}
			}
		}


		public FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityInformation> WriteEntityInformation(FlatBuffers.FlatBufferBuilder _builder)
		{
			// prepare list of entity values
			List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityValueInformation>> entityValues = new List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityValueInformation>>();
			foreach (var value in m_values)
			{
				entityValues.Add(value.WriteEntityValueInformation(_builder));
			}
			// build the rest of the information structure
			return AUT_WH.MajorDomoProtocol.EntityInformation.CreateEntityInformation(_builder,
				_builder.CreateString(Name),
				EntityUID,
				ClientUID,
				m_properties,
				AUT_WH.MajorDomoProtocol.EntityInformation.CreateValuesVector(_builder, entityValues.ToArray()));
		}


		public FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityUpdate> WriteEntityUpdate(FlatBuffers.FlatBufferBuilder _builder)
		{
			// prepare list of modified entity values
			List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityValue>> entityValues = new List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityValue>>();
			foreach (var value in m_values)
			{
				if (value.IsModified())
				{
					entityValues.Add(value.WriteEntityValue(_builder));
				}
			}
			// build the rest of the update structure
			return AUT_WH.MajorDomoProtocol.EntityUpdate.CreateEntityUpdate(_builder, 
				EntityUID,
				AUT_WH.MajorDomoProtocol.EntityUpdate.CreateValuesVector(_builder, entityValues.ToArray()));
		}


		public bool IsPersistent()
		{
			return (m_properties & ((ushort) AUT_WH.MajorDomoProtocol.EntityProperties.Persistent)) != 0;
		}


		public void SetPersistent(bool _persistent)
		{
			if (_persistent)
			{
				m_properties |= (ushort)AUT_WH.MajorDomoProtocol.EntityProperties.Persistent;
				// persistent entities must als be shared
				m_properties |= (ushort)AUT_WH.MajorDomoProtocol.EntityProperties.SharedControl;
			}
			else
			{
				m_properties &= (ushort)(~AUT_WH.MajorDomoProtocol.EntityProperties.Persistent);
			}
		}


		public bool AllowsSharedControl()
		{
			return (m_properties & ((ushort)AUT_WH.MajorDomoProtocol.EntityProperties.SharedControl)) != 0;
		}


		public void AllowSharedControl(bool _shared)
		{
			if (_shared)
			{
				m_properties |= (ushort)AUT_WH.MajorDomoProtocol.EntityProperties.SharedControl;
			}
			else
			{
				m_properties &= (ushort)(~AUT_WH.MajorDomoProtocol.EntityProperties.SharedControl);
				// not shared als means not possible to make persistent
				m_properties &= (ushort)(~AUT_WH.MajorDomoProtocol.EntityProperties.Persistent);
			}
		}


		public bool IsModified()
		{
			foreach (var value in m_values)
			{
				if (value.IsModified()) return true;
			}
			return false;
		}


		public void InvokeOnModifiedHandlers()
		{
			if (IsModified())
			{
				OnEntityModified?.Invoke(this);
			}
		}


		public void ResetModified()
		{
			foreach (var value in m_values)
			{
				value.ResetModified();
			}
		}


		public bool IsUpdated()
		{
			return m_updated;
		}


		public void InvokeOnUpdatedHandlers()
		{
			if (IsUpdated())
			{
				OnEntityUpdated?.Invoke(this);
			}
		}


		public void ResetUpdated()
		{
			foreach (var value in m_values)
			{
				value.ResetUpdated();
			}
			m_updated = false;
		}


		public bool Equals(EntityData _other)
		{
			return (Name == _other.Name) &&
				(EntityUID == _other.EntityUID) &&
				(ClientUID == _other.ClientUID);
		}


		public string ToString(bool _details, bool _values)
		{
			string s = Name + ":" + EntityUID;
			if (_details)
			{
				s += ", cUID=" + ClientUID
				+ (IsPersistent() ? ", persistent" : "")
				+ (AllowsSharedControl() ? ", shared" : "")
				;
			}
			if (_values && m_values.Count > 0)
			{
				s += ", [";
				bool useComma = false;
				foreach (EntityValue value in m_values)
				{
					if (useComma) s += ", ";
					useComma = true;
					s += value.ToString();
				}
				s += "]";
			}
			return s;
		}

		/// <summary>Property flags of the client</summary>
		private ushort m_properties;

		/// <summary>List of entity values</summary>
		private readonly List<EntityValue> m_values;

		private bool m_updated;
	}
}