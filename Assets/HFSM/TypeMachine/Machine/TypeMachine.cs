using System;

namespace HFSM
{

	[Serializable]
	public class TypeMachine<TOwnId, TEvent> : StateMachine<TOwnId, Type, TEvent>
	{

		#region Constructor

		public TypeMachine() : base() { }

		public TypeMachine(Type startState) : base(startState) { }

		#endregion

		#region Is State

		public bool IsState<t>()
		{
			return base.IsState(typeof(t));
		}

		public bool IsState<t>(t instance)
		{
			return base.IsState(instance.GetType());
		}

		#endregion

		#region Set State

		public void SetState<t>(bool forceInstantly = false)
		{
			RequestStateChange(typeof(t), forceInstantly);
		}

		public void SetState<t>(t instance, bool forceInstantly = false)
		{
			RequestStateChange(instance.GetType(), forceInstantly);
		}

		#endregion

		#region Add State

		public void AddState<t>(StateBase<Type> state)
		{
			base.AddState(typeof(t), state);
		}

		public void AddState<t>(t instance, StateBase<Type> state)
		{
			base.AddState(instance.GetType(), state);
		}

		#endregion

		#region Remove State

		public void RemoveState<t>()
		{
			base.RemoveState(typeof(t));
		}

		public void RemoveState<t>(t instance)
		{
			base.RemoveState(instance.GetType());
		}

		#endregion

	}

	public class TypeMachine<TEvent> : TypeMachine<Type, TEvent>
	{

		#region Constructor

		public TypeMachine() : base() { }

		public TypeMachine(Type startState) : base(startState) { }

		#endregion

	}

	public class TypeMachine : TypeMachine<string>
	{

		#region Constructor

		public TypeMachine() : base() { }

		public TypeMachine(Type startState) : base(startState) { }

		#endregion

	}

}
