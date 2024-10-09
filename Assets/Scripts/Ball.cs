using TickPhysics;
using UnityEngine;
using UnityEngine.Serialization;

public class Ball : MonoBehaviour, IPhysicsObject
{

	#region Fields

	protected ITickSystem tickSystem = default;
	protected MonoTickSystem3D tickSystem3D = default;

	[field: SerializeField]
	public Rigidbody CachedRigidbody { get; private set; } = default;

	[field: SerializeField]
	public Collider CachedCollider { get; private set; } = default;

	[SerializeField]
	private Transform _visual = default;

	[SerializeField]
	[FormerlySerializedAs("_ghost")]
	private Transform _physicGhost = default;

	public bool useVisual = true;

	[Range(0f, 8f)]
	public float speedScale = 1.0f;

	public bool divideByTwo = false;

	public bool multiplyByTwo = false;

	public bool debug = false;

	#endregion

	#region Init

	private void OnValidate()
	{
		if (CachedRigidbody == null)
		{
			CachedRigidbody = GetComponent<Rigidbody>();
		}

		if (CachedCollider == null)
		{
			CachedCollider = GetComponent<Collider>();
		}
	}

	private void OnEnable()
	{
		SL.TryGetIfNull(ref tickSystem);
		tickSystem3D = tickSystem as MonoTickSystem3D;

		_physicGhost.transform.parent = null;

		SaveRigidbodyData();
	}

	#endregion

	#region UpdatePhysics

	private float _mass;
	//private Vector3 _linearVelocity;
	private float _linearDamping;
	//private Vector3 _angularVelocity;
	private float _angularDamping;
	private float _sleepThreshold;

	private void SaveRigidbodyData()
	{
		_mass = CachedRigidbody.mass;

		//_linearVelocity = CachedRigidbody.linearVelocity;
		_linearDamping = CachedRigidbody.linearDamping;

		//_angularVelocity = CachedRigidbody.angularVelocity;
		_angularDamping = CachedRigidbody.angularDamping;

		_sleepThreshold = CachedRigidbody.sleepThreshold;
	}

	private float _previousSpeedScale = 1;

	public void UpdatePhysics()
	{
		if (multiplyByTwo)
		{
			multiplyByTwo = false;
			speedScale *= 2;
		}

		if (divideByTwo)
		{
			divideByTwo = false;
			speedScale /= 2;
		}

		if (_previousSpeedScale != speedScale)
		{
			CalculateForSpeedScale();
		}

		if (!CachedRigidbody.useGravity)
		{
			CachedRigidbody.AddForce(Physics.gravity * speedScale * speedScale, ForceMode.Acceleration);
		}

		velocity = CachedRigidbody.linearVelocity.magnitude;
	}

	[Header("Data")]
	public float velocity;
	public int solverIterations;
	public int solverVelocityIterations;

	private void CalculateForSpeedScale()
	{
		var changeRatio = speedScale / _previousSpeedScale;
		//var evaluated = animationCurve.Evaluate(speedScale / 2f);

		CachedRigidbody.mass = _mass / speedScale;

		CachedRigidbody.linearVelocity *= changeRatio;

		CachedRigidbody.linearDamping = _linearDamping * speedScale;

		CachedRigidbody.angularVelocity *= changeRatio;
		CachedRigidbody.angularDamping = _angularDamping * speedScale;

		CachedRigidbody.sleepThreshold = _sleepThreshold * speedScale;

		_previousSpeedScale = speedScale;

		//CachedCollider.material.dynamicFriction *= speedScale;
		//CachedCollider.material.staticFriction *= speedScale;
		//CachedCollider.material.bounciness = speedScale;

		CachedRigidbody.solverIterations = solverIterations = Mathf.Max(1, Mathf.RoundToInt(Physics.defaultSolverIterations * speedScale));
		CachedRigidbody.solverVelocityIterations = solverVelocityIterations = Mathf.Max(1, Mathf.RoundToInt(Physics.defaultSolverVelocityIterations * speedScale));

		CachedRigidbody.maxDepenetrationVelocity = Physics.defaultMaxDepenetrationVelocity * speedScale;
		CachedRigidbody.maxAngularVelocity = Physics.defaultMaxAngularSpeed * speedScale;
	}

	#endregion

	#region UpdateGraphics

	[Space]
	public bool extrapolPosition = false;

	private bool _extrapolPositionPastValue;

	public bool extrapolRotation = true;

	private bool _extrapolRotationPastValue;

	public void UpdateGraphics()
	{
		if (debug)
		{
			Debug.Log("TP: " + transform.position + " | RP: " + CachedRigidbody.position);
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			CachedRigidbody.linearVelocity = Random.onUnitSphere * 50f * speedScale;
		}

		if (Input.GetKeyDown(KeyCode.E))
		{
			extrapolPosition = !extrapolPosition;
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			extrapolRotation = !extrapolRotation;
		}	

		if (extrapolPosition)
		{
			var extraPosition = CachedRigidbody.linearVelocity * tickSystem.ExtraDeltaTime;

			if (useVisual)
			{
				_visual.position = CachedRigidbody.position + extraPosition;
			}
			else
			{
				transform.position = CachedRigidbody.position + extraPosition;
			}

			_extrapolPositionPastValue = true;
		}
		else if (_extrapolPositionPastValue)
		{
			_extrapolPositionPastValue = false;
			_visual.localPosition = Vector3.zero;
		}

		if (extrapolRotation)
		{
			var extraRotation = Quaternion.Euler(Mathf.Rad2Deg * tickSystem.ExtraDeltaTime * transform.InverseTransformVector(CachedRigidbody.angularVelocity));

			if (useVisual)
			{
				_visual.rotation = CachedRigidbody.rotation * extraRotation;
			}
			else
			{
				transform.rotation = CachedRigidbody.rotation * extraRotation;
			}

			_extrapolRotationPastValue = true;
		}
		else if (_extrapolRotationPastValue)
		{
			_extrapolRotationPastValue = false;
			_visual.localRotation = Quaternion.identity;
		}

		if (_physicGhost != null)
		{
			_physicGhost.position = CachedRigidbody.position;
			_physicGhost.rotation = CachedRigidbody.rotation;
		}
	}

	#endregion

	#region OnTrigger

	/*private void OnTriggerEnter(Collider other)
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
	}*/

	#endregion

}
