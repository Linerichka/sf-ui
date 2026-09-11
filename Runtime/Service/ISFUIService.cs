using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;


namespace SFramework.UI.Runtime
{
    public interface ISFUIService : ISFService
    {
        public UniTask ShowScreen(Type screenType);
        public void CloseScreen(Type screenType);
        
        public void ShownScreen(Type screenType);
        public void ClosedScreen(Type screenType);
        
        public UniTask LoadScreen(Type screenType);
        public UniTask LoadScreen(Type screenType, bool showScreen);
        public void UnloadScreen(Type screenType);


        public SFScreenView GetScreenView(Type screenType);
        public T GetScreenView<T>() where T : SFScreenView, new();
        public bool TryGetScreenView<T>(out T screenView) where T : SFScreenView, new();
        
        public SFScreenEntry GetScreenEntry(Type screenType);
        
        public SFScreenState GetScreenState(Type screenType);
    }
}