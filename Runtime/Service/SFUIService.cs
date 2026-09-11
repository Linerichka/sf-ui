using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SFramework.UI.Runtime
{
    public sealed class SFUIService : ISFUIService
    {
        private Dictionary<Type, SFScreenEntry> _screenEntries = new();

        readonly Transform _parentTransform;
        
        private readonly ISFConfigsService _configsService;
        
        public SFUIService(ISFConfigsService configsService)
        {
            _parentTransform = new GameObject("SFUI").transform;
            Object.DontDestroyOnLoad(_parentTransform);
            _configsService = configsService;
        }

        public async UniTask Init(CancellationToken cancellationToken)
        {
            var preloadTasks = new List<UniTask>();

            var config = _configsService.GetConfig<SFUIConfig>();
            if (config == null) return;

            _screenEntries = new(config.Screens.Length);
            
            foreach (var screenData in config.Screens)
            {
                var screenType = screenData.GetScreenType();
                if (screenData.GetScreenType() == null)
                {
                    SFDebug.LogError("[SFUI] - Unable to load screen. Type is empty!");
                    continue;
                }

                _screenEntries.TryAdd(screenType, new SFScreenEntry(screenData));

                if (screenData.Preload)
                {
                    preloadTasks.Add(LoadScreen(screenData.GetScreenType()));
                }
            }

            await UniTask.WhenAll(preloadTasks).AttachExternalCancellation(cancellationToken);
        }


        public async UniTask LoadScreen(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to load screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }

            await LoadScreen(screen, screenEntry.ScreenData.ShowByDefault);
        }

        public async UniTask LoadScreen(Type screen, bool showByDefault)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to load screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }
            if (screenEntry.IsLoaded)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' is already loaded or is loading. Ignored repeated load.");
                return;
            }

            screenEntry.IsLoaded = true;
            
            screenEntry.Handle = Addressables.LoadAssetAsync<GameObject>(screenEntry.ScreenData.Prefab);
            await screenEntry.Handle;

            if (screenEntry.Handle.Status == AsyncOperationStatus.Failed)
            {
                screenEntry.Release();
                screenEntry.IsLoaded = false;
                SFDebug.Log(LogType.Error, $"[SFUI] - Failed to load screen '{screen.Name}'.");
                return;
            }
            
            screenEntry.LoadedPrefab = screenEntry.Handle.Result.GetComponent<SFScreenView>();
            
            await SpawnScreen(screenEntry);

            if (screenEntry.ScreenData.ShowByDefault)
            {
                await ShowScreen(screen);
            }
        }

        private async UniTask SpawnScreen(SFScreenEntry screenEntry)
        {
            screenEntry.IsCreated = true;
            screenEntry.ScreenObject = Object.Instantiate(screenEntry.LoadedPrefab, _parentTransform);
            screenEntry.State = SFScreenState.Closed;
        }

        public void UnloadScreen(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to unload screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }

            if (!screenEntry.IsLoaded)
            {
                SFDebug.Log($"[SFUI] - Unable to unload screen '{screen.Name}'. Screen is not loaded before.");
                return;
            }

            if (screenEntry.IsCreated && screenEntry.State != SFScreenState.Closed)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' not closed. Force close before unload.");
                screenEntry.ScreenObject.OnCloseScreen();
                screenEntry.ScreenObject.OnScreenClosed();
                screenEntry.State = SFScreenState.Closed;
            }

            if (screenEntry.IsCreated) screenEntry.Destroy();

            screenEntry.Release();
        }

        public async UniTask ShowScreen(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to close screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }

            if (screenEntry.State == SFScreenState.Show || screenEntry.State == SFScreenState.Shown)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' is already show. Ignored repeated show.");
                return;
            }
            
            if (!screenEntry.IsLoaded)
            {
                await LoadScreen(screen);
            }

            if (!screenEntry.IsCreated)
            {
                await SpawnScreen(screenEntry);
            }
            
            screenEntry.State = SFScreenState.Show;
            screenEntry.ScreenObject.OnShowScreen();
        }

        public void CloseScreen(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to close screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }

            if (!screenEntry.IsCreated)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' was not created before. Ignored close.");
                return;
            }
            
            if (screenEntry.State == SFScreenState.Close || 
                screenEntry.State == SFScreenState.Closed)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' is already close. Ignored repeated close.");
                return;
            }
            

            screenEntry.State = SFScreenState.Close;
            screenEntry.ScreenObject.OnCloseScreen();
        }
        
        public void ShownScreen(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to shown screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }
            if (screenEntry.State != SFScreenState.Show)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' has an unexpected state '{screenEntry.State}'. Ignored shown.");
                return;
            }
            
            screenEntry.State = SFScreenState.Shown;
            screenEntry.ScreenObject.OnScreenShown();
        }

        public void ClosedScreen(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Unable to close screen '{screen.Name}', " +
                                 $"screen is not registered. Check UI config.");
                return;
            }
            
            if (screenEntry.State != SFScreenState.Close)
            {
                if (screenEntry.State == SFScreenState.Closed)
                {
                    SFDebug.Log($"[SFUI] - Screen '{screen.Name}' is already close. Ignored repeated closed.");
                    return;
                }
                if (screenEntry.IsCreated)
                {
                    SFDebug.Log($"[SFUI] - Screen '{screen.Name}' has an unexpected state '{screenEntry.State}'. Force closed.");
                    screenEntry.ScreenObject.OnCloseScreen();
                }
                else
                {
                    SFDebug.Log($"[SFUI] - Screen '{screen.Name}' is not created. Ignored closed.");
                    return;
                }
            }
            
            // SFUICloseBehaviour.Disable
            screenEntry.ScreenObject.OnScreenClosed();
            screenEntry.State = SFScreenState.Closed;

            if (screenEntry.ScreenData.CloseBehaviour == SFUICloseBehaviour.DestroyObject ||
                screenEntry.ScreenData.CloseBehaviour == SFUICloseBehaviour.DestroyAndUnload)
            {
                screenEntry.Destroy();
            }
            
            if (screenEntry.ScreenData.CloseBehaviour == SFUICloseBehaviour.DestroyAndUnload)
            {
                UnloadScreen(screen);
            }
        }

        public SFScreenView GetScreenView(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Screen '{screen.Name}', " +
                                 $"is not registered. Check UI config.");
                return null;
            }

            if (!screenEntry.IsCreated)
            {
                SFDebug.Log($"[SFUI] - Screen '{screen.Name}' is not created.");
                return null;
            }
            
            return screenEntry.ScreenObject;
        }

        public T GetScreenView<T>() where T : SFScreenView, new()
        {
            return GetScreenView(typeof(T)) as T;
        }

        public bool TryGetScreenView<T>(out T screenView) where T : SFScreenView, new()
        {
            if (!_screenEntries.TryGetValue(typeof(T), out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Screen '{typeof(T).Name}', " +
                                 $"is not registered. Check UI config.");
                screenView = null;
                return false;
            }

            if (!screenEntry.IsCreated)
            {
                screenView = null;
                return false;
            }

            screenView = screenEntry.ScreenObject as T;
            return true;
        }

        public SFScreenEntry GetScreenEntry(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Screen '{screen.Name}', " +
                                 $"is not registered. Check UI config.");
                return null;
            }

            return screenEntry;
        }

        public SFScreenState GetScreenState(Type screen)
        {
            if (!_screenEntries.TryGetValue(screen, out var screenEntry))
            {
                SFDebug.LogError($"[SFUI] - Screen '{screen.Name}', " +
                                 $"is not registered. Check UI config.");
                return SFScreenState.Closed;
            }
            
            if (!screenEntry.IsCreated)  return SFScreenState.Closed;
            return screenEntry.State;
        }

        public void Dispose()
        {
            foreach (var screenEntry in _screenEntries.Values)
            {
                if (screenEntry.IsCreated) screenEntry.Destroy();
                screenEntry.Release();
            }

            _screenEntries = null;
        }
    }
}