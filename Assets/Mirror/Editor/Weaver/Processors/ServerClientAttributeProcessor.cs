// Injects server/client active checks for [Server/Client] attributes
using Mono.CecilX;
using Mono.CecilX.Cil;

namespace Mirror.Weaver
{
	internal static class ServerClientAttributeProcessor
	{
		public static bool Process(TypeDefinition td)
		{
			bool modified = false;
			foreach (var md in td.Methods)
			{
				modified |= ProcessSiteMethod(md);
			}

			foreach (var nested in td.NestedTypes)
			{
				modified |= Process(nested);
			}
			return modified;
		}

		private static bool ProcessSiteMethod(MethodDefinition md)
		{
			if (md.Name == ".cctor" ||
				md.Name == NetworkBehaviourProcessor.ProcessedFunctionName ||
				md.Name.StartsWith(Weaver.InvokeRpcPrefix))
			{
				return false;
			}

			if (md.IsAbstract)
			{
				if (HasServerClientAttribute(md))
				{
					Weaver.Error("Server or Client Attributes can't be added to abstract method. Server and Client Attributes are not inherited so they need to be applied to the override methods instead.", md);
				}
				return false;
			}

			if (md.Body != null && md.Body.Instructions != null)
			{
				return ProcessMethodAttributes(md);
			}
			return false;
		}

		public static bool HasServerClientAttribute(MethodDefinition md)
		{
			foreach (var attr in md.CustomAttributes)
			{
				switch (attr.Constructor.DeclaringType.ToString())
				{
					case "Mirror.ServerAttribute":
					case "Mirror.ServerCallbackAttribute":
					case "Mirror.ClientAttribute":
					case "Mirror.ClientCallbackAttribute":
						return true;
					default:
						break;
				}
			}
			return false;
		}

		public static bool ProcessMethodAttributes(MethodDefinition md)
		{
			if (md.HasCustomAttribute<ServerAttribute>())
			{
				InjectServerGuard(md, true);
			}
			else if (md.HasCustomAttribute<ServerCallbackAttribute>())
			{
				InjectServerGuard(md, false);
			}
			else if (md.HasCustomAttribute<ClientAttribute>())
			{
				InjectClientGuard(md, true);
			}
			else if (md.HasCustomAttribute<ClientCallbackAttribute>())
			{
				InjectClientGuard(md, false);
			}
			else
			{
				return false;
			}

			return true;
		}

		private static void InjectServerGuard(MethodDefinition md, bool logWarning)
		{
			var worker = md.Body.GetILProcessor();
			var top = md.Body.Instructions[0];

			worker.InsertBefore(top, worker.Create(OpCodes.Call, WeaverTypes.NetworkServerGetActive));
			worker.InsertBefore(top, worker.Create(OpCodes.Brtrue, top));
			if (logWarning)
			{
				worker.InsertBefore(top, worker.Create(OpCodes.Ldstr, $"[Server] function '{md.FullName}' called when server was not active"));
				worker.InsertBefore(top, worker.Create(OpCodes.Call, WeaverTypes.logWarningReference));
			}
			InjectGuardParameters(md, worker, top);
			InjectGuardReturnValue(md, worker, top);
			worker.InsertBefore(top, worker.Create(OpCodes.Ret));
		}

		private static void InjectClientGuard(MethodDefinition md, bool logWarning)
		{
			var worker = md.Body.GetILProcessor();
			var top = md.Body.Instructions[0];

			worker.InsertBefore(top, worker.Create(OpCodes.Call, WeaverTypes.NetworkClientGetActive));
			worker.InsertBefore(top, worker.Create(OpCodes.Brtrue, top));
			if (logWarning)
			{
				worker.InsertBefore(top, worker.Create(OpCodes.Ldstr, $"[Client] function '{md.FullName}' called when client was not active"));
				worker.InsertBefore(top, worker.Create(OpCodes.Call, WeaverTypes.logWarningReference));
			}

			InjectGuardParameters(md, worker, top);
			InjectGuardReturnValue(md, worker, top);
			worker.InsertBefore(top, worker.Create(OpCodes.Ret));
		}

		// this is required to early-out from a function with "ref" or "out" parameters
		private static void InjectGuardParameters(MethodDefinition md, ILProcessor worker, Instruction top)
		{
			int offset = md.Resolve().IsStatic ? 0 : 1;
			for (int index = 0; index < md.Parameters.Count; index++)
			{
				var param = md.Parameters[index];
				if (param.IsOut)
				{
					var elementType = param.ParameterType.GetElementType();

					md.Body.Variables.Add(new VariableDefinition(elementType));
					md.Body.InitLocals = true;

					worker.InsertBefore(top, worker.Create(OpCodes.Ldarg, index + offset));
					worker.InsertBefore(top, worker.Create(OpCodes.Ldloca_S, (byte)(md.Body.Variables.Count - 1)));
					worker.InsertBefore(top, worker.Create(OpCodes.Initobj, elementType));
					worker.InsertBefore(top, worker.Create(OpCodes.Ldloc, md.Body.Variables.Count - 1));
					worker.InsertBefore(top, worker.Create(OpCodes.Stobj, elementType));
				}
			}
		}

		// this is required to early-out from a function with a return value.
		private static void InjectGuardReturnValue(MethodDefinition md, ILProcessor worker, Instruction top)
		{
			if (!md.ReturnType.Is(typeof(void)))
			{
				md.Body.Variables.Add(new VariableDefinition(md.ReturnType));
				md.Body.InitLocals = true;

				worker.InsertBefore(top, worker.Create(OpCodes.Ldloca_S, (byte)(md.Body.Variables.Count - 1)));
				worker.InsertBefore(top, worker.Create(OpCodes.Initobj, md.ReturnType));
				worker.InsertBefore(top, worker.Create(OpCodes.Ldloc, md.Body.Variables.Count - 1));
			}
		}
	}
}