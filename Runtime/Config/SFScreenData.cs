using System;
using Cysharp.Threading.Tasks;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace SFramework.UI.Runtime
{
    [Serializable]
    public sealed class SFScreenData
    {
        [SFAsset(typeof(SFScreenView))]
        public string Prefab;
        
		public bool Preload;
        public bool ShowByDefault;
        public SFUICloseBehaviour CloseBehaviour;
        
        [HideInInspector] [SerializeField]
        private string _screenTypeName;
        
        public Type GetScreenType() => Type.GetType(_screenTypeName);
        
#if UNITY_EDITOR
        internal void OnValidate()
        {
            if (string.IsNullOrEmpty(Prefab))
            {
                _screenTypeName = null;
                return;
            }

            SetScreenType().Forget();
        }

        private async UniTaskVoid SetScreenType()
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(Prefab);
            await handle;
            var view = handle.Result?.GetComponent<SFScreenView>();

            if (view == null)
            {
                _screenTypeName = null;
                handle.Release();
                return;
            }

            _screenTypeName = view.GetType().AssemblyQualifiedName;

            handle.Release();
        }
#endif
    }
}