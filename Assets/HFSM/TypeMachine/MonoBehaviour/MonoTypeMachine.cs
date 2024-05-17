using System;
using System.Collections.Generic;
using UnityEngine;

namespace HFSM
{
	[DefaultExecutionOrder(-9999)]
	public class MonoTypeMachine : TypeMachineController<MonoBehaviour>
	{

		#region Fields

		[Header("MonoTypeMachine")]

		[SerializeField]
		protected List<MonoBehaviour> states = default;

		[Header("Prefabs")]
		[SerializeField]
		private Transform _transformParentForPrefabs = default;

		[SerializeField]
		protected List<MonoBehaviour> statesPrefab = default;

		#endregion

		#region CreateTypeMachine

		protected override void CreateTypeMachine()
		{
			base.CreateTypeMachine();

			foreach (var state in states)
			{
				if (state != null)
				{
					state.gameObject.SetActive(false);

					TypeMachine.AddState(
						state, new State<Type>(
						onEnter: (P) => state.gameObject.SetActive(true),
						onExit: (P) => state.gameObject.SetActive(false)
					));
				}
			}

			foreach (var stateId in statesPrefab)
			{
				if (stateId == null)
				{
					continue;
				}

				var prefab = stateId;
				MonoBehaviour instance = null;

				TypeMachine.AddState(stateId,
					onEnter: (_) =>
					{
						if (instance != null)
						{
							instance.gameObject.SetActive(true);
						}
						else if (prefab != null)
						{
							instance = Instantiate(prefab, _transformParentForPrefabs);
						}
					},
					onExit: (_) =>
					{
						if (instance == null)
						{
							return;
						}

						Destroy(instance.gameObject);
					}
				);
				;
			}
		}

		#endregion

	}
}
