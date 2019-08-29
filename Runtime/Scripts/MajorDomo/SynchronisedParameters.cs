#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using SentienceLab.Data;

namespace SentienceLab.MajorDomo
{
	[DisallowMultipleComponent]
	[AddComponentMenu("MajorDomo/Synchronised Parameters")]
	public class SynchronisedParameters : SynchronisedEntityBase
	{
		[Tooltip("Base node of the parameter tree (default: this node)")]
		public GameObject ParameterBaseNode = null;

		
		protected override void Initialise()
		{
			m_proxies    = new List<ParameterProxy>();
			m_syncState = ESyncState.Idle;

			if (ParameterBaseNode == null)
			{
				ParameterBaseNode = this.gameObject;
			}
		}


		private ParameterBase[] FindParameters()
		{
			ParameterBase[] parameters = ParameterBaseNode.GetComponentsInChildren<ParameterBase>();

			if (parameters.Length == 0)
			{
				Debug.LogFormat("MajorDomo parameter manager '{0}' found no parameters", EntityName);
			}

			return parameters;
		}


		protected override void CreateVariables(EntityData _entity)
		{
			ParameterBase[] parameters = FindParameters();
			foreach (ParameterBase param in parameters)
			{
				ParameterProxy proxy = CreateParameterProxy(param, _entity);
			
				if (proxy != null)
				{
					if (proxy.IsValid())
					{
						m_proxies.Add(proxy);
					}
					else
					{
						Debug.LogWarningFormat("Could not create entity value '{0}' of type {1} in entity '{2}'",
							param.Name, proxy.GetTypeName(), _entity.Name);
					}
				}
				else
				{
					Debug.LogWarningFormat("Could not create proxy for type of parameter '{0}'", param.Name);
				}
			}

			Entity.OnEntityUpdated  += EntityValueChanged;
			Entity.OnControlChanged += EntityControlChanged;
		}


		protected override void FindVariables()
		{
			ParameterBase[] parameters = FindParameters();
			foreach (ParameterBase param in parameters)
			{
				ParameterProxy proxy = CreateParameterProxy(param, Entity);

				if (proxy != null)
				{
					if (proxy.IsValid())
					{
						m_proxies.Add(proxy);
					}
					else
					{
						Debug.LogWarningFormat("Could not find entity value '{0}' of type {1} in entity '{2}'",
							param.Name, proxy.GetTypeName(), Entity.Name);
					}
				}
				else
				{
					Debug.LogWarningFormat("Could not create proxy for type of parameter '{0}'", param.Name);
				}
			}

			Entity.OnEntityUpdated  += EntityValueChanged;
			Entity.OnControlChanged += EntityControlChanged;
		}


		private ParameterProxy CreateParameterProxy(ParameterBase param, EntityData _entity)
		{
			ParameterProxy proxy = null;

			if      (param is Parameter_Boolean)     { proxy = new ParameterProxy_Boolean(    this, (Parameter_Boolean)     param); }
			else if (param is Parameter_Integer)     { proxy = new ParameterProxy_Integer(    this, (Parameter_Integer)     param); }
			else if (param is Parameter_Double)      { proxy = new ParameterProxy_Double(     this, (Parameter_Double)      param); }
			else if (param is Parameter_DoubleRange) { proxy = new ParameterProxy_DoubleRange(this, (Parameter_DoubleRange) param); }
			else if (param is Parameter_List)        { proxy = new ParameterProxy_List(       this, (Parameter_List)        param); }
			else if (param is Parameter_Vector3)     { proxy = new ParameterProxy_Vector3(    this, (Parameter_Vector3)     param); }

			return proxy;
		}


		protected override void DestroyVariables()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.Destroy();
			}
			m_proxies.Clear();

			Entity.OnEntityUpdated  -= EntityValueChanged;
			Entity.OnControlChanged -= EntityControlChanged;

