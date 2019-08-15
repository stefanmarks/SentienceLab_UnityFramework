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

		
		public new void Awake()
		{
			base.m_entityType = "Parameter set";
			base.Awake();
			
			if (ParameterBaseNode == null)
			{
				ParameterBaseNode = this.gameObject;
			}

			m_proxies = new List<ParameterProxy>();
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
				ParameterProxy proxy = null;

				if      (param is Parameter_Boolean) { proxy = new ParameterProxy_Boolean((Parameter_Boolean)param, _entity); }
				else if (param is Parameter_Double)  { proxy = new ParameterProxy_Double((Parameter_Double)param, _entity); }
				else if (param is Parameter_Vector3) { proxy = new ParameterProxy_Vector3((Parameter_Vector3)param, _entity); }

				if (proxy != null)
				{
					if (proxy.GetEntityValue() != null)
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
				ParameterProxy proxy = null;

				if      (param is Parameter_Boolean)     { proxy = new ParameterProxy_Boolean(    (Parameter_Boolean)     param, m_entity); }
				else if (param is Parameter_Integer)     { proxy = new ParameterProxy_Integer(    (Parameter_Integer)     param, m_entity); }
				else if (param is Parameter_Double)      { proxy = new ParameterProxy_Double(     (Parameter_Double)      param, m_entity); }
				else if (param is Parameter_DoubleRange) { proxy = new ParameterProxy_DoubleRange((Parameter_DoubleRange) param, m_entity); }
				else if (param is Parameter_List)        { proxy = new ParameterProxy_List(       (Parameter_List)        param, m_entity); }
				else if (param is Parameter_Vector3)     { proxy = new ParameterProxy_Vector3(    (Parameter_Vector3)     param, m_entity); }

				if (proxy != null)
				{
					if (proxy.GetEntityValue() != null)
					{
						m_proxies.Add(proxy);
					}
					else
					{
						Debug.LogWarningFormat("Could not find entity value '{0}' of type {1} in entity '{2}'",
							param.Name, proxy.GetTypeName(), m_entity.Name);
					}
				}
				else
				{
					Debug.LogWarningFormat("Could not create proxy for type of parameter '{0}'", param.Name);
				}
			}
		}


		protected override void DestroyVariables()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.Revoke();
			}
			m_proxies.Clear();
		}


		protected override void SynchroniseFromEntity()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromEntityToParameter();
				proxy.ResetState();
			}
		}


		protected override void SynchroniseToEntity()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromParameterToEntity();
				proxy.ResetState();
			}
		}


		protected override bool CanDisableGameObject()
		{
			return false;
		}


		private abstract class ParameterProxy
		{
			protected enum eState { Idle, ParameterUpdated, ParameterTransferred, EntityUpdated, EntityTransferred, UpdateDone, Revoked }


			protected ParameterProxy(ParameterBase _parameter, EntityData _entity)
			{
				baseParameter = _parameter;
				baseParameter.OnValueChanged += ParameterValueChanged;
				entity = _entity;
				entity.OnEntityUpdated  += EntityValueChanged;
				entity.OnControlChanged += EntityControlChanged;
				state = eState.Idle;
			}


			public bool Registered { get
				{
					return entity.State ==  EntityData.EntityState.Registered;
				}
			}


			public void Revoke()
			{
				entity.OnEntityUpdated       -= EntityValueChanged;
				entity.OnControlChanged      -= EntityControlChanged;
				baseParameter.OnValueChanged -= ParameterValueChanged;
				state = eState.Revoked;
			}


			private void ParameterValueChanged(ParameterBase _parameter)
			{
				if (Registered && ((state == eState.Idle) || (state == eState.ParameterTransferred)))
				{
					state = eState.ParameterUpdated;
					if (entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID))
					{
						// transfer directly
						TransferValueFromParameterToEntity();
						state = eState.ParameterTransferred;
					}
					else
					{
						// need to request control first and copy value when that is done
						MajorDomoManager.Instance.RequestControl(entity);
					}
				}
			}


			private void EntityControlChanged(uint _newClientUID)
			{
				if (entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID) && (state == eState.ParameterUpdated))
				{
					// entity is under control now > copy value
					TransferValueFromParameterToEntity();
					state = eState.ParameterTransferred;
				}
				else if (entity.State == EntityData.EntityState.Revoked)
				{
					Debug.LogWarningFormat("Parameter entity {0} was revoked", entity.Name);
					state = eState.Revoked;
				}
				else if (entity.ClientUID == ClientData.UID_SERVER)
				{
					Debug.LogFormat("Parameter entity {0} was released to server", entity.Name);
				}
			}


			private void EntityValueChanged()
			{
				if (Registered && ((state == eState.Idle) || (state == eState.EntityTransferred)))
				{
					state = eState.EntityUpdated;
					TransferValueFromEntityToParameter();
					state = eState.EntityTransferred;
				}
			}


			public void ResetState()
			{
				if      (state == eState.EntityTransferred || state == eState.ParameterTransferred) state = eState.UpdateDone;
				else if (state == eState.UpdateDone) state = eState.Idle;
			}


			public abstract void TransferValueFromParameterToEntity();
			public abstract void TransferValueFromEntityToParameter();
			public abstract EntityValue GetEntityValue();
			public abstract string GetTypeName();


			protected ParameterBase baseParameter;
			protected EntityData    entity;
			protected eState        state;
		}


		private class ParameterProxy_Boolean : ParameterProxy
		{
			public ParameterProxy_Boolean(Parameter_Boolean _parameter, EntityData _entity) :
				base(_parameter, _entity)
			{
				parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					entityValue = _entity.GetValue_Boolean(parameter.Name);
				}
				else
				{
					entityValue = _entity.AddValue_Boolean(parameter.Name, parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				entityValue.Modify(parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				parameter.Value = entityValue.Value;
			}


			public override EntityValue GetEntityValue()
			{
				return entityValue;
			}


			public override string GetTypeName()
			{
				return "boolean";
			}


			private Parameter_Boolean   parameter;
			private EntityValue_Boolean entityValue;
		}


		private class ParameterProxy_Integer : ParameterProxy
		{
			public ParameterProxy_Integer(Parameter_Integer _parameter, EntityData _entity) :
				base(_parameter, _entity)
			{
				parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					entityValue = _entity.GetValue_Int64(parameter.Name);
				}
				else
				{
					entityValue = _entity.AddValue_Int64(parameter.Name, parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				entityValue.Modify(parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				parameter.Value = entityValue.Value;
			}


			public override EntityValue GetEntityValue()
			{
				return entityValue;
			}


			public override string GetTypeName()
			{
				return "integer";
			}


			private Parameter_Integer  parameter;
			private EntityValue_Int64  entityValue;
		}


		private class ParameterProxy_Double : ParameterProxy
		{
			public ParameterProxy_Double(Parameter_Double _parameter, EntityData _entity) :
				base(_parameter, _entity)
			{
				parameter = (Parameter_Double)_parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					entityValue = _entity.GetValue_Float64(parameter.Name);
				}
				else
				{
					entityValue = _entity.AddValue_Float64(parameter.Name, parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				entityValue.Modify(parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				parameter.Value = entityValue.Value;
			}


			public override EntityValue GetEntityValue()
			{
				return entityValue;
			}


			public override string GetTypeName()
			{
				return "double";
			}


			private Parameter_Double    parameter;
			private EntityValue_Float64 entityValue;
		}


		private class ParameterProxy_List : ParameterProxy
		{
			public ParameterProxy_List(Parameter_List _parameter, EntityData _entity) :
				base(_parameter, _entity)
			{
				parameter = _parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					entityValue = _entity.GetValue_Int32(parameter.Name);
				}
				else
				{
					entityValue = _entity.AddValue_Int32(parameter.Name, parameter.SelectedItemIndex);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				entityValue.Modify(parameter.SelectedItemIndex);
			}


			public override void TransferValueFromEntityToParameter()
			{
				parameter.SelectedItemIndex = entityValue.Value;
			}


			public override EntityValue GetEntityValue()
			{
				return entityValue;
			}


			public override string GetTypeName()
			{
				return "list";
			}


			private Parameter_List parameter;
			private EntityValue_Int32 entityValue;
		}


		private class ParameterProxy_Vector3 : ParameterProxy
		{
			public ParameterProxy_Vector3(Parameter_Vector3 _parameter, EntityData _entity) :
				base(_parameter, _entity)
			{
				parameter = (Parameter_Vector3)_parameter;
				if (_entity.State == EntityData.EntityState.Registered)
				{
					entityValue = _entity.GetValue_Vector3D(parameter.Name);
				}
				else
				{
					entityValue = _entity.AddValue_Vector3D(parameter.Name, parameter.Value);
				}
			}


			public override void TransferValueFromParameterToEntity()
			{
				entityValue.Modify(parameter.Value);
			}


			public override void TransferValueFromEntityToParameter()
			{
				parameter.Value = entityValue.Value;
			}


			public override EntityValue GetEntityValue()
			{
				return entityValue;
			}


			public override string GetTypeName()
			{
				return "vector3";
			}


			private Parameter_Vector3    parameter;
			private EntityValue_Vector3D entityValue;
		}


		private List<ParameterProxy> m_proxies;
	}
}