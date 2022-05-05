using System;
using Mono.CecilX;
using UnityEditor;
using UnityEngine;

namespace Mirror.Weaver
{
	// not static, because ILPostProcessor is multithreaded
	public class WeaverTypes
	{
		public MethodReference ScriptableObjectCreateInstanceMethod;

		public MethodReference NetworkBehaviourDirtyBitsReference;
		public MethodReference GetWriterReference;
		public MethodReference ReturnWriterReference;

		public MethodReference NetworkClientConnectionReference;

		public MethodReference RemoteCallDelegateConstructor;

		public MethodReference NetworkServerGetActive;
		public MethodReference NetworkClientGetActive;

		// custom attribute types
		public MethodReference InitSyncObjectReference;

		// array segment
		public MethodReference ArraySegmentConstructorReference;

		// Action<T,T> for SyncVar Hooks
		public MethodReference ActionT_T;

		// syncvar
		public MethodReference generatedSyncVarSetter;
		public MethodReference generatedSyncVarSetter_GameObject;
		public MethodReference generatedSyncVarSetter_NetworkIdentity;
		public MethodReference generatedSyncVarSetter_NetworkBehaviour_T;
		public MethodReference generatedSyncVarDeserialize;
		public MethodReference generatedSyncVarDeserialize_GameObject;
		public MethodReference generatedSyncVarDeserialize_NetworkIdentity;
		public MethodReference generatedSyncVarDeserialize_NetworkBehaviour_T;
		public MethodReference getSyncVarGameObjectReference;
		public MethodReference getSyncVarNetworkIdentityReference;
		public MethodReference getSyncVarNetworkBehaviourReference;
		public MethodReference registerCommandReference;
		public MethodReference registerRpcReference;
		public MethodReference getTypeFromHandleReference;
		public MethodReference logErrorReference;
		public MethodReference logWarningReference;
		public MethodReference sendCommandInternal;
		public MethodReference sendRpcInternal;
		public MethodReference sendTargetRpcInternal;

		public MethodReference readNetworkBehaviourGeneric;

		// attributes
		public TypeDefinition initializeOnLoadMethodAttribute;
		public TypeDefinition runtimeInitializeOnLoadMethodAttribute;
		private AssemblyDefinition assembly;

		public TypeReference Import<T>()
		{
			return Import(typeof(T));
		}

		public TypeReference Import(Type t)
		{
			return assembly.MainModule.ImportReference(t);
		}

		// constructor resolves the types and stores them in fields
		public WeaverTypes(AssemblyDefinition assembly, Logger Log, ref bool WeavingFailed)
		{
			// system types
			this.assembly = assembly;

			var ArraySegmentType = Import(typeof(ArraySegment<>));
			ArraySegmentConstructorReference = Resolvers.ResolveMethod(ArraySegmentType, assembly, Log, ".ctor", ref WeavingFailed);

			var ActionType = Import(typeof(Action<,>));
			ActionT_T = Resolvers.ResolveMethod(ActionType, assembly, Log, ".ctor", ref WeavingFailed);

			var NetworkServerType = Import(typeof(NetworkServer));
			NetworkServerGetActive = Resolvers.ResolveMethod(NetworkServerType, assembly, Log, "get_active", ref WeavingFailed);
			var NetworkClientType = Import(typeof(NetworkClient));
			NetworkClientGetActive = Resolvers.ResolveMethod(NetworkClientType, assembly, Log, "get_active", ref WeavingFailed);

			var RemoteCallDelegateType = Import<RemoteCalls.RemoteCallDelegate>();
			RemoteCallDelegateConstructor = Resolvers.ResolveMethod(RemoteCallDelegateType, assembly, Log, ".ctor", ref WeavingFailed);

			var NetworkBehaviourType = Import<NetworkBehaviour>();
			var RemoteProcedureCallsType = Import(typeof(RemoteCalls.RemoteProcedureCalls));

			var ScriptableObjectType = Import<ScriptableObject>();

			ScriptableObjectCreateInstanceMethod = Resolvers.ResolveMethod(
				ScriptableObjectType, assembly, Log,
				md => md.Name == "CreateInstance" && md.HasGenericParameters,
				ref WeavingFailed);

			NetworkBehaviourDirtyBitsReference = Resolvers.ResolveProperty(NetworkBehaviourType, assembly, "syncVarDirtyBits");
			var NetworkWriterPoolType = Import(typeof(NetworkWriterPool));
			GetWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, Log, "Get", ref WeavingFailed);
			ReturnWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, Log, "Return", ref WeavingFailed);

