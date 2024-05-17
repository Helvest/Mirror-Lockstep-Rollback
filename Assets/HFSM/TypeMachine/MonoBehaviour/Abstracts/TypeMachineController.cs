using System;
using UnityEngine;

namespace HFSM
{
	[DefaultExecutionOrder(-9999)]
	public abstract class TypeMachineController<T> : MonoBehaviour, IHoldTypeMachine where T : class
	{

		#region Fields

		[Header("TypeMachineController")]

		[SerializeField]
		protected T startState = default;

		public TypeMachine TypeMachine { get; private set; } = default;

		protected bool hasStarted = false;

		#endregion

		#region State

		public virtual Type State
		{
			get => TypeMachine.State;

			set => TypeMachine.State = value;
		}

		#endregion

		#region Init

		protected virtual void Awake()
		{
			CreateTypeMachine();
		}

		protected virtual void Start()
		{
			hasStarted = true;
			ToStartState();
		}

		protected virtual void CreateTypeMachine()
		{
			TypeMachine = new TypeMachine(startState.GetType());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			TypeMachine.useDebug = useDebug;
#endif
		}

		protected virtual void OnEnable()
		{
			ToStartState();
		}

		protected virtual void ToStartState()
		{
			if (!hasStarted)
			{
				return;
			}

			TypeMachine.OnEnter();
		}

		#endregion

		#region Debug

#if UNITY_EDITOR || DEVELOPMENT_BUILD

		[Header("Debug")]
		[SerializeField]
		protected bool useDebug = false;

#endif

		#endregion

	}
}
