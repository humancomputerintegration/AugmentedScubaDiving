using UnityEditor; // 에디터 메뉴와 실제로 읽힌 에셋을 검사하는 기능을 사용합니다.
using UnityEngine; // 오브젝트, 지형, 재질, 메시 형식을 사용합니다.
using UnityEngine.SceneManagement; // 지금 열려 있는 Scene만 검사합니다.
using UnityEngine.UI; // 지도 그림과 표식의 실제 연결을 확인합니다.

public static class Team15SceneAssetCheck // 게임에 붙이지 않고 메뉴에서만 실행하는 읽기 전용 검사입니다.
{ // 검사 도구의 범위를 시작합니다.
    private static int errors; // 이번 검사에서 발견한 오류 수입니다.
    private static int swimmers; // Scene에 있는 전체 수영 컴포넌트 수입니다.
    private static int activeSwimmers; // 켜져 있고 실제로 활성화된 수영 컴포넌트 수입니다.
    private static int maps; // 검사한 지도 컴포넌트 수입니다.
    private static int mudStorms; // 머드 효과 연결을 검사한 담당자 수입니다.
    private static Texture2D reefMap; // 지도 배경으로 사용해야 하는 실제 그림입니다.
    private static Sprite diverArrow; // 사용자 방향을 나타내는 실제 표식입니다.
    private static Sprite boatMarker; // 보트 위치를 나타내는 실제 표식입니다.
    private static Sprite goldMarker; // 보물 위치를 나타내는 실제 표식입니다.
    private static Material mudMaterial; // 머드 입자가 사용해야 하는 실제 재질입니다.

