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

	[SerializeField]
	[SyncVar]
	private Vector3 _position = default;

	[SerializeField]
	[SyncVar]
	private Vector3 _velocity = default;

	[SerializeField]
	[SyncVar]
	private Vector3 _angularVelocity = default;

	[SerializeField]
	[SyncVar]
	private Quaternion _rotation = default;

	private void ServerSync()
	{
		_position = _rigidbody.position;
		_velocity = _rigidbody.velocity;
		_angularVelocity = _rigidbody.angularVelocity;
		_rotation = _rigidbody.rotation;
	}

	private void ClientSync()
	{
		_rigidbody.position = _position;
		_rigidbody.velocity = _velocity;
		_rigidbody.angularVelocity = _angularVelocity;
		_rigidbody.rotation = _rotation;
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
			_rigidbody.velocity = Random.onUnitSphere * 50f;
			_rigidbody.angularVelocity = Random.onUnitSphere * 50f;
		}
	}

	#endregion

	#region UpdateGraphics

	private bool _inputSpaceWasPress = false;

	[SyncVar]
	public bool extrapolPosition = false;

	[SyncVar]
	public bool extrapolRotation = true;

	public void UpdateGraphics()
	{
		if (isServer)
		{
			ServerSync();
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			_inputSpaceWasPress = true;
		}

		if (Input.GetKeyDown(KeyCode.A))
		{

		}

		if (Input.GetKeyDown(KeyCode.Z))
		{

		}

		if (isServer)
		{
			if (Input.GetKeyDown(KeyCode.E))
			{
				extrapolPosition = !extrapolPosition;

				if (!extrapolPosition)
				{
					_visual.localPosition = Vector3.zero;
				}
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				extrapolRotation = !extrapolRotation;

				if (!extrapolRotation)
				{
					_visual.localRotation = Quaternion.identity;
				}
			}
		}

		if (extrapolPosition)
		{
			var extraPosition = _rigidbody.velocity * tickSystem.ExtraDeltaTime;
			_visual.position = _rigidbody.position + extraPosition;
		}

		if (extrapolRotation)
		{
			var extraRotation = Quaternion.Euler(Mathf.Rad2Deg * tickSystem.ExtraDeltaTime * transform.InverseTransformVector(_angularVelocity));
			_visual.rotation = _rigidbody.rotation * extraRotation;
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
