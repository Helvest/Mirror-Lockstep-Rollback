using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Add to NetworkConnectionToClient
// - public RollbackState rollbackState = RollbackState.NotObserving;
// - public bool isFirstSpawn = true;
// - public readonly List<NetworkIdentity> newObserving = new List<NetworkIdentity>();

//In NetworkConnectionToClient.AddToObserving(NetworkIdentity netIdentity)
//- Replace observing.Add(netIdentity);
//- By newObserving.Add(netIdentity);
//- Remove NetworkServer.ShowForConnection(netIdentity, this);

//Add to NetworkIdentity
// - public bool useRollback = false; 

//In NetworkServer.NetworkLateUpdate()
// - Replace NetworkServer.Broadcast();
// - by Rollback.Broadcast();

//In NetworkServer.UnSpawn(GameObject obj)
// - Remove all the content
// - Add Rollback.UnSpawn(obj);

//In NetworkClient.RegisterMessageHandlers(bool hostMode)
// - Replace RegisterHandler<SpawnMessage>(OnSpawn);
// - By RegisterHandler<SpawnMessage>(Rollback.OnSpawn);
// - Replace RegisterHandler<EntityStateMessage>(OnEntityStateMessage);
// - By RegisterHandler<EntityStateMessage>(Rollback.OnEntityStateMessage);

//Add to the end of NetworkClient.NetworkEarlyUpdate()
// - Rollback.OnClientNetworkEndOfEarlyUpdate();


namespace Mirror
{

	#region Enums

	public enum RollbackMode
	{
		None = -1,
		SendFullData,
		SendDeltaData
	}

	public enum RollbackState
	{
		NotObserving,
		ReadyToObserve,
		Observing
	}

	#endregion

	#region ObjectData

	public class ObjectData
	{
		public SpawnMessage Message { get; private set; }

		public ObjectData(SpawnMessage message)
		{
			byte[] data = message.payload.ToArray();
			message.payload = new ArraySegment<byte>(data);

			Message = message;
		}
	}

	public class ObjectDeltaData
	{
		public EntityStateMessage Message { get; private set; }

		public ObjectDeltaData(EntityStateMessage message)
		{
			byte[] data = message.payload.ToArray();
			message.payload = new ArraySegment<byte>(data);

			Message = message;
		}
	}

	#endregion

	#region Lockstep

	public class Lockstep
	{

		#region Pool

		private static readonly Queue<Lockstep> _pool = new Queue<Lockstep>();

		public static Lockstep GetFromPool(RollbackData data)
		{
			if (_pool.Count > 0)
			{
				var item = _pool.Dequeue();
				item.rollbackData = data;
				return item;
			}

			return new Lockstep(data);
		}

		public static void ReturnToPool(Lockstep lockstep)
		{
			lockstep.Clear();
			_pool.Enqueue(lockstep);
		}

		#endregion

		#region Fields

		public RollbackData rollbackData = default;

		public uint pastFrame = 0;

		public readonly Dictionary<uint, ObjectData> objectDataDict = new();

		public readonly Dictionary<uint, ObjectDeltaData> objectDeltaDataDict = new();

		public readonly List<uint> destroyList = new();

		#endregion

		#region Init

		public Lockstep()
		{
			rollbackData = RollbackData.GetFromPool();
		}

		public Lockstep(Lockstep lockstep)
		{
			Copy(lockstep);
		}

		public Lockstep(RollbackData rollbackData)
		{
			this.rollbackData = rollbackData;
		}

		#endregion

		#region End

		public void Reset()
		{
			objectDataDict.Clear();
			objectDeltaDataDict.Clear();
			destroyList.Clear();
		}

		public void Clear()
		{
			if (rollbackData != null)
			{
				RollbackData.ReturnToPool(rollbackData);
				rollbackData = null;
			}

			Reset();
		}

		#endregion

		#region Add 

		public void Add(NetworkIdentity networkIdentity)
		{
			objectDataDict.Add(networkIdentity.netId, networkIdentity.GetObjectData());
		}

		#endregion

		#region Copy

		public void Copy(Lockstep lockstep)
		{
			rollbackData ??= RollbackData.GetFromPool();

			rollbackData.Copy(lockstep.rollbackData);

			pastFrame = lockstep.pastFrame;

			Reset();

			foreach (var item in lockstep.objectDataDict)
			{
				objectDataDict.Add(item.Key, item.Value);
			}

			foreach (var item in lockstep.objectDeltaDataDict)
			{
				objectDeltaDataDict.Add(item.Key, item.Value);
			}

			destroyList.AddRange(lockstep.destroyList);
		}

		public void CopyTo(Lockstep lockstep)
		{
			lockstep.Copy(this);
		}

		#endregion

	}

	#endregion

	#region Memory

	public class ClientMemory
	{

		#region Fields

		public uint lastFrameReceive = 0;

		public readonly RollbackData lastResolvedRollbackData = new RollbackData();

		public uint firstFrame = 0;

