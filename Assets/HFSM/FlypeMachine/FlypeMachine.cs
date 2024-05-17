using System;
using System.Collections.Generic;

namespace HFSM
{
	[Serializable]
	public class FlypeMachine : FlagMachine<Type>
	{

		#region Constructor

		public FlypeMachine(bool acceptFlagsNotIncluded = true, bool canReenterSameFlag = false) :
			base(acceptFlagsNotIncluded, canReenterSameFlag)
		{ }

		#endregion

		#region Has Flag

		public bool HasFlag<t>()
		{
			return HasFlag(typeof(t));
		}

		public bool HasFlag<t>(t instance)
		{
			return HasFlag(instance.GetType());
		}

		#endregion

		#region Set Flags

		public void SetFlag<t>()
		{
			base.SetFlag(typeof(t));
		}

		public void SetFlag<t>(t instance)
		{
			Type type = instance.GetType();
			UnityEngine.Debug.Log("type: " + type);
			base.SetFlag(type);
		}

		public void SetFlags<t>(IEnumerable<t> instances)
		{
			foreach (var instance in instances)
			{
				base.SetFlag(instance.GetType());
			}
		}

		#endregion

		#region Unset Flags

		public void UnsetFlag<t>()
		{
			UnsetFlag(typeof(t));
		}

		public void UnsetFlags<t>(params t[] instances)
		{
			foreach (var instance in instances)
			{
				UnsetFlag(instance.GetType());
			}
		}

		public void UnsetFlags<t>(IEnumerable<t> instances)
		{
			foreach (var instance in instances)
			{
				UnsetFlag(instance.GetType());
			}
		}

		#endregion

		#region Replace Flags

		public void ReplaceFlags<t>(params t[] instances)
		{
			var types = new List<Type>();

			foreach (var instance in instances)
			{
				types.Add(instance.GetType());
			}

			ReplaceFlags(types);
		}

		public void ReplaceFlags<t>(IEnumerable<t> instances)
		{
			var types = new List<Type>();

			foreach (var instance in instances)
			{
				types.Add(instance.GetType());
			}

			ReplaceFlags(types);
		}

		#endregion

		#region Add Flag

		public void AddFlag<t>(Action<Type> enterAction = default, Action<Type> exitAction = default, Action updateAction = default)
		{
			base.AddFlag(typeof(t), enterAction, exitAction, updateAction);
		}

		public void AddFlag<t>(t type, Action<Type> enterAction = default, Action<Type> exitAction = default, Action updateAction = default)
		{
			base.AddFlag(type.GetType(), enterAction, exitAction, updateAction);
		}

		#endregion

		#region Remove Flag

		public void RemoveFlag<t>()
		{
			base.RemoveFlag(typeof(t));
		}

		public void RemoveFlag<t>(t type)
		{
			base.RemoveFlag(type.GetType());
		}

		#endregion

	}
}
