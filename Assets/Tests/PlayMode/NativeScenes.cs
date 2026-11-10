#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
namespace Fpp.Tests
{
    public static class NativeScenes
    {
        public const string Payload="Assets/Generated/AndroidPayload/CottageTest.unity";
        public static AsyncOperation Gameplay()
        {
            if(!File.Exists(Payload))throw new FileNotFoundException("Actual prepared Android scene is required; never silently test a different source scene",Payload);
            return EditorSceneManager.LoadSceneAsyncInPlayMode(Payload,new LoadSceneParameters(LoadSceneMode.Single));
        }
    }
}
#endif
