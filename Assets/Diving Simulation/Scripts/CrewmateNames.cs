/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.
using System.Collections.Generic; // List<T> 같은 자료형 이름을 사용할 수 있게 합니다. 원본의 using을 보존합니다.
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.
using UnityEngine.UI; // 기존 UGUI Text와 Button 등의 컴포넌트를 사용합니다.

public class CrewmateNames : MonoBehaviour // 원본의 동료 이름·위치 텍스트 표시입니다.
{

    public CallTowerManager ctm; // 동료 정보의 공식 조회 창구입니다.
    private Text textBox; // 결과를 표시할 기존 UGUI Text입니다.

    // Start is called before the first frame update
    void Start() // 첫 Update 전에 한 번 실행하는 원본 초기화 함수입니다.
    {
        textBox = this.GetComponent<Text>(); // 같은 GameObject의 Text를 한 번 찾아 저장합니다.
    }

    // Update is called once per frame
    void Update() // Unity가 실행 중 매 프레임 호출하는 함수입니다.
    {
        CrewInfo[] crewInformation = ctm.GetCrewmatesInformation(); // CallTowerManager를 통해 동료 정보 배열을 읽습니다.
        string crewText = ""; // 이번 프레임에 표시할 문자열을 빈 문자열로 시작합니다.

        foreach(CrewInfo currCrewmate in crewInformation) // 각 동료를 차례대로 한 줄씩 처리합니다.
        {
            string currName = currCrewmate.name; // 현재 동료의 이름을 꺼냅니다.
            string currPosition = currCrewmate.worldLocation.ToString(); // 세계 위치 벡터를 표시용 문자열로 바꿉니다.
            crewText = crewText + currName + " "  + currPosition + "\n"; // 이름·공백·위치·줄바꿈을 이어 붙입니다.
        }

        textBox.text = crewText; // 모은 문자열을 화면 텍스트에 반영합니다.
    }
}
