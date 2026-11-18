/* 원본 동작을 유지하고 각 의미 있는 코드 줄에 한국어 해설을 추가했습니다. */
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.

public class FishMovements : MonoBehaviour { // 원본 물고기가 일정 높이에서 임의 목적지를 향해 움직이는 컴포넌트입니다.
	
	public Vector3 startingPoint; // 물고기의 최초 위치입니다.
	public float maxDist; // 새 목적지를 고를 구의 반지름입니다.
	public Vector3 rndShift; // 현재 향하는 세계 목적지입니다.
	
	// Use this for initialization
	void Start () { // 첫 실행 때 시작 위치와 첫 목적지를 고릅니다.
		
		startingPoint = transform.position; // 현재 물고기의 세계 위치를 시작점으로 저장합니다.
		rndShift = startingPoint + maxDist * Random.insideUnitSphere; // 시작점 주변 반지름 maxDist의 구 안에서 임의 목적지를 고릅니다.
		rndShift.y = startingPoint.y; // 목적지 높이를 시작 높이로 고정합니다.
		
	
	}
	
	// Update is called once per frame
	void Update () { // 매 프레임 목적지에 가까워지도록 이동합니다.
		
		if ( Vector3.Distance ( transform.position, rndShift ) > 0.1f ) // 목적지와 10cm보다 멀리 떨어져 있으면 계속 이동합니다.
		{
			transform.position = Vector3.MoveTowards( transform.position, rndShift, Time.deltaTime ); // 원본대로 초당 최대 1m씩 목적지에 접근합니다.
			Quaternion targRot = Quaternion.LookRotation( rndShift  - transform.position ); // 물고기 로컬 +Z가 목적지를 향하는 목표 회전을 만듭니다.
			transform.rotation = Quaternion.Lerp( transform.rotation, targRot, Time.deltaTime ); // 현재 회전을 목표 회전 쪽으로 조금씩 보간합니다.
		}
		else // 바로 앞 조건이 거짓인 경우의 처리를 시작합니다.
		{
			rndShift = startingPoint + maxDist * Random.insideUnitSphere; // 시작점 주변 반지름 maxDist의 구 안에서 임의 목적지를 고릅니다.
			rndShift.y = startingPoint.y; // 목적지 높이를 시작 높이로 고정합니다.
		}
	
	}
		
	 void OnCollisionEnter(Collision collision) { // 물리 충돌이 새로 시작되면 호출되는 원본 처리입니다.
	
		Debug.Log("Collision"); // 충돌 사실을 Console에 기록합니다.
		
		foreach (ContactPoint contact in collision.contacts) // 충돌한 각 접촉점을 순서대로 살펴봅니다.
		{
			rndShift = maxDist * contact.normal; // 원본은 접촉 법선×거리 자체를 세계 목적지로 씁니다. 시작점이나 현재 위치를 더하지 않습니다.
			rndShift.y = startingPoint.y; // 목적지 높이를 시작 높이로 고정합니다.
        }
		
		
		
	}	

}
