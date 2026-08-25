using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Core.Runtime;
using UnityEngine.EventSystems;


namespace SFramework.UI.Runtime
{
    public interface ISFUIService : ISFService
    {
        event Action<string> OnUnloadScreen;
        event Action<string> OnScreenUnloaded;
        event Action<string, bool, object[]> OnShowScreen;
        event Action<string, bool, bool> OnCloseScreen;
        event Action<string> OnScreenShown;
        event Action<string> OnScreenClosed;
        event Action<string, SFBaseEventType, BaseEventData> OnWidgetBaseEvent;
        event Action<string, int, SFPointerEventType, PointerEventData> OnWidgetPointerEvent;
        SFScreenModel[] ScreenModels { get; }
        SFWidgetModel[] WidgetModels { get; }

        UniTask LoadScreen(string screen, bool show, bool force, IProgress<float> progress = null,
            CancellationToken cancellationToken = default, params object[] parameters);

        UniTask ShowScreen(string screen);
        UniTask ShowScreen(string screen, bool force, params object[] parameters);
        UniTask ShowScreen(string screen, bool force, IProgress<float> progress = null, CancellationToken cancellationToken = default,
            params object[] parameters);
        
        void UnloadScreen(string screen);
        void CloseScreen(string screen);
        void CloseScreen(string screen, bool force, bool unload);


        string GetScreenId(Type type);
        bool TryGetScreenView(string screen, out SFScreenView screenView);
        bool TryGetScreenView<T>(string screen, out T screenView) where T : SFScreenView;
        bool TryGetScreenModel(string screen, out SFScreenModel screenModel);
        
        bool TryGetWidgetView(string widget, int index, out SFWidgetView view);
        bool TryGetWidgetView<T>(string widget, int index, out T view) where T : SFWidgetView;
        bool TryGetWidgetNode(string widget, out SFWidgetNode widgetNode);
        bool TryGetWidgetModel(string widget, out SFWidgetModel widgetModel);
        
        void RegisterScreen(string screen, SFScreenView root);
        void RegisterWidget(string widget, SFWidgetView widgetView);
        void UnregisterWidget(string widget, SFWidgetView widgetView);
        void UnregisterScreen(string screen);
        
        void ScreenShownCallback(string screen);
        void ScreenClosedCallback(string screen, bool unload);
        void WidgetEventCallback(string widget, SFBaseEventType eventType, BaseEventData eventData);
        void WidgetEventCallback(string widget, int index, SFPointerEventType eventType, PointerEventData eventData);
    }
}