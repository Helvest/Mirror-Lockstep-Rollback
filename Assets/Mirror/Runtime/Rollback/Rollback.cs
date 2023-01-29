using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

//Add to NetworkConnection
//public RollbackState rollbackState = RollbackState.NotObserving;
//public bool isFirstSpawn = true;
//public readonly List<NetworkIdentity> newObserving = new List<NetworkIdentity>();
//NetworkConnection.AddToObserving(NetworkIdentity netIdentity)
//Replace observing.Add(netIdentity);
//By newObserving.Add(netIdentity);
//remove NetworkServer.ShowForConnection(netIdentity, this);

//Add to NetworkIdentity
//public bool useRollback = false; 

//In NetworkServer.NetworkLateUpdate()
//Replace NetworkServer.Broadcast(); by Rollback.Broadcast();

//Add to the end of NetworkClient.NetworkEarlyUpdate()
//Rollback.OnClientNetworkEndOfEarlyUpdate();

//In NetworkClient.RegisterSystemHandlers(bool hostMode)
//Replace RegisterHandler<SpawnMessage>(OnSpawn);
//By RegisterHandler<SpawnMessage>(Rollback.OnSpawn);
//And
//Replace RegisterHandler<SpawnMessage>(OnEntityStateMessage);
//By RegisterHandler<SpawnMessage>(Rollback.OnEntityStateMessage);


namespace Mirror
{

	#region Enums

	public enum RollbackMode
	{
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

		private static Queue<Lockstep> _pool = new Queue<Lockstep>();

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

		public RollbackData rollbackData;

		public uint pastFrame;

		public readonly Dictionary<uint, ObjectData> ObjectDataDict = new Dictionary<uint, ObjectData>();

		public readonly Dictionary<uint, ObjectDeltaData> ObjectDeltaDataDict = new Dictionary<uint, ObjectDeltaData>();

		public Lockstep(RollbackData rollbackData)
		{
			this.rollbackData = rollbackData;
		}

		public void Reset()
		{
			ObjectDataDict.Clear();
			ObjectDeltaDataDict.Clear();
		}

		public void Clear()
		{
			if (rollbackData != null)
			{
				RollbackData.ReturnToPool(rollbackData);
				rollbackData = null;
			}

			ObjectDataDict.Clear();
			ObjectDeltaDataDict.Clear();
		}

		#region Add 

		public void Add(NetworkIdentity networkIdentity)
		{
			ObjectDataDict.Add(networkIdentity.netId, networkIdentity.GetObjectData());
		}

		#endregion

	}

	#endregion

	#region Memory

	public class ClientMemory
	{
		public readonly RollbackData rollbackData = new RollbackData();

		public uint firstFrame;

		public Lockstep present;

		public readonly SortedList<uint, Lockstep> futurs = new SortedList<uint, Lockstep>();

		public Lockstep inConstruction;

		public bool HaveFutur => futurs.Count > 0;

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

		#region Construction

		public void StartConstruction(RollbackData data)
		{
			inConstruction = Lockstep.GetFromPool(data);
		}

		public void FinishConstruction()
		{
			uint frame = inConstruction.rollbackData.fixedFrameCount;

			if (futurs.TryGetValue(frame, out var lockstep))
			{
				inConstruction.pastFrame = lockstep.pastFrame;

				lockstep.Clear();
			}

			futurs[frame] = inConstruction;
			inConstruction = null;
		}

		#endregion

		public void RemoveAllFutur()
		{
			foreach (var futur in futurs.Values)
			{
				Lockstep.ReturnToPool(futur);
			}

			futurs.Clear();
		}

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

		#region Constructor

		public RollbackData() { }

		public RollbackData(RollbackData rollbackData)
		{
			Copy(rollbackData);
		}

		public void Copy(RollbackData rollbackData)
		{
			isPhysicUpdated = rollbackData.isPhysicUpdated;
			timeAtSimulation = rollbackData.timeAtSimulation;
			normalTime = rollbackData.normalTime;
			fixedTime = rollbackData.fixedTime;
			fixedFrameCount = rollbackData.fixedFrameCount;
		}

		#endregion

	}

	#endregion

	public static class Rollback
	{

		public static RollbackMode rollbackMode = default;

		public static bool useDebug = false;

		#region NetworkMessage

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
		}

		public struct DeltaLockstepMessage : NetworkMessage
		{
			public double timeAtSimulation;
			public double normalTime;
			public double fixedTime;
			public uint pastFrame;
			public uint presentFrame;
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

		#region Lockstep

		public static readonly ClientMemory clientMemory = new ClientMemory();

