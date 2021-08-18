using System;
using System.Collections.Generic;
using Mono.CecilX;
using Mono.CecilX.Cil;
using Mono.CecilX.Rocks;

namespace Mirror.Weaver
{
	public static class Writers
	{
		private static Dictionary<TypeReference, MethodReference> writeFuncs;

		public static void Init()
		{
			writeFuncs = new Dictionary<TypeReference, MethodReference>(new TypeReferenceComparer());
		}

		public static void Register(TypeReference dataType, MethodReference methodReference)
		{
			if (writeFuncs.ContainsKey(dataType))
			{
				// TODO enable this again later.
				// Writer has some obsolete functions that were renamed.
				// Don't want weaver warnings for all of them.
				//Weaver.Warning($"Registering a Write method for {dataType.FullName} when one already exists", methodReference);
			}

			// we need to import type when we Initialize Writers so import here in case it is used anywhere else
			var imported = Weaver.CurrentAssembly.MainModule.ImportReference(dataType);
			writeFuncs[imported] = methodReference;
		}

		private static void RegisterWriteFunc(TypeReference typeReference, MethodDefinition newWriterFunc)
		{
			Register(typeReference, newWriterFunc);

			Weaver.GeneratedCodeClass.Methods.Add(newWriterFunc);
		}

		/// <summary>
		/// Finds existing writer for type, if non exists trys to create one
		/// <para>This method is recursive</para>
		/// </summary>
		/// <param name="variable"></param>
		/// <returns>Returns <see cref="MethodReference"/> or null</returns>
		public static MethodReference GetWriteFunc(TypeReference variable)
		{
			if (writeFuncs.TryGetValue(variable, out var foundFunc))
			{
				return foundFunc;
			}
			else
			{
				// this try/catch will be removed in future PR and make `GetWriteFunc` throw instead
				try
				{
					var importedVariable = Weaver.CurrentAssembly.MainModule.ImportReference(variable);
					return GenerateWriter(importedVariable);
				}
				catch (GenerateWriterException e)
				{
					Weaver.Error(e.Message, e.MemberReference);
					return null;
				}
			}
		}

		/// <exception cref="GenerateWriterException">Throws when writer could not be generated for type</exception>
		private static MethodReference GenerateWriter(TypeReference variableReference)
		{
			if (variableReference.IsByReference)
			{
				throw new GenerateWriterException($"Cannot pass {variableReference.Name} by reference", variableReference);
			}

			// Arrays are special, if we resolve them, we get the element type,
			// e.g. int[] resolves to int
			// therefore process this before checks below
			if (variableReference.IsArray)
			{
				if (variableReference.IsMultidimensionalArray())
				{
					throw new GenerateWriterException($"{variableReference.Name} is an unsupported type. Multidimensional arrays are not supported", variableReference);
				}
				var elementType = variableReference.GetElementType();
				return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteArray));
			}

			if (variableReference.Resolve()?.IsEnum ?? false)
			{
				// serialize enum as their base type
				return GenerateEnumWriteFunc(variableReference);
			}

