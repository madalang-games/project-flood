namespace Game.Core
{
    public enum AppEnvironment { Dev, Prod }

    /// <summary>
    /// 환경별 서버 주소 및 외부 서비스 클라이언트 ID를 보관합니다.
    /// 향후 추가될 환경 변수(예: WebClientId)도 여기에 매핑합니다.
    /// </summary>
    public static class AppConfig
    {
        public const string DevGameServerUrl  = "http://localhost:20201"; // 개발 서버 URL
        public const string ProdGameServerUrl = "http://localhost:5000"; // TODO: 실제 프로덕션 서버 URL로 교체

        // Google OAuth 2.0 web client ID — Google Cloud Console > APIs & Services > Credentials
        public const string GoogleWebClientId = "598353589064-33klnpsljo3sia08kaineica4dfpknsg.apps.googleusercontent.com";
    }
}
