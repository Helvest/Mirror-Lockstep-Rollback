using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MonoNetTickSystem3D : AbstractMonoNetTickSystem3D<NetTickSystem3D> { }

public abstract class AbstractMonoNetTickSystem3D<T> : AbstractMonoTickSystem3D<T>, INetTickSystem where T : TickSystem3D, INetTickSystem
{

	#region Init

	protected override void OnEnable()
	{
		base.OnEnable();
		Rollback.EventOnLockstepReceive += TickSystem.OnLockstepReceive;
		Rollback.EventOnRollbackResolve += TickSystem.OnRollbackResolve;
		Clear();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		Rollback.EventOnLockstepReceive -= TickSystem.OnLockstepReceive;
		Rollback.EventOnRollbackResolve -= TickSystem.OnRollbackResolve;
	}

	#endregion

	#region Clear

	public void Clear()
	{
		TickSystem.Clear();
	}

	#endregion

	#region OnEvents

	public void OnLockstepReceive(RollbackData rollbackData)
	{
		TickSystem.OnLockstepReceive(rollbackData);
	}

	public void OnRollbackResolve(RollbackData rollbackData)
	{
		TickSystem.OnRollbackResolve(rollbackData);
	}

	#endregion

	#region Lockstep Messages

	public void SendConfigLockstepMessage<T1>(IEnumerable<T1> connList, bool isFirst) where T1 : NetworkConnection
	{
		TickSystem.SendConfigLockstepMessage(connList, isFirst);
	}

	#endregion

	#region ServerUpdate

	public void ServerUpdate()
	{
		TickSystem.ServerUpdate();
	}

	#endregion


	protected override void FixedUpdate()
	{
		if (!_wasUpdated)
		{
			if (AutoUpdate == SimulationMode.FixedUpdate)
			{
				_wasUpdated = true;

				var deltaTime = NetworkTime.time - previousTime;

				if (simulationMode == SimulationMode.FixedUpdate)
				{
					TickSystem.Tick(NetworkTime.time, deltaTime, Time.fixedDeltaTime);
				}
				else if (simulationMode == SimulationMode.Update)
				{
					TickSystem.Tick(NetworkTime.time, deltaTime, deltaTime);
				}
				else
				{
					TickSystem.Tick(NetworkTime.time, deltaTime, overrideFixedDeltaTime);
				}
			}

			previousTime = NetworkTime.time;
		}
	}

	protected override void Update()
	{
		if (AutoUpdate == SimulationMode.Update && !_wasUpdated)
		{
			if (simulationMode == SimulationMode.FixedUpdate)
			{
				TickSystem.Tick(NetworkTime.time, Time.deltaTime, Time.fixedDeltaTime);
			}
			else if (simulationMode == SimulationMode.Update)
			{
				TickSystem.Tick(NetworkTime.time, Time.deltaTime, Time.deltaTime);
			}
			else
			{
				TickSystem.Tick(NetworkTime.time, Time.deltaTime, overrideFixedDeltaTime);
			}
		}

		_wasUpdated = false;
	}

	protected override void CustomUpdate()
	{
		if (AutoUpdate != SimulationMode.Script || _wasUpdated)
		{
			return;
		}

		if (simulationMode == SimulationMode.FixedUpdate)
		{
			TickSystem.Tick(NetworkTime.time, Time.deltaTime, Time.fixedDeltaTime);
		}
		else if (simulationMode == SimulationMode.Update)
		{
			TickSystem.Tick(NetworkTime.time, Time.deltaTime, Time.deltaTime);
		}
		else
		{
			TickSystem.Tick(NetworkTime.time, Time.deltaTime, overrideFixedDeltaTime);
		}
	}

}
