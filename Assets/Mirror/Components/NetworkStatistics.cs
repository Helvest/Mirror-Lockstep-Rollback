using System;
using UnityEngine;

namespace Mirror
{
	/// <summary>
	/// Shows Network messages and bytes sent & received per second.
	/// <para>Add this component to the same object as Network Manager.</para>
	/// </summary>
	[AddComponentMenu("Network/Network Statistics")]
	[DisallowMultipleComponent]
	[HelpURL("https://mirror-networking.gitbook.io/docs/components/network-statistics")]
	public class NetworkStatistics : MonoBehaviour
	{
		// update interval
		private double intervalStartTime;

		// ---------------------------------------------------------------------

		// CLIENT
		// long bytes to support >2GB
		private int clientIntervalReceivedPackets;
		private long clientIntervalReceivedBytes;
		private int clientIntervalSentPackets;
		private long clientIntervalSentBytes;

		// results from last interval
		// long bytes to support >2GB
		private int clientReceivedPacketsPerSecond;
		private long clientReceivedBytesPerSecond;
		private int clientSentPacketsPerSecond;
		private long clientSentBytesPerSecond;

		// ---------------------------------------------------------------------

		// SERVER
		// capture interval
		// long bytes to support >2GB
		private int serverIntervalReceivedPackets;
		private long serverIntervalReceivedBytes;
		private int serverIntervalSentPackets;
		private long serverIntervalSentBytes;

		// results from last interval
		// long bytes to support >2GB
		private int serverReceivedPacketsPerSecond;
		private long serverReceivedBytesPerSecond;
		private int serverSentPacketsPerSecond;
		private long serverSentBytesPerSecond;

		// NetworkManager sets Transport.activeTransport in Awake().
		// so let's hook into it in Start().
		private void Start()
		{
			// find available transport
			var transport = Transport.activeTransport;
			if (transport != null)
			{
				transport.OnClientDataReceived += OnClientReceive;
				transport.OnClientDataSent += OnClientSend;
				transport.OnServerDataReceived += OnServerReceive;
				transport.OnServerDataSent += OnServerSend;
			}
			else
			{
				Debug.LogError($"NetworkStatistics: no available or active Transport found on this platform: {Application.platform}");
			}
		}

		private void OnDestroy()
		{
			// remove transport hooks
			var transport = Transport.activeTransport;
			if (transport != null)
			{
				transport.OnClientDataReceived -= OnClientReceive;
				transport.OnClientDataSent -= OnClientSend;
				transport.OnServerDataReceived -= OnServerReceive;
				transport.OnServerDataSent -= OnServerSend;
			}
		}

		private void OnClientReceive(ArraySegment<byte> data, int channelId)
		{
			++clientIntervalReceivedPackets;
			clientIntervalReceivedBytes += data.Count;
		}

		private void OnClientSend(ArraySegment<byte> data, int channelId)
		{
			++clientIntervalSentPackets;
			clientIntervalSentBytes += data.Count;
		}

		private void OnServerReceive(int connectionId, ArraySegment<byte> data, int channelId)
		{
			++serverIntervalReceivedPackets;
			serverIntervalReceivedBytes += data.Count;
		}

		private void OnServerSend(int connectionId, ArraySegment<byte> data, int channelId)
		{
			++serverIntervalSentPackets;
			serverIntervalSentBytes += data.Count;
		}

		private void Update()
		{
			// calculate results every second
			if (NetworkTime.localTime >= intervalStartTime + 1)
			{
				if (NetworkClient.active)
				{
					UpdateClient();
				}

				if (NetworkServer.active)
				{
					UpdateServer();
				}

				intervalStartTime = NetworkTime.localTime;
			}
		}

		private void UpdateClient()
		{
			clientReceivedPacketsPerSecond = clientIntervalReceivedPackets;
			clientReceivedBytesPerSecond = clientIntervalReceivedBytes;
			clientSentPacketsPerSecond = clientIntervalSentPackets;
			clientSentBytesPerSecond = clientIntervalSentBytes;

			clientIntervalReceivedPackets = 0;
			clientIntervalReceivedBytes = 0;
			clientIntervalSentPackets = 0;
			clientIntervalSentBytes = 0;
		}

		private void UpdateServer()
		{
			serverReceivedPacketsPerSecond = serverIntervalReceivedPackets;
			serverReceivedBytesPerSecond = serverIntervalReceivedBytes;
			serverSentPacketsPerSecond = serverIntervalSentPackets;
			serverSentBytesPerSecond = serverIntervalSentBytes;

			serverIntervalReceivedPackets = 0;
			serverIntervalReceivedBytes = 0;
			serverIntervalSentPackets = 0;
			serverIntervalSentBytes = 0;
		}

		private void OnGUI()
		{
			// only show if either server or client active
			if (NetworkClient.active || NetworkServer.active)
			{
				// create main GUI area
				// 105 is below NetworkManager HUD in all cases.
				GUILayout.BeginArea(new Rect(10, 105, 215, 300));

				// show client / server stats if active
				if (NetworkClient.active)
				{
					OnClientGUI();
				}

				if (NetworkServer.active)
				{
					OnServerGUI();
				}

				// end of GUI area
				GUILayout.EndArea();
			}
		}

		private void OnClientGUI()
		{
			// background
			GUILayout.BeginVertical("Box");
			GUILayout.Label("<b>Client Statistics</b>");

			// sending ("msgs" instead of "packets" to fit larger numbers)
			GUILayout.Label($"Send: {clientSentPacketsPerSecond} msgs @ {Utils.PrettyBytes(clientSentBytesPerSecond)}/s");

			// receiving ("msgs" instead of "packets" to fit larger numbers)
			GUILayout.Label($"Recv: {clientReceivedPacketsPerSecond} msgs @ {Utils.PrettyBytes(clientReceivedBytesPerSecond)}/s");

			// end background
			GUILayout.EndVertical();
		}

		private void OnServerGUI()
		{
			// background
			GUILayout.BeginVertical("Box");
			GUILayout.Label("<b>Server Statistics</b>");

			// sending ("msgs" instead of "packets" to fit larger numbers)
			GUILayout.Label($"Send: {serverSentPacketsPerSecond} msgs @ {Utils.PrettyBytes(serverSentBytesPerSecond)}/s");

			// receiving ("msgs" instead of "packets" to fit larger numbers)
			GUILayout.Label($"Recv: {serverReceivedPacketsPerSecond} msgs @ {Utils.PrettyBytes(serverReceivedBytesPerSecond)}/s");

			// end background
			GUILayout.EndVertical();
		}
	}
}