			NetworkClientConnectionReference = Resolvers.ResolveMethod(NetworkClientType, assembly, Log, "get_connection", ref WeavingFailed);

			generatedSyncVarSetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarSetter", ref WeavingFailed);
			generatedSyncVarSetter_GameObject = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarSetter_GameObject", ref WeavingFailed);
			generatedSyncVarSetter_NetworkIdentity = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarSetter_NetworkIdentity", ref WeavingFailed);
			generatedSyncVarSetter_NetworkBehaviour_T = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarSetter_NetworkBehaviour", ref WeavingFailed);

			generatedSyncVarDeserialize_GameObject = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarDeserialize_GameObject", ref WeavingFailed);
			generatedSyncVarDeserialize = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarDeserialize", ref WeavingFailed);
			generatedSyncVarDeserialize_NetworkIdentity = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarDeserialize_NetworkIdentity", ref WeavingFailed);
			generatedSyncVarDeserialize_NetworkBehaviour_T = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GeneratedSyncVarDeserialize_NetworkBehaviour", ref WeavingFailed);

			getSyncVarGameObjectReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GetSyncVarGameObject", ref WeavingFailed);
			getSyncVarNetworkIdentityReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GetSyncVarNetworkIdentity", ref WeavingFailed);
			getSyncVarNetworkBehaviourReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "GetSyncVarNetworkBehaviour", ref WeavingFailed);

			registerCommandReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, Log, "RegisterCommand", ref WeavingFailed);
			registerRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, Log, "RegisterRpc", ref WeavingFailed);

			var unityDebug = Import(typeof(UnityEngine.Debug));
			// these have multiple methods with same name, so need to check parameters too
			logErrorReference = Resolvers.ResolveMethod(unityDebug, assembly, Log, md =>
				md.Name == "LogError" &&
				md.Parameters.Count == 1 &&
				md.Parameters[0].ParameterType.FullName == typeof(object).FullName,
				ref WeavingFailed);

			logWarningReference = Resolvers.ResolveMethod(unityDebug, assembly, Log, md =>
				md.Name == "LogWarning" &&
				md.Parameters.Count == 1 &&
				md.Parameters[0].ParameterType.FullName == typeof(object).FullName,
				ref WeavingFailed);

			var typeType = Import(typeof(Type));
			getTypeFromHandleReference = Resolvers.ResolveMethod(typeType, assembly, Log, "GetTypeFromHandle", ref WeavingFailed);
			sendCommandInternal = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "SendCommandInternal", ref WeavingFailed);
			sendRpcInternal = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "SendRPCInternal", ref WeavingFailed);
			sendTargetRpcInternal = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "SendTargetRPCInternal", ref WeavingFailed);

			InitSyncObjectReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, Log, "InitSyncObject", ref WeavingFailed);

			var readerExtensions = Import(typeof(NetworkReaderExtensions));
			readNetworkBehaviourGeneric = Resolvers.ResolveMethod(readerExtensions, assembly, Log, (md =>
			{
				return md.Name == nameof(NetworkReaderExtensions.ReadNetworkBehaviour) &&
					   md.HasGenericParameters;
			}),
			ref WeavingFailed);

			// [InitializeOnLoadMethod]
			// 'UnityEditor' is not available in builds.
			// we can only import this attribute if we are in an Editor assembly.
			if (Helpers.IsEditorAssembly(assembly))
			{
				var initializeOnLoadMethodAttributeRef = Import(typeof(InitializeOnLoadMethodAttribute));
				initializeOnLoadMethodAttribute = initializeOnLoadMethodAttributeRef.Resolve();
			}

			// [RuntimeInitializeOnLoadMethod]
			var runtimeInitializeOnLoadMethodAttributeRef = Import(typeof(RuntimeInitializeOnLoadMethodAttribute));
			runtimeInitializeOnLoadMethodAttribute = runtimeInitializeOnLoadMethodAttributeRef.Resolve();
		}
	}
}
