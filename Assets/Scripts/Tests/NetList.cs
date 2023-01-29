using System.Collections;
using System.Collections.Generic;
using Mirror;
using TickPhysics;
using UnityEngine;

public class NetList : NetworkBehaviour
{
	[SerializeField]
	private NetworkBehaviour _prefab = default;

	[SyncVar]
	public List<NetworkBehaviour> listBalls = new();

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.M))
		{
			var netGo = Instantiate(_prefab);

			if (isServer)
			{
				NetworkServer.Spawn(netGo.gameObject);
			}
			else
			{
				uint fakeID = uint.MaxValue - (uint)NetworkClient.spawned.Count;

				NetworkClient.spawned.Add(fakeID, netGo.netIdentity);
			}

			listBalls.Add(netGo);
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			if (listBalls.Count > 0)
			{
				var netGo = listBalls[0];
				if (isServer)
				{
					listBalls.RemoveAt(0);
					NetworkServer.Destroy(netGo.gameObject);
				}
				else
				{
					listBalls.RemoveAt(0);
					netGo.gameObject.SetActive(false);
				}
			}
		}
	}

}
