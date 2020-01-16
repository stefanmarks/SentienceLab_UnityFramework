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
	[AddComponentMenu("MajorDomo/Synchronised GameObject")]
	public class SynchronisedGameObject : MonoBehaviour, MajorDomoManager.IAutoRegister
	{
		[Tooltip("Name of the entity to register with the server.\n" +
				 "Leave empty to use this game object's name.\n" +
				 "The string \"{GAMEOBJECT}\" will be automatically replaced by the game object's name.")]
		public string EntityName = "{GAMEOBJECT}";

		[Tooltip("Synchronisation mode for the entity.\n" 
			+ "Client: object is controlled by the client.\n" 
			+ "Server: object is controlled by the server/another client\n"
			+ "Shared: object control can be shared among clients")]
		public ESynchronisationMode SynchronisationMode = ESynchronisationMode.Client;

		[Tooltip("Entity stays on the server when the client disconnects")]
		public bool Persistent = false;


		[Header("Transform")]

		[Tooltip("Which components of this game object's transform are to be synchronised")]
		public ETransformComponents TransformComponents = ETransformComponents.TranslationRotation;

		[Tooltip("What to do when the synchronisation is lost")]
		public ESyncLostBehaviour SyncLostBehaviour = ESyncLostBehaviour.Disable;

		[Tooltip("Transform that this object's transform is based on (empty=World)")]
		public Transform ReferenceTransform = null;

		[Tooltip("Transform that this object's transform is aimed at (empty=this Game Object)")]
		public Transform TargetTransform = null;

		[Tooltip("How much translation can happen before synchronisation is requested")]
		public float MovementThreshold = 0.001f;

		[Tooltip("How much rotation (degrees) can happen before synchronisation is requested")]
		public float RotationThreshold = 0.1f;


		[Header("Parameters")]

		[Tooltip("Base node of the parameter tree (default: this node)")]
		public GameObject ParameterBaseNode = null;


		// number of frames to wait after another client has taken over control before trying to take control back
		const int CONTROL_COOLDOWN_COUNT = 10;



		/// <summary>
		/// Mode of synchronisation. Which part of the connection is the "master" and in control.
		/// </summary>
		/// 
		public enum ESynchronisationMode
		{
			Client,
			Server,
			Shared
		}

		/// <summary>
		/// What components of this game object's transform are synchronised.
		/// </summary>
		public enum ETransformComponents
		{
			TranslationRotationScale,
			TranslationRotation,
			Translation,
			Rotation,
			None
		}

		/// <summary>
		/// Possible actions for when the synchronisation is lost.
		/// </summary>
		public enum ESyncLostBehaviour
		{
			/// <summary>
			/// Freeze the object at the last synchronised position/orientation.
			/// </summary>
			Freeze,

			/// <summary>
			/// Disable the game object and re-enable when tracking continues.
			/// </summary>
			Disable
		};


		public bool IsControlledByClient
		{
			get
			{
				if (m_entity == null) return false;
				if (MajorDomoManager.Instance == null) return false;
				if (!MajorDomoManager.Instance.IsConnected()) return false;
				return m_entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID);
			}
			private set { }
		}


		public bool IsControlledByServer
		{
			get
			{
				if (m_entity == null) return false;
				if (MajorDomoManager.Instance == null) return false;
				if (!MajorDomoManager.Instance.IsConnected()) return false;
				return !m_entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID);
			}
			private set { }
		}



		public void Awake()
		{
			if (MajorDomoManager.Instance == null)
			{
				Debug.LogWarning("MajorDomoManager component needed");
				this.enabled = false;
				return;
			}
			else
			{
				RegisterWithMajorDomoManager(); // just to be sure
			}
		}


		public void RegisterWithMajorDomoManager()
		{
			if (m_initialised) return;

			Initialise();
			m_initialised = true;

			// empty name > use game object name
			if (EntityName.Trim().Length == 0)
			{
				EntityName = gameObject.name;
			}
			// replace template string
			EntityName = EntityName.Replace("{GAMEOBJECT}", this.gameObject.name);

			m_entity = null;
			m_controlChangeCooldown = 0;
			m_oldControlledByClient = IsControlledByClient;

			MajorDomoManager.Instance.OnClientUnregistered   += delegate (ClientData _c) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntitiesPublished    += delegate (List<EntityData> _l) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntitiesRevoked      += delegate (List<EntityData> _l) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntityControlChanged += delegate (List<EntityData> _l) { CheckEntity(); };

			if (SynchronisationMode == ESynchronisationMode.Server)
			{
				if (CanDisableGameObject()) gameObject.SetActive(false);
			}
		}


		public void Update()
		{
			if ((m_entity == null) || (m_entity.State != EntityData.EntityState.Registered)) return;

			if ((SynchronisationMode == ESynchronisationMode.Client) ||
				(SynchronisationMode == ESynchronisationMode.Shared))
			{
				// client control
				if (IsModified() && IsControlledByClient)
				{
					SynchroniseToEntity();
					ResetModified();
				}
			}

			if ((SynchronisationMode == ESynchronisationMode.Server) ||
				(SynchronisationMode == ESynchronisationMode.Shared))
			{
				if (IsControlledByServer && m_entity.IsUpdated())
				{
					SynchroniseFromEntity();
					m_entity.ResetUpdated();
					// every entity update resets the control change timeout
					m_controlChangeCooldown = CONTROL_COOLDOWN_COUNT;
				}
				else if (SynchronisationMode == ESynchronisationMode.Shared)
				{
					if (IsModified() && (m_controlChangeCooldown == 0))
					{
						if (IsControlledByServer)
						{
							// no server updates for some time and client wants to take control
							MajorDomoManager.Instance.RequestControl(m_entity);
						}
					}
				}
			}

			if (m_controlChangeCooldown > 0)
			{
				// prevent control takeover during cooldown period
				ResetModified();
				m_controlChangeCooldown--;
			}
		}


		private void EntityUpdated(EntityData _)
		{
			Update();
		}


		public void OnDisable()
		{
			if (m_entity == null) return;

			// make sure last state is synchronised
			if (  (SynchronisationMode == ESynchronisationMode.Client) ||
			    ( (SynchronisationMode == ESynchronisationMode.Shared) && IsControlledByClient) )
			{
				SynchroniseToEntity();
				ResetModified();
			}
		}


		public void OnEnable()
		{
			if (m_entity == null) return;

			// make sure current state is synchronised
			if (  (SynchronisationMode == ESynchronisationMode.Client) ||
				( (SynchronisationMode == ESynchronisationMode.Shared) && IsControlledByClient))
			{
				SynchroniseToEntity();
				ResetModified();
			}
			else if (  (SynchronisationMode == ESynchronisationMode.Server) ||
					 ( (SynchronisationMode == ESynchronisationMode.Shared) && IsControlledByServer))
			{
				SynchroniseFromEntity();
				m_entity.ResetUpdated();
			}
		}


		protected void CheckEntity()
		{
			if (m_entity == null)
			{
				// entity not found/created yet. Let's search first
				m_entity = MajorDomoManager.Instance.FindEntity(EntityName);
				if (m_entity != null)
				{
					// found it > find the variables, too
					FindVariables();
					// adjust flags from entity
					Persistent = m_entity.IsPersistent();

					// client+server and NOT shared doesn't work 
					if (SynchronisationMode == ESynchronisationMode.Shared && !m_entity.AllowsSharedControl())
					{
						Debug.LogWarningFormat("'{0}' mode on non-shared entity '{1}' not possible. Switching to '{2}' only.",
							SynchronisationMode, m_entity.ToString(true, true), ESynchronisationMode.Server);
						SynchronisationMode = ESynchronisationMode.Server;
					}

					if ((SynchronisationMode != ESynchronisationMode.Client) || Persistent)
					{
						// server authority or persistent > update immediately
						SynchroniseFromEntity();
					}

					m_entity.OnEntityUpdated += EntityUpdated;

					Debug.LogFormat("'{0}' synchronised with entity '{1}'",
						gameObject.name, m_entity.ToString(true, true));
				}
				else
				{
					// entity not found. let's create and publish it
					// either when in client mode, or when in shared+server mode
					if (MajorDomoManager.Instance.IsConnected() &&
					    (SynchronisationMode != ESynchronisationMode.Server) )
					{
						// Create and publish entity and then wait for the callback when server has acknowledged
						// then find the actual variables in the acknowledged entity version
						EntityData entity = MajorDomoManager.Instance.CreateClientEntity(EntityName);
						if (entity != null)
						{
							CreateVariables(entity);

							entity.AllowSharedControl(SynchronisationMode == ESynchronisationMode.Shared);
							entity.SetPersistent(Persistent);

							MajorDomoManager.Instance.PublishEntity(entity);
							Debug.LogFormat("Publishing entity '{0}'",
								entity.ToString(true, true));
						}
					}
				}
			}
			else if (m_entity.State == EntityData.EntityState.Revoked)
			{
				// entity was revoked > remove references

				Debug.LogFormat("'{0}' lost synchronisation with entity '{1}'",
					gameObject.name, m_entity.ToString(true, false));

				DestroyVariables();

				if (IsControlledByServer)
				{
					if (CanDisableGameObject()) gameObject.SetActive(false);
				}

				m_entity = null;
			}
			else if (m_oldControlledByClient != IsControlledByClient)
			{
				Debug.LogFormat("Control changed for entity '{0}' to '{1}'",
					m_entity.ToString(true, false), IsControlledByClient ? "client" : "server");
				m_oldControlledByClient = IsControlledByClient;
				m_controlChangeCooldown = m_oldControlledByClient ? 0 : CONTROL_COOLDOWN_COUNT;
			}
		}


		protected void Initialise()
		{
			m_proxies = new List<ParameterProxy>();

			if (TargetTransform == null)
			{
				TargetTransform = this.transform;
			}

			if (ParameterBaseNode == null)
			{
				ParameterBaseNode = this.gameObject;
			}
		}


		private bool DoTrans()
		{
			return
				TransformComponents == ETransformComponents.Translation ||
				TransformComponents == ETransformComponents.TranslationRotation ||
				TransformComponents == ETransformComponents.TranslationRotationScale;
		}


		private bool DoRot()
		{
			return
				TransformComponents == ETransformComponents.Rotation ||
				TransformComponents == ETransformComponents.TranslationRotation ||
				TransformComponents == ETransformComponents.TranslationRotationScale;
		}


		private bool DoScale()
		{
			return
				TransformComponents == ETransformComponents.TranslationRotationScale;
		}


		protected bool CanDisableGameObject()
		{
			return (TransformComponents != ETransformComponents.None) &&
			       (SyncLostBehaviour == ESyncLostBehaviour.Disable);
		}


		private ParameterBase[] FindParameters()
		{
			ParameterBase[] parameters = ParameterBaseNode.GetComponentsInChildren<ParameterBase>();
			return parameters;
		}


		protected void CreateVariables(EntityData _entity)
		{
			// transform variables
			if (CanDisableGameObject())
			{
				_entity.AddValue_Boolean(EntityValue.ENABLED, gameObject.activeSelf);
			}

			if (DoTrans())
			{
				_entity.AddValue_Vector3D(EntityValue.POSITION, transform.localPosition);
			}

			if (DoRot())
			{
				_entity.AddValue_Quaternion(EntityValue.ROTATION, transform.localRotation);
			}

			if (DoScale() && GetComponent<Camera>() == null)
			{
				// no scale in cameras
				_entity.AddValue_Vector3D(EntityValue.SCALE, transform.localScale);
			}

			// parameter variables
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


		protected void FindVariables()
		{
			// transform variables
			m_valEnabled  = CanDisableGameObject() ? m_entity.GetValue_Boolean(EntityValue.ENABLED) : null;
			m_valPosition = DoTrans() ? m_entity.GetValue_Vector3D(EntityValue.POSITION) : null;
			m_valRotation = DoRot() ? m_entity.GetValue_Quaternion(EntityValue.ROTATION) : null;
			m_valScale    = DoScale() ? m_entity.GetValue_Vector3D(EntityValue.SCALE) : null;

			// parameter variables
			ParameterBase[] parameters = FindParameters();
			foreach (ParameterBase param in parameters)
			{
				ParameterProxy proxy = CreateParameterProxy(param, m_entity);

				if (proxy != null)
				{
					if (proxy.IsValid())
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


		protected void DestroyVariables()
		{
			// transform variables
			m_valEnabled  = null;
			m_valPosition = null;
			m_valRotation = null;
			m_valScale    = null;

			// parameter variables
			foreach (var proxy in m_proxies)
			{
				proxy.Destroy();
			}
			m_proxies.Clear();
		}


		protected void SynchroniseFromEntity()
		{
			// Synchronise transform
			if (m_valEnabled != null && CanDisableGameObject())
			{
				TargetTransform.gameObject.SetActive(m_valEnabled.Value);
			}
			else
			{
				TargetTransform.gameObject.SetActive(true);
			}

			if (m_valPosition != null)
			{
				Vector3 pos = m_valPosition.Value;
				if (ReferenceTransform != null) { pos = ReferenceTransform.TransformPoint(pos); }
				TargetTransform.position = pos;
			}

			if (m_valRotation != null)
			{
				Quaternion rot = m_valRotation.Value;
				if (ReferenceTransform != null) { rot = ReferenceTransform.rotation * rot; }
				TargetTransform.rotation = rot;
			}

			if (m_valScale != null)
			{
				Vector3 scl = m_valScale.Value;
				// TODO: Consider global scale?
				// Since there is no absolute "global" scale, let's just use localScale for now
				// if (ReferenceTransform != null) { ReferenceTransform.lossyScale.Scale(scl); }
				TargetTransform.localScale = scl;
			}

			// Synchronise parameters
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromEntityToParameter();
			}
		}


		protected bool IsModified()
		{
			if (!m_modified)
			{
				if (DoTrans())
				{
					if ((m_oldPosition - this.transform.position).magnitude > MovementThreshold)
					{
						m_modified = true;
						m_oldPosition = this.transform.position;
					}
				}

				if (DoRot())
				{
					float angle = Quaternion.Angle(this.transform.rotation, m_oldRotation);
					if (angle > RotationThreshold)
					{
						m_modified = true;
						m_oldRotation = this.transform.rotation;
					}
				}

				if (DoScale())
				{
					if ((m_oldScale - this.transform.localScale).sqrMagnitude > 0)
					{
						m_modified = true;
						m_oldScale = this.transform.localScale;
					}
				}
			}

			if (!m_modified)
			{
				foreach (var proxy in m_proxies)
				{
					if (proxy.IsModified())
					{
						m_modified = true;
						break;
					}
				}
			}

			return m_modified;
		}


		protected void SynchroniseToEntity()
		{
			// Synchronise transform
			if (m_valEnabled != null)
			{
				m_valEnabled.Modify(TargetTransform.gameObject.activeSelf);
			}

			if (m_valPosition != null)
			{
				Vector3 pos = TargetTransform.position;
				if (ReferenceTransform != null) pos = ReferenceTransform.InverseTransformPoint(pos);
				m_valPosition.Modify(pos);
			}

			if (m_valRotation != null)
			{
				Quaternion rot = TargetTransform.rotation;
				if (ReferenceTransform != null) { rot = Quaternion.Inverse(ReferenceTransform.rotation) * rot; }
				m_valRotation.Modify(rot);
			}

			if (m_valScale != null)
			{
				// TODO: Consider global scale?
				Vector3 scl = TargetTransform.localScale;
				// if (ReferenceTransform != null) { ...??? }
				m_valScale.Modify(scl);
			}

			// Synchronise parameters
			foreach (var proxy in m_proxies)
			{
				proxy.TransferValueFromParameterToEntity();
			}
		}


		protected void ResetModified()
		{
			foreach (var proxy in m_proxies)
			{
				proxy.ResetModified();
			}
			m_modified = false;
		}



		public bool Registered
		{
			get
			{
				return m_entity.State == EntityData.EntityState.Registered;
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


			private readonly Parameter_Boolean   m_parameter;
			private readonly EntityValue_Boolean m_entityValue;
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


			private readonly Parameter_Integer m_parameter;
			private readonly EntityValue_Int64 m_entityValue;
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


			private readonly Parameter_Double m_parameter;
			private readonly EntityValue_Float64 m_entityValue;
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


			private readonly Parameter_DoubleRange m_parameter;
			private readonly EntityValue_Float64   m_entityValueMin, m_entityValueMax;
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


			private readonly Parameter_String m_parameter;
			private readonly EntityValue_String m_entityValue;
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


			private readonly Parameter_List    m_parameter;
			private readonly EntityValue_Int32 m_entityValue;
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


			private readonly Parameter_Vector3    m_parameter;
			private readonly EntityValue_Vector3D m_entityValue;
		}

		
		private bool m_initialised = false;

		private bool m_oldControlledByClient;
		private int  m_controlChangeCooldown = 0;

		private EntityData m_entity;

		private EntityValue_Boolean    m_valEnabled;
		private EntityValue_Vector3D   m_valPosition;
		private EntityValue_Quaternion m_valRotation;
		private EntityValue_Vector3D   m_valScale;

		private Vector3    m_oldPosition;
		private Quaternion m_oldRotation;
		private Vector3    m_oldScale;
		private bool       m_modified;

		private List<ParameterProxy> m_proxies;
	}
}
