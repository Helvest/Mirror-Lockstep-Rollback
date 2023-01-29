using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
	public class RollbackNetworkManager : NetworkManager
	{

		#region Fields

		private static RollbackNetworkManager _singleton;

		public static new RollbackNetworkManager singleton
		{
			get
			{
				if (_singleton == null)
				{
					_singleton = (RollbackNetworkManager)NetworkManager.singleton;
				}

				return _singleton;
			}
		}

		protected INetTickSystem netTickSystem = default;

		public bool IsServer { get; private set; } = false;

		public bool IsClient { get; private set; } = false;

		public bool RollbackSystemIsReady { get; private set; } = false;

		[Header("Rollback")]

		[SerializeField]
		private bool _autoStartRollback = false;

		[SerializeField]
		private RollbackMode _rollbackMode = default;

		public bool useDebug = false;

		#endregion

		#region Init

		private void OnEnable()
		{
			SL.TryGet(out netTickSystem);
		}

		#endregion

		#region OnStart

		public override void OnStartServer()
		{
			IsServer = true;

			if (_autoStartRollback && mode == NetworkManagerMode.ServerOnly)
			{
				StartRollbackSystem();
			}
		}

		public override void OnStartClient()
		{
			IsClient = true;

			if (mode == NetworkManagerMode.Host)
			{
				NetworkClient.RegisterHandler<Rollback.ConfigLockstepMessage>(_ => { });
				NetworkClient.RegisterHandler<Rollback.DeltaLockstepMessage>(_ => { });
				NetworkClient.RegisterHandler<Rollback.FullLockstepMessage>(_ => { });
				NetworkClient.RegisterHandler<Rollback.EndLockstepMessage>(_ => { });
			}
			else
			{
				NetworkClient.RegisterHandler<Rollback.ConfigLockstepMessage>(Rollback.OnReceiveConfigLockstep);
				NetworkClient.RegisterHandler<Rollback.DeltaLockstepMessage>(Rollback.OnReceiveDeltaLockstep);
				NetworkClient.RegisterHandler<Rollback.FullLockstepMessage>(Rollback.OnReceiveFullLockstep);
				NetworkClient.RegisterHandler<Rollback.EndLockstepMessage>(Rollback.OnReceiveEndLockstepMessage);

				NetworkClient.ReplaceHandler<SpawnMessage>(Rollback.OnSpawn);
				NetworkClient.ReplaceHandler<EntityStateMessage>(Rollback.OnEntityStateMessage);
			}

			if (_autoStartRollback)
			{
				StartRollbackSystem();
			}
		}

		#endregion

		#region OnStop

		public override void OnStopClient()
		{
			IsClient = false;
			StopRollbackSystem();
		}

		public override void OnStopServer()
		{
			IsServer = false;
			StopRollbackSystem();
		}

		#endregion

		#region Set Rollback System

		private void StartRollbackSystem()
		{
			if (RollbackSystemIsReady)
			{
				return;
			}

			netTickSystem.Clear();

			Rollback.rollbackMode = _rollbackMode;

			RollbackSystemIsReady = true;
		}

		private void StopRollbackSystem()
		{
			RollbackSystemIsReady = false;
		}

		#endregion

		#region Update Rollback System

		public override void Update()
		{
			base.Update();
#if DEBUG
			Rollback.useDebug = useDebug;
#endif

			if (RollbackSystemIsReady)
			{
				UpdateRollback();
			}
		}

		protected void UpdateRollback()
		{
			//auto: reception des messages

			//advance the simulation to the present
			netTickSystem.Tick(NetworkTime.time, Time.deltaTime, Time.fixedDeltaTime);

			if (!IsServer)
			{
				return;
			}

			//Remove conn
			RemoveConnNoMoreReadyForRollback();

			netTickSystem.ServerUpdate();

			//Send First Lockstep Message
			AddConnReadyForRollback();
		}

		#endregion

		#region ChangeRollbackState

		private readonly List<NetworkConnectionToClient> _newConnReadyForRollback = new();
		private readonly List<NetworkConnectionToClient> _connNoMoreReadyForRollback = new();

		public void ChangeRollbackState(NetworkConnectionToClient conn, bool useRollback)
		{
			if (useRollback)
			{
				if (conn.rollbackState != RollbackState.NotObserving)
				{
					return;
				}

				conn.rollbackState = RollbackState.ReadyToObserve;

				if (!_newConnReadyForRollback.Contains(conn))
				{
					_newConnReadyForRollback.Add(conn);
				}
			}
			else
			{
				if (conn.rollbackState == RollbackState.NotObserving)
				{
					return;
				}

				conn.rollbackState = RollbackState.NotObserving;

				if (!_connNoMoreReadyForRollback.Contains(conn))
				{
					_connNoMoreReadyForRollback.Add(conn);
				}
			}
		}

		private void AddConnReadyForRollback()
		{
			if (_newConnReadyForRollback.Count == 0)
			{
				return;
			}

			foreach (var conn in _newConnReadyForRollback)
			{
				NetworkServer.SetClientReady(conn);
			}

			netTickSystem.SendConfigLockstepMessage(_newConnReadyForRollback, true);

			_newConnReadyForRollback.Clear();
		}

		private void RemoveConnNoMoreReadyForRollback()
		{
			if (_connNoMoreReadyForRollback.Count == 0)
			{
				return;
			}

			foreach (var conn in _connNoMoreReadyForRollback)
			{
				if (conn.rollbackState != RollbackState.NotObserving)
				{
					continue;
				}

				foreach (var identity in NetworkServer.spawned.Values)
				{
					if (identity.useRollback)
					{
						conn.RemoveFromObserving(identity, false);
						identity.RemoveObserver(conn);
					}
				}
			}

			_connNoMoreReadyForRollback.Clear();
		}

		#endregion

	}

}
