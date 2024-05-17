using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Random;

/**
 * Hierarchichal finite state machine for Unity
 * by Helvest
 */

namespace HFSM
{
	/// <summary>
	/// A finite state machine that can also be used as a state of a parent state machine to create
	/// a hierarchy (-> hierarchical state machine)
	/// </summary>
	public class StateMachine<TOwnId, TStateId, TEvent> :
		StateBase<TOwnId>,
		ITriggerable<TEvent>,
		IStateMachine<TStateId>,
		IActionable<TEvent>
	{

		#region StateBundle

		/// <summary>
		/// A bundle of a state together with the outgoing transitions and trigger transitions.
		/// It's useful, as you only need to do one Dictionary lookup for these three items.
		/// => Much better performance
		/// </summary>
		private class StateBundle
		{
			// By default, these fields are all null and only get a value when you need them
			// => Lazy evaluation => Memory efficient, when you only need a subset of features
			public StateBase<TStateId> state;
			public List<TransitionBase<TStateId>> transitions;
			public Dictionary<TEvent, List<TransitionBase<TStateId>>> triggerToTransitions;

			public void AddTransition(TransitionBase<TStateId> t)
			{
				transitions ??= new List<TransitionBase<TStateId>>();
				transitions.Add(t);
			}

			public void AddTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
			{
				triggerToTransitions ??= new Dictionary<TEvent, List<TransitionBase<TStateId>>>();

				if (!triggerToTransitions.TryGetValue(trigger, out var transitionsOfTrigger))
				{
					transitionsOfTrigger = new List<TransitionBase<TStateId>>();
					triggerToTransitions.Add(trigger, transitionsOfTrigger);
				}

				transitionsOfTrigger.Add(transition);
			}
		}

		#endregion

		#region Fields

		// A cached empty list of transitions (For improved readability, less GC)
		private static readonly List<TransitionBase<TStateId>> _noTransitions
			= new List<TransitionBase<TStateId>>(0);
		private static readonly Dictionary<TEvent, List<TransitionBase<TStateId>>> _noTriggerTransitions
			= new Dictionary<TEvent, List<TransitionBase<TStateId>>>(0);

		private (TStateId state, bool hasState) _startState = (default, false);
		private (TStateId state, bool isPending) _pendingState = (default, false);

		// Central storage of states
		private readonly Dictionary<TStateId, StateBundle> _nameToStateBundle = new Dictionary<TStateId, StateBundle>();
		private List<TransitionBase<TStateId>> _activeTransitions = _noTransitions;
		private Dictionary<TEvent, List<TransitionBase<TStateId>>> _activeTriggerTransitions = _noTriggerTransitions;

		private readonly List<TransitionBase<TStateId>> _transitionsFromAny
			= new List<TransitionBase<TStateId>>();
		private readonly Dictionary<TEvent, List<TransitionBase<TStateId>>> _triggerTransitionsFromAny
			= new Dictionary<TEvent, List<TransitionBase<TStateId>>>();

		public StateBase<TStateId> ActiveState { get; private set; } = null;

		#endregion

		#region Init

		/// <summary>
		/// Initialises a new instance of the StateMachine class
		/// </summary>
		public StateMachine() : base()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (useDebug)
			{
				Debug.Log("Constructor - StateMachine");
			}
#endif
		}