    [MenuItem("HCI/Check Scene Assets")] // Unity 위쪽 HCI 메뉴에 검사 항목을 추가합니다.
    public static void CheckSceneAssets() // 사용자가 메뉴를 누를 때 한 번만 실행합니다.
    { // 한 번의 검사를 시작합니다.
        Debug.Log("Team15 검사 대상 Assets 폴더: " + Application.dataPath); // 여러 프로젝트 중 지금 열어 둔 실제 폴더를 알려 줍니다.
        if (EditorApplication.isPlayingOrWillChangePlaymode) // 실행 중 오류로 꺼진 생물을 놓치지 않도록 Play 밖에서만 검사합니다.
        { // 실행 중 검사 요청을 처리합니다.
            Debug.LogError("Team15: Play를 중지한 뒤 HCI > Check Scene Assets를 다시 실행하세요."); // 사용자가 먼저 눌러야 할 중지 동작을 알려 줍니다.
            return; // 게임 실행 상태나 Scene을 바꾸지 않고 검사를 끝냅니다.
        } // 실행 상태 확인을 마칩니다.
        Scene scene = SceneManager.GetActiveScene(); // 지금 선택된 Scene을 읽습니다.
        if (!scene.IsValid() || !scene.isLoaded || scene.path != "Assets/Diving Simulation/Scenes/Simulation.unity") // 다른 장면을 검사해서 통과했다고 말하지 않습니다.
        { // 잘못된 Scene 안내를 시작합니다.
            Debug.LogError("Team15: Simulation.unity를 열고 활성 Scene으로 선택한 뒤 다시 검사하세요."); // 사용자가 먼저 열어야 하는 파일을 알려 줍니다.
            return; // Scene을 열거나 바꾸지 않고 여기에서 끝냅니다.
        } // Scene 확인을 마칩니다.
        errors = 0; // 이전 검사 결과를 이번 오류 수에 섞지 않습니다.
        swimmers = 0; // 전체 생물 수를 새로 셉니다.
        activeSwimmers = 0; // 활성 생물 수도 새로 셉니다.
        maps = 0; // 실제 지도 수를 새로 셉니다.
        mudStorms = 0; // 실제 머드 담당자 수도 새로 셉니다.
        CheckPrefab("1bd964bc4e9a578cafea0509315092b9", "BabyReefShark"); // 오류에 나온 상어 프리팹을 실제 임포트 결과에서 찾습니다.
        CheckPrefab("5c38aaedde5d532a9156fd000a926d6b", "Clownfish"); // 오류에 나온 흰동가리 프리팹을 찾습니다.
        CheckPrefab("825c12f0af6e51de83318b0b1af1c27d", "GoldCache"); // 보물 프리팹을 찾습니다.
        CheckPrefab("87c82cd3941154cd8f0a7cae21b9c9ae", "BlueTang"); // 파란 물고기 프리팹을 찾습니다.
        CheckPrefab("c2b2ebeedde450248ab145e38c347481", "Romain Diver"); // 보트 위 동료 모델의 프리팹을 찾습니다.
        CheckPrefab("ebf8f52525f359949594a45771865fd0", "Eliza Diver"); // Eliza의 실제 프리팹을 찾습니다.
        CheckPrefab("f477837b2b88504da1858583ea20c9a5", "Pedro Diver"); // Pedro의 실제 프리팹을 찾습니다.
        reefMap = ReadAsset<Texture2D>("d19c7c6ef2de57cd9d4175d00d0cc1fd", "ReefMap.png"); // 지도 그림의 GUID가 실제 그림으로 읽히는지 확인합니다.
        diverArrow = ReadAsset<Sprite>("6b7568b27926589fb64c107fd5d8e01b", "DiverArrow.png"); // 사용자 화살표가 Sprite로 임포트되었는지 확인합니다.
        boatMarker = ReadAsset<Sprite>("26e1d3f9059350819543b40ef20060fd", "BoatMarker.png"); // 보트 그림이 Sprite로 읽히는지 확인합니다.
        goldMarker = ReadAsset<Sprite>("c4c37ad015bc555d894fcccd585cbe75", "GoldMarker.png"); // 보물 그림이 Sprite로 읽히는지 확인합니다.
        CheckMudShader(); // 머드 재질과 셰이더의 실제 연결과 현재 컴파일 오류를 확인합니다.
        foreach (GameObject root in scene.GetRootGameObjects()) // 현재 Scene의 최상위 오브젝트부터 하나씩 확인합니다.
        { // 계층 검사를 시작합니다.
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) CheckObject(child.gameObject); // 꺼진 자식까지 읽어 빠진 프리팹과 스크립트를 찾습니다.
        } // 계층 검사를 마칩니다.
        Require(maps > 0, "Scene에 MinimapUI가 없습니다.", null); // 지도 담당자가 통째로 빠진 경우도 오류로 알립니다.
        Require(mudStorms > 0, "Scene에 MudStorm이 없습니다.", null); // 머드 효과 담당자가 빠진 경우를 놓치지 않습니다.
        CheckModel("Team15/FriendlyFish/Clownfish"); // 실행 때 읽는 흰동가리 모델과 메시를 검사합니다.
        CheckModel("Team15/FriendlyFish/BlueTang"); // 실행 때 읽는 파란 물고기 모델과 메시를 검사합니다.
        CheckModel("Team15/FriendlyFish/BabyReefShark"); // 실행 때 읽는 상어 모델과 메시를 검사합니다.
        CheckModel("Team15/Treasure/GoldCache"); // 실행 때 읽는 보물 모델과 메시를 검사합니다.
        Debug.Log("Team15 수영 컴포넌트: 전체 " + swimmers + ", 활성 " + activeSwimmers + ". 배포 Scene의 참고 수는 57개입니다."); // 개수는 참고 정보이며 실제 수영 성공을 뜻하지 않습니다.
        if (swimmers != 57) Debug.LogWarning("Team15: 수영 컴포넌트 수가 배포 Scene의 57개와 다릅니다. 프리팹 누락 또는 직접 수정한 구성을 확인하세요."); // 의도적인 Scene 수정도 가능하므로 개수 차이만으로 실패 처리하지 않습니다.
        if (errors == 0) Debug.Log("Team15 PASS - ASSET LINKS ONLY. 에셋 연결 검사만 통과했습니다. Android 빌드, 실제 수영, 소리, Quest 화면은 별도로 확인하세요."); // 통과의 범위를 에셋 연결로 명확하게 제한합니다.
        else Debug.LogError("Team15 ASSET LINKS FAILED: " + errors + "개. 위 오류를 눌러 연결이 빠진 대상을 확인하세요. Scene은 수정하거나 저장하지 않았습니다."); // 오류 수와 다음 행동을 알려 줍니다.
    } // 한 번의 검사를 마칩니다.

    private static T ReadAsset<T>(string guid, string label) where T : UnityEngine.Object // 같은 GUID 읽기 과정을 그림과 재질 등의 형식에 재사용합니다.
    { // 에셋 읽기를 시작합니다.
        string path = AssetDatabase.GUIDToAssetPath(guid); // Unity가 이 GUID를 어느 파일로 기억하는지 읽습니다.
        T asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path); // 실제로 임포트된 파일을 요청한 형식으로 읽습니다.
        Require(asset != null, label + " 에셋을 읽지 못했습니다. GUID=" + guid + ", 경로=" + path, asset); // 파일이 없거나 임포트 결과가 잘못된 경우를 모두 알립니다.
        return asset; // 읽은 에셋 또는 실패를 나타내는 null을 돌려줍니다.
    } // 에셋 읽기를 마칩니다.

    private static void CheckPrefab(string guid, string label) // 오류에 나온 프리팹이 실제 GameObject 에셋인지 확인합니다.
    { // 프리팹 확인을 시작합니다.
        GameObject asset = ReadAsset<GameObject>(guid, label); // 프리팹의 GUID로 실제 에셋을 읽습니다.
        if (asset == null) return; // 읽기 실패는 이미 알렸으므로 없는 오브젝트를 더 조사하지 않습니다.
        PrefabAssetType type = PrefabUtility.GetPrefabAssetType(asset); // Unity가 실제로 분류한 프리팹 종류를 읽습니다.
        bool valid = type == PrefabAssetType.Regular || type == PrefabAssetType.Variant || type == PrefabAssetType.Model; // 정상 프리팹, 변형 프리팹, 모델 프리팹만 인정합니다.
        Require(valid, label + " 파일이 유효한 프리팹 에셋이 아닙니다. 종류=" + type, asset); // MissingAsset과 NotAPrefab을 모두 거절합니다.
    } // 프리팹 확인을 마칩니다.

    private static void CheckObject(GameObject target) // Scene 오브젝트 하나에 필요한 검사를 적용합니다.
    { // 오브젝트 검사를 시작합니다.
        Require(PrefabUtility.GetPrefabInstanceStatus(target) != PrefabInstanceStatus.MissingAsset, "Missing Prefab: " + target.name, target); // 원본 프리팹을 잃은 Scene 오브젝트를 표시합니다.
        int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target); // 이 오브젝트에 빠진 C# 컴포넌트가 몇 개 있는지 읽습니다.
        Require(missingScripts == 0, target.name + ": Missing Script " + missingScripts + "개", target); // 빠진 컴포넌트가 있으면 Console에서 대상을 선택할 수 있게 합니다.
        CreatureSwim swim = target.GetComponent<CreatureSwim>(); // 이 오브젝트가 수영하는 생물인지 확인합니다.
        if (swim != null) CheckSwimmer(swim); // 생물이면 수면과 바닥 연결을 검사합니다.
        MinimapUI map = target.GetComponent<MinimapUI>(); // 이 오브젝트가 지도 담당자인지 확인합니다.
        if (map != null) CheckMap(map); // 지도이면 실제 배경 그림과 표식 연결을 검사합니다.
        MudStorm mud = target.GetComponent<MudStorm>(); // 이름이 바뀌어도 실제 머드 담당자를 통해 효과를 찾습니다.
        if (mud != null) CheckMudObject(mud); // 실제로 켜고 끄는 입자 오브젝트의 재질을 검사합니다.
    } // 오브젝트 검사를 마칩니다.

    private static void CheckSwimmer(CreatureSwim swim) // 실제 활성 생물의 이동 준비 상태를 확인합니다.
    { // 수영 연결 검사를 시작합니다.
        swimmers++; // 꺼진 생물도 전체 개수에는 포함합니다.
        if (!swim.enabled || !swim.gameObject.activeInHierarchy) return; // 현재 실행하지 않는 컴포넌트는 활성 검사에서 제외합니다.
        activeSwimmers++; // 실제로 실행할 생물의 수를 셉니다.
        Require(swim.waterSurface != null, swim.name + ": waterSurface가 없습니다.", swim); // 수면 높이를 읽을 Transform이 연결되어 있어야 합니다.
        TerrainCollider floor = swim.seaFloor as TerrainCollider; // 임의 Collider가 아니라 실제 지형 Collider인지 확인합니다.
        Require(floor != null, swim.name + ": seaFloor에 TerrainCollider를 연결해야 합니다.", swim); // 바닥의 형식이 잘못되면 정확히 알립니다.
        if (floor != null) Require(floor.enabled && !floor.isTrigger && floor.gameObject.activeInHierarchy && floor.terrainData != null, swim.name + ": 바닥 지형이 꺼져 있거나 Trigger이거나 TerrainData가 없습니다.", swim); // 사용할 수 있는 실제 바닥인지를 확인합니다.
        Require(swim.speed > 0f && !float.IsInfinity(swim.speed), swim.name + ": speed는 유한한 양수여야 합니다.", swim); // 0, 음수, NaN, 무한대 속도를 거절합니다.
    } // 수영 연결 검사를 마칩니다.

    private static void CheckMudShader() // 머드 재질과 셰이더가 Unity에서 실제로 연결되었는지 확인합니다.
    { // 머드 검사를 시작합니다.
        mudMaterial = ReadAsset<Material>("39cf45aad13658e8bba46988de1f8bcf", "MudLightCone.mat"); // Scene이 사용하는 머드 재질을 읽습니다.
        Shader shader = ReadAsset<Shader>("bed88c734c2f560a823a7729b6db0514", "MudLightCone.shader"); // 같은 재질이 참조해야 하는 셰이더를 읽습니다.
        if (mudMaterial == null || shader == null) return; // 파일을 읽지 못했으면 다음 검사를 건너뜁니다.
        Require(mudMaterial.shader == shader && shader.name == "Scuba/Mud Light Cone", "MudLightCone 재질의 Shader 연결이 다릅니다.", mudMaterial); // 오류 셰이더나 다른 셰이더로 바뀌지 않았는지 확인합니다.
        Require(!ShaderUtil.ShaderHasError(shader), "MudLightCone 셰이더에 컴파일 오류가 기록되어 있습니다.", shader); // Unity가 이미 발견한 컴파일 오류를 확인합니다.
        Require(shader.isSupported, "MudLightCone 셰이더를 현재 에디터의 그래픽 API에서 지원하지 않습니다.", shader); // 현재 PC의 지원 여부를 확인하며 Quest 통과로 해석하지 않습니다.
        foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader)) // Unity가 기록한 셰이더 진단 내용을 하나씩 읽습니다.
        { // 셰이더 메시지 출력을 시작합니다.
            Debug.Log("Mud shader: " + message.file + ":" + message.line + " [" + message.platform + "] " + message.message, shader); // 실제 파일과 줄 번호를 알려 다음 수정의 근거로 남깁니다.
        } // 셰이더 메시지 출력을 마칩니다.
    } // 머드 검사를 마칩니다.

    private static void CheckMudObject(MudStorm mud) // 머드 담당자가 실제로 사용하는 입자 재질을 검사합니다.
    { // 머드 오브젝트 검사를 시작합니다.
        mudStorms++; // 실제로 발견한 머드 담당자 수를 셉니다.
        Require(mud.mudGO != null, "MudStorm의 mudGO 연결이 없습니다.", mud); // 효과 프리팹이 빠진 경우를 알립니다.
        if (mud.mudGO == null) return; // 없는 효과의 자식을 조사하지 않습니다.
        ParticleSystemRenderer[] particles = mud.mudGO.GetComponentsInChildren<ParticleSystemRenderer>(true); // 현재 꺼진 머드 입자도 모두 검사합니다.
        Require(particles.Length > 0, "머드 오브젝트에 ParticleSystemRenderer가 없습니다.", mud); // 빈 오브젝트만 남은 경우를 구분합니다.
        foreach (ParticleSystemRenderer particle in particles) Require(mudMaterial != null && particle.sharedMaterial == mudMaterial, "머드 입자의 재질이 MudLightCone에 연결되지 않았습니다.", particle); // 화면을 가리는 입자가 오류 재질을 사용하지 않는지 확인합니다.
    } // 머드 오브젝트 검사를 마칩니다.

    private static void CheckMap(MinimapUI map) // 지도 안의 실제 그림과 표식이 흰 사각형으로 빠지지 않았는지 확인합니다.
    { // 지도 검사를 시작합니다.
        maps++; // 발견한 지도 담당자 수를 셉니다.
        Require(map.terrain != null && map.terrain.terrainData != null, "지도에 Terrain 또는 TerrainData가 없습니다.", map); // 지도 계산의 원점과 크기를 읽을 지형이 있어야 합니다.
        Require(map.mapArea != null && map.player != null, "지도에 mapArea 또는 player가 없습니다.", map); // 지도 범위와 사용자 위치가 연결되어야 합니다.
        Require(map.terrainImage != null && reefMap != null && map.terrainImage.texture == reefMap, "지도 RawImage의 Texture를 ReefMap.png에 연결해야 합니다.", map); // 배경 그림의 실제 참조를 검사합니다.
        if (map.markers == null) // 표식 배열 자체가 빠진 경우를 확인합니다.
        { // 배열 누락 처리를 시작합니다.
            Require(false, "지도 markers 배열이 없습니다.", map); // 없는 배열에 접근하지 않고 오류를 알립니다.
            return; // 이번 지도 검사를 끝냅니다.
        } // 배열 누락 처리를 마칩니다.
        foreach (MinimapUI.MapMarker marker in map.markers) // Scene에 등록된 지도 표식을 하나씩 확인합니다.
        { // 표식 검사를 시작합니다.
            if (marker == null || marker.icon == null || marker.target == null) // 실제 대상 또는 표시할 아이콘이 빠졌는지 확인합니다.
            { // 표식 연결 누락 처리를 시작합니다.
                Require(false, "지도 표식의 target 또는 icon 연결이 없습니다.", map); // 빠진 프리팹 때문에 대상이 사라진 경우도 알립니다.
                continue; // 다음 표식은 계속 검사합니다.
            } // 표식 연결 누락 처리를 마칩니다.
            Sprite expected = null; // 그림을 쓰는 표식일 때만 기대하는 Sprite를 선택합니다.
            if (marker.rotateWithTarget) expected = diverArrow; // 시선에 따라 돌아가는 사용자 표식은 화살표입니다.
            else if (marker.hideOutsideMap) expected = marker.icon.name.Contains("Gold") ? goldMarker : boatMarker; // 현재 Scene의 범위 밖에서 숨는 표식은 보물 또는 보트입니다.
            if (!marker.rotateWithTarget && !marker.hideOutsideMap) continue; // 동료의 색깔 점은 별도 그림이 필요하지 않습니다.
            Image icon = marker.icon.GetComponent<Image>(); // 표식을 그리는 UI Image를 읽습니다.
            Require(icon != null && expected != null && icon.sprite == expected, marker.icon.name + ": Sprite 그림 연결을 확인하세요.", marker.icon); // 빈 Sprite나 잘못된 내부 참조를 찾습니다.
        } // 표식 검사를 마칩니다.
    } // 지도 검사를 마칩니다.

    private static void CheckModel(string path) // Resources가 실행 중 찾는 모델을 생성하지 않고 읽기만 합니다.
    { // 모델 검사를 시작합니다.
        GameObject model = Resources.Load<GameObject>(path); // 게임과 같은 확장자 없는 경로로 모델을 읽습니다.
        Require(model != null, "Resources 모델을 읽지 못했습니다: " + path, model); // 경로나 임포트가 잘못되었으면 알립니다.
        if (model == null) return; // 없는 모델의 자식을 조사하지 않습니다.
        bool hasMesh = false; // 실제로 그릴 메시가 있는지 기록합니다.
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true)) // 정적인 모델 메시를 하나씩 읽습니다.
        { // 일반 메시 검사를 시작합니다.
            if (filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0) hasMesh = true; // 점이 하나 이상 있는 실제 메시를 찾으면 표시합니다.
        } // 일반 메시 검사를 마칩니다.
        foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) // 뼈로 움직이는 모델 메시도 확인합니다.
        { // 뼈 메시 검사를 시작합니다.
            if (renderer.sharedMesh != null && renderer.sharedMesh.vertexCount > 0) hasMesh = true; // 빈 껍데기 모델을 통과시키지 않습니다.
        } // 뼈 메시 검사를 마칩니다.
        Require(hasMesh, "Resources 모델에 사용할 메시가 없습니다: " + path, model); // 모델 파일이 있어도 메시 임포트가 실패한 경우를 구분합니다.
    } // 모델 검사를 마칩니다.

    private static void Require(bool condition, string message, UnityEngine.Object context) // 공통 오류 처리만 한 곳에 모읍니다.
    { // 조건 확인을 시작합니다.
        if (condition) return; // 조건을 만족하면 어떤 Scene 값도 바꾸지 않고 돌아갑니다.
        errors++; // 이번 검사에서 실패한 조건 하나를 셉니다.
        Debug.LogError("Team15: " + message, context); // Console에서 오류를 클릭하면 관련 대상을 찾을 수 있게 합니다.
    } // 조건 확인을 마칩니다.
} // 읽기 전용 에셋 검사 도구를 마칩니다.
