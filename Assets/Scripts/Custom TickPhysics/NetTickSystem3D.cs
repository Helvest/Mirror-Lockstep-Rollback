using UnityEngine;

public class NetTickSystem3D : NetTickSystem
{

	#region AutoSimulation

	public virtual bool AutoSimulation
	{
		get => Physics.autoSimulation;

		set => Physics.autoSimulation = value;
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
