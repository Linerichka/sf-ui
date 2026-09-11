using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SFramework.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SFramework.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public abstract class SFScreenView : SFView
    {
        public Canvas Canvas => _canvas;

        public CanvasGroup CanvasGroup => _canvasGroup;

        public GraphicRaycaster GraphicRaycaster => _graphicRaycaster;

        public Type ScreenType { get; private set; }

        public SFScreenState State => _uiService.GetScreenState(ScreenType);

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GraphicRaycaster _graphicRaycaster;
        
        [SFInject]
        private ISFUIService _uiService;

        protected override void Awake()
        {
            base.Awake();
            
            ScreenType = this.GetType();
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _graphicRaycaster = GetComponent<GraphicRaycaster>();
            
            SetScreenActiveState(false);
        }

        protected internal abstract void OnShowScreen();
        protected internal abstract void OnScreenShown();
        protected internal abstract void OnCloseScreen();
        protected internal abstract void OnScreenClosed();
        protected internal abstract void OnScreenDestroy();

        
        public void ShowScreen()
        {
            _uiService.ShowScreen(ScreenType);
        }

        public void CloseScreen()
        {
            _uiService.CloseScreen(ScreenType);
        }

        protected void ShownScreen()
        {
            _uiService.ShownScreen(ScreenType);
        }

        protected void ClosedScreen()
        {
            _uiService.ClosedScreen(ScreenType);
        }

        protected virtual void SetScreenActiveState(bool isShowed)
        {
            Canvas.enabled = isShowed;
            CanvasGroup.interactable = isShowed;
            CanvasGroup.blocksRaycasts = isShowed;
            CanvasGroup.alpha = isShowed ? 1f : 0f;
        }
        
        
        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;
            OnScreenDestroy();
        }
    }
}