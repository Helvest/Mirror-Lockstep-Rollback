
namespace HFSM
{
	public interface ITimer
	{
		float Elapsed { get; }

		void Reset();
	}
}
