using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Plunderspell.Audio.Mimic
{
    /// <summary>
    /// Guard mimic prototype: a small language model run on this machine, no account and no internet.
    /// Starts llama.cpp's server (<c>StreamingAssets/LLM/llama-server.exe</c>, MIT) with Qwen2.5 0.5B
    /// Instruct (Apache-2.0) on 127.0.0.1 the first time it is asked, and stops it when this object is
    /// destroyed. Files come from <c>Tools/LLM/fetch_llm.ps1</c>. Windows only.
    /// </summary>
    public sealed class LocalLanguageModel : MonoBehaviour
    {
        public const string Folder = "LLM";
        public const string ServerExe = "llama-server.exe";
        public const string ModelFile = "qwen2.5-0.5b-instruct-q4_k_m.gguf";
        public const int Port = 8791;
        private const float StartTimeoutSeconds = 30f;

        private Process _server;
        private bool _ready;
        private bool _starting;
        private bool _failed;

        private static string BaseUrl => "http://127.0.0.1:" + Port;

        [Serializable]
        private sealed class CompletionRequest
        {
            public string prompt;
            public string grammar;
            public int n_predict = 24;
            public float temperature = 1f;
            public int seed;
        }

        [Serializable]
        private sealed class CompletionResponse
        {
            public string content;
        }

        /// <summary>
        /// Asks the model to complete <paramref name="prompt"/>, its output held to <paramref name="grammar"/>
        /// (GBNF). Calls back with the text, or null on any failure (logged).
        /// </summary>
        public IEnumerator Complete(string prompt, string grammar, Action<string> done)
        {
            if (!_ready)
                yield return EnsureStarted();
            if (!_ready)
            {
                done(null);
                yield break;
            }

            var body = new CompletionRequest { prompt = prompt, grammar = grammar, seed = UnityEngine.Random.Range(0, int.MaxValue) };
            using (var request = new UnityWebRequest(BaseUrl + "/completion", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 20;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Mimic] The language model did not answer: {request.error} {request.downloadHandler.text}");
                    done(null);
                    yield break;
                }
                done(JsonUtility.FromJson<CompletionResponse>(request.downloadHandler.text)?.content);
            }
        }

        /// <summary>Starts the server now, so the first answer does not wait for it (a few seconds).</summary>
        public void Warm()
        {
            if (!_ready && !_starting && !_failed)
                StartCoroutine(EnsureStarted());
        }

        private IEnumerator EnsureStarted()
        {
            if (_failed)
                yield break;
            if (_starting)
            {
                while (_starting)
                    yield return null;
                yield break;
            }
            _starting = true;

            // A server left running (an Editor session that stopped Play oddly) is reused.
            yield return CheckHealth(ok => _ready = ok);
            if (!_ready)
                StartServer();

            float until = Time.realtimeSinceStartup + StartTimeoutSeconds;
            while (!_ready && !_failed && Time.realtimeSinceStartup < until)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                yield return CheckHealth(ok => _ready = ok);
            }
            if (!_ready && !_failed)
            {
                _failed = true;
                Debug.LogWarning($"[Mimic] The language model did not start within {StartTimeoutSeconds} s.");
            }
            _starting = false;
        }

        private void StartServer()
        {
            string folder = Path.Combine(Application.streamingAssetsPath, Folder);
            string exe = Path.Combine(folder, ServerExe);
            string model = Path.Combine(folder, ModelFile);
            if (!File.Exists(exe) || !File.Exists(model))
            {
                _failed = true;
                Debug.LogWarning($"[Mimic] No language model in {folder}. Run Tools/LLM/fetch_llm.ps1 once.");
                return;
            }

            try
            {
                var start = new ProcessStartInfo(exe,
                    $"-m \"{model}\" --host 127.0.0.1 --port {Port} -c 1024 -t {Mathf.Clamp(SystemInfo.processorCount / 2, 2, 8)} --log-disable")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = folder,
                };
                _server = Process.Start(start);
                Debug.Log($"[Mimic] Started the local language model (pid {_server?.Id}).");
            }
            catch (Exception e)
            {
                _failed = true;
                Debug.LogWarning($"[Mimic] Could not start {exe}: {e.Message}");
            }
        }

        private static IEnumerator CheckHealth(Action<bool> result)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(BaseUrl + "/health"))
            {
                request.timeout = 2;
                yield return request.SendWebRequest();
                result(request.result == UnityWebRequest.Result.Success);
            }
        }

        private void OnDestroy() => StopServer();

        private void OnApplicationQuit() => StopServer();

        private void StopServer()
        {
            if (_server == null)
                return;
            try
            {
                if (!_server.HasExited)
                    _server.Kill();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Mimic] Could not stop the language model: {e.Message}");
            }
            _server = null;
            _ready = false;
        }
    }
}
