using Mirror;
using TickPhysics;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{

	#region Fields

	protected ITickSystem tickSystem = default;

	[SerializeField]
	private GameObject[] _pawnableArray;

	[SerializeField]
	private GameObject _ballPrefab;

	#endregion

	#region Init

	private void OnEnable()
	{
		SL.TryGetIfNull(ref tickSystem);
	}

	public override void OnStartServer()
	{
		CreateMatchScene();
	}

	#endregion

	#region CreateMatchScene

	private void CreateMatchScene()
	{
		foreach (var item in _pawnableArray)
		{
			var instance = Instantiate(item);
			NetworkServer.Spawn(instance);
		}
	}

	#endregion

	#region Update

	private void Update()
	{
		//auto: reception des messages
		//server: recois les inputs
		//client: reçois les inputs et le lockstep

		if (isClient)
		{
			//todo: enregistrer les inputs de cette frame avec un delay	
		}

		if (isServer)
		{
			//appliquer les inputs pour la frame
			if (Input.GetKeyDown(KeyCode.A))
			{
				tickSystem.IsPhysicUpdated = !tickSystem.IsPhysicUpdated;
			}

			if (Input.GetKeyDown(KeyCode.M))
			{
				var newBall = Instantiate(_ballPrefab);
				NetworkServer.Spawn(newBall);
			}
		}
	}

	#endregion

}
