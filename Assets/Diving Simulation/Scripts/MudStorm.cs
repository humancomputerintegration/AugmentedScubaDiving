/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.
using System.Collections.Generic; // List<T> 같은 자료형 이름을 사용할 수 있게 합니다. 원본의 using을 보존합니다.
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.

public class MudStorm : MonoBehaviour // 원본 흙탕물 폭풍의 타이머와 보이기 규칙을 보존합니다.
{

    public GameObject mudGO; // 흙탕물 효과를 담은 GameObject입니다.
    public Camera playerCamera; // PhysicalUI 레이어를 숨기거나 다시 보여 줄 플레이어 카메라입니다.
    public InformationManager infMan; // 수면 기준 깊이를 읽는 원본 관리자입니다.

    public bool storm = false; // 현재 폭풍이 진행 중인지 저장합니다.


    // Start is called before the first frame update
    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
        StartCoroutine("MudSurpriseOn"); // 원본처럼 문자열 이름으로 폭풍 시작 대기 코루틴을 실행합니다.
    }

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {

        if (storm && (infMan.GetDepth() > 0f)) // 폭풍 중이지만 수면 위에 올라온 상태입니다.
        {
            mudGO.SetActive(false); // 흙탕물 시각 효과를 숨깁니다.
        } else if (storm && (infMan.GetDepth() <= 0f)) // 폭풍 중이고 깊이가 0 이하인 원본의 수중 조건입니다.
        {
            mudGO.SetActive(true); // 흙탕물 시각 효과를 보입니다.
        }
    }

    IEnumerator MudSurpriseOn() // 기다렸다가 폭풍 시작 조건을 확인하는 코루틴입니다.
    {
        yield return new WaitForSeconds(Random.Range(15f, 30f)); // 15~30초 중 임의 시간 동안 이 코루틴만 기다립니다.
        if (!storm && (infMan.GetDepth() <= 0f) ) // 아직 폭풍이 아니고 수면 이하에 있을 때 시작합니다.
        {
            Debug.Log("Mud slide."); // 폭풍 시작을 Console에 기록합니다.
            playerCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("PhysicalUI")); // Hide | PhysicalUI 비트만 0으로 지워 카메라에서 그 레이어를 숨깁니다. 오브젝트 자체는 끄지 않습니다.
            mudGO.SetActive(true); // 흙탕물 시각 효과를 보입니다.
            StartCoroutine("MudSurpriseOff"); // 폭풍 종료 타이머를 시작합니다.
            storm = true; // 이제 폭풍 중임을 다른 코드에도 알립니다.
        }
        else // 바로 앞 조건이 거짓인 경우의 처리를 시작합니다.
        {
            Debug.Log("Not in water for mud."); // 시작 조건이 맞지 않았음을 Console에 기록합니다.
            StartCoroutine("MudSurpriseOn"); // 원본처럼 문자열 이름으로 폭풍 시작 대기 코루틴을 실행합니다.
        }
    }

    IEnumerator MudSurpriseOff() // 원본 폭풍을 일정 시간 뒤 종료하는 코루틴입니다.
    {
        yield return new WaitForSeconds(20f); // 폭풍 시작 뒤 20초를 기다립니다.
        Debug.Log("Mud slide subsided."); // 폭풍 종료를 Console에 기록합니다.
        playerCamera.cullingMask |= 1 << LayerMask.NameToLayer("PhysicalUI"); // Show | PhysicalUI 비트를 1로 켜 카메라에서 다시 보이게 합니다.
        mudGO.SetActive(false); // 흙탕물 시각 효과를 숨깁니다.
        storm = false; // 폭풍 진행 상태를 끝냅니다. 이 원본 함수는 다음 폭풍을 다시 예약하지 않습니다.
    }
}