		/*
		public static bool TryGetObjectData(NetworkIdentity networkIdentity, out ObjectData objectData)
		{
			if (clientMemory.IsEmpty)
			{
				Debug.LogWarning("Try Get Last Message but timeline is empty");
			}
			else
			{
				if (clientMemory.present.TryGetValue(networkIdentity.netId, out objectData))
				{
					return true;
				}
			}

			objectData = default;
			return false;
		}
		*/

		public static bool TryGetPresentLockstep(out Lockstep lockstep)
		{
			lockstep = clientMemory.present;

			return lockstep != null;
		}

		public static event Action<RollbackData> EventOnLockstepReceive;

		private static bool _isRecevingLockstep = false;

		public static void OnReceiveConfigLockstep(ConfigLockstepMessage message)
		{
			if (_isRecevingLockstep)
			{
				Debug.LogError("Receive new lockstep in between another lockstep");
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
				clientMemory.rollbackData.Copy(data);
			}

			if (message.pastFrame < clientMemory.firstFrame)
			{
				message.pastFrame = clientMemory.firstFrame;
			}

			clientMemory.inConstruction.pastFrame = message.pastFrame;
		}

		public static void OnReceiveDeltaLockstep(DeltaLockstepMessage message)
		{
			if (_isRecevingLockstep)
			{
				Debug.LogError("Receive new lockstep in between another lockstep");
			}

			_isRecevingLockstep = true;

			if (useDebug)
			{
				Debug.Log("DeltaLockstep receive: " + message.pastFrame + " => " + message.presentFrame);
			}

			var data = RollbackData.GetFromPool();

			data.isPhysicUpdated = clientMemory.rollbackData.isPhysicUpdated;
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
		}

		public static void OnReceiveFullLockstep(FullLockstepMessage message)
		{
			if (_isRecevingLockstep)
			{
				Debug.LogError("Receive new lockstep in between another lockstep");
			}

			_isRecevingLockstep = true;

			if (useDebug)
			{
				Debug.Log("FullLockstep receive: " + message.presentFrame);
			}

			var data = RollbackData.GetFromPool();

			data.isPhysicUpdated = clientMemory.rollbackData.isPhysicUpdated;
			data.timeAtSimulation = message.timeAtSimulation;
			data.normalTime = message.normalTime;
			data.fixedTime = message.fixedTime;
			data.fixedFrameCount = message.presentFrame;

			clientMemory.StartConstruction(data);
		}

		public static void OnReceiveEndLockstepMessage(EndLockstepMessage _)
		{
			if (_isRecevingLockstep)
			{
				if (useDebug)
				{
					Debug.Log("EndLockstepMessage");
				}

				_isRecevingLockstep = false;

				clientMemory.FinishConstruction();
			}
			else
			{
				Debug.LogError("EndLockstepMessage");
			}
		}

		public static Action<uint, Lockstep> EventLockstepFinish;

		#endregion

		#region Get

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

		#region NetworkClient

		#region SpawnMessage

		public static void OnSpawn(SpawnMessage message)
		{
			if (_isRecevingLockstep)
			{
				bool save = false;

				//Try get spawned netIdentity
				if (NetworkClient.spawned.TryGetValue(message.netId, out var netIdentity))
				{
					save = netIdentity.useRollback;
				}
				//Try get prefab netIdentity
				else if (NetworkClient.GetPrefab(message.assetId, out var gameObject))
				{
					if (gameObject.TryGetComponent(out netIdentity))
					{
						save = netIdentity.useRollback;
					}
				}

				if (save)
				{
					var objectData = new ObjectData(message);

					clientMemory.inConstruction.ObjectDataDict.Add(message.netId, objectData);
					return;
				}
			}

			NetworkClient.OnSpawn(message);
		}

		public static void ApplySpawn(SpawnMessage message)
		{
			if (NetworkClient.spawned.TryGetValue(message.netId, out var identity))
			{
				ApplySpawnPayloadOnClient(identity, message);
			}
			else
			{
				if (message.assetId == Guid.Empty && message.sceneId == 0)
				{
					Debug.LogError($"OnSpawn message with netId '{message.netId}' has no AssetId or sceneId");
					return;
				}

				identity = message.sceneId == 0 ? NetworkClient.SpawnPrefab(message) : NetworkClient.SpawnSceneObject(message.sceneId);

				NetworkClient.ApplySpawnPayload(identity, message);
			}
		}

		public static void ApplySpawnPayloadOnClient(NetworkIdentity identity, SpawnMessage message)
		{
			if (message.assetId != Guid.Empty)
			{
				identity.assetId = message.assetId;
			}

			if (!identity.gameObject.activeSelf)
			{
				identity.gameObject.SetActive(true);
			}

			// deserialize components if any payload
			// (Count is 0 if there were no components)
			if (message.payload.Count > 0)
			{
				using var payloadReader = NetworkReaderPool.Get(message.payload);
				identity.OnDeserializeAllSafely(reader: payloadReader, initialState: true);
			}
		}

