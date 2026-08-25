using System;
using SFramework.Configs.Runtime;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SFramework.UI.Runtime
{
    public sealed class SFUIConfig : SFNodesConfig
    {
        public SFScreenNode[] Screens;

        public override ISFConfigNode[] Children => Screens;


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