using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SFramework.UI.Runtime
{
    public sealed class SFUIService : ISFUIService
    {
        public event Action<string> OnUnloadScreen = _ => { };
        public event Action<string> OnScreenUnloaded = _ => { };
        public event Action<string, bool, object[]> OnShowScreen = (_, _, _) => { };
        public event Action<string, bool, bool> OnCloseScreen = (_, _, _) => { };
        public event Action<string> OnScreenShown = _ => { };
        public event Action<string> OnScreenClosed = _ => { };
        public event Action<string, SFBaseEventType, BaseEventData> OnWidgetBaseEvent = (_, _, _) => { };

        public event Action<string, int, SFPointerEventType, PointerEventData> OnWidgetPointerEvent = (_, _, _, _) =>
        {
        };

        public SFScreenModel[] ScreenModels => _screenModels.Values.ToArray();
        public SFWidgetModel[] WidgetModels => _widgetModelById.Values.ToArray();

        private readonly Dictionary<string, SFScreenModel> _screenModels = new();
        private readonly Dictionary<string, SFWidgetModel> _widgetModelById = new();
        private readonly Dictionary<string, AsyncOperationHandle> _operationHandleByScreen = new();
        private readonly Dictionary<string, SFScreenView> _screenViews = new();
        private readonly Dictionary<string, SFScreenNode> _screenNodes = new();
        private readonly Dictionary<string, SFWidgetNode> _widgetNodes = new();
        private readonly Dictionary<string, List<SFWidgetView>> _widgetViews = new();

        readonly Transform _parentTransform;
        private readonly ISFConfigsService _configsService;


        SFUIService(ISFConfigsService configsService)
        {
            _parentTransform = new GameObject("SFUI").GetComponent<Transform>();
            _configsService = configsService;
        }

        public async UniTask Init(CancellationToken cancellationToken)
        {
            var preloadTasks = new List<UniTask>();

            if (_configsService.TryGetConfigs(out SFUIConfig[] configs))
            {
                foreach (var repository in configs)
                {
                    if (repository.Groups == null) continue;

                    foreach (var groupNode in repository.Groups)
                    {
                        if (groupNode.Screens == null) continue;

                        foreach (var screenNode in groupNode.Screens)
                        {
                            _screenNodes.TryAdd(screenNode.FullId, screenNode);
                            _screenModels.TryAdd(screenNode.FullId, new SFScreenModel(screenNode));
                            foreach (var widgetNode in screenNode.Widgets)
                            {
                                _widgetNodes.TryAdd(widgetNode.FullId, widgetNode);
                                _widgetModelById.TryAdd(widgetNode.FullId, new SFWidgetModel(widgetNode));
                            }

                            if (screenNode.Preload)
                            {
                                preloadTasks.Add(LoadScreen(screenNode.FullId, cancellationToken: cancellationToken));
                            }
                        }
                    }
                }
            }

            await UniTask.WhenAll(preloadTasks).AttachExternalCancellation(cancellationToken);
        }


        public async UniTask LoadScreen(string screen, bool show = false, bool force = false,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default, params object[] parameters)
        {
            if (string.IsNullOrEmpty(screen))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to load screen. Id is empty!");
                return;
            }

            if (_operationHandleByScreen.TryGetValue(screen, out _)) return;

            if (!_screenNodes.TryGetValue(screen, out var screenNode))
            {
                throw new NullReferenceException("Failed to load screen node: " + screen);
            }

            var handle = Addressables.InstantiateAsync(screenNode.Prefab, _parentTransform);

            _operationHandleByScreen[screen] = handle;

            while (!handle.IsDone && !cancellationToken.IsCancellationRequested)
            {
                progress?.Report(handle.PercentComplete);
                await UniTask.Yield(cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Addressables.Release(handle);
                _operationHandleByScreen.Remove(screen);
                throw new OperationCanceledException();
            }

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                Addressables.Release(handle);
                _operationHandleByScreen.Remove(screen);
                throw new NullReferenceException("Failed to load screen: " + screen);
            }

            if (show)
            {
                _screenModels[screen].State = SFScreenState.Show;
                OnShowScreen.Invoke(screen, force, parameters);
            }
        }

        public void UnloadScreen(string screen)
        {
            if (string.IsNullOrEmpty(screen))
                throw new ArgumentNullException(nameof(screen));

            if (!_operationHandleByScreen.ContainsKey(screen))
                throw new Exception("Screen not loaded: " + screen);


            if (_screenModels[screen].State != SFScreenState.Closed)
            {
                _screenModels[screen].State = SFScreenState.Closed;
                OnScreenClosed.Invoke(screen);
            }

            OnUnloadScreen.Invoke(screen);
            Addressables.Release(_operationHandleByScreen[screen]);
            _operationHandleByScreen.Remove(screen);
            OnScreenUnloaded.Invoke(screen);
        }

        public UniTask ShowScreen(string screen, params object[] parameters)
        {
            return ShowScreen(screen, false, null, CancellationToken.None, parameters);
        }

        public UniTask ShowScreen(string screen, bool force, params object[] parameters)
        {
            return ShowScreen(screen, force, null, CancellationToken.None, parameters);
        }

        public async UniTask ShowScreen(string screen, bool force, IProgress<float> progress = null,
            CancellationToken cancellationToken = default,
            params object[] parameters)
        {
            if (string.IsNullOrEmpty(screen))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to show screen. Id is empty!");
                return;
            }

            if (!_screenModels.TryGetValue(screen, out var screenModel)) return;
            if (screenModel.State == SFScreenState.Show || screenModel.State == SFScreenState.Shown) return;
            screenModel.State = SFScreenState.Show;
            if (!_operationHandleByScreen.ContainsKey(screen))
            {
                await LoadScreen(screen, true, force, progress, cancellationToken, parameters);
                return;
            }

            OnShowScreen.Invoke(screen, force, parameters);
        }

        public void SetParameters(string screen, params object[] parameters)
        {
            if (parameters == null)
            {
                SFDebug.Log("Parameters NULL");
                return;
            }

            if (_screenViews.TryGetValue(screen, out var view))
            {
                view.SetParameters(parameters);
            }
            else
            {
                SFDebug.Log("Cant find view");
            }
        }

        public void CloseScreen(string screen, bool force, bool unload)
        {
            if (string.IsNullOrEmpty(screen))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to close screen. Id is empty!");
                return;
            }

            if (!_screenModels.TryGetValue(screen, out var screenModel)) return;
            if (screenModel.State == SFScreenState.Close || screenModel.State == SFScreenState.Closed) return;
            screenModel.State = SFScreenState.Close;
            OnCloseScreen.Invoke(screen, force, unload);
        }

        public bool TryGetScreenView(string screen, out SFScreenView screenView)
        {
            return _screenViews.TryGetValue(screen, out screenView);
        }

        public bool TryGetScreenView<T>(string screen, out T screenView) where T : SFScreenView
        {
            if (_screenViews.TryGetValue(screen, out var view))
            {
                screenView = view as T;
                return screenView != null;
            }

            screenView = null;
            return false;
        }

        public bool TryGetScreenModel(string screen, out SFScreenModel screenModel)
        {
            return _screenModels.TryGetValue(screen, out screenModel);
        }

        public void RegisterScreen(string screen, SFScreenView root)
        {
            if (string.IsNullOrEmpty(screen))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to register screen. Id is empty!");
                return;
            }

            _screenModels[screen].State = SFScreenState.Closed;
            _screenViews[screen] = root;
            if (TryGetScreenModel(screen, out var model))
            {
                model.IsLoaded = true;
            }
        }

        public void RegisterWidget(string widget, SFWidgetView widgetView)
        {
            if (string.IsNullOrEmpty(widget))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to register widget. Id is empty!");
                return;
            }

            if (_widgetViews.TryGetValue(widget, out var widgetViews))
            {
                widgetViews.Add(widgetView);
            }
            else
            {
                var initialWidgetViews = new List<SFWidgetView>() { widgetView };
                if (!_widgetViews.TryAdd(widget, initialWidgetViews))
                {
                    SFDebug.Log(LogType.Error, "Can't add WidgetView to widget {0}", widget);
                }
            }

            if (TryGetWidgetModel(widget, out var model))
            {
                model.RegisterView(widgetView);
            }
        }

        public void UnregisterWidget(string widget, SFWidgetView widgetView)
        {
            if (string.IsNullOrEmpty(widget))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to unregister widget. Id is empty!");
                return;
            }

            if (_widgetViews.ContainsKey(widget))
            {
                _widgetViews.Remove(widget);
            }

            if (TryGetWidgetModel(widget, out var model))
            {
                model.UnregisterView(widgetView);
            }
        }

        public bool TryGetWidgetView(string widget, int index, out SFWidgetView view)
        {
            view = null;
            if (string.IsNullOrEmpty(widget)) return false;
            if (!_widgetViews.TryGetValue(widget, out var widgets)) return false;
            index = Mathf.Clamp(index, 0, widgets.Count);
            view = widgets[index];
            return true;
        }

        public bool TryGetWidgetView<T>(string widget, int index, out T view) where T : SFWidgetView
        {
            if (TryGetWidgetView(widget, index, out var widgetView))
            {
                view = widgetView as T;
                return view != null;
            }

            view = null;
            return false;
        }

        public bool TryGetWidgetNode(string widget, out SFWidgetNode widgetNode)
        {
            return _widgetNodes.TryGetValue(widget, out widgetNode);
        }

        public bool TryGetWidgetModel(string widget, out SFWidgetModel widgetModel)
        {
            if (string.IsNullOrWhiteSpace(widget))
            {
                widgetModel = null;
                return false;
            }

            if (_widgetModelById.TryGetValue(widget, out widgetModel)) return true;
            SFDebug.Log(LogType.Warning, "Can't get widget model for widget {0}", widget);
            return false;
        }

        public void UnregisterScreen(string screen)
        {
            if (string.IsNullOrEmpty(screen))
            {
                SFDebug.Log(LogType.Warning, "[SFUI] - Unable to unregister screen. Id is empty!");
                return;
            }

            _screenModels[screen].State = SFScreenState.Closed;

            if (_screenViews.ContainsKey(screen))
            {
                _screenViews.Remove(screen);
            }

            if (TryGetScreenModel(screen, out var model))
            {
                model.IsLoaded = false;
            }
        }

        public void ScreenShownCallback(string screen)
        {
            if (string.IsNullOrWhiteSpace(screen)) return;
            if (!_screenModels.TryGetValue(screen, out var screenModel)) return;
            if (screenModel.State == SFScreenState.Shown) return;
            screenModel.State = SFScreenState.Shown;
            OnScreenShown.Invoke(screen);
        }

        public void ScreenClosedCallback(string screen, bool unload)
        {
            if (string.IsNullOrWhiteSpace(screen)) return;
            if (!_screenModels.TryGetValue(screen, out var screenModel)) return;
            if (screenModel.State == SFScreenState.Closed) return;
            screenModel.State = SFScreenState.Closed;
            OnScreenClosed.Invoke(screen);

            if (unload)
            {
                UnloadScreen(screen);
            }
        }

        public void WidgetEventCallback(string widget, SFBaseEventType eventType, BaseEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(widget)) return;
            OnWidgetBaseEvent.Invoke(widget, eventType, eventData);
        }

        public void WidgetEventCallback(string widget, int index, SFPointerEventType eventType,
            PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(widget)) return;
            OnWidgetPointerEvent.Invoke(widget, index, eventType, eventData);
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}