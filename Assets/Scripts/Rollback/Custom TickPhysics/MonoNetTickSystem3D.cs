using UnityEngine;

public class MonoNetTickSystem3D : MonoNetTickSystem
{

	#region Fields

	protected override INetTickSystem NetTickSystem => NetTickSystem3D;

	[field: SerializeField]
	protected virtual NetTickSystem3D NetTickSystem3D { get; private set; } = new NetTickSystem3D();

	[SerializeField]
	protected bool autoSimulation = false;

	public virtual bool AutoSimulation
	{
		get => NetTickSystem3D.AutoSimulation;

		set => NetTickSystem3D.AutoSimulation = autoSimulation = value;
	}

	#endregion

	#region OnEnable

	protected override void OnEnable()
	{
		AutoSimulation = autoSimulation;
		base.OnEnable();
	}

	#endregion

}