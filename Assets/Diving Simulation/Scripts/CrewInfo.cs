using UnityEngine; // using은 Vector3 같은 Unity 형식의 긴 이름을 짧게 쓰게 한다.

// struct는 관련 값들을 한 묶음으로 복사하는 자료형이다. 이 묶음은 동료 자체가 아니라 조회 순간의 정보다.
// int frequency는 무전 주파수, string phoneNumber는 앞자리 0도 보존하는 게임 내 전화 주소다.
public struct CrewInfo // public은 다른 스크립트에서도 CrewInfo를 사용할 수 있다는 뜻이다.
{ // 동료 정보 자료형의 범위를 연다.
    public string name; // 이름을 문자열로 보관한다. string은 글자들의 순서 있는 묶음이다.
    public Vector3 worldLocation; // 월드 좌표 x, y, z를 저장한다. 다시 조회하면 움직인 현재 위치를 받는다.
    public int frequency; // 100 이상 1000 미만의 동료별 무전 채널 정수다.
    public string phoneNumber; // 2001 같은 게임 내선이다. 실제 전화망으로 전화를 걸지 않는다.

    public CrewInfo(string n, Vector3 w, int f) : this(n, w, f, "") // 기존 3개 인수 호출은 4개 인수 생성자를 재사용한다.
    { // 예전 스크립트가 같은 생성 방법을 계속 사용할 수 있게 한다.
    } // 필요한 대입은 this(...)로 호출한 아래 생성자가 담당한다.

    public CrewInfo(string n, Vector3 w, int f, string p) // 생성자는 new CrewInfo(...)가 새 묶음을 만들 때 실행된다.
    { // 매개변수 n, w, f, p를 실제 정보 칸으로 옮긴다.
        name = n; // =는 오른쪽 값을 왼쪽 칸에 넣는 대입이다. 이름을 저장한다.
        worldLocation = w; // 이번 조회 시점의 위치를 복사한다.
        frequency = f; // 같은 동료를 무전으로 찾을 때 사용할 주파수를 복사한다.
        phoneNumber = p; // 같은 동료를 전화 모드로 찾을 때 사용할 내선을 복사한다.
    } // 정보 묶음 초기화를 끝낸다.
} // CrewInfo 정의를 끝낸다.
