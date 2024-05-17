using System;

namespace HFSM
{
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// It does not interfere with the wrapped state's timing / ...behaviour.
	/// </summary>
	public class StateWrapper<TStateId, TEvent>
	{
		public class WrappedState : StateBase<TStateId>, ITriggerable<TEvent>, IActionable<TEvent>
		{

			#region Fields

			private readonly Action<StateBase<TStateId>>
				_beforeOnEnter,
				_afterOnEnter,
				_beforeOnLogic,
				_afterOnLogic,
				_beforeOnExit,
				_afterOnExit;

			private readonly StateBase<TStateId> _state;

			#endregion

			#region Init

			public WrappedState(
					StateBase<TStateId> state,

					Action<StateBase<TStateId>> beforeOnEnter = null,
					Action<StateBase<TStateId>> afterOnEnter = null,

					Action<StateBase<TStateId>> beforeOnLogic = null,
					Action<StateBase<TStateId>> afterOnLogic = null,

					Action<StateBase<TStateId>> beforeOnExit = null,
					Action<StateBase<TStateId>> afterOnExit = null) : base()
			{
				_state = state;

				_beforeOnEnter = beforeOnEnter;
				_afterOnEnter = afterOnEnter;

				_beforeOnLogic = beforeOnLogic;
				_afterOnLogic = afterOnLogic;

				_beforeOnExit = beforeOnExit;
				_afterOnExit = afterOnExit;
			}

			public override void Init()
			{
				_state.name = name;
				_state.Init();
			}

			#endregion

			#region StateMethods

			public override void OnEnter()
			{
				_beforeOnEnter?.Invoke(this);
				_state.OnEnter();
				_afterOnEnter?.Invoke(this);
			}

			public override void OnLogic()
			{
				_beforeOnLogic?.Invoke(this);
				_state.OnLogic();
				_afterOnLogic?.Invoke(this);
			}

			public override void OnExit()
			{
				_beforeOnExit?.Invoke(this);
				_state.OnExit();
				_afterOnExit?.Invoke(this);
			}

			public override bool OnExitRequest()
			{
				return _state.OnExitRequest();
			}

			#endregion

			#region Trigger

			public void Trigger(TEvent trigger)
			{
				(_state as ITriggerable<TEvent>)?.Trigger(trigger);
			}

			public void OnAction(TEvent trigger)
			{
				(_state as IActionable<TEvent>)?.OnAction(trigger);
			}

			public void OnAction<TData>(TEvent trigger, TData data)
			{
				(_state as IActionable<TEvent>)?.OnAction(trigger, data);
			}

			#endregion

		}

		#region Fields

		private readonly Action<StateBase<TStateId>>
			_beforeOnEnter,
			_afterOnEnter,
			_beforeOnLogic,
			_afterOnLogic,
			_beforeOnExit,
			_afterOnExit;

		#endregion

		#region Init

		/// <summary>
		/// Initialises a new instance of the StateWrapper class
		/// </summary>
		public StateWrapper(
				Action<StateBase<TStateId>> beforeOnEnter = null,
				Action<StateBase<TStateId>> afterOnEnter = null,
				Action<StateBase<TStateId>> beforeOnLogic = null,
				Action<StateBase<TStateId>> afterOnLogic = null,
				Action<StateBase<TStateId>> beforeOnExit = null,
				Action<StateBase<TStateId>> afterOnExit = null)
		{
			_beforeOnEnter = beforeOnEnter;
			_afterOnEnter = afterOnEnter;
			_beforeOnLogic = beforeOnLogic;
			_afterOnLogic = afterOnLogic;
			_beforeOnExit = beforeOnExit;
			_afterOnExit = afterOnExit;
		}

		#endregion

		#region Wrap

		public WrappedState Wrap(StateBase<TStateId> state)
		{
			return new WrappedState(
				state,
				_beforeOnEnter,
				_afterOnEnter,
				_beforeOnLogic,
				_afterOnLogic,
				_beforeOnExit,
				_afterOnExit
			);
		}

		#endregion

	}

	public class StateWrapper : StateWrapper<string, string>
	{
		public StateWrapper(
			Action<StateBase<string>> beforeOnEnter = null,
			Action<StateBase<string>> afterOnEnter = null,

			Action<StateBase<string>> beforeOnLogic = null,
			Action<StateBase<string>> afterOnLogic = null,

			Action<StateBase<string>> beforeOnExit = null,
			Action<StateBase<string>> afterOnExit = null) : base(
			beforeOnEnter, afterOnEnter,
			beforeOnLogic, afterOnLogic,
			beforeOnExit, afterOnExit)
		{
		}
	}
}
