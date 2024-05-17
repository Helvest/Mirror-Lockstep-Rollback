using System.Collections.Generic;
using UnityEngine;

namespace HFSM
{
	public abstract class FlagMachineController<T> : MonoBehaviour, IHoldFlagMachine<T>
	{

		#region Fields

		[Header("StateMachineController")]

		[SerializeField]
		protected bool apceptFlagNotIncluded = false;

		[SerializeField]
		protected bool canReenterSameFlag = false;

		[SerializeField]
		protected List<T> startFlags = default;

		public FlagMachine<T> FlagMachine { get; private set; } = default;

		protected bool hasStarted = false;

		#endregion

		#region Init

		protected virtual void Awake()
		{
			CreateFlagMachine();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			FlagMachine.useDebug = useDebug;
#endif
		}

		protected virtual void Start()
		{
			hasStarted = true;
			ToStartState();
		}

		protected virtual void CreateFlagMachine()
		{
			FlagMachine = new FlagMachine<T>(apceptFlagNotIncluded, canReenterSameFlag);
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

			this.SetFlags(startFlags);
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
