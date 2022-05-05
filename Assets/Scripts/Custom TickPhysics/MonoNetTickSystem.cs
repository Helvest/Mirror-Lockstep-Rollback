using System.Collections.Generic;
using Mirror;
using TickPhysics;

public abstract class MonoNetTickSystem : MonoTickSystem, INetTickSystem
{

	#region Fields

	protected override ITickSystem TickSystem => NetTickSystem;

	protected abstract INetTickSystem NetTickSystem { get; }

	#endregion

	#region Init

	protected override void OnEnable()
	{
		//Clear();

		base.OnEnable();

		Rollback.EventOnLockstepReceive += NetTickSystem.OnRollbackData;
	}

	protected virtual void OnDisable()
	{
		Rollback.EventOnLockstepReceive -= NetTickSystem.OnRollbackData;
	}

	#endregion

	#region Clear

	public void Clear()
	{
		NetTickSystem.Clear();
	}

	#endregion

	#region RollbackData

	public void OnRollbackData(RollbackData rollbackData)
	{
		NetTickSystem.OnRollbackData(rollbackData);
	}

	#endregion

	#region Lockstep Messages

	public void SendConfigLockstepMessage<T1>(IEnumerable<T1> connList, bool isFirst) where T1 : NetworkConnectionToClient
	{
		NetTickSystem.SendConfigLockstepMessage(connList, isFirst);
	}

	public void SendDeltaLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnectionToClient
	{
		NetTickSystem.SendDeltaLockstepMessage(connList);
	}

	public void SendFullLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnectionToClient
	{
		NetTickSystem.SendFullLockstepMessage(connList);
	}

	public void OnFinishSendLockstepMessage()
	{
		NetTickSystem.OnFinishSendLockstepMessage();
	}

	#endregion

	#region ServerUpdate

	public void ServerUpdate()
	{
		NetTickSystem.ServerUpdate();
	}

	#endregion

}
