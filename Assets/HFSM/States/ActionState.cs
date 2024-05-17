using System;
using System.Collections.Generic;
using UnityEngine;

namespace HFSM
{
	/// <summary>
	/// Base class of states that should support custom actions.
	/// </summary>
	public class ActionState<TStateId, TEvent> : StateBase<TStateId>, IActionable<TEvent>
	{
		// Lazy initialized
		private Dictionary<TEvent, Delegate> _actionsByEvent;

		public ActionState() : base() { }

		private void AddGenericAction(TEvent trigger, Delegate action)
		{
			_actionsByEvent ??= new Dictionary<TEvent, Delegate>();
			_actionsByEvent[trigger] = action;
		}

		private TTarget TryGetAndCastAction<TTarget>(TEvent trigger) where TTarget : Delegate
		{
			if (_actionsByEvent == null
				|| !_actionsByEvent.TryGetValue(trigger, out var action)
				|| action is null)
			{
				return null;
			}

			if (!(action is TTarget target))
			{
				Debug.LogError(
					$"The expected argument type ({typeof(TTarget)}) does not match the type of the added action ({action})."
				);
				return null;
			}

			return target;
		}

		/// <summary>
		/// Adds an action that can be called with OnAction(). Actions are like the builtin events
		/// OnEnter / OnLogic / ... but are defined by the user.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="action">Function that should be called when the action is run</param>
		/// <returns>Itself</returns>
		public ActionState<TStateId, TEvent> AddAction(TEvent trigger, Action action)
		{
			AddGenericAction(trigger, action);
			// Fluent interface
			return this;
		}

		/// <summary>
		/// Adds an action that can be called with OnAction<T>(). This overload allows you to
		/// run a function that takes one data parameter.
		/// Actions are like the builtin events OnEnter / OnLogic / ... but are defined by the user.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="action">Function that should be called when the action is run</param>
		/// <typeparam name="TData">Data type of the parameter of the function</typeparam>
		/// <returns>Itself</returns>
		public ActionState<TStateId, TEvent> AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			AddGenericAction(trigger, action);
			// Fluent interface
			return this;
		}

		/// <summary>
		/// Runs an action with the given name.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		public void OnAction(TEvent trigger)
			=> TryGetAndCastAction<Action>(trigger)?.Invoke();

		/// <summary>
		/// Runs an action with a given name and lets you pass in one parameter to the action function.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="data">Data to pass as the first parameter to the action</param>
		/// <typeparam name="TData">Type of the data parameter</typeparam>
		public void OnAction<TData>(TEvent trigger, TData data)
			=> TryGetAndCastAction<Action<TData>>(trigger)?.Invoke(data);
	}

	public class ActionState<TStateId> : ActionState<TStateId, string> { }

	public class ActionState : ActionState<string, string> { }

}
