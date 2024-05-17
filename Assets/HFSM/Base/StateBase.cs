
namespace HFSM
{
	/// <summary>
	/// The base class of all states
	/// </summary>
	public class StateBase<TStateId>
	{

		#region Fields

		public bool canExit = true;
		public TStateId name;

		#endregion

		#region Init

		/// <summary>
		/// Initialises a new instance of the BaseState class
		/// </summary>
		public StateBase() { }


		/// <summary>
		/// Called to initialise the state, after values like name, mono and fsm have been set
		/// </summary>
		public virtual void Init() { }

		#endregion

		#region StateMethods

		/// <summary>
		/// Called when the state machine transitions to this state (enters this state)
		/// </summary>
		public virtual void OnEnter() { }

		/// <summary>
		/// Called while this state is active
		/// </summary>
		public virtual void OnLogic() { }

		/// <summary>
		/// Called when the state machine transitions from this state to another state (exits this state)
		/// </summary>
		public virtual void OnExit() { }

		/// <summary>
		/// (Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, it should call fsm.StateCanExit() later in OnLogic().
		/// </summary>
		public virtual bool OnExitRequest() => !canExit;

		#endregion

	}

	public class StateBase : StateBase<string> { }

}