		#endregion

		#region EntityStateMessage

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
				//si null, on va supposer que c'est un nouveau rollback object
				if (identity == null || identity.useRollback)
				{
					var objectDeltaData = new ObjectDeltaData(message);
					clientMemory.inConstruction.ObjectDeltaDataDict.Add(message.netId, objectDeltaData);
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
				identity.OnDeserializeAllSafely(networkReader, false);
			}
		}

		#endregion

		#region NetworkEarlyUpdate

		public static void OnClientNetworkEndOfEarlyUpdate()
		{
			if (!clientMemory.HaveFutur)
			{
				return;
			}

			Lockstep lastFutur = null;

			if (rollbackMode == RollbackMode.SendDeltaData)
			{
				bool needToResetScene = true;

				bool needDebug = clientMemory.futurs.Count > 1;

				uint presentFrame = clientMemory.rollbackData.fixedFrameCount;

				foreach (var data in clientMemory.futurs)
				{
					var futur = data.Value;

					//if is not next delta or not completed, we wait for next frame
					if (futur.pastFrame != presentFrame)
					{
						break;
					}

					lastFutur = futur;

					if (needToResetScene)
					{
						needToResetScene = false;

						//Callback: apply past
						if (clientMemory.present != null)
						{
							PrepareClientSceneForLockstep(clientMemory.present.ObjectDataDict.Keys);
							GoToLockstep(clientMemory.present);
						}
					}

					if (useDebug)
					{
						Debug.Log("Apply: " + presentFrame + " => " + futur.rollbackData.fixedFrameCount);
					}

					foreach (var objectData in futur.ObjectDataDict.Values)
					{
						ApplySpawn(objectData.Message);
					}

					foreach (var objectDeltaData in futur.ObjectDeltaDataDict.Values)
					{
						ApplyEntityState(objectDeltaData.Message);
					}

					presentFrame = futur.rollbackData.fixedFrameCount;
				}

				if (lastFutur != null)
				{
					clientMemory.rollbackData.Copy(lastFutur.rollbackData);

					//Save new present
					if (clientMemory.present == null)
					{
						clientMemory.present = lastFutur;
						clientMemory.futurs.Remove(lastFutur.rollbackData.fixedFrameCount);
					}
					else
					{
						clientMemory.present.Reset();
						clientMemory.present.rollbackData.Copy(lastFutur.rollbackData);

						CreatePresentLockStep(clientMemory.present);
					}

					EventOnLockstepReceive?.Invoke(clientMemory.rollbackData);
				}
			}
			else
			{
				lastFutur = clientMemory.futurs.Last().Value;

				PrepareClientSceneForLockstep(lastFutur.ObjectDataDict.Keys);

				clientMemory.rollbackData.Copy(lastFutur.rollbackData);
				EventOnLockstepReceive?.Invoke(clientMemory.rollbackData);

				foreach (var objectData in lastFutur.ObjectDataDict.Values)
				{
					ApplySpawn(objectData.Message);
				}
			}

			clientMemory.RemoveAllFutur();
		}

		private static readonly List<uint> _identityToRemove = new List<uint>();

		private static void PrepareClientSceneForLockstep<T>(T identityList) where T : IEnumerable<uint>
		{
			_identityToRemove.Clear();

			foreach (var pair in NetworkClient.spawned)
			{
				if (pair.Value.useRollback && !identityList.Contains(pair.Key))
				{
					_identityToRemove.Add(pair.Key);
				}
			}

			//Clean scene by destroying not needed netId
			foreach (uint netId in _identityToRemove)
			{
				if (NetworkClient.spawned.TryGetValue(netId, out var identity))
				{
					identity.OnStopClient();
					NetworkClient.InvokeUnSpawnHandler(identity.assetId, identity.gameObject);
					Object.Destroy(identity.gameObject);
					NetworkClient.spawned.Remove(netId);
				}
			}
		}

		public static void PrepareServerSceneForLockstep<T>(T identityList) where T : IEnumerable<uint>
		{
			_identityToRemove.Clear();

			foreach (var pair in NetworkServer.spawned)
			{
				if (pair.Value.useRollback && !identityList.Contains(pair.Key))
				{
					_identityToRemove.Add(pair.Key);
				}
			}

			//Clean scene by destroying not needed netId
			foreach (uint item in _identityToRemove)
			{
				if (NetworkServer.spawned.TryGetValue(item, out var identity))
				{
					NetworkServer.Destroy(identity.gameObject);
				}
			}
		}

