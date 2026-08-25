using System;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;

namespace SFramework.UI.Runtime
{
    public sealed class SFScreenAttribute : SFIdAttribute
    {
        public SFScreenAttribute() : base( typeof(SFUIConfig),1)
        {
        }
    }
}