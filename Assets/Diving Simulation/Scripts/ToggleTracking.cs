/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.
using System.Collections.Generic; // List<T> 같은 자료형 이름을 사용할 수 있게 합니다. 원본의 using을 보존합니다.
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.

public class ToggleTracking : MonoBehaviour // 원본의 Transform 추종 보조 스크립트입니다. 이름에 Oculus가 있어도 이 파일은 Transform만 복사합니다.
{

    public bool trackOculusLeftHand = true; // 손 위치를 따라갈지 Inspector에서 선택합니다.
    public Transform OculusLeftHand; // 원본에서 추종할 손 Transform을 연결하는 필드입니다.

    // Start is called before the first frame update
    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
    }

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        if (trackOculusLeftHand) // 추종 옵션이 켜졌을 때만 복사합니다.
        {
            transform.position = OculusLeftHand.position; // 이 물체의 세계 위치를 연결한 손의 세계 위치와 같게 합니다.
            transform.rotation = OculusLeftHand.rotation; // 이 물체의 세계 회전을 연결한 손의 회전과 같게 합니다.
        }
    }
}
