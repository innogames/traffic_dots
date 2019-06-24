using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Config
{
	public abstract class BaseGenerator : MonoBehaviour
	{
#if UNITY_EDITOR
		public virtual void Generate(CityConfig config)
		{
			var proxys = GetComponents<ComponentDataProxyBase>();
			foreach (var proxy in proxys)
			{
				DestroyImmediate(proxy);
			}
			var goEntity = gameObject.GetComponent<GameObjectEntity>();
			if (goEntity != null)
			{
				DestroyImmediate(goEntity);
			}
			EditorUtility.SetDirty(this);
		}
#endif
	}
}