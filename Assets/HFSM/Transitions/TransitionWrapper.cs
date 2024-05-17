using System;

namespace HFSM
{
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// </summary>
	public class TransitionWrapper<TStateId>
	{

		#region WrappedTransition Class

		public class WrappedTransition : TransitionBase<TStateId>
		{

			#region Fields

			private readonly Action<TransitionBase<TStateId>>
				_beforeOnEnter,
				_afterOnEnter,
				_beforeShouldTransition,
				_afterShouldTransition;

			private readonly TransitionBase<TStateId> _transition;

			#endregion

			#region Init

			public WrappedTransition(
					TransitionBase<TStateId> transition,

					Action<TransitionBase<TStateId>> beforeOnEnter = null,
					Action<TransitionBase<TStateId>> afterOnEnter = null,

					Action<TransitionBase<TStateId>> beforeShouldTransition = null,
					Action<TransitionBase<TStateId>> afterShouldTransition = null) : base(
					transition.from, transition.to, forceInstantly: transition.forceInstantly)
			{
				_transition = transition;

				_beforeOnEnter = beforeOnEnter;
				_afterOnEnter = afterOnEnter;

				_beforeShouldTransition = beforeShouldTransition;
				_afterShouldTransition = afterShouldTransition;
			}

			#endregion

			#region TransitionMethods

			public override void OnEnter()
			{
				_beforeOnEnter?.Invoke(_transition);
				_transition.OnEnter();
				_afterOnEnter?.Invoke(_transition);
			}

			public override bool ShouldTransition()
			{
				_beforeShouldTransition?.Invoke(_transition);
				bool shouldTransition = _transition.ShouldTransition();
				_afterShouldTransition?.Invoke(_transition);
				return shouldTransition;
			}

			#endregion

		}

		#endregion

		#region Fields

		private readonly Action<TransitionBase<TStateId>>
			_beforeOnEnter,
			_afterOnEnter,
			_beforeShouldTransition,
			_afterShouldTransition;

		#endregion

		#region Init

		public TransitionWrapper(
			Action<TransitionBase<TStateId>> beforeOnEnter = null,
			Action<TransitionBase<TStateId>> afterOnEnter = null,

			Action<TransitionBase<TStateId>> beforeShouldTransition = null,
			Action<TransitionBase<TStateId>> afterShouldTransition = null)
		{
			_beforeOnEnter = beforeOnEnter;
			_afterOnEnter = afterOnEnter;
			_beforeShouldTransition = beforeShouldTransition;
			_afterShouldTransition = afterShouldTransition;
		}

		#endregion

		#region Wrap

		public WrappedTransition Wrap(TransitionBase<TStateId> transition)
		{
			return new WrappedTransition(
				transition,
				_beforeOnEnter,
				_afterOnEnter,
				_beforeShouldTransition,
				_afterShouldTransition
			);
		}

		#endregion

	}

	public class TransitionWrapper : TransitionWrapper<string>
	{
		public TransitionWrapper(
			Action<TransitionBase<string>> beforeOnEnter = null,
			Action<TransitionBase<string>> afterOnEnter = null,
			Action<TransitionBase<string>> beforeShouldTransition = null,
			Action<TransitionBase<string>> afterShouldTransition = null)
			: base(beforeOnEnter, afterOnEnter, beforeShouldTransition, afterShouldTransition)
		{
		}
	}

}
