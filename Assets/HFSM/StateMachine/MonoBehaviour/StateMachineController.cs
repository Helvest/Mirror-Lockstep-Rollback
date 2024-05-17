using System;
using UnityEngine;

namespace HFSM
{
	public abstract class StateMachineController<TStateId> : MonoBehaviour, IHoldStateMachine<TStateId>
	{

		#region Fields

		[Header("StateMachineController")]

		[SerializeField]
		protected bool apceptValuesNotIncluded = false;

		[SerializeField]
		protected bool canReenterSameState = false;

		[SerializeField]
		protected TStateId startState = default;

		public StateMachine<TStateId> StateMachine { get; private set; }

		protected bool hasStarted = false;

		#endregion

		#region Init

		protected virtual void Awake()
		{
			CreateTypeMachine();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			StateMachine.useDebug = useDebug;
#endif
		}

		protected virtual void Start()
		{
			hasStarted = true;
			ToStartState();
		}

		protected virtual void CreateTypeMachine()
		{
			StateMachine = new StateMachine<TStateId>();
		}

		protected virtual void OnEnable()
		{
			ToStartState();
		}

		protected virtual void ToStartState()
		{
			if (!hasStarted || startState == null)
			{
				return;
			}

			this.SetState(startState);
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
