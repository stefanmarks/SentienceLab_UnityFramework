#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MajorDomo
{
	public abstract class AbstractSynchronisedComponent : MonoBehaviour
	{
		/// <summary>
		/// Initialise the component.
		/// </summary>
		public virtual void Initialise() { }

		/// <summary>
		/// Called when the entity variables have to be created.
		/// </summary>
		/// <param name="_entity">entity to create the variables for</param>
		public abstract void CreateEntityVariables(EntityData _entity);

		/// <summary>
		/// Called when the entity variables have to be searched for.
		/// </summary>
		/// <param name="_entity">The entity to search the variables in</param>
		public abstract void FindEntityVariables(EntityData _entity);

		/// <summary>
		/// Called to destroy the entity variables.
		/// </summary>
		public abstract void DestroyEntityVariables();


		/// <summary>
		/// Called during the Unity Update phase.
		/// </summary>
		/// <param name="_controlledByServer"><c>true</c> when the synchronised object is controlled by the server, 
		///                                   <c>false</c> when controlled by the client</param>
		public virtual void OnUpdate(bool _controlledByServer) { }

		/// <summary>
		/// Called during the Unity FixedUpdate phase.
		/// </summary>
		/// <param name="_controlledByServer"><c>true</c> when the synchronised object is controlled by the server, 
		///                                   <c>false</c> when controlled by the client</param>
		public virtual void OnFixedUpdate(bool _controlledByServer) { }


		/// <summary>
		/// Called when data needs to be synchronised from the entity variables to the component (server control)
		/// </summary>
		/// <param name="_firstTime"><c>true</c> when this is the first call of this kind</param>
		/// 
		public abstract void SynchroniseFromEntity(bool _firstTime);

		
		/// <summary>
		/// Check is the synchronsised component has been modified in a way that needs updating the entity variables.
		/// </summary>
		/// <returns><c>true</c> when the component has been modified</returns>
		public abstract bool IsModified();

		/// <summary>
		/// Called when data needs to be synchronised from the component to the entity variables (client control).
		/// </summary>
		/// <param name="_firstTime"><c>true</c> when this is the first call of this kind</param>
		///
		public abstract void SynchroniseToEntity(bool _firstTime);

		/// <summary>
		/// Called to reset the "modified" flag in the component.
		/// </summary>
		public abstract void ResetModified();
	}
}
