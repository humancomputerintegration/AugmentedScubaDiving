# 통화 로직 검증 하네스

실제 프로젝트의 통화 C# 6개를 상대 경로로 직접 링크합니다. 같은 로직을 별도 언어로 다시 구현한 테스트가 아닙니다. Unity API의 작은 표면만 UnityApiDoubles.cs에서 대체합니다. 이 폴더는 Assets 바깥에 있어 Unity 게임 코드로 포함되지 않습니다.

작성 환경에서는 .NET SDK 10.0.404, C# 9 언어 설정으로 14개 시나리오·272개 검사가 통과했습니다. 이 결과는 Unity 프로젝트 빌드나 Quest 실행 결과가 아닙니다. 재실행하려면 .NET 10 SDK가 있는 환경에서 이 폴더의 CallLogicHarness.csproj를 dotnet run으로 실행합니다. 참조 경로는 최종 프로젝트의 Assets를 가리키므로 Docs와 Assets의 폴더 관계를 유지해야 합니다.

결과 기록은 상위 폴더의 Call_Logic_Test_Results.json입니다.
