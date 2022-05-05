using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Mirror
{
	// Mirror's Weaver automatically detects all NetworkReader function types,
	// but they do all need to be extensions.
	public static class NetworkReaderExtensions
	{
		// cache encoding instead of creating it each time
		// 1000 readers before:  1MB GC, 30ms
		// 1000 readers after: 0.8MB GC, 18ms
		private static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte ReadByte(this NetworkReader reader)
		{
			return reader.ReadBlittable<byte>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte? ReadByteNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<byte>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte ReadSByte(this NetworkReader reader)
		{
			return reader.ReadBlittable<sbyte>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte? ReadSByteNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<sbyte>();
		}

		// bool is not blittable. read as ushort.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char ReadChar(this NetworkReader reader)
		{
			return (char)reader.ReadBlittable<ushort>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char? ReadCharNullable(this NetworkReader reader)
		{
			return (char?)reader.ReadBlittableNullable<ushort>();
		}

		// bool is not blittable. read as byte.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ReadBool(this NetworkReader reader)
		{
			return reader.ReadBlittable<byte>() != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool? ReadBoolNullable(this NetworkReader reader)
		{
			byte? value = reader.ReadBlittableNullable<byte>();
			return value.HasValue ? (value.Value != 0) : default(bool?);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short ReadShort(this NetworkReader reader)
		{
			return (short)reader.ReadUShort();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short? ReadShortNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<short>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort ReadUShort(this NetworkReader reader)
		{
			return reader.ReadBlittable<ushort>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort? ReadUShortNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<ushort>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ReadInt(this NetworkReader reader)
		{
			return reader.ReadBlittable<int>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int? ReadIntNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<int>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint ReadUInt(this NetworkReader reader)
		{
			return reader.ReadBlittable<uint>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint? ReadUIntNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<uint>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ReadLong(this NetworkReader reader)
		{
			return reader.ReadBlittable<long>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long? ReadLongNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<long>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ReadULong(this NetworkReader reader)
		{
			return reader.ReadBlittable<ulong>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong? ReadULongNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<ulong>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ReadFloat(this NetworkReader reader)
		{
			return reader.ReadBlittable<float>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float? ReadFloatNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<float>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double ReadDouble(this NetworkReader reader)
		{
			return reader.ReadBlittable<double>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double? ReadDoubleNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<double>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static decimal ReadDecimal(this NetworkReader reader)
		{
			return reader.ReadBlittable<decimal>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static decimal? ReadDecimalNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<decimal>();
		}

		/// <exception cref="T:System.ArgumentException">if an invalid utf8 string is sent</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ReadString(this NetworkReader reader)
		{
			// read number of bytes
			ushort size = reader.ReadUShort();

			// null support, see NetworkWriter
			if (size == 0)
			{
				return null;
			}

			int realSize = size - 1;

			// make sure it's within limits to avoid allocation attacks etc.
			if (realSize >= NetworkWriter.MaxStringLength)
			{
				throw new EndOfStreamException($"ReadString too long: {realSize}. Limit is: {NetworkWriter.MaxStringLength}");
			}

			var data = reader.ReadBytesSegment(realSize);

			// convert directly from buffer to string via encoding
			return encoding.GetString(data.Array, data.Offset, data.Count);
		}

		/// <exception cref="T:OverflowException">if count is invalid</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] ReadBytesAndSize(this NetworkReader reader)
		{
			// count = 0 means the array was null
			// otherwise count -1 is the length of the array
			uint count = reader.ReadUInt();
			// Use checked() to force it to throw OverflowException if data is invalid
			return count == 0 ? null : reader.ReadBytes(checked((int)(count - 1u)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] ReadBytes(this NetworkReader reader, int count)
		{
			byte[] bytes = new byte[count];
			reader.ReadBytes(bytes, count);
			return bytes;
		}

		/// <exception cref="T:OverflowException">if count is invalid</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ArraySegment<byte> ReadBytesAndSizeSegment(this NetworkReader reader)
		{
			// count = 0 means the array was null
			// otherwise count - 1 is the length of the array
			uint count = reader.ReadUInt();
			// Use checked() to force it to throw OverflowException if data is invalid
			return count == 0 ? default : reader.ReadBytesSegment(checked((int)(count - 1u)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 ReadVector2(this NetworkReader reader)
		{
			return reader.ReadBlittable<Vector2>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2? ReadVector2Nullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Vector2>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ReadVector3(this NetworkReader reader)
		{
			return reader.ReadBlittable<Vector3>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3? ReadVector3Nullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Vector3>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4 ReadVector4(this NetworkReader reader)
		{
			return reader.ReadBlittable<Vector4>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4? ReadVector4Nullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Vector4>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2Int ReadVector2Int(this NetworkReader reader)
		{
			return reader.ReadBlittable<Vector2Int>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2Int? ReadVector2IntNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Vector2Int>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ReadVector3Int(this NetworkReader reader)
		{
			return reader.ReadBlittable<Vector3Int>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int? ReadVector3IntNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Vector3Int>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color ReadColor(this NetworkReader reader)
		{
			return reader.ReadBlittable<Color>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color? ReadColorNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Color>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 ReadColor32(this NetworkReader reader)
		{
			return reader.ReadBlittable<Color32>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32? ReadColor32Nullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Color32>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion ReadQuaternion(this NetworkReader reader)
		{
			return reader.ReadBlittable<Quaternion>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion? ReadQuaternionNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Quaternion>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect ReadRect(this NetworkReader reader)
		{
			return reader.ReadBlittable<Rect>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect? ReadRectNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Rect>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane ReadPlane(this NetworkReader reader)
		{
			return reader.ReadBlittable<Plane>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane? ReadPlaneNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Plane>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Ray ReadRay(this NetworkReader reader)
		{
			return reader.ReadBlittable<Ray>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Ray? ReadRayNullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Ray>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix4x4 ReadMatrix4x4(this NetworkReader reader)
		{
			return reader.ReadBlittable<Matrix4x4>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix4x4? ReadMatrix4x4Nullable(this NetworkReader reader)
		{
			return reader.ReadBlittableNullable<Matrix4x4>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid ReadGuid(this NetworkReader reader)
		{
			return new Guid(reader.ReadBytes(16));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Guid? ReadGuidNullable(this NetworkReader reader)
		{
			return reader.ReadBool() ? ReadGuid(reader) : default(Guid?);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static NetworkIdentity ReadNetworkIdentity(this NetworkReader reader)
		{
			uint netId = reader.ReadUInt();
			if (netId == 0)
			{
				return null;
			}

			// NOTE: a netId not being in spawned is common.
			// for example, "[SyncVar] NetworkIdentity target" netId would not
			// be known on client if the monster walks out of proximity for a
			// moment. no need to log any error or warning here.
			return Utils.GetSpawnedInServerOrClient(netId);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static NetworkBehaviour ReadNetworkBehaviour(this NetworkReader reader)
		{
			// read netId first.
			//
			// IMPORTANT: if netId != 0, writer always writes componentIndex.
			//   reusing ReadNetworkIdentity() might return a null NetworkIdentity
			//   even if netId was != 0 but the identity disappeared on the client,
			//   resulting in unequal amounts of data being written / read.
			//   https://github.com/vis2k/Mirror/issues/2972
			uint netId = reader.ReadUInt();
			if (netId == 0)
			{
				return null;
			}

			// read component index in any case, BEFORE searching the spawned
			// NetworkIdentity by netId.
			byte componentIndex = reader.ReadByte();

			// NOTE: a netId not being in spawned is common.
			// for example, "[SyncVar] NetworkIdentity target" netId would not
			// be known on client if the monster walks out of proximity for a
			// moment. no need to log any error or warning here.
			var identity = Utils.GetSpawnedInServerOrClient(netId);

			return identity != null
				   ? identity.NetworkBehaviours[componentIndex]
				   : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ReadNetworkBehaviour<T>(this NetworkReader reader) where T : NetworkBehaviour
		{
			return reader.ReadNetworkBehaviour() as T;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static NetworkBehaviour.NetworkBehaviourSyncVar ReadNetworkBehaviourSyncVar(this NetworkReader reader)
		{
			uint netId = reader.ReadUInt();
			byte componentIndex = default;

			// if netId is not 0, then index is also sent to read before returning
			if (netId != 0)
			{
				componentIndex = reader.ReadByte();
			}

			return new NetworkBehaviour.NetworkBehaviourSyncVar(netId, componentIndex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Transform ReadTransform(this NetworkReader reader)
		{
			// Don't use null propagation here as it could lead to MissingReferenceException
			var networkIdentity = reader.ReadNetworkIdentity();
			return networkIdentity != null ? networkIdentity.transform : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GameObject ReadGameObject(this NetworkReader reader)
		{
			// Don't use null propagation here as it could lead to MissingReferenceException
			var networkIdentity = reader.ReadNetworkIdentity();
			return networkIdentity != null ? networkIdentity.gameObject : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<T> ReadList<T>(this NetworkReader reader)
		{
			int length = reader.ReadInt();
			if (length < 0)
			{
				return null;
			}

			var result = new List<T>(length);
			for (int i = 0; i < length; i++)
			{
				result.Add(reader.Read<T>());
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] ReadArray<T>(this NetworkReader reader)
		{
			int length = reader.ReadInt();

			//  we write -1 for null
			if (length < 0)
			{
				return null;
			}

			// todo throw an exception for other negative values (we never write them, likely to be attacker)

			// this assumes that a reader for T reads at least 1 bytes
			// we can't know the exact size of T because it could have a user created reader
			// NOTE: don't add to length as it could overflow if value is int.max
			if (length > reader.Length - reader.Position)
			{
				throw new EndOfStreamException($"Received array that is too large: {length}");
			}

			var result = new T[length];
			for (int i = 0; i < length; i++)
			{
				result[i] = reader.Read<T>();
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Uri ReadUri(this NetworkReader reader)
		{
			string uriString = reader.ReadString();
			return (string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D ReadTexture2D(this NetworkReader reader)
		{
			var texture2D = new Texture2D(32, 32);
			texture2D.SetPixels32(reader.Read<Color32[]>());
			texture2D.Apply();
			return texture2D;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Sprite ReadSprite(this NetworkReader reader)
		{
			return Sprite.Create(reader.ReadTexture2D(), reader.ReadRect(), reader.ReadVector2());
		}
	}
}
