using System;
using System.Collections;
using System.Text;
using Game.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Services
{
    /// <summary>
    /// 로그 노출 범위를 제어하는 레벨입니다.
    /// None       — 모든 HTTP 로그 비활성화
    /// ErrorOnly  — 전송 오류 및 4xx/5xx 응답만 출력
    /// Normal     — 요청 메소드·URL·응답 코드 출력 (바디 제외)
    /// Verbose    — 요청/응답 바디까지 모두 출력
    /// </summary>
    public enum NetworkLogLevel { None, ErrorOnly, Normal, Verbose }

    /// <summary>
    /// 중앙 집중식 HTTP 통신 서비스. DDOL 싱글톤.
    /// - AppConfig 에서 BaseUrl 을 가져오고, Application.version 을 X-Client-Version 헤더에 주입합니다.
    /// - AuthService 가 SetAuthToken 을 통해 토큰을 주입합니다.
    /// - 모든 요청/응답은 LogLevel 에 따라 로그를 남깁니다.
    /// </summary>
    public class NetworkService : MonoBehaviour
    {
        // ─── 싱글톤 ───────────────────────────────────────────────────────
        static NetworkService _instance;

        public static NetworkService Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[NetworkService]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<NetworkService>();
                return _instance;
            }
        }

        // ─── 인스펙터 필드 ────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] AppEnvironment _environment = AppEnvironment.Dev;
#else
        [SerializeField] AppEnvironment _environment = AppEnvironment.Prod;
#endif
        [SerializeField] string _protocolVersion = "1";
        [SerializeField] bool   _enableLogging   = true;
        [SerializeField] NetworkLogLevel _logLevel = NetworkLogLevel.Normal;

        // ─── 런타임 상태 ─────────────────────────────────────────────────
        string _authToken = string.Empty;

        // ─── 프로퍼티 ────────────────────────────────────────────────────
        public AppEnvironment Environment => _environment;
        string BaseUrl => _environment == AppEnvironment.Prod
            ? AppConfig.ProdGameServerUrl
            : AppConfig.DevGameServerUrl;

        // ─── 생명주기 ────────────────────────────────────────────────────
        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_environment == AppEnvironment.Prod)
                _enableLogging = false;

            Debug.Log($"[NetworkService] env={_environment} url={BaseUrl} logging={_enableLogging}({_logLevel})");
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ─── 공개 API ────────────────────────────────────────────────────

        /// <summary>인증 JWT 토큰을 설정합니다. AuthService 가 로그인 완료 시 호출합니다.</summary>
        public void SetAuthToken(string token) => _authToken = token ?? string.Empty;

        /// <summary>HTTP GET 요청. onComplete(success, responseText|errorText)</summary>
        public void Get(string path, Action<bool, string> onComplete)
            => StartCoroutine(SendGet(path, onComplete));

        /// <summary>HTTP POST 요청. onComplete(success, responseText|errorText)</summary>
        public void Post(string path, string jsonPayload, Action<bool, string> onComplete)
            => StartCoroutine(SendWithBody(path, "POST", jsonPayload, onComplete));

        // ─── 내부 구현 ────────────────────────────────────────────────────

        IEnumerator SendGet(string path, Action<bool, string> onComplete)
        {
            using var req = UnityWebRequest.Get(BuildUrl(path));
            ApplyHeaders(req);
            LogRequest("GET", path, null);
            yield return req.SendWebRequest();
            Complete(req, "GET", null, onComplete);
        }

        IEnumerator SendWithBody(string path, string method, string jsonPayload, Action<bool, string> onComplete)
        {
            var body = string.IsNullOrEmpty(jsonPayload) ? "{}" : jsonPayload;
            using var req = new UnityWebRequest(BuildUrl(path), method);
            var bytes = Encoding.UTF8.GetBytes(body);
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyHeaders(req);
            LogRequest(method, path, body);
            yield return req.SendWebRequest();
            Complete(req, method, body, onComplete);
        }

        string BuildUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return BaseUrl;
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
            return $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        void ApplyHeaders(UnityWebRequest req)
        {
            req.SetRequestHeader("X-Client-Version",   Application.version);
            req.SetRequestHeader("X-Protocol-Version", _protocolVersion);
            if (!string.IsNullOrEmpty(_authToken))
                req.SetRequestHeader("Authorization", $"Bearer {_authToken}");
        }

        void Complete(UnityWebRequest req, string method, string requestBody, Action<bool, string> onComplete)
        {
            var responseBody = req.downloadHandler?.text ?? string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                LogResponse(method, req.url, req.responseCode, requestBody, responseBody, isError: false);
                onComplete?.Invoke(true, responseBody);
                return;
            }

            var status = req.responseCode > 0 ? req.responseCode.ToString() : req.result.ToString();
            var error  = string.IsNullOrEmpty(responseBody) ? req.error : responseBody;
            LogResponse(method, req.url, req.responseCode, requestBody, error, isError: true);
            onComplete?.Invoke(false, error);
        }

        // ─── 로깅 헬퍼 ────────────────────────────────────────────────────

        void LogRequest(string method, string path, string body)
        {
            if (!_enableLogging || _logLevel == NetworkLogLevel.None) return;
            if (_logLevel < NetworkLogLevel.Verbose) return;

            var sb = new StringBuilder($"[HTTP ▶] {method} {path}");
            if (body != null) sb.Append($"\n  body: {body}");
            Debug.Log(sb);
        }

        void LogResponse(string method, string url, long code, string requestBody, string responseBody, bool isError)
        {
            if (!_enableLogging || _logLevel == NetworkLogLevel.None) return;

            // ErrorOnly: 에러일 때만 출력
            if (_logLevel == NetworkLogLevel.ErrorOnly && !isError) return;

            var sb = new StringBuilder($"[HTTP {(isError ? "✗" : "✓")}] {method} {url} → {code}");

            if (_logLevel == NetworkLogLevel.Verbose)
            {
                sb.Append($"\n  request : {requestBody ?? ""}");
                sb.Append($"\n  response: {responseBody}");
            }
            else if (_logLevel == NetworkLogLevel.Normal && isError)
            {
                sb.Append($"\n  error: {responseBody}");
            }

            if (isError)
            {
                if (code >= 500 || code == 0)
                    Debug.LogError(sb);
                else
                    Debug.LogWarning(sb);
            }
            else
            {
                Debug.Log(sb);
            }
        }
    }
}
