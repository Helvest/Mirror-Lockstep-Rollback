using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RecoderManager : MonoBehaviour
{

	#region Init

	private void OnEnable()
	{
		SL.Add(this);

		Rollback.EventLockstepFinish += OnLockStepIsFinish;
	}

	private void OnDisable()
	{
		SL.Remove(this);

		Rollback.EventLockstepFinish -= OnLockStepIsFinish;
	}

	#endregion

	private readonly SortedList<uint, Lockstep> _serverRecording = new();

	private void OnLockStepIsFinish(uint frame, Lockstep lockstep)
	{
		_serverRecording.Add(frame, new Lockstep(lockstep));
	}
}
