using System.Collections;
using System.Collections.Generic;
using Mirror;
using TickPhysics;
using UnityEngine;

public class NetList : NetworkBehaviour
{
	[SerializeField]
	private NetworkBehaviour _prefab;

	[SyncVar]
	public List<NetworkBehaviour> listBalls = new();

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.M))
		{
			var netGo = Instantiate(_prefab);
			Rollback.Spawn(netGo);
			listBalls.Add(netGo);
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			if (listBalls.Count > 0)
			{
				var netGo = listBalls[0];
				listBalls.RemoveAt(0);
				Rollback.Destroy(netGo.gameObject);
			}
		}
	}

}
