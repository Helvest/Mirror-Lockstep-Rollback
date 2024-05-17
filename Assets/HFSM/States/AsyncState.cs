using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HFSM
{

	/// <summary>
	/// A state that can run an asynchronous loop as its OnLogic method
	/// </summary>
	public class AsyncState<TStateId, TEvent> : ActionState<TStateId, TEvent>
	{

		#region Fields

		private readonly Action<AsyncState<TStateId, TEvent>> _onEnter;
		private readonly Func<AsyncState<TStateId, TEvent>, Task> _onLogicAsync;
		private readonly Action<AsyncState<TStateId, TEvent>> _onExit;
		private readonly Func<AsyncState<TStateId, TEvent>, bool> _canExit;

		private Task _task;
		private CancellationTokenSource _cancellationTokenSource;

		#endregion

		#region Init

		/// <summary>
		/// Initializes a new instance of the AsyncState class
		/// </summary>
		/// <param name="onEnter">An asynchronous function that is called when the state machine enters this state</param>
		/// <param name="onLogicAsync">An asynchronous loop that is run while this state is active</param>
		/// <param name="onExit">An asynchronous function that is called when the state machine exits this state</param>
		/// <param name="canExit">A function that determines if the state is allowed to instantly
		/// exit on a transition (false), or if the state machine should wait until the state is ready for a
		/// state change (true)</param>
		public AsyncState(
			Action<AsyncState<TStateId, TEvent>> onEnter = null,
			Func<AsyncState<TStateId, TEvent>, Task> onLogicAsync = null,
			Action<AsyncState<TStateId, TEvent>> onExit = null,
			Func<AsyncState<TStateId, TEvent>, bool> canExit = null) : base()
		{
			_onEnter = onEnter;
			_onLogicAsync = onLogicAsync;
			_onExit = onExit;
			_canExit = canExit;
		}

		#endregion

		#region StateMethods

		public override void OnEnter()
		{
			StopLoopAsync();
			_onEnter?.Invoke(this);
		}

		public override void OnLogic()
		{
			if (_cancellationTokenSource == null || !IsLoopRunning)
			{
				_cancellationTokenSource = new CancellationTokenSource();
				_task = LoopAsync(_cancellationTokenSource.Token);
			}
		}

		public override void OnExit()
		{
			StopLoopAsync();
			_onExit?.Invoke(this);
		}

		public override bool OnExitRequest()
		{
			return canExit || (_canExit != null && _canExit(this));
		}

		#endregion

		#region LoopAsync

		protected virtual async Task LoopAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_onLogicAsync != null)
				{
					await _onLogicAsync(this);
				}
			}
		}

		protected virtual void StopLoopAsync()
		{
			if (_cancellationTokenSource != null && IsLoopRunning)
			{
				_cancellationTokenSource.Cancel();
				_cancellationTokenSource.Dispose();
				_cancellationTokenSource = null;
				_task = null;
			}
		}

		private bool IsLoopRunning => _task != null && !_task.IsCompleted && !_task.IsCanceled && !_task.IsFaulted;

		#endregion

	}

	public class AsyncState<TStateId> : AsyncState<TStateId, string>
	{
		public AsyncState(
			MonoBehaviour mono,
			Action<AsyncState<TStateId, string>> onEnter = null,
			Func<AsyncState<TStateId, string>, Task> onLogic = null,
			Action<AsyncState<TStateId, string>> onExit = null,
			Func<AsyncState<TStateId, string>, bool> canExit = null)
			: base(onEnter, onLogic, onExit, canExit)
		{
		}
	}

	public class AsyncState : AsyncState<string, string>
	{
		public AsyncState(
			MonoBehaviour mono,
			Action<AsyncState<string, string>> onEnter = null,
			Func<AsyncState<string, string>, Task> onLogic = null,
			Action<AsyncState<string, string>> onExit = null,
			Func<AsyncState<string, string>, bool> canExit = null)
			: base(onEnter, onLogic, onExit, canExit)
		{
		}
	}

}
