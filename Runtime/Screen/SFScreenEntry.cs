using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SFramework.UI.Runtime
{
    [Serializable]
    public class SFScreenEntry
    {
        internal SFScreenData ScreenData {get; private set; }
        
        public SFScreenState State { get; internal set; }
        public bool IsLoaded { get; internal set; }
        public bool IsCreated { get; internal set; }
        
        internal AsyncOperationHandle<GameObject> Handle;
        internal SFScreenView LoadedPrefab;
        internal SFScreenView ScreenObject;

        internal SFScreenEntry(SFScreenData data)
        {
            ScreenData = data;
        }

        internal void Release()
        {
            ScreenObject = null;
            LoadedPrefab = null;
            if (Handle.IsValid()) Handle.Release();
            IsLoaded = false;
            IsCreated = false;
        }

        internal void Destroy()
        {
            if (ScreenObject != null)
            {
                Object.Destroy(ScreenObject.gameObject);
            }
            IsCreated = false;
            ScreenObject = null;
        }
    }
}