		public Lockstep present = default;

		public readonly SortedList<uint, Lockstep> futurs = new SortedList<uint, Lockstep>();

		public Lockstep inConstruction = default;

		public bool HaveFutur => futurs.Count > 0;

		#endregion

		#region Construction

		public void StartConstruction(RollbackData data)
		{
			inConstruction = Lockstep.GetFromPool(data);
		}

		public Lockstep FinishConstruction()
		{
			uint frame = inConstruction.rollbackData.fixedFrameCount;

			if (futurs.TryGetValue(frame, out var lockstep))
			{
				inConstruction.pastFrame = lockstep.pastFrame;

				lockstep.Clear();
			}

			var lastLockstep = inConstruction;

			futurs[frame] = inConstruction;
			inConstruction = null;

			return lastLockstep;
		}

		#endregion

		#region End

		public void Clear()
		{
			firstFrame = 0;

			if (present != null)
			{
				Lockstep.ReturnToPool(present);
				present = null;
			}

			if (inConstruction != null)
			{
				Lockstep.ReturnToPool(inConstruction);
				inConstruction = null;
			}

			RemoveAllFutur();
		}

		public void RemoveAllFutur()
		{
			foreach (var futur in futurs.Values)
			{
				Lockstep.ReturnToPool(futur);
			}

			futurs.Clear();
		}

		public void RemoveFuturUntil(uint frame)
		{
			for (int i = 0; i < futurs.Count; i++)
			{
				if (futurs.Keys[i] <= frame)
				{
					Lockstep.ReturnToPool(futurs.Values[i]);
					futurs.RemoveAt(i);
					i--;
				}
				else
				{
					return;
				}
			}
		}

		public bool TryGetFuturOrClosestPrevious(uint frame, out Lockstep lockstep)
		{
			if (futurs.ContainsKey(frame))
			{
				lockstep = futurs[frame];
				return true;
			}

			lockstep = null;

			foreach (var item in futurs)
			{
				if (item.Key <= frame)
				{
					lockstep = item.Value;
				}
				else
				{
					break;
				}
			}

			return lockstep != null;
		}

		#endregion

	}

	#endregion

	#region RollbackData

	public class RollbackData
	{

		#region Pool

		private static readonly Queue<RollbackData> _pool = new Queue<RollbackData>();

		public static RollbackData GetFromPool()
		{
			if (_pool.Count > 0)
			{
				return _pool.Dequeue();
			}

			return new RollbackData();
		}

		public static void ReturnToPool(RollbackData rollbackData)
		{
			_pool.Enqueue(rollbackData);
		}

		#endregion

		#region Fields

		public bool isPhysicUpdated;
		public double timeAtSimulation;
		public double normalTime;
		public double fixedTime;
		public uint fixedFrameCount;

		#endregion

		#region Init

		public RollbackData() { }

		public RollbackData(RollbackData rollbackData)
		{
			Copy(rollbackData);
		}

		#endregion

		#region Copy

		public void Copy(RollbackData rollbackData)
		{
			isPhysicUpdated = rollbackData.isPhysicUpdated;
			timeAtSimulation = rollbackData.timeAtSimulation;
			normalTime = rollbackData.normalTime;
			fixedTime = rollbackData.fixedTime;
			fixedFrameCount = rollbackData.fixedFrameCount;
		}

		public void CopyTo(RollbackData rollbackData)
		{
			rollbackData.Copy(this);
		}

		#endregion

	}

	#endregion

	public static class Rollback
	{

		#region Fields

		public static RollbackMode rollbackMode = default;

		public static bool useDebug = false;

		public static readonly ClientMemory clientMemory = new ClientMemory();

		#endregion

		#region Struct Messages

		public struct ConfigLockstepMessage : NetworkMessage
		{
			public bool isFirst;
			public RollbackMode rollbackMode;
			public bool isPhysicUpdated;
			public double timeAtSimulation;
			public double normalTime;
			public double fixedTime;
			public uint pastFrame;
			public uint presentFrame;
			public uint[] destroyList;
		}

		public struct DeltaLockstepMessage : NetworkMessage
		{
			public double timeAtSimulation;
			public double normalTime;
			public double fixedTime;
			public uint pastFrame;
			public uint presentFrame;
			public uint[] destroyList;
		}

		public struct FullLockstepMessage : NetworkMessage
		{
			public double timeAtSimulation;
			public double normalTime;
			public double fixedTime;
			public uint presentFrame;
		}

		public struct EndLockstepMessage : NetworkMessage { }

		#endregion

		#region Get

		public static bool TryGetPresentLockstep(out Lockstep lockstep)
		{
			lockstep = clientMemory.present;

			return lockstep != null;
		}

		public static ObjectData GetObjectData(this NetworkIdentity identity)
		{
			var message = identity.GetSpawnMessageOnClient();

			return new ObjectData(message);
		}

