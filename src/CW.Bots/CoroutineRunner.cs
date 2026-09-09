using UnityEngine;

namespace CW.Bots
{
    public static class CoroutineRunner
    {
        private static MonoBehaviour _runner;
        
        public static void Init()
        {
            if (_runner == null)
            {
                var go = new GameObject("BotCoroutineRunner");
                GameObject.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                _runner = go.AddComponent<MonoBehaviour>();
            }
        }
        
        public static void Start(System.Collections.IEnumerator coroutine)
        {
            if (_runner == null) Init();
            _runner.StartCoroutine(coroutine);
        }
    }
}