			// check for collections
			if (variableReference.Is(typeof(ArraySegment<>)))
			{
				var genericInstance = (GenericInstanceType)variableReference;
				var elementType = genericInstance.GenericArguments[0];

				return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteArraySegment));
			}
			if (variableReference.Is(typeof(List<>)))
			{
				var genericInstance = (GenericInstanceType)variableReference;
				var elementType = genericInstance.GenericArguments[0];

				return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteList));
			}

			if (variableReference.IsDerivedFrom<NetworkBehaviour>())
			{
				return GetNetworkBehaviourWriter(variableReference);
			}

			// check for invalid types
			var variableDefinition = variableReference.Resolve();
			if (variableDefinition == null)
			{
				throw new GenerateWriterException($"{variableReference.Name} is not a supported type. Use a supported type or provide a custom writer", variableReference);
			}
			if (variableDefinition.IsDerivedFrom<UnityEngine.Component>())
			{
				throw new GenerateWriterException($"Cannot generate writer for component type {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
			}
			if (variableReference.Is<UnityEngine.Object>())
			{
				throw new GenerateWriterException($"Cannot generate writer for {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
			}
			if (variableReference.Is<UnityEngine.ScriptableObject>())
			{
				throw new GenerateWriterException($"Cannot generate writer for {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
			}
			if (variableDefinition.HasGenericParameters)
			{
				throw new GenerateWriterException($"Cannot generate writer for generic type {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
			}
			if (variableDefinition.IsInterface)
			{
				throw new GenerateWriterException($"Cannot generate writer for interface {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
			}
			if (variableDefinition.IsAbstract)
			{
				throw new GenerateWriterException($"Cannot generate writer for abstract class {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
			}

			// generate writer for class/struct
			return GenerateClassOrStructWriterFunction(variableReference);
		}

		private static MethodReference GetNetworkBehaviourWriter(TypeReference variableReference)
		{
			// all NetworkBehaviours can use the same write function
			if (writeFuncs.TryGetValue(WeaverTypes.Import<NetworkBehaviour>(), out var func))
			{
				// register function so it is added to writer<T>
				// use Register instead of RegisterWriteFunc because this is not a generated function
				Register(variableReference, func);

				return func;
			}
			else
			{
				// this exception only happens if mirror is missing the WriteNetworkBehaviour method
				throw new MissingMethodException($"Could not find writer for NetworkBehaviour");
			}
		}

		private static MethodDefinition GenerateEnumWriteFunc(TypeReference variable)
		{
			var writerFunc = GenerateWriterFunc(variable);

			var worker = writerFunc.Body.GetILProcessor();

			var underlyingWriter = GetWriteFunc(variable.Resolve().GetEnumUnderlyingType());

			worker.Emit(OpCodes.Ldarg_0);
			worker.Emit(OpCodes.Ldarg_1);
			worker.Emit(OpCodes.Call, underlyingWriter);

			worker.Emit(OpCodes.Ret);
			return writerFunc;
		}

		private static MethodDefinition GenerateWriterFunc(TypeReference variable)
		{
			string functionName = "_Write_" + variable.FullName;
			// create new writer for this type
			var writerFunc = new MethodDefinition(functionName,
					MethodAttributes.Public |
					MethodAttributes.Static |
					MethodAttributes.HideBySig,
					WeaverTypes.Import(typeof(void)));

			writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, WeaverTypes.Import<NetworkWriter>()));
			writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, variable));
			writerFunc.Body.InitLocals = true;

			RegisterWriteFunc(variable, writerFunc);
			return writerFunc;
		}

		private static MethodDefinition GenerateClassOrStructWriterFunction(TypeReference variable)
		{
			var writerFunc = GenerateWriterFunc(variable);

			var worker = writerFunc.Body.GetILProcessor();

			if (!variable.Resolve().IsValueType)
			{
				WriteNullCheck(worker);
			}

			if (!WriteAllFields(variable, worker))
			{
				return null;
			}

			worker.Emit(OpCodes.Ret);
			return writerFunc;
		}

		private static void WriteNullCheck(ILProcessor worker)
		{
			// if (value == null)
			// {
			//     writer.WriteBoolean(false);
			//     return;
			// }
			//

			var labelNotNull = worker.Create(OpCodes.Nop);
			worker.Emit(OpCodes.Ldarg_1);
			worker.Emit(OpCodes.Brtrue, labelNotNull);
			worker.Emit(OpCodes.Ldarg_0);
			worker.Emit(OpCodes.Ldc_I4_0);
			worker.Emit(OpCodes.Call, GetWriteFunc(WeaverTypes.Import<bool>()));
			worker.Emit(OpCodes.Ret);
			worker.Append(labelNotNull);

			// write.WriteBoolean(true);
			worker.Emit(OpCodes.Ldarg_0);
			worker.Emit(OpCodes.Ldc_I4_1);
			worker.Emit(OpCodes.Call, GetWriteFunc(WeaverTypes.Import<bool>()));
		}

		/// <summary>
		/// Find all fields in type and write them
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="worker"></param>
		/// <returns>false if fail</returns>
		private static bool WriteAllFields(TypeReference variable, ILProcessor worker)
		{
			uint fields = 0;
			foreach (var field in variable.FindAllPublicFields())
			{
				var writeFunc = GetWriteFunc(field.FieldType);
				// need this null check till later PR when GetWriteFunc throws exception instead
				if (writeFunc == null)
				{ return false; }

				var fieldRef = Weaver.CurrentAssembly.MainModule.ImportReference(field);

				fields++;
				worker.Emit(OpCodes.Ldarg_0);
				worker.Emit(OpCodes.Ldarg_1);
				worker.Emit(OpCodes.Ldfld, fieldRef);
				worker.Emit(OpCodes.Call, writeFunc);
			}

			return true;
		}

		private static MethodDefinition GenerateCollectionWriter(TypeReference variable, TypeReference elementType, string writerFunction)
		{

			var writerFunc = GenerateWriterFunc(variable);

			var elementWriteFunc = GetWriteFunc(elementType);
			var intWriterFunc = GetWriteFunc(WeaverTypes.Import<int>());

			// need this null check till later PR when GetWriteFunc throws exception instead
			if (elementWriteFunc == null)
			{
				Weaver.Error($"Cannot generate writer for {variable}. Use a supported type or provide a custom writer", variable);
				return writerFunc;
			}

			var module = Weaver.CurrentAssembly.MainModule;
			var readerExtensions = module.ImportReference(typeof(NetworkWriterExtensions));
			var collectionWriter = Resolvers.ResolveMethod(readerExtensions, Weaver.CurrentAssembly, writerFunction);

			var methodRef = new GenericInstanceMethod(collectionWriter);
			methodRef.GenericArguments.Add(elementType);

			// generates
			// reader.WriteArray<T>(array);

			var worker = writerFunc.Body.GetILProcessor();
			worker.Emit(OpCodes.Ldarg_0); // writer
			worker.Emit(OpCodes.Ldarg_1); // collection

			worker.Emit(OpCodes.Call, methodRef); // WriteArray

			worker.Emit(OpCodes.Ret);

			return writerFunc;
		}

		/// <summary>
		/// Save a delegate for each one of the writers into <see cref="Writer{T}.write"/>
		/// </summary>
		/// <param name="worker"></param>
		internal static void InitializeWriters(ILProcessor worker)
		{
			var module = Weaver.CurrentAssembly.MainModule;

			var genericWriterClassRef = module.ImportReference(typeof(Writer<>));

			var fieldInfo = typeof(Writer<>).GetField(nameof(Writer<object>.write));
			var fieldRef = module.ImportReference(fieldInfo);
			var networkWriterRef = module.ImportReference(typeof(NetworkWriter));
			var actionRef = module.ImportReference(typeof(Action<,>));
			var actionConstructorRef = module.ImportReference(typeof(Action<,>).GetConstructors()[0]);

			foreach (var kvp in writeFuncs)
			{
				var targetType = kvp.Key;
				var writeFunc = kvp.Value;

				// create a Action<NetworkWriter, T> delegate
				worker.Emit(OpCodes.Ldnull);
				worker.Emit(OpCodes.Ldftn, writeFunc);
				var actionGenericInstance = actionRef.MakeGenericInstanceType(networkWriterRef, targetType);
				var actionRefInstance = actionConstructorRef.MakeHostInstanceGeneric(actionGenericInstance);
				worker.Emit(OpCodes.Newobj, actionRefInstance);

				// save it in Writer<T>.write
				var genericInstance = genericWriterClassRef.MakeGenericInstanceType(targetType);
				var specializedField = fieldRef.SpecializeField(genericInstance);
				worker.Emit(OpCodes.Stsfld, specializedField);
			}
		}

	}
}