		public static SpawnMessage GetSpawnMessageOnClient(this NetworkIdentity identity)
		{
			using NetworkWriterPooled ownerWriter = NetworkWriterPool.Get(), observersWriter = NetworkWriterPool.Get();

			var conn = identity.connectionToServer;

			//bool isOwner = identity.connectionToClient == conn;

			var payload = NetworkServer.CreateSpawnMessagePayload(false, identity, ownerWriter, observersWriter);

			var transform = identity.transform;

			var message = new SpawnMessage
			{
				netId = identity.netId,
				isLocalPlayer = false,
				isOwner = false,
				sceneId = identity.sceneId,
				assetId = identity.assetId,
				// use local values for VR support
				position = transform.localPosition,
				rotation = transform.localRotation,
				scale = transform.localScale,
				payload = payload,
			};

			return message;
		}

		#endregion

		#region SendMessages

		private static bool _broadcastLockstep = false;

		public static void BroacastNextLockstep()
		{
			_broadcastLockstep = true;
		}

		#region - LockstepMessage

		public static void SendConfigLockstepMessage<T1>(ConfigLockstepMessage message, IEnumerable<T1> connList) where T1 : NetworkConnection
		{
			message.rollbackMode = rollbackMode;

			if (useDebug)
			{
				Debug.Log("SendConfigLockstepMessage - isFirst: " + message.isFirst
					+ " | isPhysicUpdated: " + message.isPhysicUpdated);
			}

			if (message.isFirst)
			{
				message.destroyList = new uint[0];
			}
			else
			{
				message.destroyList = _netIdToDestroy.ToArray();
				_netIdToDestroy.Clear();
			}

			SendMessage(message, connList);

			if (!message.isFirst)
			{
				BroacastNextLockstep();
			}
		}

		public static void SendDeltaLockstepMessage<T1>(DeltaLockstepMessage message, IEnumerable<T1> connList) where T1 : NetworkConnection
		{
			message.destroyList = _netIdToDestroy.ToArray();
			_netIdToDestroy.Clear();

			if (useDebug)
			{
				Debug.Log("SendDeltaLockstepMessage: " + message.presentFrame);
			}

			SendMessage(message, connList);
			BroacastNextLockstep();
		}

		public static void SendFullLockstepMessage<T1>(FullLockstepMessage message, IEnumerable<T1> connList) where T1 : NetworkConnection
		{
			if (useDebug)
			{
				Debug.Log("SendFullLockstepMessage: " + message.presentFrame);
			}

			SendMessage(message, connList);
			BroacastNextLockstep();
		}

		public static void SendMessage<T1, T2>(T1 lockstepMessage, IEnumerable<T2> connList, int channelId = Channels.Reliable)
		where T1 : struct, NetworkMessage
		where T2 : NetworkConnection
		{
			foreach (var conn in connList)
			{
				conn.Send(lockstepMessage, channelId);
			}
		}

		#endregion

		#region - SpawnMessage
		public static void Spawn(NetworkBehaviour netGo)
		{
			if (NetworkServer.active)
			{
				NetworkServer.Spawn(netGo.gameObject);
			}
			else
			{
				uint fakeID = uint.MaxValue - (uint)NetworkClient.spawned.Count;

				NetworkClient.spawned.Add(fakeID, netGo.netIdentity);
			}
		}

		#endregion

		#region - DestroyObject

		public static void Destroy(GameObject go)
		{
			if (NetworkServer.active)
			{
				NetworkServer.Destroy(go);
			}
			else
			{
				go.SetActive(false);
			}
		}

		private readonly static List<uint> _netIdToDestroy = new();

