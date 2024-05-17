using Mirror;
using TickPhysics;
using UnityEngine;

public class PlayerBall : NetworkBehaviour, IPhysicsObject
{

	#region Fields

	protected ITickSystem tickSystem = default;

	private Rigidbody _rigidbody = default;

	[SerializeField]
	private Transform _visual = default;

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

	private void ServerSync()
	{
		if (!isServer)
		{
			return;
		}

		if (_syncPos)
		{
			_position = _rigidbody.position;
		}

		if (_syncRot)
		{
			_rotation = _rigidbody.rotation;
		}

		if (_syncVel)
		{
			_velocity = _rigidbody.linearVelocity;
		}

		if (_syncAngVel)
		{
			_angularVelocity = _rigidbody.angularVelocity;
		}
	}

	private void ClientSync()
	{
		if (_syncPos)
		{
			_rigidbody.position = _position;
		}

		if (_syncRot)
		{
			_rigidbody.rotation = _rotation;
		}

		if (_syncVel)
		{
			_rigidbody.linearVelocity = _velocity;
		}

		if (_syncAngVel)
		{
			_rigidbody.angularVelocity = _angularVelocity;
		}
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
		ClientSync();
	}

	#endregion

	#region Init

	private void Awake()
	{
		TryGetComponent(out _rigidbody);
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

	#endregion

	#region UpdatePhysics

	public void UpdatePhysics()
	{
		if (_inputSpaceWasPress)
		{
			_inputSpaceWasPress = false;
			_rigidbody.linearVelocity = Random.onUnitSphere * 50f;
			_rigidbody.angularVelocity = Random.onUnitSphere * 50f;
		}
	}

	#endregion

	#region UpdateGraphics

	private bool _inputSpaceWasPress = false;

	[Space, SyncVar]
	public bool extrapolPosition = false;

	private bool _extrapolPositionPastValue;

	[SyncVar]
	public bool extrapolRotation = true;

	private bool _extrapolRotationPastValue;

	public void UpdateGraphics()
	{
		ServerSync();	

		if (Input.GetKeyDown(KeyCode.Space))
		{
			_inputSpaceWasPress = true;
		}

		if (isServer)
		{
			if (Input.GetKeyDown(KeyCode.E))
			{
				extrapolPosition = !extrapolPosition;
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				extrapolRotation = !extrapolRotation;
			}
		}

		if (extrapolPosition)
		{
			var extraPosition = _rigidbody.linearVelocity * tickSystem.ExtraDeltaTime;
			_visual.position = _rigidbody.position + extraPosition;
			_extrapolPositionPastValue = true;
		}
		else if (_extrapolPositionPastValue)
		{
			_extrapolPositionPastValue = false;
			_visual.localPosition = Vector3.zero;
		}

		if (extrapolRotation)
		{
			var extraRotation = Quaternion.Euler(Mathf.Rad2Deg * tickSystem.ExtraDeltaTime * transform.InverseTransformVector(_angularVelocity));
			_visual.rotation = _rigidbody.rotation * extraRotation;
			_extrapolRotationPastValue = true;
		}
		else if(_extrapolRotationPastValue)
		{
			_extrapolRotationPastValue = false;
			_visual.localRotation = Quaternion.identity;
		}
	}

	#endregion

	#region OnTrigger

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log("OnTriggerEnter: " + tickSystem.FixedFrameCount + " | " + Time.frameCount);
	}

	private void OnTriggerExit(Collider other)
	{
		//Debug.Log("OnTriggerExit: " + tickSystem.FixedFrameCount + " | " + Time.frameCount);
	}

	private void OnTriggerStay(Collider other)
	{
		//Debug.Log("OnTriggerStay: " + tickSystem.FixedFrameCount + " | " + Time.frameCount);
	}

	#endregion

}
