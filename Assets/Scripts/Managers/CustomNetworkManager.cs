using Mirror;
using Helvest.Framework;
using GameMode;

public class CustomNetworkManager : RollbackNetworkManager
{

	#region Variables

	private GameManager _gameManager;

	#endregion

	#region Awake

	public override void Awake()
	{
		SL.TryGetOrFindComponent(out _gameManager);

		base.Awake();
	}

	#endregion

	#region OnEnable OnDisable

	private void OnEnable()
	{
		SL.AddOrDestroy(this);
	}

	private void OnDisable()
	{
		SL.Remove(this);
	}

	#endregion

	#region Net Start

	public override void OnStartServer()
	{
		base.OnStartServer();

		_gameManager.SetState<Match>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();

		_gameManager.SetState<Match>();
	}

	#endregion

	#region Net Stop

	public override void OnStopServer()
	{
		base.OnStopServer();

		_gameManager.SetState<Menu>();
	}

	public override void OnStopClient()
	{
		base.OnStopClient();

		_gameManager.SetState<Menu>();
	}

	#endregion

}
