using Mirror;
using TickPhysics;
using UnityEngine;

public class BallNet : NetworkBehaviour, IPhysicsObject
{

	#region Fields

	protected ITickSystem tickSystem = default;

	[SerializeField]
	private Ball _ball = default;

	private Rigidbody CachedRigidbody => _ball.CachedRigidbody;

	[SerializeField]
	private Transform _networkGhost;

	#endregion

	#region Sync vars

	[SerializeField, SyncVar]
	private bool _syncPos = true;
	[SerializeField, SyncVar]
	private Vector3 _position = default;

	[Space, SerializeField, SyncVar]
	private bool _syncRot = true;
	[SerializeField, SyncVar]
	private Quaternion _rotation = default;

	[Space, SerializeField, SyncVar]
	private bool _syncVel = true;
	[SerializeField, SyncVar]
	private Vector3 _velocity = default;

	[Space, SerializeField, SyncVar]
	private bool _syncAngVel = true;
	[SerializeField, SyncVar]
	private Vector3 _angularVelocity = default;

	[Space, SerializeField, SyncVar]
	private bool _extrapolPosition = false;

	[SerializeField, SyncVar]
	private bool _extrapolRotation = false;

	private void ServerSync()
	{
		if (!isServer)
		{
			return;
		}

		if (_syncPos)
		{
			_position = CachedRigidbody.position;
		}

		if (_syncRot)
		{
			_rotation = CachedRigidbody.rotation;
		}

		if (_syncVel)
		{
			_velocity = CachedRigidbody.linearVelocity;
		}

		if (_syncAngVel)
		{
			_angularVelocity = CachedRigidbody.angularVelocity;
		}

		_extrapolPosition = _ball.extrapolPosition;
		_extrapolRotation = _ball.extrapolRotation;
	}

	private void ClientSync()
	{
		if (_syncPos)
		{
			CachedRigidbody.position = _position;
			//transform.position = _position;
		}

		if (_syncRot)
		{
			CachedRigidbody.rotation = _rotation;
		}

		if (_syncVel)
		{
			CachedRigidbody.linearVelocity = _velocity;
		}

		if (_syncAngVel)
		{
			CachedRigidbody.angularVelocity = _angularVelocity;
		}

		_ball.extrapolPosition = _extrapolPosition;
		_ball.extrapolRotation = _extrapolRotation;
	}

	private void UpdateGhost()
	{
		if (_networkGhost == null)
		{
			return;
		}

		if (_syncPos)
		{
			_networkGhost.position = _position;
		}

		if (_syncRot)
		{
			_networkGhost.rotation = _rotation;
		}
	}

	public override void OnSerialize(NetworkWriter writer, bool initialState)
	{
		base.OnSerialize(writer, initialState);
		UpdateGhost();
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
		ClientSync();
		UpdateGhost();
	}

	#endregion

	#region Init

	protected override void OnValidate()
	{
		if (_ball == null)
		{
			TryGetComponent(out _ball);
		}

		base.OnValidate();
	}

	private void OnEnable()
	{
		SL.TryGetIfNull(ref tickSystem);
	}

	#endregion

	#region OnStartServer

	public override void OnStartServer()
	{
		ServerSync();
	}

	public override void OnStartClient()
	{
		if (_networkGhost != null)
		{
			_networkGhost.parent = null;
		}
	}

	public override void OnStopClient()
	{
		if (_networkGhost != null)
		{
			_networkGhost.parent = transform;
		}
	}

	#endregion

	#region Update
	
	public void UpdatePhysics()
	{
		
	}

	public void UpdateGraphics()
	{
		ServerSync();
	}
	
	#endregion

}
