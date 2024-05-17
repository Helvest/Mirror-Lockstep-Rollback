using System;
using System.Collections.Generic;
using UnityEngine;

public static class FlagMachineControllerExtension
{

	#region Has Flags

	public static bool HasFlag<T>(this IHoldFlagMachine<T> hold, T state)
	{
		return hold.FlagMachine.HasFlag(state);
	}

	public static bool HasFlags<T>(this IHoldFlagMachine<T> hold, params T[] state)
	{
		return hold.FlagMachine.HasFlags(state);
	}

	public static bool HasFlags<T>(this IHoldFlagMachine<T> hold, IEnumerable<T> state)
	{
		return hold.FlagMachine.HasFlags(state);
	}

	#endregion

	#region Get Flags

	public static List<T> GetFlags<T>(this IHoldFlagMachine<T> hold)
	{
		return hold.FlagMachine.GetFlags();
	}

	#endregion

	#region Set Flag

	public static bool SetFlag<T>(this IHoldFlagMachine<T> hold, T flag)
	{
		return hold.FlagMachine.SetFlag(flag);
	}

	public static void SetFlags<T>(this IHoldFlagMachine<T> hold, IEnumerable<T> flags)
	{
		hold.FlagMachine.SetFlags(flags);
	}

	#endregion

	#region Remove Flags

	public static bool UnsetFlag<T>(this IHoldFlagMachine<T> hold, T value)
	{
		return hold.FlagMachine.UnsetFlag(value);
	}

	public static void UnsetAllFlags<T>(this IHoldFlagMachine<T> hold)
	{
		hold.FlagMachine.UnsetAllFlags();
	}

	#endregion

	#region Replace Flags

	public static void ReplaceFlags<T>(this IHoldFlagMachine<T> hold, params T[] flags)
	{
		hold.FlagMachine.ReplaceFlags(flags);
	}

	public static void ReplaceFlags<T>(this IHoldFlagMachine<T> hold, IEnumerable<T> flags)
	{
		hold.FlagMachine.ReplaceFlags(flags);
	}

	#endregion

	#region Add Flag

	public static void AddFlag<T>(this IHoldFlagMachine<T> hold, T state, Action<T> enterAction = default, Action<T> exitAction = default, Action updateAction = default)
	{
		hold.FlagMachine.AddFlag(state, enterAction, exitAction, updateAction);
	}

	#endregion

	#region Remove Flag

	public static void RemoveFlag<T>(this IHoldFlagMachine<T> hold, T state)
	{
		hold.FlagMachine.RemoveFlag(state);
	}

	#endregion

}
