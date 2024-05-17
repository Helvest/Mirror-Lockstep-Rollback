using UnityEngine;

public class MonoNetTickSystem3D : MonoNetTickSystem
{

	#region Fields

	protected override INetTickSystem NetTickSystem => NetTickSystem3D;

	[field: SerializeField]
	protected virtual NetTickSystem3D NetTickSystem3D { get; private set; } = new NetTickSystem3D();

	#endregion

	#region SimulationMode

	[SerializeField]
	protected bool autoSimulation = false;

	[SerializeField]
	protected SimulationMode simulationMode = SimulationMode.Script;

	public virtual bool AutoSimulation
	{
		get => NetTickSystem3D.AutoSimulation;

		set
		{
			NetTickSystem3D.AutoSimulation = autoSimulation = value;
			simulationMode = NetTickSystem3D.SimulationMode;
		}
	}

	public virtual SimulationMode SimulationMode
	{
		get => NetTickSystem3D.SimulationMode;

		set
		{
			NetTickSystem3D.SimulationMode = simulationMode = value;
			autoSimulation = NetTickSystem3D.AutoSimulation;
		}
	}

	#endregion

	#region OnEnable

	protected override void OnEnable()
	{
		NetTickSystem3D.SimulationMode = simulationMode;
		AutoSimulation = autoSimulation;
		base.OnEnable();
	}

	#endregion

	#region LateUpdate

#if UNITY_EDITOR

	protected override void LateUpdate()
	{
		base.LateUpdate();

		if (AutoSimulation != autoSimulation)
		{
			AutoSimulation = autoSimulation;
		}
		else if (SimulationMode != simulationMode)
		{
			SimulationMode = simulationMode;
		}
	}

#endif

	#endregion

}