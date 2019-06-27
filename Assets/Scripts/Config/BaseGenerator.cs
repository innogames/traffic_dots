
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.Entities;
using UnityEngine;

namespace Config
{
	public abstract class BaseGenerator : MonoBehaviour
	{
		public CityConfig CachedConfig;
#if UNITY_EDITOR
		public CityConfig GetConfig
		{
			get
			{
				if (CachedConfig != null)
				{
					return CachedConfig;
				}
				else
				{
					CachedConfig = Resources.FindObjectsOfTypeAll<CityConfig>().First();
					EditorUtility.SetDirty(this);
					return CachedConfig;
				}
			}
		}

		public virtual void Generate(CityConfig config)
		{
			CachedConfig = config;
			CleanComponentProxys();
			EditorUtility.SetDirty(this);
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
		public virtual void PlayModeGenerate(CityConfig config)
		{			
		}
	}
}