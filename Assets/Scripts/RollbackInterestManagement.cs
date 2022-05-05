using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RollbackInterestManagement : InterestManagement
{
	[SerializeField]
	private bool _useDebug = false;

	//Call when player added
	public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient newObserver)
	{
		if (_useDebug)
		{
			Debug.Log("OnCheckObserver: " + identity.name + " | useRollback:" + identity.useRollback + " | rollbackState: " + newObserver.rollbackState);
		}

		return !identity.useRollback || newObserver.rollbackState != RollbackState.NotObserving;
	}

	//Call when new object is pawned
	public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers)
	{
		if (identity.useRollback)
		{
			foreach (var conn in NetworkServer.connections.Values)
			{
				if (conn == null || !conn.isAuthenticated || conn.identity == null || conn.rollbackState == RollbackState.NotObserving)
				{
					continue;
				}

				if (_useDebug)
				{
					Debug.Log("OnRebuildObservers Add: " + identity.name + " to observer " + conn.connectionId + " | useRollback:" + identity.useRollback);
				}

				newObservers.Add(conn);
			}
		}
		else
		{
			foreach (var conn in NetworkServer.connections.Values)
			{
				if (conn != null && conn.isAuthenticated && conn.identity != null)
				{
					newObservers.Add(conn);
				}
			}
		}
	}

}
