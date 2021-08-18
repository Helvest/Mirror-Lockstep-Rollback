using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomInterestManagement : InterestManagement
{
	[SerializeField]
	private bool _useDebug = false;

	//Call when player added
	public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnection newObserver)
	{
		if (_useDebug)
		{
			Debug.Log("OnCheckObserver: " + identity.name);
		}

		if (!identity.useRollback)
		{
			return true;
		}

		if (newObserver.rollbackState != RollbackState.NotObserving)
		{
			return true;
		}

		return false;
	}

	//Call when new object is pawned
	public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnection> newObservers, bool initialize)
	{
		if (_useDebug)
		{
			Debug.Log("OnRebuildObservers: " + identity.name);
		}

		if (identity.useRollback)
		{
			foreach (var conn in NetworkServer.connections.Values)
			{
				if (conn != null && conn.isAuthenticated && conn.identity != null)
				{
					if(conn.rollbackState != RollbackState.NotObserving)
					{
						Debug.Log("Add " + identity.name + " to observer " + conn.connectionId);
						newObservers.Add(conn);
					}
				}
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
