using System;
using UnityEngine;

[Serializable]
public class NetTickSystem3D : NetTickSystem
{

	#region AutoSimulation

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
