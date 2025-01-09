using HFSM;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : RollbackNetworkManager
{

	#region Fields

	[field: SerializeField, Header("Custom")]
	protected MonoTypeMachine TM { get; private set; }

	#endregion

	#region Net Start

	public override void OnStartServer()
	{
		base.OnStartServer();

		TM.SetState<Match>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();

		TM.SetState<Match>();
	}

	#endregion

	#region Net Stop

	public override void OnStopServer()
	{
		base.OnStopServer();

		TM.SetState<Menu>();
	}

	public override void OnStopClient()
	{
		base.OnStopClient();

		TM.SetState<Menu>();
	}

	#endregion

}
