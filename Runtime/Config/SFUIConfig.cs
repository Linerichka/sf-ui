using SFramework.Configs.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SFramework.UI.Runtime
{
    [CreateAssetMenu(menuName = "SFramework/UI/Config", fileName = "ui_cfg")]
    public sealed class SFUIConfig : SFConfig
    {
        public SFScreenData[] Screens;


#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var screen in Screens)
            {
                screen?.OnValidate();
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}