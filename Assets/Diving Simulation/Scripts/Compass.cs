/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.
using System.Collections.Generic; // List<T> 같은 자료형 이름을 사용할 수 있게 합니다. 원본의 using을 보존합니다.
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.

public class Compass : MonoBehaviour // 원본 나침반 표시 컴포넌트이며 목표 방향 표시를 보존합니다.
{
    public Transform arrow; // 목표를 가리킬 화살표 Transform입니다.
    public Transform northMarker; // 북쪽 기준 눈금을 회전시킬 Transform입니다.
    
    public Transform target; // 가리킬 목표 물체의 위치입니다.
    public Transform player; // 플레이어의 위치와 방향입니다.

    private Vector3 northDirection = Vector3.zero; // 북쪽 눈금에 사용할 Euler 각도 세 개를 0으로 시작합니다.
    private Quaternion targetDirection; // 목표 회전을 Quaternion 네 성분으로 저장합니다.

    // Start is called before the first frame update
    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
        Debug.Log(arrow.up); // 시작할 때 화살표의 세계 위 방향을 Console에 표시하는 원본 로그입니다.

    }

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        UpdateNorthDirection(); // 플레이어의 방향으로 북쪽 눈금 값을 갱신합니다.
        UpdateTargetDirection(); // 목표와 플레이어의 위치 차이로 화살표 회전을 갱신합니다.
    }
    
    void UpdateNorthDirection() // 북쪽 눈금의 회전만 계산하는 함수입니다.
    {
        northDirection.y = player.eulerAngles.y; // 플레이어의 좌우 회전각을 y 각도에 복사합니다.
        northMarker.localEulerAngles = northDirection; // 부모 기준 회전각을 북쪽 눈금에 적용합니다.
    }

    void UpdateTargetDirection() // 현재 목표를 향하는 원본 회전 계산입니다.
    {
        Vector3 direction = target.position - player.position; // 플레이어에서 목표까지의 방향 벡터를 뺄셈으로 만듭니다.

        targetDirection = Quaternion.LookRotation(direction); // 로컬 +Z가 목표 방향을 향하는 회전을 만듭니다.

        targetDirection.y = -targetDirection.y; // 원본대로 Quaternion의 y 성분 부호를 바꿉니다. Euler y 각도를 뒤집는 연산과는 다릅니다.
        targetDirection.x = 0; // 원본대로 Quaternion의 x 성분을 0으로 바꿉니다.
        targetDirection.z = 0; // 원본대로 Quaternion의 z 성분을 0으로 바꿉니다.
        

        arrow.localRotation = targetDirection * Quaternion.Euler(northDirection); // 목표 회전과 북쪽 기준 회전을 곱해 화살표의 로컬 회전으로 적용합니다.
    }
}
