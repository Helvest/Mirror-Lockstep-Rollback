using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityAssembly = UnityEditor.Compilation.Assembly;

namespace Mirror.Weaver
{
	public static class CompilationFinishedHook
	{
		private const string MirrorRuntimeAssemblyName = "Mirror";
		private const string MirrorWeaverAssemblyName = "Mirror.Weaver";

		// delegate for subscription to Weaver warning messages
		public static Action<string> OnWeaverWarning;
		// delete for subscription to Weaver error messages
		public static Action<string> OnWeaverError;

		// controls weather Weaver errors are reported direct to the Unity console (tests enable this)
		public static bool UnityLogEnabled = true;

		// warning message handler that also calls OnWarningMethod delegate
		private static void HandleWarning(string msg)
		{
			if (UnityLogEnabled)
			{
				Debug.LogWarning(msg);
			}

			OnWeaverWarning?.Invoke(msg);
		}

		// error message handler that also calls OnErrorMethod delegate
		private static void HandleError(string msg)
		{
			if (UnityLogEnabled)
			{
				Debug.LogError(msg);
			}

			OnWeaverError?.Invoke(msg);
		}

		[InitializeOnLoadMethod]
		public static void OnInitializeOnLoad()
		{
			CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;

			// We only need to run this once per session
			// after that, all assemblies will be weaved by the event
			if (!SessionState.GetBool("MIRROR_WEAVED", false))
			{
				// reset session flag
				SessionState.SetBool("MIRROR_WEAVED", true);
				SessionState.SetBool("MIRROR_WEAVE_SUCCESS", true);

				WeaveExistingAssemblies();
			}
		}

		public static void WeaveExistingAssemblies()
		{
			foreach (var assembly in CompilationPipeline.GetAssemblies())
			{
				if (File.Exists(assembly.outputPath))
				{
					OnCompilationFinished(assembly.outputPath, new CompilerMessage[0]);
				}
			}

#if UNITY_2019_3_OR_NEWER
			EditorUtility.RequestScriptReload();
#else
            UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
#endif
		}

		private static string FindMirrorRuntime()
		{
			foreach (var assembly in CompilationPipeline.GetAssemblies())
			{
				if (assembly.name == MirrorRuntimeAssemblyName)
				{
					return assembly.outputPath;
				}
			}
			return "";
		}

		private static bool CompilerMessagesContainError(CompilerMessage[] messages)
		{
			return messages.Any(msg => msg.type == CompilerMessageType.Error);
		}

		private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
		{
			// Do nothing if there were compile errors on the target
			if (CompilerMessagesContainError(messages))
			{
				Debug.Log("Weaver: stop because compile errors on target");
				return;
			}

			// Should not run on the editor only assemblies
			if (assemblyPath.Contains("-Editor") || assemblyPath.Contains(".Editor"))
			{
				return;
			}

			// don't weave mirror files
			string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
			if (assemblyName == MirrorRuntimeAssemblyName || assemblyName == MirrorWeaverAssemblyName)
			{
				return;
			}

			// find Mirror.dll
			string mirrorRuntimeDll = FindMirrorRuntime();
			if (string.IsNullOrEmpty(mirrorRuntimeDll))
			{
				Debug.LogError("Failed to find Mirror runtime assembly");
				return;
			}
			if (!File.Exists(mirrorRuntimeDll))
			{
				// this is normal, it happens with any assembly that is built before mirror
				// such as unity packages or your own assemblies
				// those don't need to be weaved
				// if any assembly depends on mirror, then it will be built after
				return;
			}

			// find UnityEngine.CoreModule.dll
			string unityEngineCoreModuleDLL = UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath();
			if (string.IsNullOrEmpty(unityEngineCoreModuleDLL))
			{
				Debug.LogError("Failed to find UnityEngine assembly");
				return;
			}

			var dependencyPaths = GetDependecyPaths(assemblyPath);
			dependencyPaths.Add(Path.GetDirectoryName(mirrorRuntimeDll));
			dependencyPaths.Add(Path.GetDirectoryName(unityEngineCoreModuleDLL));
			Log.Warning = HandleWarning;
			Log.Error = HandleError;

			if (!Weaver.WeaveAssembly(assemblyPath, dependencyPaths.ToArray()))
			{
				// Set false...will be checked in \Editor\EnterPlayModeSettingsCheck.CheckSuccessfulWeave()
				SessionState.SetBool("MIRROR_WEAVE_SUCCESS", false);
				if (UnityLogEnabled)
				{
					Debug.LogError("Weaving failed for: " + assemblyPath);
				}
			}
		}

		private static HashSet<string> GetDependecyPaths(string assemblyPath)
		{
			// build directory list for later asm/symbol resolving using CompilationPipeline refs
			var dependencyPaths = new HashSet<string>
			{
				Path.GetDirectoryName(assemblyPath)
			};
			foreach (var unityAsm in CompilationPipeline.GetAssemblies())
			{
				if (unityAsm.outputPath == assemblyPath)
				{
					foreach (string unityAsmRef in unityAsm.compiledAssemblyReferences)
					{
						dependencyPaths.Add(Path.GetDirectoryName(unityAsmRef));
					}
				}
			}

			return dependencyPaths;
		}
	}
}