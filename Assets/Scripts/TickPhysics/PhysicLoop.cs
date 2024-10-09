using System;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class PhysicLoop
{

	#region Enum

	internal enum AddMode { Beginning, End, Before, After }

	#endregion

	#region ResetStatics

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void ResetStatics()
	{
		OnPhysicUpdateEvent = null;
	}

	#endregion

	#region RuntimeInitializeOnLoad

	// hook into Unity runtime to actually add our custom functions
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void RuntimeInitializeOnLoad()
	{
		// get loop
		// 2019 has GetCURRENTPlayerLoop which is safe to use without
		// breaking other custom system's custom loops.
		var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

		// add PhysicLoop to the beginning of PhysicsFixedUpdate
		AddToPlayerLoop(OnPhysicUpdate, typeof(PhysicLoop), ref playerLoop, typeof(PreUpdate.PhysicsUpdate), AddMode.Before);

		// set the new loop
		PlayerLoop.SetPlayerLoop(playerLoop);

		//ShowPlayerLoop(ref playerLoop);
	}

	#endregion

	#region AddToPlayerLoop

	internal static bool AddToPlayerLoop(
		   PlayerLoopSystem.UpdateFunction function,
		   Type ownerType,
		   ref PlayerLoopSystem playerLoop,
		   Type playerLoopSystemType,
		   AddMode addMode)
	{

		bool subSystemListIsNotNull = playerLoop.subSystemList != null;

		// did we find the type? e.g. EarlyUpdate/PreLateUpdate/etc.
		if (playerLoop.type == playerLoopSystemType)
		{
			// debugging
			//Debug.Log($"Found playerLoop of type {playerLoop.type} with {playerLoop.subSystemList.Length} Functions:");
			//foreach (var sys in playerLoop.subSystemList)
			// Debug.Log($"  ->{sys.type}");

			// make sure the function wasn't added yet.
			// with domain reload disabled, it would otherwise be added twice:
			if (subSystemListIsNotNull && Array.FindIndex(playerLoop.subSystemList, s => s.updateDelegate == function) != -1)
			{
				// loop contains the function, so return true.
				return true;
			}

			// resize & expand subSystemList to fit one more entry
			int oldListLength = subSystemListIsNotNull ? playerLoop.subSystemList.Length : 0;
			Array.Resize(ref playerLoop.subSystemList, oldListLength + 1);

			// IMPORTANT: always insert a FRESH PlayerLoopSystem!
			// We CAN NOT resize and then OVERWRITE an entry's type/loop.
			// => PlayerLoopSystem has native IntPtr loop members
			// => forgetting to clear those would cause undefined behaviour!
			var system = new PlayerLoopSystem
			{
				type = ownerType,
				updateDelegate = function
			};

			// prepend our custom loop to the beginning
			if (addMode == AddMode.Beginning)
			{
				// shift to the right, write into first array element
				Array.Copy(playerLoop.subSystemList, 0, playerLoop.subSystemList, 1, playerLoop.subSystemList.Length - 1);
				playerLoop.subSystemList[0] = system;
			}
			// append our custom loop to the end
			else if (addMode == AddMode.End)
			{
				// simply write into last array element
				playerLoop.subSystemList[oldListLength] = system;
			}

			// new code for 'Before' mode
			else if (addMode == AddMode.Before && Array.FindIndex(playerLoop.subSystemList, s => s.type == playerLoopSystemType) != -1)
			{
				int index = Array.FindIndex(playerLoop.subSystemList, s => s.type == playerLoopSystemType);
				Array.Resize(ref playerLoop.subSystemList, playerLoop.subSystemList.Length + 1);
				Array.Copy(playerLoop.subSystemList, index, playerLoop.subSystemList, index + 1, playerLoop.subSystemList.Length - index - 1);
				playerLoop.subSystemList[index] = system;
			}
			// new code for 'After' mode
			else if (addMode == AddMode.After && Array.FindIndex(playerLoop.subSystemList, s => s.type == playerLoopSystemType) != -1)
			{
				int index = Array.FindIndex(playerLoop.subSystemList, s => s.type == playerLoopSystemType) + 1;
				Array.Resize(ref playerLoop.subSystemList, playerLoop.subSystemList.Length + 1);
				Array.Copy(playerLoop.subSystemList, index, playerLoop.subSystemList, index + 1, playerLoop.subSystemList.Length - index - 1);
				playerLoop.subSystemList[index] = system;
			}

			// debugging
			//Debug.Log($"New playerLoop of type {playerLoop.type} with {playerLoop.subSystemList.Length} Functions:");
			//foreach (PlayerLoopSystem sys in playerLoop.subSystemList)
			//    Debug.Log($"  ->{sys.type}");

			return true;
		}

		if (addMode == AddMode.Before || addMode == AddMode.After)
		{
			if (!subSystemListIsNotNull)
			{
				return false;
			}

			var system = new PlayerLoopSystem
			{
				type = ownerType,
				updateDelegate = function
			};

			if (Array.FindIndex(playerLoop.subSystemList, s => s.type == playerLoopSystemType) != -1)
			{
				int index = Array.FindIndex(playerLoop.subSystemList, s => s.type == playerLoopSystemType);

				if (addMode == AddMode.After)
				{
					index++;
				}

				Array.Resize(ref playerLoop.subSystemList, playerLoop.subSystemList.Length + 1);
				Array.Copy(playerLoop.subSystemList, index, playerLoop.subSystemList, index + 1, playerLoop.subSystemList.Length - index - 1);
				playerLoop.subSystemList[index] = system;

				return true;
			}
		}

		// recursively keep looking
		if (playerLoop.subSystemList != null)
		{
			for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
			{
				if (AddToPlayerLoop(function, ownerType, ref playerLoop.subSystemList[i], playerLoopSystemType, addMode))
					return true;
			}
		}

		return false;
	}

	#endregion

	#region Event

	public static event Action OnPhysicUpdateEvent;

	private static void OnPhysicUpdate()
	{
		if (Application.isPlaying)
		{
			OnPhysicUpdateEvent?.Invoke();
		}
	}

	#endregion

	#region Debug

	//[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void ShowLoop()
	{
		var currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
		ShowPlayerLoop(ref currentPlayerLoop);
	}

	private static void ShowPlayerLoop(ref PlayerLoopSystem playerLoop)
	{
		StringBuilder sb = new();
		ShowPlayerLoop(in playerLoop, sb, 0);
		Debug.Log(sb);
	}

	private static void ShowPlayerLoop(in PlayerLoopSystem playerLoopSystem, StringBuilder text, int inline)
	{
		if (playerLoopSystem.type != null)
		{
			for (var i = 0; i < inline; i++)
			{
				text.Append("\t");
			}
			text.AppendLine(playerLoopSystem.type.Name);
		}

		if (playerLoopSystem.subSystemList != null)
		{
			inline++;
			foreach (var subSystem in playerLoopSystem.subSystemList)
			{
				ShowPlayerLoop(in subSystem, text, inline);
			}
		}
	}

	#endregion

}

