using Model.Components;
using UnityEngine;

namespace Config
{
	[RequireComponent(typeof(Connection))]
	public class TargetMarker : MonoBehaviour
	{
		[EnumFlags] public TargetType TargetMask;
	}
}