		public StateMachine(TStateId startState) : base()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (useDebug)
			{
				Debug.Log("Constructor - StateMachine");
			}
#endif
			SetStartState(startState);
		}

		#endregion

		#region Exit

		/// <summary>
		/// Notifies the state machine that the state can cleanly exit,
		/// and if a state change is pending, it will execute it.
		/// </summary>
		public void StateCanExit()
		{
			if (_pendingState.isPending)
			{
				var state = _pendingState.state;
				_pendingState = (default, false);
				State = state;
			}

			//fsm?.StateCanExit();
		}

		/*public override bool OnExitRequest()
		{
			if (!ActiveState.canExit)
			{
				return ActiveState.OnExitRequest();
			}

			return true;
		}*/

		#endregion

		#region Get Set State

		public bool IsState(TStateId state)
		{
			return EqualityComparer<TStateId>.Default.Equals(state, State);
		}

		/// <summary>
		/// Instantly changes to the target state
		/// </summary>
		/// <param name="value">The name / identifier of the active state</param>
		public TStateId State
		{
			get
			{
				return ActiveState != null ? ActiveState.name : default;
			}

			set
			{
				ActiveState?.OnExit();

				if (!_nameToStateBundle.TryGetValue(value, out var bundle) || bundle.state == null)
				{
					AddState(value);
					_nameToStateBundle.TryGetValue(value, out bundle);
				}

				_activeTransitions = bundle.transitions ?? _noTransitions;
				_activeTriggerTransitions = bundle.triggerToTransitions ?? _noTriggerTransitions;

				ActiveState = bundle.state;
				ActiveState.OnEnter();

				for (int i = 0; i < _activeTransitions.Count; i++)
				{
					_activeTransitions[i].OnEnter();
				}

				foreach (var transitions in _activeTriggerTransitions.Values)
				{
					for (int i = 0; i < transitions.Count; i++)
					{
						transitions[i].OnEnter();
					}
				}
			}
		}

		#endregion

		#region StateChange

		/// <summary>
		/// Checks if a transition can take place, and if this is the case, transition to the
		/// "to" state and return true. Otherwise it returns false.
		/// </summary>
		/// <param name="transition"></param>
		/// <returns></returns>
		private bool TryTransition(TransitionBase<TStateId> transition)
		{
			if (!transition.ShouldTransition())
			{
				return false;
			}

			RequestStateChange(transition.to, transition.forceInstantly);

			return true;
		}

		/// <summary>
		/// Requests a state change, respecting the <c>needsExitTime</c> property of the active state
		/// </summary>
		/// <param name="name">The name / identifier of the target state</param>
		/// <param name="forceInstantly">Overrides the needsExitTime of the active state if true,
		/// therefore forcing an immediate state change</param>
		public void RequestStateChange(TStateId name, bool forceInstantly = false)
		{
			if (ActiveState.canExit || forceInstantly || ActiveState.OnExitRequest())
			{
				State = name;
				return;
			}

			_pendingState = (name, true);
		}

		#endregion

		#region StateMethods

		/// <summary>
		/// Initialises the state machine and must be called before OnLogic is called.
		/// It sets the activeState to the selected startState.
		/// </summary>
		public override void OnEnter()
		{
			if (!_startState.hasState)
			{
				Debug.LogError("No start state is selected");
				return;
			}

			State = _startState.state;

			for (int i = 0; i < _transitionsFromAny.Count; i++)
			{
				_transitionsFromAny[i].OnEnter();
			}

			foreach (var transitions in _triggerTransitionsFromAny.Values)
			{
				for (int i = 0; i < transitions.Count; i++)
				{
					transitions[i].OnEnter();
				}
			}
		}

		/// <summary>
		/// Runs one logic step. It does at most one transition itself and
		/// calls the active state's logic function (after the state transition, if
		/// one occurred).
		/// </summary>
		public override void OnLogic()
		{
			TransitionBase<TStateId> transition;

			// Try the "global" transitions that can transition from any state
			for (int i = 0; i < _transitionsFromAny.Count; i++)
			{
				transition = _transitionsFromAny[i];

				// Don't transition to the "to" state, if that state is already the active state
				if (EqualityComparer<TStateId>.Default.Equals(transition.to, ActiveState.name))
				{
					continue;
				}

				if (TryTransition(transition))
				{
					break;
				}
			}

			// Try the "normal" transitions that transition from one specific state to another
			for (int i = 0; i < _activeTransitions.Count; i++)
			{
				transition = _activeTransitions[i];

				if (TryTransition(transition))
				{
					break;
				}
			}

			ActiveState.OnLogic();
		}

		public override void OnExit()
		{
			if (ActiveState == null)
			{
				return;
			}

			ActiveState.OnExit();

			// By setting the activeState to null, the state's onExit method won't be called
			// a second time when the state machine enters again (and changes to the start state)
			ActiveState = null;
		}

		#endregion

		#region Add State

		/// <summary>
		/// Adds a new node / state to the state machine.
		/// </summary>
		/// <param name="name">The name / identifier of the new state</param>
		/// <param name="state">The new state instance, e.g. <c>State</c>, <c>CoState</c>, <c>StateMachine</c></param>
		public void AddState(TStateId name, StateBase<TStateId> state = null)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (useDebug)
			{
				Debug.Log("AddState - ID: " + name + " | State: " + state);
			}
