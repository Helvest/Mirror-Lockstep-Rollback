﻿using System;
using System.Collections.Generic;
using Mirror;
using TickPhysics;
using UnityEngine;

[Serializable]
public abstract class NetTickSystem : TickSystem, INetTickSystem
{

	#region Fields

	public bool StateWasReceive { get; set; } = false;

	public bool SendConfigMessage { get; set; } = false;

	protected uint pastLockstep = 0;

	public override bool IsPhysicUpdated
	{
		get => base.IsPhysicUpdated;

		set
		{
			if (base.IsPhysicUpdated == value)
			{
				return;
			}

			SendConfigMessage = true;
			base.IsPhysicUpdated = value;
		}
	}

	[SerializeField, Range(0f, 10f)]
	private float _sendTimeBetweenMessage = 1f;

	public float SendTimeBetweenMessage
	{
		get => _sendTimeBetweenMessage;
		//There is not point sending messages faster than the simulation
		set => _sendTimeBetweenMessage = value < Time.fixedDeltaTime ? Time.fixedDeltaTime : value;
	}

	private float _nextSendTime = 0;

	private uint _lastFrameSend = 0;

	public bool useDebug = false;

	#endregion

	#region Clear

	public virtual void Clear()
	{
		StateWasReceive = false;
		SendConfigMessage = false;
		pastLockstep = 0;

		TimeAtSimulation = 0;
		NormalTime = 0;
		FixedTime = 0;
		FixedFrameCount = 0;

		SendTimeBetweenMessage = _sendTimeBetweenMessage;

		_nextSendTime = 0;
	}

	#endregion

	#region Tick

	public override void Tick(double time, double deltaTime, double fixedDeltaTime)
	{
		ReadInput();

		if (IsPhysicUpdated)
		{
			TimeAtSimulation = time;

			if (!StateWasReceive)
			{
				NormalTime += deltaTime;
			}

			PhysicTick(deltaTime, fixedDeltaTime);

			// Here you can access the transforms state right after the simulation, if needed...	
			UpdateGraphic();
		}

		StateWasReceive = false;
	}

	#endregion

	#region RollbackData

	public void OnRollbackData(RollbackData rollbackData)
	{
		StateWasReceive = true;

		IsPhysicUpdated = rollbackData.isPhysicUpdated;

		if (IsPhysicUpdated)
		{
			double decalTime = NetworkTime.time - rollbackData.timeAtSimulation;

			if (useDebug)
			{
				Debug.Log("OnRollbackData - DecalTime: " + decalTime);
			}

			NormalTime = rollbackData.normalTime + decalTime;
		}
		else
		{
			NormalTime = rollbackData.normalTime;
		}

		FixedTime = rollbackData.fixedTime;

		FixedFrameCount = rollbackData.fixedFrameCount;
	}

	#endregion

	#region Lockstep Messages

	public void SendConfigLockstepMessage<T1>(IEnumerable<T1> connList, bool isFirst) where T1 : NetworkConnection
	{
		var lockstepMessage = new Rollback.ConfigLockstepMessage()
		{
			isFirst = isFirst,
			rollbackMode = Rollback.rollbackMode,
			isPhysicUpdated = IsPhysicUpdated,
			timeAtSimulation = TimeAtSimulation,
			normalTime = NormalTime,
			fixedTime = FixedTime,
			pastFrame = pastLockstep,
			presentFrame = FixedFrameCount
		};

		if (useDebug)
		{
			Debug.Log("SendConfigLockstepMessage - isFirst: " + isFirst
				+ " | isPhysicUpdated: " + lockstepMessage.isPhysicUpdated);
		}

		SendMessage(connList, lockstepMessage);

		if (!isFirst)
		{
			Rollback.BroacastNextLockstep();
		}
	}

	public void SendDeltaLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnection
	{
		var lockstepMessage = new Rollback.DeltaLockstepMessage()
		{
			timeAtSimulation = TimeAtSimulation,
			normalTime = NormalTime,
			fixedTime = FixedTime,
			pastFrame = pastLockstep,
			presentFrame = FixedFrameCount
		};

		if (useDebug)
		{
			Debug.Log("SendDeltaLockstepMessage: " + lockstepMessage.presentFrame);
		}

		SendMessage(connList, lockstepMessage);
		Rollback.BroacastNextLockstep();
	}

	public void SendFullLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnection
	{
		var lockstepMessage = new Rollback.FullLockstepMessage()
		{
			timeAtSimulation = TimeAtSimulation,
			normalTime = NormalTime,
			fixedTime = FixedTime,
			presentFrame = FixedFrameCount
		};

		if (useDebug)
		{
			Debug.Log("SendFullLockstepMessage: " + lockstepMessage.presentFrame);
		}

		SendMessage(connList, lockstepMessage);
		Rollback.BroacastNextLockstep();
	}

	public void OnFinishSendLockstepMessage()
	{
		pastLockstep = FixedFrameCount;

		if (useDebug)
		{
			Debug.Log("OnFinishSendLockstepMessage: " + FixedFrameCount);
		}
	}

	#endregion

	#region ServerUpdate

	public bool TryGetRollbackConnections(out List<NetworkConnectionToClient> rollbackConnections, RollbackState rollbackState)
	{
		rollbackConnections = new List<NetworkConnectionToClient>();

		foreach (var conn in NetworkServer.connections.Values)
		{
			if (conn.connectionId != 0 && conn.rollbackState == rollbackState)
			{
				rollbackConnections.Add(conn);
			}
		}

		return rollbackConnections.Count > 0;
	}

	public void ServerUpdate()
	{
		//Auto: Server send spawn and delta lockstep to all connections
		if (SendConfigMessage)
		{
			SendConfigMessage = false;

			if (TryGetRollbackConnections(out var rollbackConnections, RollbackState.Observing))
			{
				SendConfigLockstepMessage(rollbackConnections, false);
				_lastFrameSend = FixedFrameCount;
				OnFinishSendLockstepMessage();
			}

			_nextSendTime = (float)FixedTime + _sendTimeBetweenMessage;
			return;
		}

		//Check if enough time is spent for a lockstep
		if (FixedTime > _nextSendTime)
		{
			//Check if the physic frame has changed since the last send
			if (_lastFrameSend != FixedFrameCount)
			{
				if (TryGetRollbackConnections(out var rollbackConnections, RollbackState.Observing))
				{
					//Send lockstep to clients
					switch (Rollback.rollbackMode)
					{
						case RollbackMode.SendFullData:
						{
							SendFullLockstepMessage(rollbackConnections);
							OnFinishSendLockstepMessage();
							break;
						}

						case RollbackMode.SendDeltaData:
						{
							SendDeltaLockstepMessage(rollbackConnections);
							break;
						}
					}

					_lastFrameSend = FixedFrameCount;
					OnFinishSendLockstepMessage();
				}

				_nextSendTime = (float)FixedTime + _sendTimeBetweenMessage;
			}
		}
	}

	#endregion

	#region SendMessages

	public static void SendMessage<T1, T2>(IEnumerable<T1> connList, T2 lockstepMessage, int channelId = Channels.Reliable)
	where T1 : NetworkConnection
	where T2 : struct, NetworkMessage
	{
		foreach (var conn in connList)
		{
			conn.Send(lockstepMessage, channelId);
		}
	}

	#endregion

}
