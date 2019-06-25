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
			CleanComponentProxys();
			EditorUtility.SetDirty(this);
		}

		public virtual void PlayModeGenerate(CityConfig config)
		{			
		}

		public virtual void Clean()
		{
			CleanComponentProxys();
			CleanGOEntity();
			EditorUtility.SetDirty(gameObject);
		}

		private void CleanGOEntity()
		{
			var goEntity = gameObject.GetComponent<GameObjectEntity>();
			if (goEntity != null)
			{
				DestroyImmediate(goEntity);
			}
		}

		protected virtual bool ShouldCleanComponent() => true;

		private void CleanComponentProxys()
		{
			if (ShouldCleanComponent())
			{
				var proxys = GetComponents<ComponentDataProxyBase>();
				foreach (var proxy in proxys)
				{
					DestroyImmediate(proxy);
				}				
			}
		}
#endif
	}
}