		//Replace NetworkServer.UnSpawn(GameObject obj)
		public static void UnSpawn(GameObject obj, bool resetState)
		{
			// Debug.Log($"DestroyObject instance:{identity.netId}");

			// NetworkServer.Unspawn should only be called on server or host.
			// on client, show a warning to explain what it does.
			if (!NetworkServer.active)
			{
				Debug.LogWarning("NetworkServer.Unspawn() called without an active server. Servers can only destroy while active, clients can only ask the server to destroy (for example, with a [Command]), after which the server may decide to destroy the object and broadcast the change to all clients.");
				return;
			}

			if (obj == null)
			{
				Debug.Log("NetworkServer.Unspawn(): object is null");
				return;
			}

			if (!NetworkServer.GetNetworkIdentity(obj, out NetworkIdentity identity))
			{
				return;
			}

			// only call OnRebuildObservers while active,
			// not while shutting down
			// (https://github.com/vis2k/Mirror/issues/2977)
			if (NetworkServer.active && NetworkServer.aoi)
			{
				// This calls user code which might throw exceptions
				// We don't want this to leave us in bad state
				try
				{
					NetworkServer.aoi.OnDestroyed(identity);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			// remove from NetworkServer (this) dictionary
			NetworkServer.spawned.Remove(identity.netId);

			identity.connectionToClient?.RemoveOwnedObject(identity);

			if (!identity.useRollback)
			{
				// send object destroy message to all observers, clear observers
				NetworkServer.SendToObservers(identity, new ObjectDestroyMessage
				{
					netId = identity.netId
				});
			}
			else if (rollbackMode == RollbackMode.SendDeltaData)
			{
				_netIdToDestroy.Add(identity.netId);
			}

			identity.ClearObservers();

			// in host mode, call OnStopClient/OnStopLocalPlayer manually
			if (NetworkClient.active && NetworkServer.activeHost)
			{
				if (identity.isLocalPlayer)
					identity.OnStopLocalPlayer();

				identity.OnStopClient();
				// The object may have been spawned with host client ownership,
				// e.g. a pet so we need to clear hasAuthority and call
				// NotifyAuthority which invokes OnStopAuthority if hasAuthority.
				identity.isOwned = false;
				identity.NotifyAuthority();

				// remove from NetworkClient dictionary
				NetworkClient.connection.owned.Remove(identity);
				NetworkClient.spawned.Remove(identity.netId);
			}

			// we are on the server. call OnStopServer.
			identity.OnStopServer();

			// finally reset the state and deactivate it
			if (resetState)
			{
				identity.ResetState();
				identity.gameObject.SetActive(false);
			}
		}

		#endregion

		#endregion

		#region ReceiveMessages

		#region - LockstepMessage

		public static event Action<uint, Lockstep> EventLockstepFinish;
		public static event Action<RollbackData> EventOnLockstepReceive;
		public static event Action<RollbackData> EventOnRollbackResolve;

		private static bool _isRecevingLockstep = false;

		public static void OnReceiveConfigLockstep(ConfigLockstepMessage message)
		{
			if (_isRecevingLockstep)
			{
				Debug.LogError("Receive config lockstep in between another lockstep");
			}

			_isRecevingLockstep = true;

			//Is First Message
			if (message.isFirst)
			{
				uint startFrame = message.presentFrame;

				if (useDebug)
				{
					Debug.Log("FIRST ConfigLockstep receive: " + startFrame);
				}

				clientMemory.Clear();
				clientMemory.firstFrame = message.presentFrame;
			}
			else
			{
				if (useDebug)
				{
					Debug.Log("ConfigLockstep receive: " + message.pastFrame + " => " + message.presentFrame);
				}
			}

			rollbackMode = message.rollbackMode;

			var data = RollbackData.GetFromPool();

			data.isPhysicUpdated = message.isPhysicUpdated;
			data.timeAtSimulation = message.timeAtSimulation;
			data.normalTime = message.normalTime;
			data.fixedTime = message.fixedTime;
			data.fixedFrameCount = message.presentFrame;

			clientMemory.StartConstruction(data);

			if (message.isFirst)
			{
				clientMemory.lastResolvedRollbackData.Copy(data);
			}

			if (message.pastFrame < clientMemory.firstFrame)
			{
				message.pastFrame = clientMemory.firstFrame;
			}

			clientMemory.inConstruction.pastFrame = message.pastFrame;
			clientMemory.inConstruction.destroyList.AddRange(message.destroyList);
		}

		public static void OnReceiveDeltaLockstep(DeltaLockstepMessage message)
		{
			if (_isRecevingLockstep)
			{
				Debug.LogError("Receive delta lockstep in between another lockstep");
			}

			_isRecevingLockstep = true;

			if (useDebug)
			{
				Debug.Log("DeltaLockstep receive: " + message.pastFrame + " => " + message.presentFrame);
			}

			var data = RollbackData.GetFromPool();

			//todo : replace by last message
			data.isPhysicUpdated = clientMemory.lastResolvedRollbackData.isPhysicUpdated;
			data.timeAtSimulation = message.timeAtSimulation;
			data.normalTime = message.normalTime;
			data.fixedTime = message.fixedTime;
			data.fixedFrameCount = message.presentFrame;

			clientMemory.StartConstruction(data);

			if (message.pastFrame < clientMemory.firstFrame)
			{
				message.pastFrame = clientMemory.firstFrame;
			}

			clientMemory.inConstruction.pastFrame = message.pastFrame;
			clientMemory.inConstruction.destroyList.AddRange(message.destroyList);
		}

		public static void OnReceiveFullLockstep(FullLockstepMessage message)
		{
			if (_isRecevingLockstep)
			{
				Debug.LogError("Receive full lockstep in between another lockstep");
			}

			_isRecevingLockstep = true;

			if (useDebug)
			{
				Debug.Log("FullLockstep receive: " + message.presentFrame);
			}

			var data = RollbackData.GetFromPool();

			//todo : replace by last message
			data.isPhysicUpdated = clientMemory.lastResolvedRollbackData.isPhysicUpdated;
			data.timeAtSimulation = message.timeAtSimulation;
			data.normalTime = message.normalTime;
			data.fixedTime = message.fixedTime;
			data.fixedFrameCount = message.presentFrame;

			clientMemory.StartConstruction(data);
		}

		public static void OnReceiveEndLockstepMessage(EndLockstepMessage _)
		{
			if (!_isRecevingLockstep)
			{
				Debug.LogError("Receive EndLockstepMessage outside a lockstep");
				return;
			}

			if (useDebug)
			{
				Debug.Log("EndLockstepMessage");
			}

			_isRecevingLockstep = false;

			var lockstep = clientMemory.FinishConstruction();

			EventLockstepFinish?.Invoke(lockstep.rollbackData.fixedFrameCount, lockstep);

			_lockstepWereReceive = true;
		}

		#endregion

		#region - SpawnMessage

		public static void OnSpawn(SpawnMessage message)
		{
			if (!_isRecevingLockstep)
			{
				NetworkClient.OnSpawn(message);
				return;
			}

			//Try get spawned identity
			if (!NetworkClient.spawned.TryGetValue(message.netId, out var identity))
			{
				//Try get scene identity
				if (message.sceneId != 0)
				{
					NetworkClient.spawnableObjects.TryGetValue(message.sceneId, out identity);
				}
				//Try get prefab identity
				else if (NetworkClient.GetPrefab(message.assetId, out var gameObject))
				{
					gameObject.TryGetComponent(out identity);
				}
			}

			//Debug.Log("OnSpawn: " + identity.useRollback + " | " + message.netId + " | " + message.assetId);

			if (identity.useRollback)
			{
				var objectData = new ObjectData(message);

				clientMemory.inConstruction.objectDataDict.Add(message.netId, objectData);
			}
			else
			{
				NetworkClient.OnSpawn(message);
			}
		}

		#endregion

		#region - EntityStateMessage

		public static void OnEntityStateMessage(EntityStateMessage message)
		{
			NetworkClient.spawned.TryGetValue(message.netId, out var identity);

			if (!SaveEntityState(message, identity))
			{
				ApplyEntityState(message, identity);
			}
		}

		private static bool SaveEntityState(EntityStateMessage message, NetworkIdentity identity)
		{
			if (_isRecevingLockstep)
			{
				//If null, we'll assume it's a new rollback object
				if (identity == null || identity.useRollback)
				{
					var objectDeltaData = new ObjectDeltaData(message);
					clientMemory.inConstruction.objectDeltaDataDict.Add(message.netId, objectDeltaData);
					return true;
				}
			}

			return false;
		}

		private static void ApplyEntityState(EntityStateMessage message)
		{
			NetworkClient.spawned.TryGetValue(message.netId, out var identity);
			ApplyEntityState(message, identity);
		}

		private static void ApplyEntityState(EntityStateMessage message, NetworkIdentity identity)
		{
			if (identity == null)
			{
				Debug.LogWarning("Did not find target for sync message for " + message.netId
					+ " . Note: this can be completely normal because UDP messages may arrive out of order, so this message might have arrived after a Destroy message.");
				return;
			}

			//Apply delta
			using (var networkReader = NetworkReaderPool.Get(message.payload))
			{
				identity.DeserializeClient(networkReader, false);
			}
		}

		#endregion

		#endregion

		#region NetworkEarlyUpdate

		private static bool _lockstepWereReceive = false;

		//Updated all frame by NetworkClient.NetworkEarlyUpdate
		public static void OnClientNetworkEndOfEarlyUpdate()
		{
			if (_lockstepWereReceive)
			{
				_lockstepWereReceive = false;

				if (!clientMemory.HaveFutur)
				{
					return;
				}

				switch (rollbackMode)
				{
					case RollbackMode.SendFullData:
					{
						DetectClientRollback();
						break;
					}
					case RollbackMode.SendDeltaData:
					{
						DetectClientDeltaRollback();
						break;
					}
				}
			}

			//todo : remove futur when apply
			//clientMemory.RemoveAllFutur();
		}

		#endregion

		#region DetectRollback

		private static void DetectClientRollback()
		{
			var lastFutur = clientMemory.futurs.Keys[^1];

			if (lastFutur == clientMemory.firstFrame || lastFutur > clientMemory.lastFrameReceive)
			{
				clientMemory.lastFrameReceive = lastFutur;
				EventOnLockstepReceive?.Invoke(clientMemory.futurs[lastFutur].rollbackData);
			}
		}

		private static void DetectClientDeltaRollback()
		{
			uint lastFutur = clientMemory.lastResolvedRollbackData.fixedFrameCount;

			foreach (var futur in clientMemory.futurs.Values)
			{
				if (futur.pastFrame != lastFutur)
				{
					break;
				}

				lastFutur = futur.rollbackData.fixedFrameCount;
			}

			if (lastFutur == clientMemory.firstFrame || lastFutur > clientMemory.lastFrameReceive)
			{
				clientMemory.lastFrameReceive = lastFutur;
				EventOnLockstepReceive?.Invoke(clientMemory.futurs[lastFutur].rollbackData);
			}
		}

		#endregion

		#region ResolveRollback

		private static readonly List<NetworkIdentity> _newNetIdList = new();

		public static bool TryResolveClientLastRollback()
		{
			if (clientMemory.futurs.Count == 0)
			{
				return false;
			}

			var fixedFrameCount = clientMemory.futurs.Keys[^1];
			return TryResolveClientRollback(fixedFrameCount);
		}

		public static bool TryResolveClientRollback(uint fixedFrameCount)
		{
			switch (rollbackMode)
			{
				case RollbackMode.SendFullData:
				{
					if(TryResolveClientFullRollback(fixedFrameCount, out var lockstep))
					{
						EventOnRollbackResolve?.Invoke(lockstep.rollbackData);
						clientMemory.RemoveFuturUntil(lockstep.rollbackData.fixedFrameCount);
						return true;
					}
					break;
				}
				case RollbackMode.SendDeltaData:
				{
					if (TryResolveClientDeltaRollback(fixedFrameCount, out var lockstep))
					{
						EventOnRollbackResolve?.Invoke(lockstep.rollbackData);
						clientMemory.RemoveFuturUntil(lockstep.rollbackData.fixedFrameCount);
						return true;
					}
					break;
				}
			}

			return false;
		}

		public static bool TryResolveClientFullRollback(uint fixedFrameCount, out Lockstep lockstep)
		{
			if (!clientMemory.TryGetFuturOrClosestPrevious(fixedFrameCount, out lockstep))
			{
				return false;
			}

			clientMemory.lastResolvedRollbackData.Copy(lockstep.rollbackData);

			//Remove no used netId from the scene
			UnspawnForClientLockstep(lockstep.objectDataDict.Keys);

			ApplyClientFullLockstep(lockstep);

			return true;
		}

		private static bool TryResolveClientDeltaRollback(uint fixedFrameCount, out Lockstep lockstep)
		{
			uint presentFrame = clientMemory.lastResolvedRollbackData.fixedFrameCount;
			Lockstep lastFutur = null;
			bool needToResetScene = true;

			foreach (var data in clientMemory.futurs)
			{
				//if futur is over target or if no next delta, stop
				if (data.Key > fixedFrameCount || data.Value.pastFrame != presentFrame)
				{
					break;
				}

				lastFutur = data.Value;

				if (needToResetScene)
				{
					needToResetScene = false;

					//Callback: apply past
					if (clientMemory.present != null)
					{
						UnspawnForClientLockstep(clientMemory.present.objectDataDict.Keys);
						ApplyClientFullLockstep(clientMemory.present);
					}
				}

				if (useDebug)
				{
					Debug.Log("Apply: " + presentFrame + " => " + data.Value.rollbackData.fixedFrameCount);
				}

				ApplyClientDeltaLockstep(data.Value);

				presentFrame = data.Value.rollbackData.fixedFrameCount;
			}

			if (lastFutur == null)
			{
				lockstep = null;
				return false;
			}

			clientMemory.lastResolvedRollbackData.Copy(lastFutur.rollbackData);

			//Save new present
			if (clientMemory.present == null)
			{
				clientMemory.present = lastFutur;
				//Manually removed to not return to pool
				clientMemory.futurs.Remove(lastFutur.rollbackData.fixedFrameCount);
			}
			else
			{
				clientMemory.present.Reset();
				clientMemory.present.rollbackData.Copy(lastFutur.rollbackData);

				CreatePresentLockStep(clientMemory.present);
			}

			lockstep = clientMemory.present;
			return true;
		}

		#endregion

		#region ApplyLockstep

		public static void ApplyClientFullLockstep(Lockstep lockstep)
		{
			_newNetIdList.Clear();

			//Spawn all new netId
			SpawnForClientLockstep(lockstep.objectDataDict.Values, _newNetIdList);

			//Apply payload on all netId
			PayloadForClientLockstep(lockstep.objectDataDict.Values);

			//Call OnStart on all the news netID
			BootstrapForClientLockstep(_newNetIdList);
		}

		public static void ApplyClientDeltaLockstep(Lockstep lockstep)
		{
			_newNetIdList.Clear();

			//Destroy
			DestroyForClientLockstep(lockstep.destroyList);

			//Spawn all new netId
			SpawnForClientLockstep(lockstep.objectDataDict.Values, _newNetIdList);

			//Apply payload on all netId
			PayloadForClientLockstep(lockstep.objectDataDict.Values);

			//Call OnStart on all the news netID
			BootstrapForClientLockstep(_newNetIdList);

			foreach (var objectDeltaData in lockstep.objectDeltaDataDict.Values)
			{
				ApplyEntityState(objectDeltaData.Message);
			}
		}


		#region - UnspawnForLockstep

		private static readonly List<uint> _identityToDestroy = new List<uint>();

		private static void UnspawnForClientLockstep<T>(T netIdsToKeep) where T : IEnumerable<uint>
		{
			_identityToDestroy.Clear();

			foreach (var pair in NetworkClient.spawned)
			{
				if (pair.Value.useRollback && !netIdsToKeep.Contains(pair.Key))
				{
					_identityToDestroy.Add(pair.Key);
				}
			}

			DestroyForClientLockstep(_identityToDestroy);
		}

		private static void UnspawnForServerLockstep<T>(T identityList) where T : IEnumerable<uint>
		{
			_identityToDestroy.Clear();

			foreach (var pair in NetworkServer.spawned)
			{
				if (pair.Value.useRollback && !identityList.Contains(pair.Key))
				{
					_identityToDestroy.Add(pair.Key);
				}
			}

			DestroyForServerLockstep(_identityToDestroy);
		}

		#endregion

		#region - DestroyForLockstep

		private static void DestroyForClientLockstep<T>(T netIdsToDestroy) where T : IEnumerable<uint>
		{
			//Clean scene by destroying and disabling not needed netId
			foreach (uint netId in netIdsToDestroy)
			{
				if (useDebug)
				{
					Debug.Log("Destroy - netId: " + netId);
				}

				NetworkClient.DestroyObject(netId);
			}
		}

		private static void DestroyForServerLockstep<T>(T netIdsToDestroy) where T : IEnumerable<uint>
		{
			//Clean scene by destroying and disabling not needed netId
			foreach (uint netId in netIdsToDestroy)
			{
				if (useDebug)
				{
					Debug.Log("Remove: " + netId);
				}

				NetworkServer.spawned.TryGetValue(netId, out var value);

				//DestroyObject(value, NetworkServer.DestroyMode.Destroy);
				NetworkServer.Destroy(value.gameObject);
			}
		}

		#endregion

		#region - SpawnForLockstep

		public static void SpawnForClientLockstep<T>(IEnumerable<T> objectDataList, List<NetworkIdentity> newNetIdList) where T : ObjectData
		{
			NetworkIdentity identity;

			//Spawn all new netId
			foreach (var item in objectDataList)
			{
				if (NetworkClient.spawned.TryGetValue(item.Message.netId, out identity))
				{
					//Debug.LogWarning("Old netId: " + item.Message.netId + " | " + identity.name, identity);

					if (item.Message.assetId != 0)
					{
						identity.assetId = item.Message.assetId;
					}

					if (!identity.gameObject.activeSelf)
					{
						identity.gameObject.SetActive(true);
					}
					continue;
				}

				if (item.Message.assetId == 0 && item.Message.sceneId == 0)
				{
					Debug.LogError($"OnSpawn message with netId '{item.Message.netId}' has no AssetId or sceneId");
					continue;
				}

				identity = item.Message.sceneId == 0 ?
					NetworkClient.SpawnPrefab(item.Message) : NetworkClient.SpawnSceneObject(item.Message.sceneId);

				if (identity == null)
				{
					continue;
				}

				//Debug.LogWarning("New netId: " + item.Message.netId + " | " + identity.name, identity);

				if (item.Message.assetId != 0)
				{
					identity.assetId = item.Message.assetId;
				}

				if (!identity.gameObject.activeSelf)
				{
					identity.gameObject.SetActive(true);
				}

				// apply local values for VR support
				identity.transform.localPosition = item.Message.position;
				identity.transform.localRotation = item.Message.rotation;
				identity.transform.localScale = item.Message.scale;
				identity.isOwned = item.Message.isOwner;
				identity.netId = item.Message.netId;

				if (item.Message.isLocalPlayer)
				{
					NetworkClient.InternalAddPlayer(identity);
				}

				NetworkClient.spawned[item.Message.netId] = identity;

				if (identity.isOwned)
				{
					NetworkClient.connection?.owned.Add(identity);
				}

				newNetIdList.Add(identity);
			}
		}

		#endregion

		#region - PayloadForLockstep

		private static void PayloadForClientLockstep<T>(IEnumerable<T> objectDataList) where T : ObjectData
		{
			NetworkIdentity identity;

			//Apply payload on all netId
			foreach (var item in objectDataList)
			{
				if (item.Message.payload.Count > 0)
				{
					using (NetworkReaderPooled payloadReader = NetworkReaderPool.Get(item.Message.payload))
					{
						identity = NetworkClient.spawned[item.Message.netId];
						identity.DeserializeClient(payloadReader, true);
					}
				}
			}
		}

		#endregion

		#region - BootstrapForLockstep

		private static void BootstrapForClientLockstep(IEnumerable<NetworkIdentity> identities)
		{
			//Call OnStart on all the news netID
			foreach (var identity in identities)
			{
				NetworkClient.BootstrapIdentity(identity);
			}
		}

		#endregion

		#endregion

		#region CreatePresentLockStep

		public static void CreatePresentLockStep(Lockstep newLockstep)
		{
			foreach (NetworkIdentity identity in NetworkClient.spawned.Values)
			{
				if (identity.useRollback)
				{
					newLockstep.Add(identity);
				}
			}

			if (useDebug)
			{
				Debug.Log("CreatePresentLockStep - size: " + newLockstep.objectDataDict.Count);
			}
		}

		#endregion

		#region Broadcast

		public static void Broadcast()
		{
			NetworkServer.connectionsCopy.Clear();
			NetworkServer.connections.Values.CopyTo(NetworkServer.connectionsCopy);

			// go through all connections
			foreach (var connection in NetworkServer.connectionsCopy)
			{
				// has this connection joined the world yet?
				// for each READY connection:
				//   pull in UpdateVarsMessage for each entity it observes
				if (connection.isReady)
				{
					// send time for snapshot interpolation every sendInterval.
					// BroadcastToConnection() may not send if nothing is new.
					//
					// sent over unreliable.
					// NetworkTime / Transform both use unreliable.
					//
					// make sure Broadcast() is only called every sendInterval,
					// even if targetFrameRate isn't set in host mode (!)
					// (done via AccurateInterval)
					connection.Send(new TimeSnapshotMessage(), Channels.Unreliable);

					// broadcast world state to this connection
					BroadcastToConnection(connection);
				}

				// update connection to flush out batched messages
				connection.Update();
			}

			_broadcastLockstep = false;
		}

		private static readonly List<NetworkIdentity> _networkIdentities = new List<NetworkIdentity>();

		private static void BroadcastToConnection(NetworkConnectionToClient conn)
		{
			// Check for null, because object could have been spawn
			// and then destroy before sending a single message
			for (int i = conn.newObserving.Count - 1; i >= 0; i--)
			{
				if (conn.newObserving[i] == null)
				{
					conn.newObserving.RemoveAt(i);
				}
			}

			bool spawn = conn.newObserving.Count > 0;

			if (spawn)
			{
				if (useDebug)
				{
					Debug.Log("BroadcastToConnection conn: " + conn.connectionId
					+ " | Count: " + conn.newObserving.Count
					+ " | rollbackState: " + conn.rollbackState
					+ " | isFirstSpawn: " + conn.isFirstSpawn
					+ " | broadcastLockstep: " + _broadcastLockstep);
				}

				if (conn.isFirstSpawn)
				{
					conn.Send(new ObjectSpawnStartedMessage());
				}

				if (conn.rollbackState == RollbackState.Observing)
				{
					foreach (var identity in conn.newObserving)
					{
						if (conn.connectionId != 0 && identity.useRollback && !_broadcastLockstep)
						{
							continue;
						}
						NetworkServer.SendSpawnMessage(identity, conn);
						_networkIdentities.Add(identity);
					}
				}
				else
				{
					foreach (var identity in conn.newObserving)
					{
						NetworkServer.SendSpawnMessage(identity, conn);
					}
				}

				if (conn.isFirstSpawn)
				{
					conn.Send(new ObjectSpawnFinishedMessage());
				}
			}

			conn.isFirstSpawn = false;

			// for each entity that this connection is seeing
			foreach (var identity in conn.observing)
			{
				// make sure it's not null or destroyed.
				// (which can happen if someone uses
				//  GameObject.Destroy instead of
				//  NetworkServer.Destroy)
				if (identity != null)
				{
					if (identity.useRollback)
					{
						if (!_broadcastLockstep)
						{
							continue;
						}

						if (rollbackMode == RollbackMode.SendFullData)
						{
							NetworkServer.SendSpawnMessage(identity, conn);
							continue;
						}
					}

					// get serialization for this entity viewed by this connection
					// (if anything was serialized this time)
					var serialization = NetworkServer.SerializeForConnection(identity, conn);
					if (serialization != null)
					{
						var message = new EntityStateMessage
						{
							netId = identity.netId,
							payload = serialization.ToArraySegment()
						};

						conn.Send(message);
					}
				}
				// spawned list should have no null entries because we
				// always call Remove in OnObjectDestroy everywhere.
				// if it does have null then someone used
				// GameObject.Destroy instead of NetworkServer.Destroy.
				else
				{
					Debug.LogWarning("Found 'null' entry in observing list for connectionId=" + conn.connectionId + ". Please call NetworkServer.Destroy to destroy networked objects. Don't use GameObject.Destroy.");
				}
			}

			if (spawn)
			{
				if (conn.rollbackState == RollbackState.Observing)
				{
					foreach (var identity in _networkIdentities)
					{
						conn.observing.Add(identity);
						conn.newObserving.Remove(identity);
					}

					_networkIdentities.Clear();
				}
				else
				{
					foreach (var identity in conn.newObserving)
					{
						conn.observing.Add(identity);
					}

					conn.newObserving.Clear();
				}
			}

			if (conn.rollbackState == RollbackState.ReadyToObserve)
			{
				conn.rollbackState = RollbackState.Observing;
				conn.Send(new EndLockstepMessage());
			}
			else if (_broadcastLockstep && conn.rollbackState == RollbackState.Observing)
			{
				conn.Send(new EndLockstepMessage());
			}
		}

		#endregion

	}

}
