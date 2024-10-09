using UnityEngine;

namespace TickPhysics
{
	public class AutoAddToTickManager : MonoBehaviour
	{
		private IPhysicsObject[] _physicObjects;

		[SerializeField]
		private bool _alsoAddChildrenComponents = false;

		private void Awake()
		{
			if (_alsoAddChildrenComponents)
			{
				_physicObjects = GetComponentsInChildren<IPhysicsObject>();
			}
			else
			{
				_physicObjects = GetComponents<IPhysicsObject>();
			}
		}

		private void OnEnable()
		{
			if (_physicObjects.Length != 0 && SL.TryGet(out ITickSystem tickSystem))
			{
				//Debug.Log("Add " + _physicObjects?.Length +" script from " + gameObject.name, gameObject);

				tickSystem.Add(_physicObjects);
			}
		}

		private void OnDisable()
		{
			if (_physicObjects.Length != 0 && SL.TryGet(out ITickSystem tickSystem))
			{
				tickSystem.Remove(_physicObjects);
			}
		}
	}
}
