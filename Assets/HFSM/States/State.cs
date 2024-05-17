using System;

namespace HFSM
{
	/// <summary>
	/// The "normal" state class that can run code on Enter, on Logic and on Exit,
	/// while also handling the timing of the next state transition
	/// </summary>
	public class State<TStateId, TEvent> : ActionState<TStateId, TEvent>
	{

		#region Fields

		private readonly Action<State<TStateId, TEvent>> _onEnter;
		private readonly Action<State<TStateId, TEvent>> _onLogic;
		private readonly Action<State<TStateId, TEvent>> _onExit;
		private readonly Func<State<TStateId, TEvent>, bool> _canExit;

		#endregion

		#region Init

		/// <summary>
		/// Initialises a new instance of the State class
		/// </summary>
		/// <param name="onEnter">A function that is called when the state machine enters this state</param>
		/// <param name="onLogic">A function that is called by the logic function of the state machine if this state is active</param>
		/// <param name="onExit">A function that is called when the state machine exits this state</param>
		/// <param name="canExit">(Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, later in OnLogic() it should call fsm.StateCanExit()</param>
		public State(
				Action<State<TStateId, TEvent>> onEnter = null,
				Action<State<TStateId, TEvent>> onLogic = null,
				Action<State<TStateId, TEvent>> onExit = null,
				Func<State<TStateId, TEvent>, bool> canExit = null) : base()
		{
			_onEnter = onEnter;
			_onLogic = onLogic;
			_onExit = onExit;
			_canExit = canExit;
		}

		#endregion

		#region StateMethods

		public override void OnEnter()
		{
			_onEnter?.Invoke(this);
		}

		public override void OnLogic()
		{
			_onLogic?.Invoke(this);
		}

		public override bool OnExitRequest()
		{
			return canExit || (_canExit != null && _canExit(this));
		}

		public override void OnExit()
		{
			_onExit?.Invoke(this);
		}

		#endregion

	}

	public class State<TStateId> : State<TStateId, string>
	{
		public State(
				Action<State<TStateId, string>> onEnter = null,
				Action<State<TStateId, string>> onLogic = null,
				Action<State<TStateId, string>> onExit = null,
				Func<State<TStateId, string>, bool> canExit = null)
			: base(onEnter, onLogic, onExit, canExit)
		{
		}
	}

	public class State : State<string, string>
	{
		public State(
				Action<State<string, string>> onEnter = null,
				Action<State<string, string>> onLogic = null,
				Action<State<string, string>> onExit = null,
				Func<State<string, string>, bool> canExit = null)
			: base(onEnter, onLogic, onExit, canExit)
		{
		}
	}
}
