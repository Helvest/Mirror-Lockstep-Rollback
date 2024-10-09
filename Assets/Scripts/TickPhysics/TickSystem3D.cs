using System;
using TickPhysics;
using UnityEngine;

[Serializable]
public class TickSystem3D : TickSystem
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

	#region CalculateExtraDeltaTime

	protected override void CalculateExtraDeltaTime()
	{
		switch (SimulationMode)
		{
			default:
			case SimulationMode.Update:
				ExtraDeltaTime = 0;
				break;

			case SimulationMode.Script:
			case SimulationMode.FixedUpdate:
				ExtraDeltaTime = (float)(NormalTime - FixedTime);
				break;
		}
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
