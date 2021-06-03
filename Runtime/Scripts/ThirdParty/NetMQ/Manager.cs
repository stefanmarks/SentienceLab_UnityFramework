#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
#endregion Copyright Information

using System.Collections.Generic;

namespace NetMQ
{
	/// <summary>
	/// Class for managing code blocks that use NetMQ. By registeribng and unregistering,
	/// it is ensured that proper NetNQ initialisation and deinitialisation happens.
	/// </summary>
	/// 
	public class Manager
	{
		public static void Register(object user)
		{
			if (m_users == null)
			{
				m_users = new HashSet<object>();
			}

			if (m_users.Count == 0)
			{
				/// to make sure, Unity doesn't crash on second run
				AsyncIO.ForceDotNet.Force();
			}

			m_users.Add(user);
		}

		
		public static void Unregister(object user)
		{
			if (m_users != null)
			{
				m_users.Remove(user);
				if (m_users.Count == 0)
				{
					/// properly cleanup and close connections
					NetMQ.NetMQConfig.Cleanup(false);
					m_users = null;
				}
			}
		}

		private static ISet<object> m_users = null;
	}
}