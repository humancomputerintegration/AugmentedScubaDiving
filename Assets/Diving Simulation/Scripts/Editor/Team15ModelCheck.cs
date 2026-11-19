#if UNITY_EDITOR // 이 파일은 Unity 편집기에서만 쓰고 헤드셋 앱에는 넣지 않습니다.
using System; // 파일을 읽는 중에 생긴 예외를 기록할 때 사용합니다.
using System.IO; // 컴퓨터에 OBJ와 MTL 파일이 실제로 있는지 확인할 때 사용합니다.
using UnityEditor; // Unity 메뉴를 만들고 지정한 모델을 다시 가져올 때 사용합니다.
using UnityEngine; // 모델, 메시, 렌더러와 Console 메시지를 다룰 때 사용합니다.

public static class Team15ModelCheck // 장면에 붙이지 않고 메뉴에서 실행하는 검사 도구입니다.
{ // 검사 도구의 내용을 시작합니다.
    [MenuItem("HCI/Reimport Fish and Treasure Models")] // Unity 위쪽 HCI 메뉴에 이 항목을 만듭니다.
    public static void ReimportModels() // 사용자가 이 메뉴를 누르면 실행합니다.
    { // 메뉴가 할 일을 시작합니다.
        if (EditorApplication.isPlayingOrWillChangePlaymode) // 게임 실행 중이거나 실행하려는 중인지 확인합니다.
        { // 실행 중이면 파일을 다시 가져오지 않습니다.
            Debug.LogWarning("[Team15 Models] Stop Play mode, then run this menu again."); // 먼저 Play를 멈추라고 알려줍니다.
            return; // 이번 검사를 여기서 끝냅니다.
        } // Play 상태 확인을 끝냅니다.

        string[] resourcePaths = new string[] // 이번 오류에 나온 네 모델의 Resources 경로만 적습니다.
        { // 네 경로를 차례대로 담습니다.
            "Team15/FriendlyFish/Clownfish", // 흰동가리 모델입니다.
            "Team15/FriendlyFish/BlueTang", // 블루탱 모델입니다.
            "Team15/FriendlyFish/BabyReefShark", // 작은 상어 모델입니다.
            "Team15/Treasure/GoldCache" // 보물 모델입니다.
        }; // 검사할 모델 목록을 끝냅니다.
        int passed = 0; // 실제로 검사를 통과한 모델 수를 셉니다.
        foreach (string resourcePath in resourcePaths) // 네 모델을 하나씩 검사합니다.
        { // 현재 모델의 검사를 시작합니다.
            try // 하나의 모델에서 문제가 생겨도 나머지 모델은 검사합니다.
            { // 파일을 다시 가져오고 확인합니다.
                if (CheckModel(resourcePath)) // 이 모델이 모든 확인을 통과했는지 봅니다.
                { // 통과했을 때만 숫자를 늘립니다.
                    passed = passed + 1; // 통과한 모델을 하나 추가합니다.
                } // 통과 수 계산을 끝냅니다.
            } // 현재 모델의 검사 시도를 끝냅니다.
            catch (Exception exception) // Unity가 파일을 가져오다가 예외를 내면 받아서 기록합니다.
            { // 어떤 모델에서 실패했는지 알려줍니다.
                Debug.LogError("[Team15 Models] FAIL " + resourcePath + "\n" + exception); // 모델 경로와 실제 예외 내용을 함께 보여줍니다.
            } // 예외 기록을 끝냅니다.
        } // 네 모델 검사를 끝냅니다.

        string summary = "[Team15 Models] " + passed + "/4 passed. Project Assets: " + Application.dataPath; // 결과와 지금 열린 프로젝트의 실제 폴더를 적습니다.
        if (passed == resourcePaths.Length) // 네 모델 모두 통과했는지 확인합니다.
        { // 모두 통과한 경우입니다.
            Debug.Log("PASS 4/4. " + summary); // 전체 통과 결과를 Console에 표시합니다.
        } // 전체 통과 처리를 끝냅니다.
        else // 하나라도 실패한 경우입니다.
        { // 위쪽의 개별 오류를 확인하도록 결과를 남깁니다.
            Debug.LogError("FAIL. " + summary); // 실패했는데 성공으로 표시하지 않습니다.
        } // 최종 결과 표시를 끝냅니다.
    } // 메뉴가 할 일을 끝냅니다.

