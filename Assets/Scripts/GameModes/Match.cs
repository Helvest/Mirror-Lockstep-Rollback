using Mirror;
using UnityEngine;

public class Match : MonoBehaviour
{
	protected NetworkManager networkManager;
	protected MatchManager matchManager;

	[SerializeField]
	private MonoServiceLocator _serverService;
	[SerializeField]
	private MonoServiceLocator _clientService;

	private bool _isServer;

	private bool _isClient;

	private void OnEnable()
	{
		SL.TryGetIfNull(ref networkManager);

		_isServer = networkManager.mode == NetworkManagerMode.Host || networkManager.mode == NetworkManagerMode.ServerOnly;
		_isClient = networkManager.mode == NetworkManagerMode.Host || networkManager.mode == NetworkManagerMode.ClientOnly;

		if (_isServer)
		{
			_serverService.gameObject.SetActive(true);

			if (_serverService.TryGet(out matchManager))
			{
				NetworkServer.Spawn(matchManager.gameObject);
			}
		}

		if (_isClient)
		{
			_clientService.gameObject.SetActive(true);
		}
	}

	private void OnDisable()
	{
		_clientService.gameObject.SetActive(false);
		_serverService.gameObject.SetActive(false);
	}

}
