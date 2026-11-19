using UnityEngine; // 같은 물체의 컴포넌트를 찾고 연결 상태를 알려 주는 기능을 사용한다.

[DisallowMultipleComponent] // 한 잠수부에 같은 통화 담당자를 두 번 붙이지 못하게 한다.
[RequireComponent(typeof(CreatureSwim))] // 이 동료를 새로 붙이면 같은 물체에 수영 컴포넌트도 필요하다.
public class DiverCrewmate : CrewmateBehavior // 기존 동료의 전화 기능을 그대로 물려받는 수중 동료다.
{ // 수중 동료가 준비됐는지 확인하는 범위다.
    protected override void Start() // 부모의 전화 준비에 수영 연결 검사를 추가한다.
    { // 이 잠수부가 처음 실행될 때 한 번 확인한다.
        CreatureSwim swimming = GetComponent<CreatureSwim>(); // 전화번호를 가진 바로 그 물체의 수영 담당자를 찾는다.
        if (swimming == null || !swimming.enabled) // 이동 담당자가 없거나 꺼져 있는지 확인한다.
        { // 잠수부가 멈춰 있을 원인을 Console에 남긴다.
            Debug.LogWarning("DiverCrewmate: enable CreatureSwim on this diver.", this); // 문제가 있는 잠수부를 클릭해서 찾을 수 있게 한다.
        } // 수영 컴포넌트 활성 상태 검사를 마친다.
        else if (swimming.waterSurface == null || swimming.seaFloor == null) // 수영할 수면과 바닥이 연결됐는지도 확인한다.
        { // 실제 수영 경계가 빠진 경우를 알린다.
            Debug.LogWarning("DiverCrewmate: assign waterSurface and seaFloor on CreatureSwim.", this); // 물 밖으로 이동시키지 않고 누락된 연결을 알린다.
        } // 수면과 바닥 연결 검사를 마친다.
        base.Start(); // 부모에게 전화 준비를 맡기고 실제 이동은 기존 CreatureSwim 한 개에 맡긴다.
    } // 수중 동료의 초기화를 마친다.
} // DiverCrewmate 정의를 마친다.
