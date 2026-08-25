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
    public sealed class SFScreenNode : SFConfigNode
    {
        [SFAsset(typeof(SFScreenView))]
        public string Prefab;
        [HideInInspector] [SerializeField] [ReadOnly]
        private string ScreenType;
        
		public bool Preload;
        public SFUICloseBehaviour CloseBehaviour;
        public SFWidgetNode[] Widgets;
        
        public override ISFConfigNode[] Children => Widgets;
        
        public Type GetScreenType() => Type.GetType(ScreenType);
        
#if UNITY_EDITOR
        internal void OnValidate()
        {
            if (string.IsNullOrEmpty(Prefab))
            {
                ScreenType = null;
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
                ScreenType = null;
                handle.Release();
                return;
            }

            ScreenType = view.GetType().AssemblyQualifiedName;

            handle.Release();
        }
#endif
    }
}