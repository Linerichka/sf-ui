using System;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;

namespace SFramework.UI.Runtime
{
    public sealed class SFWidgetAttribute : SFIdAttribute
    {
        public SFWidgetAttribute() : base( typeof(SFUIConfig), 2)
        {
        }
    }
}