    private static bool CheckModel(string resourcePath) // 모델 하나를 가져올 수 있으면 true를 돌려줍니다.
    { // 모델 하나의 검사를 시작합니다.
        string modelPath = "Assets/Diving Simulation/Resources/" + resourcePath + ".obj"; // Unity가 사용하는 OBJ 경로를 만듭니다.
        string materialPath = "Assets/Diving Simulation/Resources/" + resourcePath + ".mtl"; // 이 OBJ가 읽을 MTL 경로를 만듭니다.
        string absoluteModelPath = Path.Combine(Application.dataPath, "Diving Simulation/Resources/" + resourcePath + ".obj"); // 컴퓨터에서 확인할 실제 OBJ 경로입니다.
        string absoluteMaterialPath = Path.ChangeExtension(absoluteModelPath, ".mtl"); // 같은 폴더의 MTL 파일 경로입니다.
        bool hasModel = File.Exists(absoluteModelPath); // OBJ 파일이 실제로 있는지 확인합니다.
        bool hasMaterial = File.Exists(absoluteMaterialPath); // MTL 파일이 실제로 있는지 확인합니다.
        if (!hasModel) // OBJ가 없을 때입니다.
        { // 빠진 파일을 정확히 알려줍니다.
            Debug.LogError("[Team15 Models] FAIL missing OBJ: " + absoluteModelPath); // 사용자가 확인할 실제 경로를 표시합니다.
        } // OBJ 누락 확인을 끝냅니다.
        if (!hasMaterial) // MTL이 없을 때입니다.
        { // 빠진 재질 설명 파일을 알려줍니다.
            Debug.LogError("[Team15 Models] FAIL missing MTL: " + absoluteMaterialPath); // 사용자가 확인할 실제 경로를 표시합니다.
        } // MTL 누락 확인을 끝냅니다.
        if (!hasModel || !hasMaterial) // 필요한 파일 중 하나라도 없는지 확인합니다.
        { // 파일이 없으면 이 모델을 가져올 수 없습니다.
            return false; // 이 모델을 실패로 기록합니다.
        } // 실제 파일 확인을 끝냅니다.

        ImportAssetOptions options = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport; // 강제로 다시 가져오고 완료될 때까지 기다리는 두 옵션을 함께 사용합니다.
        AssetDatabase.ImportAsset(materialPath, options); // 이 모델의 MTL을 먼저 다시 가져옵니다.
        AssetDatabase.ImportAsset(modelPath, options); // 이 모델의 OBJ만 다시 가져옵니다.
        GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath); // 정확한 파일 경로로 모델을 읽어봅니다.
        GameObject resourceModel = Resources.Load<GameObject>(resourcePath); // ReefModel 코드가 쓰는 Resources 경로로도 읽어봅니다.
        if (importedModel == null || resourceModel == null) // 두 방법 중 하나라도 모델을 못 찾았는지 확인합니다.
        { // 파일은 있지만 Unity가 모델로 읽지 못한 경우입니다.
            AssetImporter importer = AssetImporter.GetAtPath(modelPath); // Unity가 이 파일에 연결한 가져오기 도구를 확인합니다.
            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(modelPath); // Unity가 이 파일을 어떤 자료형으로 읽었는지 확인합니다.
            Debug.LogError("[Team15 Models] FAIL load: " + modelPath + " | AssetDatabase null=" + (importedModel == null) + " | Resources null=" + (resourceModel == null) + " | Importer=" + importer + " | Main type=" + mainType); // 어느 읽기 방법이 실패했는지와 실제 가져오기 정보를 남깁니다.
            return false; // 모델을 읽지 못했으므로 실패입니다.
        } // 두 읽기 방법의 결과 확인을 끝냅니다.
        if (importedModel != resourceModel) // Resources가 다른 파일의 모델을 가져왔는지 확인합니다.
        { // 같은 Resources 이름을 가진 다른 모델이 선택된 경우입니다.
            Debug.LogError("[Team15 Models] FAIL different resource: expected " + modelPath + " | actual " + AssetDatabase.GetAssetPath(resourceModel)); // 예상 경로와 실제 경로를 나란히 표시합니다.
            return false; // 엉뚱한 모델을 읽었으므로 실패입니다.
        } // 모델이 같은지 확인하는 일을 끝냅니다.

        MeshFilter[] meshFilters = importedModel.GetComponentsInChildren<MeshFilter>(true); // 꺼진 자식까지 포함해 OBJ의 메시 목록을 찾습니다.
        int usableMeshes = 0; // 꼭짓점이 있는 메시 수를 셉니다.
        foreach (MeshFilter meshFilter in meshFilters) // 메시를 연결하는 구성요소를 하나씩 봅니다.
        { // 현재 메시를 검사합니다.
            if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertexCount > 0) // 실제 메시가 있고 꼭짓점도 있는지 확인합니다.
            { // 화면에 그릴 모양 데이터가 있는 경우입니다.
                usableMeshes = usableMeshes + 1; // 사용할 수 있는 메시를 하나 추가합니다.
            } // 현재 메시의 모양 데이터 확인을 끝냅니다.
        } // 메시 목록 확인을 끝냅니다.
        Renderer[] renderers = importedModel.GetComponentsInChildren<Renderer>(true); // 모델을 화면에 그릴 렌더러도 찾습니다.
        if (usableMeshes == 0 || renderers.Length == 0) // 모양 데이터 또는 그리는 구성요소가 없는지 확인합니다.
        { // 이름만 있고 그릴 모델이 없는 경우입니다.
            Debug.LogError("[Team15 Models] FAIL empty model: " + modelPath + " | meshes=" + usableMeshes + " | renderers=" + renderers.Length); // 어떤 데이터가 없는지 숫자로 알려줍니다.
            return false; // 빈 모델은 통과시키지 않습니다.
        } // 모델 내용 확인을 끝냅니다.
        Debug.Log("[Team15 Models] PASS " + resourcePath + " | meshes=" + usableMeshes + " | renderers=" + renderers.Length); // 이 모델의 읽기와 모양 데이터 확인이 통과했음을 알립니다.
        return true; // 현재 모델이 통과했다고 메뉴에 알려줍니다.
    } // 모델 하나의 검사를 끝냅니다.
} // 검사 도구의 내용을 끝냅니다.
#endif // Unity 편집기에서만 사용하는 코드의 범위를 끝냅니다.
