using TickPhysics;
using UnityEngine;

public class TickSystem3D : TickSystem
{

	#region SimulationMode

	public virtual bool AutoSimulation
	{
		get => Physics.simulationMode != SimulationMode.Script;
		set
		{
			if (AutoSimulation != value)
			{
				Physics.simulationMode = value ? SimulationMode.FixedUpdate : SimulationMode.Script;
			}
		}
	}

	public virtual SimulationMode SimulationMode
	{
		get => Physics.simulationMode;

		set => Physics.simulationMode = value;
	}

	#endregion

	#region SimulatePhysic

	protected override void SimulatePhysic(double fixedDeltaTime)
	{
		if (!AutoSimulation)
		{
			Physics.Simulate((float)fixedDeltaTime);
		}
	}

	#endregion

}

public class MonoTickSystem3D : MonoTickSystem
{

	#region Fields

	protected override ITickSystem TickSystem => TickSystem3D;

	protected TickSystem3D TickSystem3D { get; } = new TickSystem3D();

	#endregion

	#region SimulationMode

	[SerializeField]
	protected bool autoSimulation = false;

	[SerializeField]
	protected SimulationMode simulationMode = SimulationMode.Script;

	public virtual bool AutoSimulation
	{
		get => TickSystem3D.AutoSimulation;

		set
		{
			TickSystem3D.AutoSimulation = autoSimulation = value;
			simulationMode = TickSystem3D.SimulationMode;
		}
	}

	public virtual SimulationMode SimulationMode
	{
		get => TickSystem3D.SimulationMode;

		set
		{
			TickSystem3D.SimulationMode = simulationMode = value;
			autoSimulation = TickSystem3D.AutoSimulation;
		}
	}

	#endregion

	#region OnEnable

	protected override void OnEnable()
	{
		TickSystem3D.SimulationMode = simulationMode;
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
