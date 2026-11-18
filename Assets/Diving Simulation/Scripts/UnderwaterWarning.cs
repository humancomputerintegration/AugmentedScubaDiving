/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.
using System.Collections.Generic; // List<T> 같은 자료형 이름을 사용할 수 있게 합니다. 원본의 using을 보존합니다.
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.

public class UnderwaterWarning : MonoBehaviour // 원본의 수중 진입 안내 음성을 관리합니다.
{

    public InformationManager im; // 현재 깊이를 읽는 정보 관리자입니다.
    private AudioSource warningAS; // 안내 음원을 재생할 AudioSource입니다.
    bool audioFinished = true; // 안내 재생이 끝났을 때만 다음 재생을 허용합니다.
    bool isUnderwater = false; // 현재 프레임의 수중 여부입니다.
    bool prevState = false; // 직전 프레임의 수중 여부입니다.

    // Start is called before the first frame update
    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
        warningAS = this.GetComponent<AudioSource>(); // 같은 물체에 있는 AudioSource를 찾습니다.
    }

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        float currDepth = im.GetDepth(); // 현재 깊이를 한 번 읽습니다. 원본은 음수일 때 수중입니다.
        Debug.Log("The current depth is " + currDepth.ToString()); // 깊이를 매 프레임 Console에 쓰는 원본 로그입니다.
        if (currDepth < 0f) // 수면보다 아래에 있는지 검사합니다.
        {
            prevState = isUnderwater; // 현재 값을 바꾸기 전에 이전 상태를 기억합니다.
            isUnderwater = true; // 현재 프레임을 수중으로 표시합니다.
        } else // 수면 이상인 경우의 상태 갱신입니다.
        {
            prevState = isUnderwater; // 현재 값을 바꾸기 전에 이전 상태를 기억합니다.
            isUnderwater = false; // 현재 프레임을 물 밖으로 표시합니다.
        }

        if (!prevState && isUnderwater && audioFinished) // 물 밖→물속 전환이며 이전 안내가 끝났을 때만 실행합니다.
        {
            Debug.Log("I am underwater. Playing audio."); // 수중 진입 안내를 재생한다고 기록합니다.
            StartCoroutine(AudioCompletion()); // 재생과 완료 대기를 한 코루틴으로 시작합니다.
        }
    }

    IEnumerator AudioCompletion() // 안내 재생 중의 중복 재생을 막는 원본 코루틴입니다.
    {
        audioFinished = false; // 재생이 진행 중이라고 표시합니다.
        warningAS.Play(); // Inspector에 연결된 안내 클립을 재생합니다.
        yield return new WaitForSeconds(warningAS.clip.length); // 클립의 길이만큼 기다립니다. 원본은 clip 연결이 필요합니다.
        audioFinished = true; // 다음 수중 진입 안내를 허용합니다.
    }
}
