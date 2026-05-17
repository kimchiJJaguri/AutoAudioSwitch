# AudioSwitcher

Windows 시스템 트레이에서 오디오 출력 장치를 빠르게 전환하는 가벼운 유틸리티입니다.

## 기능

- 시스템 트레이 상주
- **좌클릭** — 다음 오디오 장치로 즉시 순환 전환
- **우클릭** — 장치 목록에서 직접 선택
- **글로벌 단축키** — 키보드만으로 장치 순환 (기본: `Ctrl + Alt + F11`)
- **단축키 직접 설정** — 설정 창에서 원하는 키 조합으로 변경 가능
- 설정은 `%AppData%\AudioSwitcher\settings.json`에 자동 저장

## 빌드

**.NET 10 SDK** 이상 필요

```bash
dotnet publish -c Release
```

결과물: `bin\Release\net10.0-windows\win-x64\publish\AudioSwitcher.exe`  
외부 의존성 없는 단독 실행 파일입니다.

## 기술 스택

- C# / .NET 10 / WinForms
- Windows Core Audio API (`IMMDeviceEnumerator`)
- `IPolicyConfig` COM 인터페이스 (비공개 API, 장치 기본값 변경)
- `RegisterHotKey` WinAPI (글로벌 단축키)
