using HFSM;

public interface IHoldStateMachine<T>
{
	public StateMachine<T> StateMachine { get; }
}
