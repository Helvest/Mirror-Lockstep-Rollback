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

	//private readonly Timeline _serverRecording = new Timeline();

	private void OnLockStepIsFinish(uint frame, Lockstep lockstep)
	{
		//_serverRecording.Add(frame, lockstep);
	}
}
