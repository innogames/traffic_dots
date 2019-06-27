using System;

namespace Model.Components
{
	[Flags]
	public enum TargetType
	{
		Donut = 1 << 0,
		Shop = 1 << 1,
		Police = 1 << 2,
		Hospital = 1 << 3,
		Fire = 1 << 4,
		Bus = 1 << 5,
	}
}