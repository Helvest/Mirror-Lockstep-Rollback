using System;
using HFSM;

public static class StateMachineControllerExtension
{

	#region IsState

	public static bool IsState<T>(this IHoldStateMachine<T> hold, T state)
	{
		return hold.StateMachine.IsState(state);
	}

	#endregion

	#region GetState

	public static T GetState<T>(this IHoldStateMachine<T> hold)
	{
		return hold.StateMachine.State;
	}

	#endregion

	#region SetState

	public static void SetState<T>(this IHoldStateMachine<T> hold, T state)
	{
		hold.StateMachine.State = state;
	}

	#endregion

	#region AddState

	public static void AddState<T>(this IHoldStateMachine<T> hold, T name, StateBase<T> state = null)
	{
		hold.StateMachine.AddState(name, state);
	}

	#endregion

	#region RemoveState

	public static void RemoveState<T>(this IHoldStateMachine<T> hold, T state)
	{
		hold.StateMachine.RemoveState(state);
	}

	#endregion

}
