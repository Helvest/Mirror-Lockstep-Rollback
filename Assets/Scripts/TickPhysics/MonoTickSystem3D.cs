using System;
using TickPhysics;
using UnityEngine;

// MonoTickSystem3D is a concrete implementation of AbstractMonoTickSystem3D
public class MonoTickSystem3D : AbstractMonoTickSystem3D<TickSystem3D> { }

// AbstractMonoTickSystem3D is an abstract class that provides a base implementation for a MonoBehaviour-based tick system
public abstract class AbstractMonoTickSystem3D<T> : AbstractMonoTickSystem<T> where T : TickSystem3D
{

	#region SimulationMode

	// autoSimulation is a flag that determines whether the simulation should run automatically
	[SerializeField]
	protected bool autoSimulation = false;

	// simulationMode determines the deltaTime the simulation use
	[SerializeField]
	protected SimulationMode simulationMode = SimulationMode.Script;

	// AutoSimulation is a property that gets and sets the value of autoSimulation
	public virtual bool AutoSimulation
	{
		get => TickSystem.AutoSimulation;
		set
		{
			TickSystem.AutoSimulation = autoSimulation = value;
			simulationMode = TickSystem.SimulationMode;
		}
	}

	// SimulationMode is a property that gets and sets the value of simulationMode
	public virtual SimulationMode SimulationMode
	{
		get => TickSystem.SimulationMode;
		set
		{
			TickSystem.SimulationMode = simulationMode = value;
			autoSimulation = TickSystem.AutoSimulation;
		}
	}

	#endregion

	#region Init

	protected override void OnEnable()
	{
		TickSystem.SimulationMode = simulationMode;
		AutoSimulation = autoSimulation;
		base.OnEnable();

		// Register the CustomUpdate method to be called on each physics update
		PhysicLoop.OnPhysicUpdateEvent += CustomUpdate;
	}

	protected virtual void OnDisable()
	{
		// Unregister the CustomUpdate method from being called on each physics update
		PhysicLoop.OnPhysicUpdateEvent -= CustomUpdate;
	}

	#endregion

	#region Update

	// AutoUpdate determines the mode in which the simulation should automatically update
	[field: SerializeField]
	public virtual SimulationMode AutoUpdate { get; set; } = SimulationMode.Script;

	// _wasUpdated is a flag that determines whether the simulation has been updated in the current frame
	protected bool _wasUpdated = false;

	// overrideFixedDeltaTime is the fixed delta time to use when the simulation mode is set to Script
	[Range(1f, 0.01f)]
	public float overrideFixedDeltaTime = 0.5f;

	// previousTime is the time of the previous frame
	protected double previousTime = 0f;

	// FixedUpdate is called at a fixed interval
	protected virtual void FixedUpdate()
	{
		if (_wasUpdated)
		{
			return;
		}

		if (AutoUpdate == SimulationMode.FixedUpdate)
		{
			_wasUpdated = true;

			var deltaTime = Time.timeAsDouble - previousTime;

			switch (simulationMode)
			{
				case SimulationMode.FixedUpdate:
				{
					// Tick the simulation using the fixed delta time
					TickSystem.Tick(Time.timeAsDouble, deltaTime, Time.fixedDeltaTime);
					break;
				}
				case SimulationMode.Update:
				{
					// Tick the simulation using the delta time
					TickSystem.Tick(Time.timeAsDouble, deltaTime, deltaTime);
					break;
				}
				case SimulationMode.Script:
				{
					// Tick the simulation using the override fixed delta time
					TickSystem.Tick(Time.timeAsDouble, deltaTime, overrideFixedDeltaTime);
					break;
				}
			}
		}

		previousTime = Time.timeAsDouble;
	}

	// Update is called once per frame
	protected virtual void Update()
	{
		if (AutoUpdate == SimulationMode.Update && !_wasUpdated)
		{
			switch (simulationMode)
			{
				case SimulationMode.FixedUpdate:
				{
					// Tick the simulation using the fixed delta time
					TickSystem.Tick(Time.timeAsDouble, Time.deltaTime, Time.fixedDeltaTime);
					break;
				}

				case SimulationMode.Update:
				{
					// Tick the simulation using the delta time
					TickSystem.Tick(Time.timeAsDouble, Time.deltaTime, Time.deltaTime);
					break;
				}

				case SimulationMode.Script:
				{
					// Tick the simulation using the override fixed delta time
					TickSystem.Tick(Time.timeAsDouble, Time.deltaTime, overrideFixedDeltaTime);
					break;
				}
			}
		}

		_wasUpdated = false;
	}

	// CustomUpdate is called on each physics update
	protected virtual void CustomUpdate()
	{
		if (AutoUpdate != SimulationMode.Script || _wasUpdated)
		{
			return;
		}

		switch (simulationMode)
		{
			case SimulationMode.FixedUpdate:
			{
				// Tick the simulation using the fixed delta time
				TickSystem.Tick(Time.timeAsDouble, Time.deltaTime, Time.fixedDeltaTime);
				break;
			}

			case SimulationMode.Update:
			{
				// Tick the simulation using the delta time
				TickSystem.Tick(Time.timeAsDouble, Time.deltaTime, Time.deltaTime);
				break;
			}

			case SimulationMode.Script:
			{
				// Tick the simulation using the override fixed delta time
				TickSystem.Tick(Time.timeAsDouble, Time.deltaTime, overrideFixedDeltaTime);
				break;
			}
		}
	}

	// Tick is called to update the simulation
	public override void Tick(double time, double deltaTime, double fixedDeltaTime)
	{
		TickSystem.Tick(time, deltaTime, overrideFixedDeltaTime);
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
