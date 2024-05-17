using System.Collections.Generic;
using HFSM;
using UnityEngine;

[DefaultExecutionOrder(-9999)]
public class MonoFlypeMachine : FlypeMachineController<MonoBehaviour>
{

	#region Fields

	[Header("MonoFlypeMachine")]

	[SerializeField]
	protected List<MonoBehaviour> flags = default;

	[Header("Prefabs")]
	[SerializeField]
	private Transform _transformParentForPrefabs = default;

	[SerializeField]
	protected List<MonoBehaviour> flagPrefab = default;

	#endregion

	#region CreateTypeMachine

	protected override void CreateFlypeMachine()
	{
		base.CreateFlypeMachine();

		foreach (var flag in flags)
		{
			if (flag != null)
			{
				if (!startFlags.Contains(flag))
				{
					flag.gameObject.SetActive(false);
				}

				FlypeMachine.AddFlag(
					flag,
					enterAction: (P) =>
					{
						if (flag != null)
						{
							flag.gameObject.SetActive(true);
						}
					},
					exitAction: (P) =>
					{
						if (flag != null)
						{
							flag.gameObject.SetActive(false);
						}
					});
			}
		}

		foreach (var flag in flagPrefab)
		{
			if (flag == null)
			{
				continue;
			}

			var prefab = flag;
			MonoBehaviour instance = null;

			FlypeMachine.AddFlag(flag,
				enterAction: (P) =>
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
				exitAction: (P) =>
				{
					if (instance != null)
					{
						Destroy(instance.gameObject);
					}
				}
			);
		}
	}

	#endregion

	#region DebugLog

#if UNITY_EDITOR || DEVELOPMENT_BUILD

	[Header("Debug")]
	[SerializeField]
	private bool _autoSetActiveFlags = false;
	[SerializeField]
	private bool _startFlagActiveValueToSet = true;
	[SerializeField]
	private bool _flagActiveValueToSet = false;

	protected virtual void OnValidate()
	{
		if (_autoSetActiveFlags)
		{
			_autoSetActiveFlags = false;

			foreach (var flag in flags)
			{
				if (flag != null)
				{
					if (startFlags.Contains(flag))
					{
						flag.gameObject.SetActive(_startFlagActiveValueToSet);
					}
					else
					{
						flag.gameObject.SetActive(_flagActiveValueToSet);
					}
				}
			}
		}
	}

#endif

	#endregion DebugLog

}
