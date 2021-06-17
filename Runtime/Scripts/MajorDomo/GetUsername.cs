#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for listening to registration of a SynchronisedGameObject
	/// and changing the content of a text element to the username.
	/// </summary>
	/// 
	[RequireComponent(typeof(Text))]
	public class GetUsername : MonoBehaviour
	{
		[Tooltip("SynchronisedGameObject to register with (Leave empty to use first SynchronisedGameObject in this or its parent nodes)")]
		public SynchronisedGameObject m_synchronisedGameObject = null;

		[Tooltip("Format string for the username ({0} is replaced by the username)")]
		public string FormatString = "{0}";

		[Tooltip("Text to use when there is no username")]
		public string NoUserString = "";


		public void Start()
		{
			if (m_synchronisedGameObject == null)
			{
				m_synchronisedGameObject = GetComponentInParent<SynchronisedGameObject>();
			}
			if (m_synchronisedGameObject != null)
			{
				m_synchronisedGameObject.OnSynchronisationStart += OnSynchronisationStart;
				m_synchronisedGameObject.OnSynchronisationEnd   += OnSynchronisationEnd;
			}
			m_text = GetComponent<Text>();
			OnSynchronisationEnd(null);
		}


		public void OnSynchronisationStart(SynchronisedGameObject _gameObject)
		{
			string userName = _gameObject.GetClient().UserName;
			m_text.text = string.Format(FormatString, userName);
		}


		public void OnSynchronisationEnd(SynchronisedGameObject _gameObject)
		{
			m_text.text = NoUserString;
		}


		public void OnDestroy()
		{
			if (m_synchronisedGameObject != null)
			{
				m_synchronisedGameObject.OnSynchronisationStart -= OnSynchronisationStart;
				m_synchronisedGameObject.OnSynchronisationEnd   -= OnSynchronisationEnd;
			}
		}


		private Text m_text;
	}
}