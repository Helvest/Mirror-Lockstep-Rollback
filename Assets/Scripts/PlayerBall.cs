using Mirror;
using TickPhysics;
using UnityEngine;

public class PlayerBall : NetworkBehaviour, IPhysicsObject
{

	#region Fields

	protected ITickSystem tickSystem = default;

	private Transform _cachedTransform = default;
	private Rigidbody _rigidbody = default;

	[SerializeField]
	private Transform _visual = default;


	/// <summary>
	/// Ignore value if is host or client with Authority
	/// </summary>
	/// <returns></returns>
	private bool IgnoreSync => isServer || hasAuthority;

	#endregion

	#region Sync vars

	[SyncVar(hook = nameof(OnPositionChanged))]
	private Vector3 _position;

	private void OnPositionChanged(Vector3 _, Vector3 newValue)
	{
		if (IgnoreSync)
		{
			return;
		}

		//var diff = newValue - _rigidbody.position;
		//Debug.Log($"OnPositionChanged: {diff.x:00.00000}, {diff.y:00.00000}, {diff.z:00.00000}");

		//Debug.Log($"OnPositionChanged: {newValue.x:00.00000}, {newValue.y:00.00000}, {newValue.z:00.00000}");

		_rigidbody.position = newValue;
	}

	[SyncVar(hook = nameof(OnVelocityChanged))]
	private Vector3 _velocity;

	private void OnVelocityChanged(Vector3 _, Vector3 newValue)
	{
		if (IgnoreSync)
		{
			return;
		}

		_rigidbody.velocity = newValue;
	}

	[SyncVar(hook = nameof(OnAngularVelocityChanged))]
	private Vector3 _angularVelocity;

	private void OnAngularVelocityChanged(Vector3 _, Vector3 newValue)
	{
		if (IgnoreSync)
		{
			return;
		}

		_rigidbody.angularVelocity = newValue;
	}

	[SyncVar(hook = nameof(OnRotationChanged))]
	private Quaternion _rotation;

	private void OnRotationChanged(Quaternion _, Quaternion newValue)
	{
		if (IgnoreSync)
		{
			return;
		}

		//Debug.Log("oldValue: " + oldValue + " | newValue: " + newValue);
		_rigidbody.rotation = newValue;
	}

	#endregion

	#region Init

	private void Awake()
	{
		TryGetComponent(out _rigidbody);
		TryGetComponent(out _cachedTransform);
	}

	private void OnEnable()
	{
		SL.TryGetIfNull(ref tickSystem);
	}

	#endregion

	#region OnStartServer

	public override void OnStartServer()
	{
		_position = _rigidbody.position;
		_velocity = _rigidbody.velocity;
		_angularVelocity = _rigidbody.angularVelocity;
		_rotation = _rigidbody.rotation;
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
			_position = _rigidbody.position;
			_velocity = _rigidbody.velocity;
			_angularVelocity = _rigidbody.angularVelocity;
			_rotation = _rigidbody.rotation;
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			_inputSpaceWasPress = true;
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

}
