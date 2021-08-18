using System.Collections.Generic;
using Mirror;
using UnityEngine;
using TickPhysics;

public class NetTickManager3D : TickManager3D
{

	#region Variables

	public bool StateWasReceive { get; set; } = false;

	protected uint pastLockstep = 0;

	public override bool IsPhysicUpdated
	{
		get
		{
			return base.IsPhysicUpdated;
		}

		set
		{
			if (base.IsPhysicUpdated == value)
			{
				return;
			}

			Rollback.sendConfigMessage = true;
			base.IsPhysicUpdated = value;
		}
	}

	#endregion

	#region OnEnable And OnDisable

	protected override void OnEnable()
	{
		if (!SL.AddOrDestroy(this))
		{
			return;
		}

		Clear();

		AutoSimulation = _autoSimulation;

		Rollback.EventOnLockstepReceive += OnRollbackData;
	}

	protected virtual void OnDisable()
	{
		SL.Remove(this);

		Rollback.EventOnLockstepReceive -= OnRollbackData;
	}

	#endregion

	#region Clear

	public virtual void Clear()
	{
		StateWasReceive = false;
		IsPhysicUpdated = false;
		TimeAtSimulation = 0;
		NormalTime = 0;
		FixedTime = 0;
		pastLockstep = 0;
		FixedFrameCount = 0;
		Rollback.sendConfigMessage = false;
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
			var decalTime = NetworkTime.time - rollbackData.timeAtSimulation;

			if (_useDebug)
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

		if (isFirst)
		{
			Rollback.SendLockstepMessageToReadyToObserve(connList, lockstepMessage);
		}
		else
		{
			Rollback.SendLockstepMessageToObserving(connList, lockstepMessage);
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

		Rollback.SendLockstepMessageToObserving(connList, lockstepMessage);
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

		Rollback.SendLockstepMessageToObserving(connList, lockstepMessage);
	}

	public void OnFinishSendLockstepMessage()
	{
		pastLockstep = FixedFrameCount;
	}

	#endregion

	#region Debug

	[Header("Debug")]
	[SerializeField]
	private bool _useDebug = false;

	#endregion

}
