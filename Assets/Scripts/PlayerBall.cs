using Mirror;
using TickPhysics;
using UnityEngine;

public class PlayerBall : NetworkBehaviour, IPhysicsObject
{
	private Rigidbody _rigidbody;

	[SerializeField]
	private Transform _visual;

	private TickManager3D _tickManager;

	/// <summary>
	/// Ignore value if is host or client with Authority
	/// </summary>
	/// <returns></returns>
	private bool IgnoreSync => isServer || hasAuthority;

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

	private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
	{
		if (IgnoreSync)
		{
			return;
		}

		//Debug.Log("oldValue: " + oldValue + " | newValue: " + newValue);
		_rigidbody.rotation = newValue;
	}

	#endregion

	private void Awake()
	{
		SL.TryGet(out _tickManager);
		TryGetComponent(out _rigidbody);
	}

	public override void OnStartServer()
	{
		_position = _rigidbody.position;
		_velocity = _rigidbody.velocity;
		_angularVelocity = _rigidbody.angularVelocity;
		_rotation = _rigidbody.rotation;
	}

	public void UpdatePhysics()
	{
		if (_inputSpaceWasPress)
		{
			_inputSpaceWasPress = false;
			_rigidbody.velocity = Random.onUnitSphere * 50f;
			_rigidbody.angularVelocity = Random.onUnitSphere * 50f;
		}
	}

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
			var extraPosition = _rigidbody.velocity * _tickManager.ExtraDeltaTime;
			_visual.position = _rigidbody.position + extraPosition;
		}

		if (extrapolRotation)
		{
			var extraRotation = Quaternion.Euler(transform.InverseTransformVector(_angularVelocity) * Mathf.Rad2Deg * _tickManager.ExtraDeltaTime);
			_visual.rotation = _rigidbody.rotation * extraRotation;
		}
	}

}
