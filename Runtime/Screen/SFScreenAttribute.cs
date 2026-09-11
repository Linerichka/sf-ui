using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace SFramework.UI.Runtime
{
    [Preserve]
    [AttributeUsage(AttributeTargets.Field)]
    public class SFScreenAttribute : PropertyAttribute
    {
    }
}