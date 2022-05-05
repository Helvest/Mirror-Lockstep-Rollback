using UnityEngine;

namespace Mirror.Experimental
{
	[AddComponentMenu("Network/ Experimental/Network Lerp Rigidbody")]
	[HelpURL("https://mirror-networking.gitbook.io/docs/components/network-lerp-rigidbody")]
	public class NetworkLerpRigidbody : NetworkBehaviour
	{
		[Header("Settings")]
		[SerializeField] internal Rigidbody target = null;
		[Tooltip("How quickly current velocity approaches target velocity")]
		[SerializeField] private float lerpVelocityAmount = 0.5f;
		[Tooltip("How quickly current position approaches target position")]
		[SerializeField] private float lerpPositionAmount = 0.5f;

		[Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
		[SerializeField] private bool clientAuthority = false;
		private float nextSyncTime;


		[SyncVar()]
		private Vector3 targetVelocity;

		[SyncVar()]
		private Vector3 targetPosition;

		/// <summary>
		/// Ignore value if is host or client with Authority
		/// </summary>
		/// <returns></returns>
		private bool IgnoreSync => isServer || ClientWithAuthority;

		private bool ClientWithAuthority => clientAuthority && hasAuthority;

		private void OnValidate()
		{
			if (target == null)
			{
				target = GetComponent<Rigidbody>();
			}
		}

		private void Update()
		{
			if (isServer)
			{
				SyncToClients();
			}
			else if (ClientWithAuthority)
			{
				SendToServer();
			}
		}

		private void SyncToClients()
		{
			targetVelocity = target.velocity;
			targetPosition = target.position;
		}

		private void SendToServer()
		{
			float now = Time.time;
			if (now > nextSyncTime)
			{
				nextSyncTime = now + syncInterval;
				CmdSendState(target.velocity, target.position);
			}
		}

		[Command]
		private void CmdSendState(Vector3 velocity, Vector3 position)
		{
			target.velocity = velocity;
			target.position = position;
			targetVelocity = velocity;
			targetPosition = position;
		}

		private void FixedUpdate()
		{
			if (IgnoreSync)
			{ return; }

			target.velocity = Vector3.Lerp(target.velocity, targetVelocity, lerpVelocityAmount);
			target.position = Vector3.Lerp(target.position, targetPosition, lerpPositionAmount);
			// add velocity to position as position would have moved on server at that velocity
			target.position += target.velocity * Time.fixedDeltaTime;

			// TODO does this also need to sync acceleration so and update velocity?
		}
	}
}
