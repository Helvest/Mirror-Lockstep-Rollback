using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
	public override void OnStartServer()
	{
		if (rollbackIsReady)
		{
			RollbackNetworkManager.singleton.ChangeRollbackState(connectionToClient, rollbackIsReady);
		}
	}

	public bool rollbackIsReady = true;

	[Command]
	private void SwitchClientRollbackState()
	{
		rollbackIsReady = !rollbackIsReady;
		RollbackNetworkManager.singleton.ChangeRollbackState(connectionToClient, rollbackIsReady);
	}

	private void Update()
	{
		if (isLocalPlayer)
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				SwitchClientRollbackState();
			}
		}
	}
}
