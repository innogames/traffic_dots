using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Connection : IComponentData, IEquatable<Connection>
	{
		public Entity StartNode;
		public Entity EndNode;
		public float Speed;//TODO move speed out to separate component
		public int Level;//TODO move Level to NetworkLevel component
		public Entity OnlyNext;

		public bool Equals(Connection other)
		{
			return StartNode.Equals(other.StartNode) && EndNode.Equals(other.EndNode);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Connection other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (StartNode.GetHashCode() * 397) ^ EndNode.GetHashCode();
			}
		}
	}
}