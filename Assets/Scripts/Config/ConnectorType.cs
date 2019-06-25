namespace Config
{
	[System.Flags]
	public enum ConnectorType
	{
		TwoLane = 1 << 0,
		FourLane = 1 << 1,
		OneLane = 1 << 2,
		HighFour = 1 << 3,
		HighRoad = 1 << 4,
		Pavement = 1 << 5,
		RoadProps = 1 << 6,
		Building = 1 << 7,
		BigBuilding = 1 << 8,
	}

	public static class ConnectorTypeUtility
	{
		public static bool Compatible(this ConnectorType a, ConnectorType b)
		{
			return (a & b) != 0;
		}
	}
}