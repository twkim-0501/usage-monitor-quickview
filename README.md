# Usage Monitor Quick View

**Usage Monitor 웹 대시보드를 작업표시줄의 작은 차트 아이콘으로 여는 독립 Windows 앱입니다.** 클릭하면 크기 조절 가능한 웹 창이 열립니다. 기존 [Codex Account Monitor](https://github.com/twkim-0501/codex-account-monitor)의 세 계정 위젯을 발견하면 바로 오른쪽에 붙습니다. 두 프로그램은 실행 파일, 설정, 자동 시작, 종료가 각각 별개입니다.

이 앱은 대시보드를 보여주는 클라이언트입니다. 토큰을 수집하는 서버나 Codex 로그인 기능은 포함하지 않습니다. 이미 동작하는 웹 대시보드 또는 접속 도우미가 필요합니다.

## 처음 사용하기

1. [최신 릴리스](https://github.com/twkim-0501/usage-monitor-quickview/releases/latest)의 **UsageMonitorQuickView.exe**를 내려받아 계속 사용할 폴더에 둡니다. Windows 10/11 x64용이며 .NET 런타임이 포함됩니다.
2. 실행 후 설정에 자신의 웹 대시보드 **HTTP/HTTPS 주소**를 입력하고 저장합니다. 비밀번호·인증 토큰을 주소에 넣지 마세요. 이 주소는 로컬 설정 파일에 평문으로 저장됩니다.
3. 왼쪽 아래 **차트 아이콘**을 누릅니다. 웹 창에서 필요한 사이트 로그인을 진행합니다.
4. 아이콘을 다시 누르거나 창을 닫으면 접힙니다. 위쪽의 **새로고침**, **브라우저로 열기**도 사용할 수 있습니다.
5. 자동 시작을 원하면 아이콘 우클릭 → **설정 → Windows 로그인 시 아이콘만 표시**를 켭니다. 기본값은 꺼짐입니다.

이 PC에 [Microsoft WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)이 있어야 웹 창을 띄울 수 있습니다. 없으면 설치하거나 **브라우저로 열기**를 사용하세요. 웹 화면은 첫 클릭 때 생성됩니다.

완전히 종료하려면 아이콘 우클릭 → **종료**를 누릅니다. 오른쪽 아래 알림 영역에도 별도의 차트 아이콘이 있습니다. `^` 안에 숨겨져 있을 수도 있습니다. 기존 계정 모니터를 종료해도 이 앱은 계속 동작합니다.

## 작업표시줄 배치

- Codex Account Monitor가 있으면 그 미니 위젯의 오른쪽을 2초마다 확인하여 따라갑니다. 계정 수가 바뀌어 위젯 폭이 달라져도 위치를 다시 계산합니다.
- 기존 위젯을 발견하지 못하면 왼쪽 아래에 단독으로 표시합니다.
- 작업표시줄 내부 도킹은 Windows 11의 가운데 정렬된 가로 작업표시줄을 대상으로 합니다. Windows 10·왼쪽 정렬·세로 작업표시줄 또는 도킹 실패 시 작업표시줄 바로 위에 표시합니다. 설정에서 도킹을 해제할 수도 있습니다.
- 도킹된 버튼은 Explorer의 실제 자식 창이며 항상 위에 표시를 강제하지 않습니다. 작업표시줄 바로 위 모드는 전경 앱이 모니터 전체를 덮으면 숨김을 시도합니다.
- Explorer 내부 창 구조는 공식 확장 API가 아니므로 Windows 업데이트에 따라 달라질 수 있습니다. 일반 Windows 작업표시줄 고정 아이콘과는 다른 방식입니다.

## SSH·저장된 로그인으로 접속하는 대시보드

고정 URL 대신 자신의 로컬 접속 도우미를 등록할 수 있습니다. `%LOCALAPPDATA%\UsageMonitorQuickView\settings.json`의 예:

```json
{
  "Url": null,
  "HelperExecutable": "C:\\Python314\\python.exe",
  "HelperArguments": [
    "-B",
    "C:\\Tools\\usage-access-bridge.py",
    "--launcher",
    "C:\\Tools\\CodexUsageAccess\\codex_usage_access.py"
  ],
  "StartWithWindows": false,
  "DockButton": true
}
```

`tools/usage-access-bridge.py`는 별도로 설치된 **CodexUsageAccess-v1 호환 Python 접속 도우미**를 위한 선택적 어댑터입니다. 원본 접속 도우미는 이 저장소에 포함되지 않습니다. 일반 웹 주소만 사용하는 사람은 Python이나 SSH 어댑터를 설치할 필요가 없습니다.

어댑터는 자신의 PC에 있는 원본 모듈을 불러와 기존 Windows DPAPI 로그인 저장소와 localhost SSH 프록시를 재사용합니다. 이미 실행 중인 프록시가 있으면 재사용하고, 없으면 새 프록시를 시작합니다. 웹 창을 닫아도 공유 프록시를 강제로 종료하지 않으며, 원본 도우미의 유휴 종료 시간을 따릅니다. 로그인 또는 서버 연결을 복구해야 하면 기존 바탕화면 바로가기로 확인한 뒤 다시 시도합니다. 이 어댑터에는 새 로그인 화면이 없습니다.

다른 접속 도우미를 구현할 때는 stdout 첫 줄로 `{"ready":true,"url":"http://127.0.0.1:PORT/..."}`를 반환하세요. 앱은 최대 40초 기다리며, 도우미가 반환한 주소는 HTTP/HTTPS localhost만 허용합니다. 실패 시 `{"ready":false}`를 반환할 수 있습니다. 도우미 실행파일은 절대 경로로 지정하고 인수는 배열로 전달합니다. 셸 명령 문자열은 실행하지 않습니다. 일반 `Url`이 있으면 그 주소가 우선합니다.

임시 접속 키는 파이프로만 전달하며 앱의 설정·로그에 기록하지 않습니다. 어댑터를 터미널에서 직접 실행하거나 출력을 파일로 리다이렉트하지 마세요. 성공 결과에 임시 localhost 접속 키가 포함될 수 있습니다. 설정 예시는 가상 경로이며 개인 서버 주소·로그인·비밀번호는 저장소나 릴리스에 포함하지 않습니다.

## 개인정보·업데이트·삭제

설정과 WebView2 브라우저 프로필은 `%LOCALAPPDATA%\UsageMonitorQuickView`에 저장됩니다. 웹 프로필은 사이트의 쿠키와 세션 데이터를 저장할 수 있습니다. 이 폴더와 실제 대시보드 화면을 공개하지 마세요. 앱은 Codex 인증 파일을 읽거나 AI 작업을 시작하지 않습니다. 연결한 사이트는 자신의 서버에서 별도로 데이터를 수집할 수 있습니다.

업데이트 시 이 앱만 우클릭 → **종료**한 뒤 실행파일을 교체합니다. 기존 계정 모니터를 종료할 필요가 없습니다. 삭제 시 설정에서 자동 시작을 끄고 종료한 뒤 실행파일을 삭제합니다. 로컬 설정과 웹 세션도 없애려면 위 폴더를 삭제합니다. 기본 Codex 로그인이나 서버 파일은 삭제하지 않습니다. 현재 릴리스에는 코드 서명이 없습니다.

## 개발·검증

Windows의 .NET 10 SDK가 필요합니다. Microsoft WebView2 SDK는 NuGet으로 복원됩니다.

```powershell
dotnet build src/UsageMonitorQuickView.csproj -c Release
dotnet run --project tests/QuickView.Tests.csproj -c Release
dotnet publish src/UsageMonitorQuickView.csproj -c Release -r win-x64 --self-contained true -o dist
# 개인 주소 없이 예시 웹 화면:
.\dist\UsageMonitorQuickView.exe --demo --open
# 대화형 Windows 바탕화면에서 자체 창/도킹/WebView 검사:
.\dist\UsageMonitorQuickView.exe --demo --check C:\Temp\quickview-demo
# 자신의 연결 설정을 사용합니다. 캡처 결과는 비공개로 유지하세요.
.\dist\UsageMonitorQuickView.exe --check C:\Temp\quickview-live
```

`--settings <path>`로 별도의 설정을 읽을 수 있습니다. `--check`는 검사 후 종료하고 버튼 캡처·웹 화면·상태 JSON을 저장합니다. 테스트 클릭은 자신의 버튼 창에 메시지를 보냅니다. 물리 마우스를 이동하거나 입력을 주입하지 않습니다. 개발 PC에서 독립 실행, 기존 위젯 바로 오른쪽 배치, 실제 localhost/SSH 대시보드 연결, 열기/접기/닫기, 메뉴, 오버레이를 확인했습니다. 물리 마우스, Explorer 재시작, 다중 모니터/DPI 변경, 전체 화면 앱은 별도의 수동 확인이 필요합니다. CI는 주소 검증·접속 도우미 파이프 테스트와 Windows 빌드를 수행합니다.

## 출처와 제작 방식

- [Codex Account Monitor — twkim-0501](https://github.com/twkim-0501/codex-account-monitor)의 Win32 작업표시줄 창 구현과 WPF 공통 스타일을 재사용·개조했습니다. 계정 모니터 자체는 이 앱에 포함되지 않습니다.
- 원래의 작업표시줄 사용량 위젯 아이디어와 사용 경험은 [Codex Pulse — pjhun0412](https://github.com/pjhun0412/codex-pulse)를 계승합니다. Codex Pulse의 코드·아이콘·바이너리를 복사하지 않았습니다.
- **OpenAI Codex로 기존 구현을 개조하고 웹 창·접속 어댑터·검증·문서를 작성했습니다.** 원작 아이디어를 독창적 창작으로 주장하지 않습니다.
- 웹 대시보드와 사용자가 별도로 설치한 접속 도우미는 독립 프로그램이며 이 저장소에 포함되지 않습니다.
- [Microsoft WebView2 배포 안내](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution), [Microsoft WebView2 SDK](https://www.nuget.org/packages/Microsoft.Web.WebView2)를 참고·사용합니다.

MIT License. OpenAI 또는 Codex Pulse 원작자가 공식 제공하거나 보증하는 프로그램이 아닙니다.

## English quick start

Run the portable Windows x64 executable, enter your existing dashboard's HTTP/HTTPS URL in Settings, then click the chart button. It opens a resizable WebView2 window. The button follows the right edge of the separate Codex Account Monitor widget when present. Closing the window hides it; right-click the button to quit the independent app. WebView2 Runtime is required for the embedded view, and **Open in browser** remains available. Optional SSH helper integration is described above. This app is a viewer; it does not install a collector or authenticate Codex accounts. Settings and browser sessions remain local. Credits: Codex Pulse for the original concept, Codex Account Monitor for reused native/WPF code, and OpenAI Codex for this adaptation.
