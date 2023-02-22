﻿using System.Collections.Generic;
using Mirror;
using TickPhysics;

public interface INetTickSystem : ITickSystem
{

	#region Methods

	void Clear();

	void OnRollbackData(RollbackData rollbackData);

	void ServerUpdate();

	#endregion

	#region Lockstep Messages

	void SendConfigLockstepMessage<T1>(IEnumerable<T1> connList, bool isFirst) where T1 : NetworkConnection;
	void SendDeltaLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnection;
	void SendFullLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnection;
	void OnFinishSendLockstepMessage();

	#endregion

}