			m_syncState = ESyncState.Revoked;
		}


		protected override void SynchroniseFromEntity()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromEntityToParameter();
			}
			ResetSyncState();
		}


		protected override void SynchroniseToEntity()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromParameterToEntity();
			}
			ResetSyncState();
		}


		protected override bool CanDisableGameObject()
		{
			return false;
		}


		protected override string GetEntityTypeName()
		{
			return "Parameter set";
		}


		protected void ResetSyncState()
		{
			if (m_syncState == ESyncState.DoneSyncFromEntity ||
				m_syncState == ESyncState.DoneSyncToEntity)
			{
				// sync done, give it one more cycle before becoming idle
				m_syncState = ESyncState.SyncFinished;
			}
			else if (m_syncState == ESyncState.SyncFinished)
			{
				// "rest" cycle done
				m_syncState = ESyncState.Idle;
			}
		}


		public bool Registered
		{
			get
			{
				return Entity.State == EntityData.EntityState.Registered;
			}
		}


		private void EntityControlChanged(uint _newClientUID)
		{
			if (Entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID) && (m_syncState == ESyncState.RequestSyncToEntity))
			{
				// entity is under control now > copy parameter values over
				SynchroniseToEntity();
				m_syncState = ESyncState.DoneSyncToEntity;
			}
			else if (Entity.State == EntityData.EntityState.Revoked)
			{
				Debug.LogWarningFormat("Parameter entity {0} was revoked", Entity.Name);
				m_syncState = ESyncState.Revoked;
			}
			else if (Entity.ClientUID == ClientData.UID_SERVER)
			{
				Debug.LogFormat("Parameter entity {0} was released to server", Entity.Name);
			}
		}


		private void EntityValueChanged()
		{
			if (Registered && ((m_syncState == ESyncState.Idle) || (m_syncState == ESyncState.DoneSyncFromEntity)))
			{
				m_syncState = ESyncState.RequestSyncFromEntity;
				SynchroniseFromEntity();
				m_syncState = ESyncState.DoneSyncFromEntity;
			}
		}


		private abstract class ParameterProxy
		{
			protected ParameterProxy(SynchronisedParameters _parent, ParameterBase _parameter)
			{
				m_baseParameter = _parameter;
				m_baseParameter.OnValueChanged += ParameterValueChanged;
				m_parent = _parent;
			}


			public void Destroy()
			{
				m_baseParameter.OnValueChanged -= ParameterValueChanged;
			}


			private void ParameterValueChanged(ParameterBase _parameter)
			{
				if (m_parent.Registered && 
					((m_parent.m_syncState == ESyncState.Idle) || (m_parent.m_syncState == ESyncState.DoneSyncToEntity)))
				{
					m_parent.m_syncState = ESyncState.RequestSyncToEntity;
					if (m_parent.Entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID))
					{
						// transfer directly
						TransferValueFromParameterToEntity();
						m_parent.m_syncState = ESyncState.DoneSyncToEntity;
					}
					else
					{
						// need to request control first and copy value when that is done
						MajorDomoManager.Instance.RequestControl(m_parent.Entity);
					}
				}
			}

			
			public abstract void TransferValueFromParameterToEntity();
			public abstract void TransferValueFromEntityToParameter();
			public abstract bool IsValid();
			public abstract string GetTypeName();


			protected SynchronisedParameters m_parent;
			protected ParameterBase          m_baseParameter;
		}


		private class ParameterProxy_Boolean : ParameterProxy
		{
			public ParameterProxy_Boolean(SynchronisedParameters _parent, Parameter_Boolean _parameter) :
				base(_parent, _parameter)
			{
				m_parameter = _parameter;
				if (m_parent.Entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = m_parent.Entity.GetValue_Boolean(m_parameter.Name);
				}
				else
				{
					m_entityValue = m_parent.Entity.AddValue_Boolean(m_parameter.Name, m_parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				m_entityValue.Modify(m_parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				m_parameter.Value = m_entityValue.Value;
			}


			public override bool IsValid()
			{
				return m_entityValue != null;
			}


			public override string GetTypeName()
			{
				return "boolean";
			}


			private Parameter_Boolean   m_parameter;
			private EntityValue_Boolean m_entityValue;
		}


		private class ParameterProxy_Integer : ParameterProxy
		{
			public ParameterProxy_Integer(SynchronisedParameters _parent, Parameter_Integer _parameter) :
				base(_parent, _parameter)
			{
				m_parameter = _parameter;
				if (m_parent.Entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = m_parent.Entity.GetValue_Int64(m_parameter.Name);
				}
				else
				{
					m_entityValue = m_parent.Entity.AddValue_Int64(m_parameter.Name, m_parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				m_entityValue.Modify(m_parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				m_parameter.Value = m_entityValue.Value;
			}


			public override bool IsValid()
			{
				return m_entityValue != null;
			}


			public override string GetTypeName()
			{
				return "integer";
			}


			private Parameter_Integer  m_parameter;
			private EntityValue_Int64  m_entityValue;
		}


		private class ParameterProxy_Double : ParameterProxy
		{
			public ParameterProxy_Double(SynchronisedParameters _parent, Parameter_Double _parameter) :
				base(_parent, _parameter)
			{
				m_parameter = _parameter;
				if (m_parent.Entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = m_parent.Entity.GetValue_Float64(m_parameter.Name);
				}
				else
				{
					m_entityValue = m_parent.Entity.AddValue_Float64(m_parameter.Name, m_parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				m_entityValue.Modify(m_parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				m_parameter.Value = m_entityValue.Value;
			}


			public override bool IsValid()
			{
				return m_entityValue != null;
			}


			public override string GetTypeName()
			{
				return "double";
			}


			private Parameter_Double    m_parameter;
			private EntityValue_Float64 m_entityValue;
		}


		private class ParameterProxy_DoubleRange : ParameterProxy
		{
			public ParameterProxy_DoubleRange(SynchronisedParameters _parent, Parameter_DoubleRange _parameter) :
				base(_parent, _parameter)
			{
				m_parameter = _parameter;
				if (m_parent.Entity.State == EntityData.EntityState.Registered)
				{
					m_entityValueMin = m_parent.Entity.GetValue_Float64(m_parameter.Name + "Min");
					m_entityValueMax = m_parent.Entity.GetValue_Float64(m_parameter.Name + "Max");
				}
				else
				{
					m_entityValueMin = m_parent.Entity.AddValue_Float64(m_parameter.Name + "Min", m_parameter.ValueMin);
					m_entityValueMax = m_parent.Entity.AddValue_Float64(m_parameter.Name + "Max", m_parameter.ValueMax);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				m_entityValueMin.Modify(m_parameter.ValueMin);
				m_entityValueMax.Modify(m_parameter.ValueMax);
			}


			public override void TransferValueFromEntityToParameter()
			{
				m_parameter.ValueMin = m_entityValueMin.Value;
				m_parameter.ValueMax = m_entityValueMax.Value;
			}


			public override bool IsValid()
			{
				return (m_entityValueMin != null) && (m_entityValueMax != null);
			}


			public override string GetTypeName()
			{
				return "double range";
			}


			private Parameter_DoubleRange m_parameter;
			private EntityValue_Float64   m_entityValueMin, m_entityValueMax;
		}


		private class ParameterProxy_List : ParameterProxy
		{
			public ParameterProxy_List(SynchronisedParameters _parent, Parameter_List _parameter) :
				base(_parent, _parameter)
			{
				m_parameter = _parameter;
				if (m_parent.Entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = m_parent.Entity.GetValue_Int32(m_parameter.Name);
				}
				else
				{
					m_entityValue = m_parent.Entity.AddValue_Int32(m_parameter.Name, m_parameter.SelectedItemIndex);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				m_entityValue.Modify(m_parameter.SelectedItemIndex);
			}


			public override void TransferValueFromEntityToParameter()
			{
				m_parameter.SelectedItemIndex = m_entityValue.Value;
			}


			public override bool IsValid()
			{
				return m_entityValue != null;
			}


			public override string GetTypeName()
			{
				return "list";
			}


			private Parameter_List    m_parameter;
			private EntityValue_Int32 m_entityValue;
		}


		private class ParameterProxy_Vector3 : ParameterProxy
		{
			public ParameterProxy_Vector3(SynchronisedParameters _parent, Parameter_Vector3 _parameter) :
				base(_parent, _parameter)
			{
				m_parameter = _parameter;
				if (m_parent.Entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = m_parent.Entity.GetValue_Vector3D(m_parameter.Name);
				}
				else
				{
					m_entityValue = m_parent.Entity.AddValue_Vector3D(m_parameter.Name, m_parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				m_entityValue.Modify(m_parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				m_parameter.Value = m_entityValue.Value;
			}


			public override bool IsValid()
			{
				return m_entityValue != null;
			}


			public override string GetTypeName()
			{
				return "vector3";
			}


			private Parameter_Vector3    m_parameter;
			private EntityValue_Vector3D m_entityValue;
		}


		protected enum ESyncState {
			Idle,
			RequestSyncToEntity, DoneSyncToEntity,
			RequestSyncFromEntity, DoneSyncFromEntity,
			SyncFinished,
			Revoked
		}

		protected ESyncState m_syncState;

		private List<ParameterProxy> m_proxies;
	}
}