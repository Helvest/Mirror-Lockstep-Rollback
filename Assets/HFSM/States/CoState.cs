using System.Collections;
using UnityEngine;
using System;

namespace HFSM
{
	/// <summary>
	/// A state that can run a Unity coroutine as its OnLogic method
	/// </summary>
	public class CoState<TStateId, TEvent> : ActionState<TStateId, TEvent>
	{

		#region Fields

		private readonly MonoBehaviour _mono;

		private readonly Action<CoState<TStateId, TEvent>> _onEnter;
		private readonly Func<CoState<TStateId, TEvent>, IEnumerator> _onLogic;
		private readonly Action<CoState<TStateId, TEvent>> _onExit;
		private readonly Func<CoState<TStateId, TEvent>, bool> _canExit;

		private Coroutine _coroutine;

		#endregion

		#region Init

		/// <summary>
		/// Initialises a new instance of the CoState class
		/// </summary>
		/// <param name="mono">The MonoBehaviour of the script that should run the coroutine.</param>
		/// <param name="onEnter">A function that is called when the state machine enters this state</param>
		/// <param name="onLogic">A coroutine that is run while this state is active
		/// 	It runs independently from the parent state machine's OnLogic(), because it is handled by Unity.
		/// 	It is run again once it has completed.
		/// 	It is terminated when the state exits.</param>
		/// <param name="onExit">A function that is called when the state machine exits this state</param>
		/// <param name="canExit">Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, later in OnLogic() it should call fsm.StateCanExit().</param>
		public CoState(
				MonoBehaviour mono,
				Action<CoState<TStateId, TEvent>> onEnter = null,
				Func<CoState<TStateId, TEvent>, IEnumerator> onLogic = null,
				Action<CoState<TStateId, TEvent>> onExit = null,
				Func<CoState<TStateId, TEvent>, bool> canExit = null) : base()
		{
			_mono = mono;
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
			_coroutine = null;
		}

		protected virtual IEnumerator LoopCoroutine()
		{
			var routine = _onLogic(this);

			while (true)
			{
				// This checks if the routine needs at least one frame to execute.
				// If not, LoopCoroutine will wait 1 frame to avoid an infinite
				// loop which will crash Unity
				yield return routine.MoveNext() ? routine.Current : null;

				// Iterate from the onLogic coroutine until it is depleted
				while (routine.MoveNext())
				{
					yield return routine.Current;
				}

				// Restart the onLogic coroutine
				routine = _onLogic(this);
			}
		}

		public override void OnLogic()
		{
			if (_coroutine == null && _onLogic != null)
			{
				_coroutine = _mono.StartCoroutine(LoopCoroutine());
			}
		}

		public override void OnExit()
		{
			if (_coroutine != null)
			{
				_mono.StopCoroutine(_coroutine);
				_coroutine = null;
			}

			_onExit?.Invoke(this);
		}

		public override bool OnExitRequest()
		{
			return canExit || (_canExit != null && _canExit(this));
		}

		#endregion

	}

	public class CoState<TStateId> : CoState<TStateId, string>
	{
		public CoState(
			MonoBehaviour mono,
			Action<CoState<TStateId, string>> onEnter = null,
			Func<CoState<TStateId, string>, IEnumerator> onLogic = null,
			Action<CoState<TStateId, string>> onExit = null,
			Func<CoState<TStateId, string>, bool> canExit = null)
			: base(mono, onEnter, onLogic, onExit, canExit)
		{
		}
	}

	public class CoState : CoState<string, string>
	{
		public CoState(
			MonoBehaviour mono,
			Action<CoState<string, string>> onEnter = null,
			Func<CoState<string, string>, IEnumerator> onLogic = null,
			Action<CoState<string, string>> onExit = null,
			Func<CoState<string, string>, bool> canExit = null)
			: base(mono, onEnter, onLogic, onExit, canExit)
		{
		}
	}
}
