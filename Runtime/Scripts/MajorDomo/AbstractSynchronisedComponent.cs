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
		public abstract void CreateEntityVariables(EntityData _entity);
		public abstract void FindEntityVariables(EntityData _entity);
		public abstract void DestroyEntityVariables();
		
		public abstract void SynchroniseFromEntity(bool _initialise);
		
		public abstract bool IsModified();
		public abstract void SynchroniseToEntity();
		public abstract void ResetModified();
	}
}
