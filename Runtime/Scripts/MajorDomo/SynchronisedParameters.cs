﻿#region Copyright Information
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
			m_proxies = new List<ParameterProxy>();
			
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
		}


		private ParameterProxy CreateParameterProxy(ParameterBase param, EntityData _entity)
		{
			ParameterProxy proxy = null;

			if      (param is Parameter_Boolean)     { proxy = new ParameterProxy_Boolean(    _entity, (Parameter_Boolean)     param); }
			else if (param is Parameter_Integer)     { proxy = new ParameterProxy_Integer(    _entity, (Parameter_Integer)     param); }
			else if (param is Parameter_Double)      { proxy = new ParameterProxy_Double(     _entity, (Parameter_Double)      param); }
			else if (param is Parameter_DoubleRange) { proxy = new ParameterProxy_DoubleRange(_entity, (Parameter_DoubleRange) param); }
			else if (param is Parameter_String)      { proxy = new ParameterProxy_String(     _entity, (Parameter_String)      param); }
			else if (param is Parameter_List)        { proxy = new ParameterProxy_List(       _entity, (Parameter_List)        param); }
			else if (param is Parameter_Vector3)     { proxy = new ParameterProxy_Vector3(    _entity, (Parameter_Vector3)     param); }

			return proxy;
		}


		protected override void DestroyVariables()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.Destroy();
			}
			m_proxies.Clear();
		}


		protected override void SynchroniseFromEntity()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromEntityToParameter();
			}
		}


		protected override bool IsModified()
		{
			foreach (var proxy in m_proxies)
			{
				if (proxy.IsModified()) return true;
			}
			return false;
		}


		protected override void SynchroniseToEntity()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromParameterToEntity();
			}
		}


		protected override void ResetModified()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.ResetModified();
			}
		}

		protected override bool CanDisableGameObject()
		{
			return false;
		}


		protected override string GetEntityTypeName()
		{
			return "Parameter set";
		}


		public bool Registered
		{
			get
			{
				return Entity.State == EntityData.EntityState.Registered;
			}
		}


		private abstract class ParameterProxy
		{
			protected ParameterProxy(ParameterBase _parameter)
			{
				m_baseParameter = _parameter;
				m_baseParameter.OnValueChanged += ParameterValueChanged;
			}


			public void Destroy()
			{
				m_baseParameter.OnValueChanged -= ParameterValueChanged;
			}


			private void ParameterValueChanged(ParameterBase _parameter)
			{
				m_modified = true;
			}


			public bool IsModified()
			{
				return m_modified;
			}


			public void ResetModified()
			{
				m_modified = false;
			}

			
			public abstract void TransferValueFromParameterToEntity();
			public abstract void TransferValueFromEntityToParameter();
			public abstract bool IsValid();
			public abstract string GetTypeName();


			protected bool           m_modified;
			protected ParameterBase  m_baseParameter;
		}


		private class ParameterProxy_Boolean : ParameterProxy
		{
			public ParameterProxy_Boolean(EntityData _entity, Parameter_Boolean _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = _entity.GetValue_Boolean(m_parameter.Name);
				}
				else
				{
					m_entityValue = _entity.AddValue_Boolean(m_parameter.Name, m_parameter.Value);
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
			public ParameterProxy_Integer(EntityData _entity, Parameter_Integer _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = _entity.GetValue_Int64(m_parameter.Name);
				}
				else
				{
					m_entityValue = _entity.AddValue_Int64(m_parameter.Name, m_parameter.Value);
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
			public ParameterProxy_Double(EntityData _entity, Parameter_Double _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = _entity.GetValue_Float64(m_parameter.Name);
				}
				else
				{
					m_entityValue = _entity.AddValue_Float64(m_parameter.Name, m_parameter.Value);
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
			public ParameterProxy_DoubleRange(EntityData _entity, Parameter_DoubleRange _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValueMin = _entity.GetValue_Float64(m_parameter.Name + "Min");
					m_entityValueMax = _entity.GetValue_Float64(m_parameter.Name + "Max");
				}
				else
				{
					m_entityValueMin = _entity.AddValue_Float64(m_parameter.Name + "Min", m_parameter.ValueMin);
					m_entityValueMax = _entity.AddValue_Float64(m_parameter.Name + "Max", m_parameter.ValueMax);
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


		private class ParameterProxy_String : ParameterProxy
		{
			public ParameterProxy_String(EntityData _entity, Parameter_String _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = _entity.GetValue_String(m_parameter.Name);
				}
				else
				{
					m_entityValue = _entity.AddValue_String(m_parameter.Name, m_parameter.Value);
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
				return (m_entityValue != null);
			}


			public override string GetTypeName()
			{
				return "string";
			}


			private Parameter_String   m_parameter;
			private EntityValue_String m_entityValue;
		}


		private class ParameterProxy_List : ParameterProxy
		{
			public ParameterProxy_List(EntityData _entity, Parameter_List _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = _entity.GetValue_Int32(m_parameter.Name);
				}
				else
				{
					m_entityValue = _entity.AddValue_Int32(m_parameter.Name, m_parameter.SelectedItemIndex);
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
			public ParameterProxy_Vector3(EntityData _entity, Parameter_Vector3 _parameter) :
				base(_parameter)
			{
				m_parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					m_entityValue = _entity.GetValue_Vector3D(m_parameter.Name);
				}
				else
				{
					m_entityValue = _entity.AddValue_Vector3D(m_parameter.Name, m_parameter.Value);
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


		private List<ParameterProxy> m_proxies;
	}
}