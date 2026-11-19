/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.
using System.Collections.Generic; // List<T> 같은 자료형 이름을 사용할 수 있게 합니다. 원본의 using을 보존합니다.
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.

public class ScreenManager : MonoBehaviour // 원본 화면 전환 보조 컴포넌트입니다.
{

    public List<GameObject> screens; // 전환할 화면들을 순서 있는 목록에 저장합니다.


    // Start is called before the first frame update
    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
        
    }

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        
    }

    public void SwitchToScreen (int i) // 외부에서 1부터 시작하는 화면 번호를 받아 전환합니다.
    {
        int correctedIndex = (int)Mathf.Clamp(i-1, 0, screens.Count); // 원본대로 1을 빼고 0~Count로 제한합니다. Count이면 선택되는 화면 없이 모두 꺼질 수 있습니다.
        for(int s = 0; s < screens.Count; s++) // 실제로 존재하는 0~Count-1 화면을 하나씩 검사합니다.
        {
            if (s == correctedIndex) // 현재 화면 번호가 선택 번호와 같은지 비교합니다.
            {
                screens[s].SetActive(true); // 선택된 화면을 보입니다.
            } else // 다른 번호의 화면에 대한 처리입니다.
            {
                screens[s].SetActive(false); // 선택되지 않은 화면을 숨깁니다.
            }
        }
    }
}
