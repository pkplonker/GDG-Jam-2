using System.Collections.Generic;

public enum TrapType
{
	Trap,
	Explosives
}

public class Trap
{
	public List<Node> nodes = new();
	public TrapType trapType;

	public Trap(List<Node> nodes, TrapType type)
	{
		trapType = type;
		this.nodes = nodes;
	}

	public Trap(List<Node> nodes)
	{
		trapType = Extensions.GetRandomEnumValue<TrapType>();
		this.nodes = nodes;
	}
}