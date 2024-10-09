using System;
using TickPhysics;
using UnityEngine;

[Serializable]
public class TickSystem2D : TickSystem
{

	#region AutoSimulation

	public virtual bool AutoSimulation
	{
		get => Physics2D.simulationMode != SimulationMode2D.Script;
		set
		{
			if (AutoSimulation != value)
			{
				Physics2D.simulationMode = value ? SimulationMode2D.FixedUpdate : SimulationMode2D.Script;
			}
		}
	}

	public virtual SimulationMode2D SimulationMode
	{
		get => Physics2D.simulationMode;

		set => Physics2D.simulationMode = value;
	}

	#endregion

	#region CalculateExtraDeltaTime

	protected override void CalculateExtraDeltaTime()
	{
		switch (SimulationMode)
		{
			default:
			case SimulationMode2D.Update:
				ExtraDeltaTime = 0;
				break;

			case SimulationMode2D.Script:
			case SimulationMode2D.FixedUpdate:
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
			Physics2D.Simulate((float)fixedDeltaTime);
		}
	}

	#endregion

}
