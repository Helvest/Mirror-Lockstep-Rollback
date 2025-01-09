using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using static Mirror.Rollback;

[Serializable]
public class NetTickSystem3D : TickSystem3D, INetTickSystem
{

	#region Fields

	public enum WhenToApplyRollback
	{
		OnPhysicTickOneFrameLate,
		OnTick
	}

	public WhenToApplyRollback whenToApplyRollback = WhenToApplyRollback.OnPhysicTickOneFrameLate;

	public bool StateWasReceive { get; set; }

	//public bool StateWasRollback { get; set; } = false;

	public bool SendConfigMessage { get; set; }

	protected uint pastLockstep;

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

	private float _nextSendTime;

	private uint _lastFrameSend;

	public bool useDebug;

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

			PhysicTick(fixedDeltaTime);

			CalculateExtraDeltaTime();

			// Here you can access the transforms state right after the simulation, if needed...	
			UpdateGraphic();

			IsInUpdateLoop = false;
		}
		else
		{
			//apply last rollback
			Rollback.TryResolveClientLastRollback();
		}

		StateWasReceive = false;
	}

	#endregion

	protected override void PhysicTick(double fixedDeltaTime)
	{
		if (fixedDeltaTime <= 0)
		{
			return;
		}

		//var decalTime = NormalTime - FixedTime;

		var newFrame = (uint)(NormalTime / fixedDeltaTime);

		if (whenToApplyRollback == WhenToApplyRollback.OnPhysicTickOneFrameLate)
		{
			if (newFrame > FixedFrameCount)
			{
				Rollback.TryResolveClientRollback(newFrame - 1u);
			}
		}
		else
		{
			Rollback.TryResolveClientRollback(newFrame);
		}

		// Catch up with the game time.
		// Advance the physics simulation by portions of fixedDeltaTime
		while (NormalTime >= FixedTime + fixedDeltaTime)
		{
			FixedFrameCount++;

			FixedTime += fixedDeltaTime;

			//A custom FixedUpdate
			UpdatePhysic();

			//Advance the simulation, this will also call OnTrigger and OnCollider
			SimulatePhysic(fixedDeltaTime);

			//Prepare inputs for next physic frame
			ProcessInput();
		}
	}

	#region RollbackData

	public void OnLockstepReceive(RollbackData rollbackData)
	{
		if (!IsPhysicUpdated)
		{
			IsPhysicUpdated = rollbackData.isPhysicUpdated;
		}

		if (IsPhysicUpdated)	
		{
			StateWasReceive = true;

			double decalTime = NetworkTime.time - rollbackData.timeAtSimulation;
				
			if (useDebug)
			{
				Debug.Log("OnLockstepReceive - DecalTime: " + decalTime);
			}

			NormalTime = rollbackData.normalTime + decalTime;		
		}
	}

	public void OnRollbackResolve(RollbackData rollbackData)
	{
		//StateWasRollback = true;

		IsPhysicUpdated = rollbackData.isPhysicUpdated;

		//NormalTime = rollbackData.normalTime;

		FixedTime = rollbackData.fixedTime;

		FixedFrameCount = rollbackData.fixedFrameCount;
	}

	#endregion

	#region Lockstep Messages

	public void SendConfigLockstepMessage<T1>(IEnumerable<T1> connList, bool isFirst) where T1 : NetworkConnection
	{
		var lockstepMessage = new ConfigLockstepMessage()
		{
			isFirst = isFirst,
			isPhysicUpdated = IsPhysicUpdated,
			timeAtSimulation = TimeAtSimulation,
			normalTime = NormalTime,
			fixedTime = FixedTime,
			pastFrame = pastLockstep,
			presentFrame = FixedFrameCount
		};

		Rollback.SendConfigLockstepMessage(lockstepMessage, connList);
	}

	public void SendDeltaLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnection
	{
		var lockstepMessage = new DeltaLockstepMessage()
		{
			timeAtSimulation = TimeAtSimulation,
			normalTime = NormalTime,
			fixedTime = FixedTime,
			pastFrame = pastLockstep,
			presentFrame = FixedFrameCount
		};

		Rollback.SendDeltaLockstepMessage(lockstepMessage, connList);
	}

	public void SendFullLockstepMessage<T1>(IEnumerable<T1> connList) where T1 : NetworkConnection
	{
		var lockstepMessage = new FullLockstepMessage()
		{
			timeAtSimulation = TimeAtSimulation,
			normalTime = NormalTime,
			fixedTime = FixedTime,
			presentFrame = FixedFrameCount
		};

		Rollback.SendFullLockstepMessage(lockstepMessage, connList);
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
					switch (rollbackMode)
					{
						case RollbackMode.SendFullData:
						{
							SendFullLockstepMessage(rollbackConnections);
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

}
