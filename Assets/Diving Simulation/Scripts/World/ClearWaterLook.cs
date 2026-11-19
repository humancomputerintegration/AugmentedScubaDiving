/*
역할: 기본 수중 안개의 색·밀도만 맑은 청록색으로 정합니다.
InformationManager가 물 안/밖에서 fog를 켜고 끄는 기존 규칙은 그대로 둡니다.
MudStorm의 storm, mudGO, 카메라 cullingMask, UI, 손 추적에는 접근하지 않습니다.
Color는 색, float는 소수, private은 내부 자료, public은 Inspector에서 조정할 값입니다.
*/
using UnityEngine; // Unity의 색과 렌더 설정을 사용합니다.

[DisallowMultipleComponent] // 같은 오브젝트에 기본 수색 관리자를 중복 부착하지 않습니다.
public class ClearWaterLook : MonoBehaviour // Scene에 하나 두는 기본 수중 색 설정입니다.
{ // 클래스 정의를 시작합니다.
    public Color waterFogColor = new Color(0.15f, 0.66f, 0.73f, 1f); // 몰디브 느낌을 목표로 한 밝은 청록색 기본 안개입니다.
    public float fogDensity = 0.012f; // 원본 0.08보다 낮은 기본 안개 밀도입니다.
    private Color previousColor; // 비활성화될 때 되돌릴 원래 색입니다.
    private float previousDensity; // 원래 안개 밀도를 보관합니다.
    private FogMode previousMode; // 원래 안개 계산 방식을 보관합니다.
    private void OnEnable() // 이 설정을 켤 때 현재 렌더 설정을 저장합니다.
    { // 적용 처리의 시작입니다.
        previousColor = RenderSettings.fogColor; // Scene의 원래 안개색을 기억합니다.
        previousDensity = RenderSettings.fogDensity; // Scene의 원래 밀도를 기억합니다.
        previousMode = RenderSettings.fogMode; // Scene의 원래 계산 방식을 기억합니다.
        RenderSettings.fogColor = waterFogColor; // 기본 수색만 밝은 청록색으로 지정합니다.
        RenderSettings.fogDensity = Mathf.Clamp(fogDensity, 0.001f, 0.05f); // 실수로 극단적인 밀도를 입력하지 않게 제한합니다.
        RenderSettings.fogMode = FogMode.ExponentialSquared; // 밀도에 따라 서서히 먼 곳이 흐려지는 방식을 사용합니다.
    } // 적용 처리를 마칩니다.
    private void OnDisable() // 이 설정을 끄면 자신이 바꾼 세 값만 복구합니다.
    { // 복구 처리의 시작입니다.
        RenderSettings.fogColor = previousColor; // 원래 안개색을 돌려놓습니다.
        RenderSettings.fogDensity = previousDensity; // 원래 밀도를 돌려놓습니다.
        RenderSettings.fogMode = previousMode; // 원래 계산 방식을 돌려놓습니다.
    } // 복구 처리를 마칩니다.
} // ClearWaterLook 정의를 마칩니다.
