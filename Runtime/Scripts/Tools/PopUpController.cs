#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Controller script for popup hints on objects.
	/// This needs to be set up in conjunction with an event handler, e.g.,
	/// linking a PointerEnter event to "TriggerAppearance" and a PointerExit event to "TriggerDisappearance".
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/Popup Controller")]
	
	public class PopUpController : MonoBehaviour
	{
		[Tooltip("The game object to appear/disappear")]
		public GameObject PopupObject;

		[Tooltip("Time in seconds from the start signal to the game object appearing")]
		public float AppearanceDelay    = 0.5f;

		[Tooltip("Time in seconds from the stop signal to the game object appearing")]
		public float DisappearanceDelay = 2.0f;


		public void Start()
		{
			SetVisibility(false);
		}


		public void OnEnable()
		{
			SetVisibility(false);
		}


		public void OnDisable()
		{
			SetVisibility(false);
		}


		public void TriggerAppearance()
		{
			if (m_state == EPopupState.HIDDEN)
			{
				m_timer = AppearanceDelay;
				m_state = EPopupState.APPEARING;
			}
			else if (m_state == EPopupState.DISAPPEARING)
			{
				// stop disappearance timer
				m_state = EPopupState.VISIBLE;
			}
		}


		public void TriggerImmediateAppearance()
		{
			if (m_state == EPopupState.HIDDEN || m_state == EPopupState.APPEARING)
			{
				m_timer = 0;
				m_state = EPopupState.APPEARING;
			}
			else if (m_state == EPopupState.DISAPPEARING)
			{
				// stop disappearance timer
				m_state = EPopupState.VISIBLE;
			}
		}


		public void TriggerDisappearance()
		{
			if (m_state == EPopupState.VISIBLE)
			{
				m_timer = DisappearanceDelay;
				m_state = EPopupState.DISAPPEARING;
			}
			else if (m_state == EPopupState.APPEARING)
			{
				// stop appearance timer
				m_state = EPopupState.HIDDEN;
			}
		}


		public void TriggerImmediateDisappearance()
		{
			if (m_state == EPopupState.VISIBLE || m_state == EPopupState.DISAPPEARING)
			{
				m_timer = 0;
				m_state = EPopupState.DISAPPEARING;
			}
			else if (m_state == EPopupState.APPEARING)
			{
				// stop appearance timer
				m_state = EPopupState.HIDDEN;
			}
		}


		public void Update()
		{
			if (m_state == EPopupState.APPEARING)
			{
				m_timer -= Time.deltaTime;
				if (m_timer < 0)
				{
					SetVisibility(true);
				}
			}
			else if (m_state == EPopupState.DISAPPEARING)
			{
				m_timer -= Time.deltaTime;
				if (m_timer < 0)
				{
					SetVisibility(false);
				}
			}
		}


		private void SetVisibility(bool _visible)
		{
			m_state = _visible ? EPopupState.VISIBLE : EPopupState.HIDDEN;
			PopupObject?.SetActive(_visible);
		}


		private enum EPopupState
		{
			HIDDEN, APPEARING, VISIBLE, DISAPPEARING
		}

		private EPopupState m_state;
		private float       m_timer;
	}
}
