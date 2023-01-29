using System.Collections;
using System.Collections.Generic;
using Mirror;
using TickPhysics;
using UnityEngine;

public class TestTrigger : MonoBehaviour
{

	protected ITickSystem tickSystem = default;

	private void Start()
	{
		SL.TryGetOrFindInterface(out tickSystem);
	}

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log("OnTriggerEnter: " + tickSystem.FixedFrameCount);
	}

	private void OnTriggerExit(Collider other)
	{
		//Debug.Log("OnTriggerExit: " + tickSystem.FixedFrameCount);
	}

	private void OnTriggerStay(Collider other)
	{
		//Debug.Log("OnTriggerStay: " + tickSystem.FixedFrameCount);
	}

	private void OnCollisionEnter(Collision collision)
	{
		//Debug.Log("OnCollisionEnter: " + tickSystem.FixedFrameCount);
	}

	private void OnCollisionExit(Collision collision)
	{
		//Debug.Log("OnCollisionExit: " + tickSystem.FixedFrameCount);
	}

	private void OnCollisionStay(Collision collision)
	{
		//Debug.Log("OnCollisionStay: " + tickSystem.FixedFrameCount);
	}

}
