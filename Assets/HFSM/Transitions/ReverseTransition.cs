namespace HFSM
{
	/// <summary>
	/// A ReverseTransition wraps another transition, but reverses it. The "from"
	/// and "to" states are swapped. Only when the condition of the wrapped transition
	/// is false does it transition.
	/// </summary>
	public class ReverseTransition<TStateId> : TransitionBase<TStateId>
	{

		#region Fields

		public TransitionBase<TStateId> wrappedTransition;
		private readonly bool _shouldInitWrappedTransition;

		#endregion

		#region Init

		public ReverseTransition(
				TransitionBase<TStateId> wrappedTransition,
				bool shouldInitWrappedTransition = true)
			: base(
				from: wrappedTransition.to,
				to: wrappedTransition.from,
				forceInstantly: wrappedTransition.forceInstantly)
		{
			this.wrappedTransition = wrappedTransition;
			_shouldInitWrappedTransition = shouldInitWrappedTransition;
		}

		public override void Init()
		{
			if (_shouldInitWrappedTransition)
			{
				wrappedTransition.Init();
			}
		}

		#endregion

		#region TransitionMaethods

		public override void OnEnter()
		{
			wrappedTransition.OnEnter();
		}

		public override bool ShouldTransition()
		{
			return ! wrappedTransition.ShouldTransition();
		}

		#endregion

	}

	public class ReverseTransition : ReverseTransition<string>
	{
		public ReverseTransition(
			TransitionBase<string> wrappedTransition,
			bool shouldInitWrappedTransition = true)
			: base(wrappedTransition, shouldInitWrappedTransition)
		{
		}
	}

}