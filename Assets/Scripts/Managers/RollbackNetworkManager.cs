using UnityEngine;

namespace Mirror
{
	public class RollbackNetworkManager : NetworkManager
	{

		#region Variables

		public bool IsServer { get; private set; } = false;

		public bool IsClient { get; private set; } = false;

		public bool RollbackSystemIsReady { get; private set; } = false;

		private NetTickManager3D _tickManager;

		[Header("Rollback")]
		[SerializeField]
		[Range(0f, 2f)]
		private float _sendTime = 0.1f;

		private float _sendCounter = 0;

		private uint _lastFrameSend = 0;

		[SerializeField]
		private bool _autoStartRollback = false;

		[SerializeField]
		private RollbackMode _rollbackMode = default;

		#endregion

		#region Start

		public override void Start()
		{
			SL.TryGet(out _tickManager);
			base.Start();
		}

		public override void OnStartServer()
		{
			IsServer = true;

			base.OnStartServer();

			if (_autoStartRollback)
			{
				StartRollbackSystem();
			}
		}

		public override void OnStartClient()
		{
			IsClient = true;

			if (mode != NetworkManagerMode.Host)
			{
				NetworkClient.RegisterHandler<Rollback.ConfigLockstepMessage>(Rollback.OnReceiveConfigLockstep);
				NetworkClient.RegisterHandler<Rollback.DeltaLockstepMessage>(Rollback.OnReceiveDeltaLockstep);
				NetworkClient.RegisterHandler<Rollback.FullLockstepMessage>(Rollback.OnReceiveFullLockstep);
				NetworkClient.RegisterHandler<Rollback.EndLockstepMessage>(Rollback.OnReceiveEndLockstepMessage);
			}

			if (_autoStartRollback)
			{
				StartRollbackSystem();
			}
		}

		#endregion

		#region Stop

		public override void OnStopClient()
		{
			IsClient = false;
			StopRollbackSystem();
			base.OnStopClient();
		}

		public override void OnStopServer()
		{
			IsServer = false;
			StopRollbackSystem();
			base.OnStopServer();
		}

		#endregion

		#region Rollback System

		private void StartRollbackSystem()
		{
			if (RollbackSystemIsReady)
			{
				return;
			}

			_tickManager.Clear();

			Rollback.rollbackMode = _rollbackMode;

			if (_sendTime < Time.fixedDeltaTime)
			{
				_sendTime = Time.fixedDeltaTime;
			}

			_sendCounter = _sendTime;

			RollbackSystemIsReady = true;
		}

		private void StopRollbackSystem()
		{
			RollbackSystemIsReady = false;
		}

		public virtual void Update()
		{
			if (RollbackSystemIsReady && (IsServer || IsClient))
			{
				UpdateRollback();
			}
		}

		public void UpdateRollback()
		{
			//auto: reception des messages

			//avancer la simulation jusqu'au present
			_tickManager.Tick(NetworkTime.time, Time.deltaTime, Time.fixedDeltaTime);

			if (IsServer)
			{
				if (Rollback.sendConfigMessage)
				{
					Rollback.sendConfigMessage = false;

					_tickManager.SendConfigLockstepMessage(NetworkServer.connections.Values, false);

					_sendCounter = (float)_tickManager.FixedTime + _sendTime;

					_lastFrameSend = _tickManager.FixedFrameCount;

					_tickManager.OnFinishSendLockstepMessage();
				}

				//Check si assez de temps est passer pour un lockstep
				else if (_tickManager.FixedTime > _sendCounter)
				{
					if (_lastFrameSend != _tickManager.FixedFrameCount)
					{
						//Send lockstep to clients
						switch (Rollback.rollbackMode)
						{
							case RollbackMode.SendFullData:
								_tickManager.SendFullLockstepMessage(NetworkServer.connections.Values);
								break;
							case RollbackMode.SendDeltaData:
								_tickManager.SendDeltaLockstepMessage(NetworkServer.connections.Values);
								break;
						}

						_sendCounter = (float)_tickManager.FixedTime + _sendTime;

						_lastFrameSend = _tickManager.FixedFrameCount;

						_tickManager.OnFinishSendLockstepMessage();
					}
				}

				//Send First Lockstep Message
				if (Rollback.newPlayerReadyForRollback.Count > 0)
				{
					foreach (var conn in Rollback.newPlayerReadyForRollback)
					{
						NetworkServer.SetClientReady(conn);
					}

					_tickManager.SendConfigLockstepMessage(Rollback.newPlayerReadyForRollback, true);

					Rollback.newPlayerReadyForRollback.Clear();
				}

				//auto: Server send spawn and delta lockstep to all connections
			}
		}

		#endregion

	}

}
