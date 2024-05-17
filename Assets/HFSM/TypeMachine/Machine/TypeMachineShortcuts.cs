using System;

namespace HFSM
{
	public static class TypeMachineShortcuts
	{
		/*
			"Shortcut" methods
			- These are meant to reduce the boilerplate code required by the user for simple
			states and transitions.
			- They do this by creating a new State / Transition instance in the background
			and then setting the desired fields.
			- They can also optimise certain cases for you by choosing the best type,
			such as a StateBase for an empty state instead of a State instance.
		*/

		/// <summary>
		/// Shortcut method for adding a regular state.
		/// It creates a new State() instance under the hood. => See State for more information.
		/// For empty states with no logic it creates a new StateBase for optimal performance.
		/// </summary>
		public static void AddState<TOwnId, TEvent, T>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			T name,
			Action<State<Type, TEvent>> onEnter = null,
			Action<State<Type, TEvent>> onLogic = null,
			Action<State<Type, TEvent>> onExit = null,
			Func<State<Type, TEvent>, bool> canExit = null)
		{
			// Optimise for empty states
			if (onEnter == null && onLogic == null && onExit == null && canExit == null)
			{
				fsm.AddState(name.GetType(), new StateBase<Type>());
				return;
			}

			fsm.AddState(name.GetType(), new State<Type, TEvent>(onEnter, onLogic, onExit, canExit));
		}

		/// <summary>
		/// Creates the most efficient transition type possible for the given parameters.
		/// It creates a Transition instance when a condition is specified and otherwise
		/// it returns a TransitionBase.
		/// </summary>
		private static TransitionBase<Type> CreateOptimizedTransition(
			Type from,
			Type to,
			Func<Transition<Type>, bool> condition = null,
			bool forceInstantly = false)
		{
			if (condition == null)
			{
				return new TransitionBase<Type>(from, to, forceInstantly);
			}

			return new Transition<Type>(from, to, condition, forceInstantly);
		}

		/// <summary>
		/// Shortcut method for adding a regular transition.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTransition<TOwnId, TEvent>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			Type from,
			Type to,
			Func<Transition<Type>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTransition(CreateOptimizedTransition(from, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding a regular transition that can happen from any state.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTransitionFromAny<TOwnId, TEvent>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			Type to,
			Func<Transition<Type>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTransitionFromAny(CreateOptimizedTransition(default, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding a new trigger transition between two states that is only checked
		/// when the specified trigger is activated.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTriggerTransition<TOwnId, TEvent>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			TEvent trigger,
			Type from,
			Type to,
			Func<Transition<Type>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTriggerTransition(trigger, CreateOptimizedTransition(from, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding a new trigger transition that can happen from any possible state, but is only
		/// checked when the specified trigger is activated.
		/// It creates a new Transition() instance under the hood. => See Transition for more information.
		/// When no condition is required, it creates a TransitionBase for optimal performance.
		/// </summary>
		public static void AddTriggerTransitionFromAny<TOwnId, TEvent>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			TEvent trigger,
			Type to,
			Func<Transition<Type>, bool> condition = null,
			bool forceInstantly = false)
		{
			fsm.AddTriggerTransitionFromAny(trigger, CreateOptimizedTransition(default, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding two transitions:
		/// If the condition function is true, the fsm transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		public static void AddTwoWayTransition<TOwnId, TEvent>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			Type from,
			Type to,
			Func<Transition<Type>, bool> condition,
			bool forceInstantly = false)
		{
			fsm.AddTwoWayTransition(new Transition<Type>(from, to, condition, forceInstantly));
		}

		/// <summary>
		/// Shortcut method for adding two transitions that are only checked when the specified trigger is activated:
		/// If the condition function is true, the fsm transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		public static void AddTwoWayTriggerTransition<TOwnId, TEvent>(
			this StateMachine<TOwnId, Type, TEvent> fsm,
			TEvent trigger,
			Type from,
			Type to,
			Func<Transition<Type>, bool> condition,
			bool forceInstantly = false)
		{
			fsm.AddTwoWayTriggerTransition(trigger, new Transition<Type>(from, to, condition, forceInstantly));
		}
	}
}