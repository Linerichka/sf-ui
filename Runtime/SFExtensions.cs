using System;
using System.Runtime.CompilerServices;

namespace SFramework.UI.Runtime
{
    public static partial class SFExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type ToType(this string qualifiedTypeName)
        {
            return Type.GetType(qualifiedTypeName);
        }
    }
}
