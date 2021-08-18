using Mirror;
using UnityEngine;
using Helvest.Framework;

namespace GameMode
{
	public class Match : GameModeBase
	{
		[SerializeField]
		private MatchManager _matchManagerPrefab;

		private MatchManager _matchManager;

		private bool IsServer = false;

		//private bool IsClient = false;

		public override void EnterState()
		{
			base.EnterState();

			SL.TryGet<NetworkManager>(out var networkManager);

			IsServer = networkManager.mode == NetworkManagerMode.Host || networkManager.mode == NetworkManagerMode.ServerOnly;
			//IsClient = networkManager.mode == NetworkManagerMode.Host || networkManager.mode == NetworkManagerMode.ClientOnly;

			Debug.Log("networkManager.mode: " + networkManager.mode);

			if (IsServer)
			{
				_matchManager = Instantiate(_matchManagerPrefab);
				NetworkServer.Spawn(_matchManager);

				//SL.TryGet(out NetworkManager networkManager);
				//networkManager.StartHost();
			}
		}

		public override void ExitState()
		{
			base.ExitState();

			if (_matchManager)
			{
				if (IsServer)
				{
					NetworkServer.Destroy(_matchManager);
				}
			}
		}	
	}

}
