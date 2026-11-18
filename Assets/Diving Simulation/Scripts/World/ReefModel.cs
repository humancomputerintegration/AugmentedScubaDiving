/*
역할: 무료 원본 OBJ 모델을 Resources에서 읽어 산호·해초·잠수함 프리팹에 넣습니다.
Awake는 첫 프레임 전에 실행됩니다. Resources 경로에는 Resources 폴더 이름과 확장자를 쓰지 않습니다.
배열의 materialNames와 materials는 같은 번호끼리 짝입니다. 모델의 실제 재질 이름으로 URP 재질을 연결합니다.
원본 OBJ의 내부 mesh fileID를 추측해서 프리팹을 깨뜨리지 않도록 모델 전체를 읽습니다.
*/
using UnityEngine; // Unity의 모델·재질·오브젝트 형식을 사용합니다.

[DisallowMultipleComponent] // 같은 프리팹에 모델 생성기를 중복해서 붙이지 않습니다.
public class ReefModel : MonoBehaviour // 산호·해초·잠수함의 실제 모델을 준비하는 컴포넌트입니다.
{ // 클래스 정의를 시작합니다.
    public string modelResource; // Resources 폴더 기준 실제 OBJ 에셋 경로입니다.
    public string[] materialNames; // OBJ 안에 들어 있는 원본 재질 이름입니다.
    public Material[] materials; // 같은 번호의 원본 재질을 대체할 실제 URP 재질입니다.
    private void Awake() // Scene의 첫 화면을 그리기 전에 한 번 모델을 만듭니다.
    { // 준비 범위의 시작입니다.
        if (string.IsNullOrWhiteSpace(modelResource)) { Debug.LogError("ReefModel: Resources 경로를 연결하세요.", this); return; } // 빈 경로로 모든 에셋을 찾거나 잘못된 요청을 하지 않습니다.
        GameObject asset = Resources.Load<GameObject>(modelResource); // 원본 OBJ의 전체 모델을 읽습니다.
        if (asset == null) { Debug.LogError("ReefModel: Resources 모델이 없습니다: " + modelResource, this); return; } // 실제 파일이 없으면 정확히 알립니다.
        if (materialNames == null || materials == null || materialNames.Length != materials.Length) { Debug.LogError("ReefModel: 재질 이름과 재질 배열 수가 다릅니다.", this); return; } // 번호가 어긋난 재질 연결을 거절합니다.
        GameObject model = Instantiate(asset, transform, false); // 원본 자식 구조와 UV를 보존해 복사합니다.
        model.name = asset.name; // Hierarchy에서 원본 모델을 쉽게 찾게 합니다.
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) // 실제 모델의 모든 렌더러를 조사합니다.
        { // 렌더러 처리 범위입니다.
            Material[] slots = renderer.sharedMaterials; // 이 렌더러의 실제 재질 슬롯을 읽습니다.
            for (int i = 0; i < slots.Length; i++) // 슬롯마다 연결할 URP 재질을 찾습니다.
            { // 슬롯 검사 범위입니다.
                if (slots[i] == null) continue; // 원본에 빈 슬롯이 있으면 없는 이름을 읽지 않습니다.
                string sourceName = slots[i].name.Replace(" (Instance)", ""); // 복사본 표시는 제거하고 원본 이름으로 비교합니다.
                int match = System.Array.IndexOf(materialNames, sourceName); // 같은 이름의 항목 번호를 찾습니다.
                if (match >= 0 && materials[match] != null) slots[i] = materials[match]; // 실제로 일치하고 연결된 재질만 교체합니다.
                else Debug.LogWarning("ReefModel: URP 재질 연결을 확인하세요: " + sourceName, this); // 누락된 슬롯을 조용히 숨기지 않습니다.
            } // 슬롯 검사를 마칩니다.
            renderer.sharedMaterials = slots; // 복사본의 렌더러에 준비한 배열을 적용합니다.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 저비용 수중 장식은 추가 그림자 그리기를 하지 않습니다.
        } // 렌더러 처리를 마칩니다.
    } // 모델 준비를 마칩니다.
} // ReefModel 정의를 마칩니다.
