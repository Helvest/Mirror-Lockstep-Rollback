using UnityEngine;
using Mirror;

public class NetworkPlayer : NetworkBehaviour
{
	public override void OnStartClient()
	{
		RollbackIsReady = true;
	}

	[SyncVar(hook = nameof(SetRollbackState))]
	public bool RollbackIsReady = false;

    public void SetRollbackState(bool _, bool newValue)
	{
		if (isServer)
		{
			if (newValue)
			{
				if(connectionToClient.rollbackState == RollbackState.NotObserving)
				{				
					connectionToClient.rollbackState = RollbackState.ReadyToObserve;

					Rollback.AddPlayerReadyForRollback(connectionToClient);
				}
			}
			else
			{
				connectionToClient.rollbackState = RollbackState.NotObserving;
			}

		}
	}

	private void Update()
	{
		if (isLocalPlayer)
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				RollbackIsReady = !RollbackIsReady;
			}
		}
	}
}
