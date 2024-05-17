using System;
using System.Collections.Generic;

public static class FlypeMachineControllerExtension
{

	#region Has Flag

	public static bool HasFlag<t>(this IHoldFlypeMachine hold)
	{
		return hold.FlypeMachine.HasFlag<t>();
	}

	public static bool HasFlag<t>(this IHoldFlypeMachine hold, t instance)
	{
		return hold.FlypeMachine.HasFlag(instance);
	}

	#endregion

	#region Set Flag

	public static void SetFlag<t>(this IHoldFlypeMachine hold)
	{
		hold.FlypeMachine.SetFlag<t>();
	}

	public static void SetFlag<t>(this IHoldFlypeMachine hold, t instances)
	{
		hold.FlypeMachine.SetFlag(instances);
	}

	public static void SetFlags<t>(this IHoldFlypeMachine hold, IEnumerable<t> instances)
	{
		hold.FlypeMachine.SetFlags(instances);
	}

	#endregion

	#region Unset Flags

	public static void UnsetFlag<t>(this IHoldFlypeMachine hold)
	{
		hold.FlypeMachine.UnsetFlag<t>();
	}

	public static void UnsetFlags<t>(this IHoldFlypeMachine hold, params t[] instances)
	{
		hold.FlypeMachine.UnsetFlags(instances);
	}

	public static void UnsetFlags<t>(this IHoldFlypeMachine hold, IEnumerable<t> instances)
	{
		hold.FlypeMachine.UnsetFlags(instances);
	}

	#endregion

	#region Replace Flags

	public static void ReplaceFlags<t>(this IHoldFlypeMachine hold, params t[] instances)
	{
		hold.FlypeMachine.ReplaceFlags(instances);
	}

	public static void ReplaceFlags<t>(this IHoldFlypeMachine hold, IEnumerable<t> instances)
	{
		hold.FlypeMachine.ReplaceFlags(instances);
	}

	#endregion

	#region Add Flag

	public static void AddFlag<T>(this IHoldFlypeMachine hold, Action<Type> enterAction = default, Action<Type> exitAction = default, Action updateAction = default)
	{
		hold.FlypeMachine.AddFlag<T>(enterAction, exitAction, updateAction);
	}

	public static void AddFlag<T>(this IHoldFlypeMachine hold, T state, Action<Type> enterAction = default, Action<Type> exitAction = default, Action updateAction = default)
	{
		hold.FlypeMachine.AddFlag(state, enterAction, exitAction, updateAction);
	}

	public static void AddFlag(this IHoldFlypeMachine hold, Type state, Action<Type> enterAction = default, Action<Type> exitAction = default, Action updateAction = default)
	{
		hold.FlypeMachine.AddFlag(state, enterAction, exitAction, updateAction);
	}

	#endregion

	#region Remove Flag

	public static void RemoveFlag<T>(this IHoldFlypeMachine hold)
	{
		hold.FlypeMachine.RemoveFlag<T>();
	}

	public static void RemoveFlag<T>(this IHoldFlypeMachine hold, T state)
	{
		hold.FlypeMachine.RemoveFlag(state);
	}

	#endregion

}