		public static void GoToLockstep(Lockstep lockstep)
		{
			//Apply or spawn
			foreach (var objectData in lockstep.ObjectDataDict.Values)
			{
				ApplySpawn(objectData.Message);
			}
		}

		public static void CreatePresentLockStep(Lockstep newLockstep)
		{
			if (useDebug)
			{
				Debug.Log("CreatePresentLockStep");
			}

			foreach (var identity in NetworkClient.spawned.Values)
			{
				if (identity.useRollback)
				{
					newLockstep.Add(identity);
				}
			}
		}

		#endregion

		#endregion

		#region SendLockstepMessage

		private static bool _broadcastLockstep = false;

		public static void BroacastNextLockstep()
		{
			_broadcastLockstep = true;
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
					// broadcast world state to this connection
					BroadcastToConnection(connection);
				}

				// update connection to flush out batched messages
				connection.Update();
			}

			_broadcastLockstep = false;
		}

		private static readonly List<NetworkIdentity> _networkIdentities = new List<NetworkIdentity>();

		private static void BroadcastToConnection(NetworkConnectionToClient connection)
		{
			// Check for null, because object could have been spawn
			// and then destroy even before sending a single message
			for (int i = connection.newObserving.Count - 1; i >= 0; i--)
			{
				if (connection.newObserving[i] == null)
				{
					connection.newObserving.RemoveAt(i);
				}
			}

			bool spawn = connection.newObserving.Count > 0;

			if (spawn)
			{
				if (connection.isFirstSpawn)
				{
					connection.Send(new ObjectSpawnStartedMessage());
				}

				if (connection.rollbackState == RollbackState.Observing)
				{
					foreach (var identity in connection.newObserving)
					{
						if (identity.useRollback && !_broadcastLockstep)
						{
							continue;
						}

						NetworkServer.SendSpawnMessage(identity, connection);
						_networkIdentities.Add(identity);
					}
				}
				else
				{
					foreach (var identity in connection.newObserving)
					{
						NetworkServer.SendSpawnMessage(identity, connection);
					}
				}

				if (connection.isFirstSpawn)
				{
					connection.Send(new ObjectSpawnFinishedMessage());
				}
			}

			connection.isFirstSpawn = false;

			// for each entity that this connection is seeing
			foreach (var identity in connection.observing)
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
							NetworkServer.SendSpawnMessage(identity, connection);
							continue;
						}
					}

					// get serialization for this entity viewed by this connection
					// (if anything was serialized this time)
					var serialization = NetworkServer.GetEntitySerializationForConnection(identity, connection);
					if (serialization != null)
					{
						var message = new EntityStateMessage
						{
							netId = identity.netId,
							payload = serialization.ToArraySegment()
						};

						connection.Send(message);
					}


					// clear dirty bits only for the components that we serialized
					// DO NOT clean ALL component's dirty bits, because
					// components can have different syncIntervals and we don't
					// want to reset dirty bits for the ones that were not
					// synced yet.
					// (we serialized only the IsDirty() components, or all of
					//  them if initialState. clearing the dirty ones is enough.)
					//
					// NOTE: this is what we did before push->pull
					//       broadcasting. let's keep doing this for
					//       feature parity to not break anyone's project.
					//       TODO make this more simple / unnecessary later.
					identity.ClearDirtyComponentsDirtyBits();
				}
				// spawned list should have no null entries because we
				// always call Remove in OnObjectDestroy everywhere.
				// if it does have null then someone used
				// GameObject.Destroy instead of NetworkServer.Destroy.
				else
				{
					Debug.LogWarning("Found 'null' entry in observing list for connectionId=" + connection.connectionId + ". Please call NetworkServer.Destroy to destroy networked objects. Don't use GameObject.Destroy.");
				}
			}

			if (spawn)
			{
				if (connection.rollbackState == RollbackState.Observing)
				{
					foreach (var identity in _networkIdentities)
					{
						connection.observing.Add(identity);
						connection.newObserving.Remove(identity);
					}

					_networkIdentities.Clear();
				}
				else
				{
					foreach (var identity in connection.newObserving)
					{
						connection.observing.Add(identity);
					}

					connection.newObserving.Clear();
				}

				if (connection.rollbackState == RollbackState.ReadyToObserve)
				{
					connection.rollbackState = RollbackState.Observing;
					connection.Send(new EndLockstepMessage());
				}
				else if (_broadcastLockstep && connection.rollbackState == RollbackState.Observing)
				{
					connection.Send(new EndLockstepMessage());
				}
			}
			else if (_broadcastLockstep && connection.rollbackState == RollbackState.Observing)
			{
				connection.Send(new EndLockstepMessage());
			}
		}

		#endregion

	}

}