#endif
			state ??= new StateBase<TStateId>();

			state.name = name;
			state.Init();

			var bundle = GetOrCreateStateBundle(name);
			bundle.state = state;

			//If is first state, is set as Start State
			if (_nameToStateBundle.Count == 1 && !_startState.hasState)
			{
				SetStartState(name);
			}
		}

		/// <summary>
		/// Gets the StateBundle belonging to the <c>name</c> state "slot" if it exists.
		/// Otherwise it will create a new StateBundle, that will be added to the Dictionary,
		/// and return the newly created instance.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private StateBundle GetOrCreateStateBundle(TStateId name)
		{
			if (!_nameToStateBundle.TryGetValue(name, out var bundle))
			{
				bundle = new StateBundle();
				_nameToStateBundle.Add(name, bundle);
			}

			return bundle;
		}

		/// <summary>
		/// Defines the entry point of the state machine
		/// </summary>
		/// <param name="name">The name / identifier of the start state</param>
		public void SetStartState(TStateId name)
		{
			_startState = (name, true);
		}

		#endregion

		#region Remove State

		public void RemoveState(TStateId name)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (useDebug)
			{
				Debug.Log("Remove State: " + state);
			}
#endif
			_nameToStateBundle.Remove(name);
		}

		#endregion

		#region Get State

		public bool TryGetState(TStateId name, out StateBase<TStateId> state)
		{
			if (!_nameToStateBundle.TryGetValue(name, out var bundle) || bundle.state == null)
			{
				state = null;
				return false;
			}

			state = bundle.state;
			return true;
		}

		public StateBase<TStateId> GetState(TStateId name)
		{
			if (!_nameToStateBundle.TryGetValue(name, out var bundle) || bundle.state == null)
			{
				return default;
			}

			return bundle.state;
		}

		#endregion

		#region Transition

		/// <summary>
		/// Initialises a transition, i.e. sets its fsm attribute, and then calls its Init method.
		/// </summary>
		/// <param name="transition"></param>
		private void InitTransition(TransitionBase<TStateId> transition)
		{
			transition.Init();
		}

		/// <summary>
		/// Adds a new transition between two states.
		/// </summary>
		/// <param name="transition">The transition instance</param>
		public void AddTransition(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			var bundle = GetOrCreateStateBundle(transition.from);
			bundle.AddTransition(transition);
		}

		/// <summary>
		/// Adds a new transition that can happen from any possible state
		/// </summary>
		/// <param name="transition">The transition instance; The "from" field can be
		/// left empty, as it has no meaning in this context.</param>
		public void AddTransitionFromAny(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			_transitionsFromAny.Add(transition);
		}

		/// <summary>
		/// Adds two transitions:
		/// If the condition of the transition instance is true, it transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a ReverseTransition.
		/// </remarks>
		public void AddTwoWayTransition(TransitionBase<TStateId> transition)
		{
			AddTransition(transition);

			var reverse = new ReverseTransition<TStateId>(transition, false);
			AddTransition(reverse);
		}

		#endregion

		#region Add Trigger

		/// <summary>
		/// Adds a new trigger transition between two states that is only checked
		/// when the specified trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <param name="transition">The transition instance, e.g. Transition, TransitionAfter, ...</param>
		public void AddTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			var bundle = GetOrCreateStateBundle(transition.from);
			bundle.AddTriggerTransition(trigger, transition);
		}

		/// <summary>
		/// Adds a new trigger transition that can happen from any possible state, but is only
		/// checked when the specified trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <param name="transition">The transition instance; The "from" field can be
		/// left empty, as it has no meaning in this context.</param>
		public void AddTriggerTransitionFromAny(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			if (!_triggerTransitionsFromAny.TryGetValue(trigger, out var transitionsOfTrigger))
			{
				transitionsOfTrigger = new List<TransitionBase<TStateId>>();
				_triggerTransitionsFromAny.Add(trigger, transitionsOfTrigger);
			}

			transitionsOfTrigger.Add(transition);
		}

		/// <summary>
		/// Adds two transitions that are only checked when the specified trigger is activated:
		/// If the condition of the transition instance is true, it transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a ReverseTransition.
		/// </remarks>
		public void AddTwoWayTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);
			AddTriggerTransition(trigger, transition);

			var reverse = new ReverseTransition<TStateId>(transition, false);
			InitTransition(reverse);
			AddTriggerTransition(trigger, reverse);
		}

		#endregion

		#region Trigger

		/// <summary>
		/// Activates the specified trigger, checking all targeted trigger transitions to see whether
		/// a transition should occur.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <returns>True when a transition occurred, otherwise false</returns>
		private bool TryTrigger(TEvent trigger)
		{
			TransitionBase<TStateId> transition;

			if (_triggerTransitionsFromAny.TryGetValue(trigger, out var triggerTransitions))
			{
				for (int i = 0; i < triggerTransitions.Count; i++)
				{
					transition = triggerTransitions[i];

					if (EqualityComparer<TStateId>.Default.Equals(transition.to, ActiveState.name))
					{
						continue;
					}

					if (TryTransition(transition))
					{
						return true;
					}
				}
			}

			if (_activeTriggerTransitions.TryGetValue(trigger, out triggerTransitions))
			{
				for (int i = 0; i < triggerTransitions.Count; i++)
				{
					transition = triggerTransitions[i];

					if (TryTransition(transition))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Activates the specified trigger in all active states of the hierarchy, checking all targeted
		/// trigger transitions to see whether a transition should occur.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		public void Trigger(TEvent trigger)
		{
			// If a transition occurs, then the trigger should not be activated
			// in the new active state, that the state machine just switched to.
			if (TryTrigger(trigger))
			{
				return;
			}

			(ActiveState as ITriggerable<TEvent>)?.Trigger(trigger);
		}

		/// <summary>
		/// Only activates the specified trigger locally in this state machine.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		public void TriggerLocally(TEvent trigger)
		{
			TryTrigger(trigger);
		}

		#endregion

		#region OnAction

		/// <summary>
		/// Runs an action on the currently active state.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		public void OnAction(TEvent trigger)
		{
			(ActiveState as IActionable<TEvent>)?.OnAction(trigger);
		}

		/// <summary>
		/// Runs an action on the currently active state and lets you pass one data parameter.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="data">Any custom data for the parameter</param>
		/// <typeparam name="TData">Type of the data parameter.
		/// 	Should match the data type of the action that was added via AddAction<T>(...).</typeparam>
		public void OnAction<TData>(TEvent trigger, TData data)
		{
			(ActiveState as IActionable<TEvent>)?.OnAction(trigger, data);
		}

		#endregion

		#region Debug

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		public bool useDebug = false;
#endif

		#endregion

	}

	// Overloaded classes to allow for an easier usage of the StateMachine for common cases.
	// E.g. new StateMachine() instead of new StateMachine<string, string, string>()

	public class StateMachine<TStateId, TEvent> : StateMachine<TStateId, TStateId, TEvent> { }

	public class StateMachine<TStateId> : StateMachine<TStateId, TStateId, string> { }

	public class StateMachine : StateMachine<string, string, string> { }

}
