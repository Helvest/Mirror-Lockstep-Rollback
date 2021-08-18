using UnityEngine;

public class AutoAddToSL : MonoBehaviour
{
	[SerializeField]
	private MonoBehaviour[] _monoBehaviourToAdd = default;

	private void OnEnable()
	{
		foreach (var monoBehaviour in _monoBehaviourToAdd)
		{
			SL.Add(monoBehaviour);
		}
	}

	private void OnDisable()
	{
		foreach (var monoBehaviour in _monoBehaviourToAdd)
		{
			SL.Remove(monoBehaviour);
		}
	}
}
