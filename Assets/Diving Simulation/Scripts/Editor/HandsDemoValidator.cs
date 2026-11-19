#if UNITY_EDITOR // 이 파일은 Unity Editor에서만 컴파일되며 Quest 빌드에는 포함하지 않습니다.
/*
 * HCI > Validate Hands Demo: 현재 열린 Scene의 연결만 읽습니다. Scene/에셋을 자동 수정하지 않습니다.
 * static은 검사 도구의 공통 함수, List<T>는 검사할 항목 목록, foreach는 각 항목을 차례로 검사합니다.
 * SerializedObject는 private [SerializeField] 참조도 Inspector와 같은 방식으로 읽게 해 줍니다.
 * 구성 검사 통과는 실제 Quest에서 손 추적·충돌·오디오·클릭 시연을 통과했다는 뜻이 아닙니다.
 */
using System; // Type과 문자열 비교 기능을 사용합니다.
using System.Collections.Generic; // 검사 대상과 중복 방지 목록을 사용합니다.
using System.Text; // 여러 결과를 한 보고 문자열로 합칩니다.
using UnityEditor; // 메뉴와 Inspector 직렬화 읽기 기능을 사용합니다.
using UnityEngine; // Scene의 GameObject와 컴포넌트를 읽습니다.
using UnityEngine.SceneManagement; // 현재 열린 Scene을 선택합니다.
using UnityEngine.UI; // 실제 Button의 Inspector 콜백을 검사합니다.

namespace HciHands.EditorTools // 게임 코드와 이름이 겹치지 않는 Editor 전용 영역입니다.
{
    public static class HandsDemoValidator // Scene에 붙이지 않고 메뉴에서 실행하는 검사 도구입니다.
    {
        static int passed; // 통과한 구성 조건 수입니다.
        static int errors; // 수정이 필요한 구성 조건 수입니다.
        static int warnings; // 실행 시 따로 확인할 경고 수입니다.
        static StringBuilder details; // 결과를 순서대로 모을 문자열입니다.
        static readonly List<MonoBehaviour> behaviours = new List<MonoBehaviour>(); // 현재 Scene의 스크립트 목록입니다.
        static readonly List<Transform> transforms = new List<Transform>(); // 꺼진 자식까지 포함한 현재 Scene의 위치 목록입니다.

        [MenuItem("HCI/Validate Hands Demo")] // Unity 상단 HCI 메뉴에 읽기 전용 검사를 추가합니다.
        public static void ValidateActiveScene() // 사용자가 메뉴를 누를 때만 실행합니다.
        {
            passed = errors = warnings = 0; // 이전 검사 수를 지웁니다.
            details = new StringBuilder(); // 이번 검사 보고서를 새로 시작합니다.
            behaviours.Clear(); // 이전 Scene의 참조를 보관하지 않습니다.
            transforms.Clear(); // 위치 목록도 비웁니다.
            Scene scene = SceneManager.GetActiveScene(); // 현재 활성 Scene만 검사합니다.
            if (!scene.IsValid() || !scene.isLoaded) // 검사할 Scene이 없다면 중단합니다.
            {
                Debug.LogError("Hands Demo: open Simulation.unity before validating."); // 필요한 첫 작업을 Console에 표시합니다.
                return; // 빈 Scene을 성공으로 표시하지 않습니다.
            }
            details.AppendLine("SCENE: " + scene.path); // 어떤 Scene을 검사했는지 기록합니다.
            details.AppendLine("READ ONLY CONFIGURATION CHECK - Quest runtime test is still required."); // 검증의 범위를 명시합니다.
            foreach (GameObject root in scene.GetRootGameObjects()) // 현재 Scene의 루트부터 내려갑니다.
            {
                transforms.AddRange(root.GetComponentsInChildren<Transform>(true)); // inactive 보관 객체도 목록에는 포함합니다.
                behaviours.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true)); // 이후 실행 여부와 보관 여부를 구분합니다.
            }
            Check(scene.name == "Simulation", "The active Scene is Simulation.", null); // 시연 Scene을 실수로 바꾸지 않았는지 봅니다.
            Check(FindNamed("00_Systems") != null, "Hierarchy contains 00_Systems.", null); // 시스템 그룹 존재를 확인합니다.
            Check(FindNamed("02_Hand_UI") != null, "Hierarchy contains 02_Hand_UI.", null); // 새 손 UI 그룹 존재를 확인합니다.
            CheckMissingScripts(); // 깨진 script GUID가 있는지 검사합니다.
            CheckCore(); // 입력·이동·통화의 핵심 참조와 실행 개수를 검사합니다.
            CheckButtonsAndAnchors(); // 손 버튼의 실제 콜백과 반대 손목 규칙을 검사합니다.
            CheckHandsOnly(); // 컨트롤러와 옛 이동 경로가 실행되지 않도록 검사합니다.
            string summary = "Hands Demo: " + passed + " passed, " + errors + " errors, " + warnings + " warnings."; // 한눈에 볼 결과 수입니다.
            details.AppendLine(summary); // 상세 보고서에도 같은 집계를 붙입니다.
            Debug.Log(details.ToString()); // Console에서 결과 전체를 복사할 수 있습니다.
            EditorUtility.DisplayDialog("Hands Demo validation", summary + "\nSee Console for details.\nThis does not certify a Quest runtime demo.", "OK"); // 파일이나 Scene은 바꾸지 않습니다.
        }

        static void CheckCore() // 핵심 컴포넌트는 항상 켜진 상태여야 합니다.
        {
            MonoBehaviour input = OneActive("HandsInput"); // 손 상태를 만드는 관리자는 하나입니다.
            MonoBehaviour move = OneActive("SimpleMovement"); // 몸을 옮기는 사용자 이동 코드는 하나입니다.
            MonoBehaviour ui = OneActive("HandUIInteractor"); // 손/마우스 UI 입력의 소유자도 하나입니다.
            MonoBehaviour tower = OneActive("CallTowerManager"); // 동료 주소록은 하나입니다.
            MonoBehaviour dial = OneActive("SimpleDial"); // 실제 통화 상태는 항상 켜진 한 통화기가 소유합니다.
            MonoBehaviour dashboard = OneActive("CallDashboardManager"); // 닫힌 패널 밖의 항상 켜진 통화 표시 담당자를 검사합니다.
            RequireRefs(input, "trackingSpace", "head", "leftAim", "rightAim"); // 좌표 기준과 양손 aim 연결입니다.
            RequireRefs(move, "xrOrigin", "xrCamera", "handsInput", "informationManager"); // 이동의 입력과 깊이 기준입니다.
            RequireRefs(ui, "hands", "headCamera", "pointerLight"); // UI가 같은 손 데이터와 카메라를 쓰게 합니다.
            RequireRefs(tower, "playerTransmitter", "wrongFrequency", "emergencyAudio"); // 수신과 오류/SOS 음성을 검사합니다.
            RequireRefs(dial, "ctm", "ringtone", "voiceSource", "audioTestClip", "feedbackSource", "buttonCue", "connectedCue", "endedCue"); // 실제 통화와 피드백 소리 연결입니다.
            RequireRefs(dashboard, "dial", "ctm", "numberDisplay", "modeDisplay"); // 주소 입력 UI의 필수 연결입니다.
            if (dashboard != null) // 담당자가 있어야 Inspector 배열을 읽을 수 있습니다.
            {
                RequireArray(dashboard, "contactButtons", 5); // 연락처 선택 버튼은 정확히 다섯 개입니다.
                RequireArray(dashboard, "contactLabels", 5); // 버튼과 같은 순서의 연락처 글자도 다섯 개입니다.
                Check(ReadRef(dashboard, "dial") == dial && ReadRef(dashboard, "ctm") == tower, "Dashboard uses the active dial and call tower.", dashboard); // 보관한 옛 UI의 통화기에 잘못 연결하지 않습니다.
            }
            if (move != null) // 이동 코드가 있을 때 물리 연결을 검사합니다.
            {
                Component origin = ReadRef(move, "xrOrigin") as Component; // private xrOrigin 참조를 Inspector처럼 읽습니다.
                CharacterController cc = origin != null ? origin.GetComponent<CharacterController>() : null; // 원본 rig의 CC입니다.
                Check(cc != null, "XR Origin has CharacterController.", move); // 없으면 transform 이동으로 우회하면 안 됩니다.
                if (cc != null) Check(cc.enabled, "CharacterController is enabled in this Scene.", cc); // Play 전 Scene 상태도 확인합니다.
                Rigidbody body = origin != null ? origin.GetComponent<Rigidbody>() : null; // 별도 힘/중력 이동 여부를 검사합니다.
                if (body != null) Check(body.isKinematic && !body.useGravity, "Rig Rigidbody is kinematic with gravity off.", body); // 중립 중 낙하를 막습니다.
                CapsuleCollider extra = origin != null ? origin.GetComponent<CapsuleCollider>() : null; // 원본의 중복 몸통 collider입니다.
                if (extra != null) Check(!extra.enabled, "Extra rig CapsuleCollider is disabled.", extra); // CC와 중복 충돌하지 않아야 합니다.
                Check(ReadRef(move, "handsInput") == input, "Movement reads the active HandsInput.", move); // 입력 출처가 같은지 확인합니다.
            }
            if (ui != null) Check(ReadRef(ui, "hands") == input, "UI reads the same active HandsInput.", ui); // UI와 이동이 같은 신호를 사용합니다.
            if (tower != null && dial != null) Check(ReadRef(tower, "playerTransmitter") == dial, "Call tower points to the active dial.", tower); // inactive 옛 통화기 연결을 잡습니다.
            if (input != null && ui != null && move != null) // 셋 모두 있을 때 실제 script 실행 순서를 검사합니다.
            {
                int inputOrder = ScriptOrder(input); // 입력의 실제 순서를 읽습니다.
                int uiOrder = ScriptOrder(ui); // UI의 실제 순서를 읽습니다.
                int moveOrder = ScriptOrder(move); // 이동의 실제 순서를 읽습니다.
                Check(inputOrder < uiOrder && uiOrder < moveOrder, "Execution order: HandsInput < HandUIInteractor < SimpleMovement.", ui); // 서로 다른 기본 Update 순서를 요구합니다.
            }
            if (tower != null) CheckContacts(tower); // 다섯 동료와 음성 연결을 검사합니다.
        }

        static void CheckContacts(MonoBehaviour tower) // Inspector 배열 순서와 참조의 기본 무결성을 확인합니다.
        {
            SerializedProperty crew = new SerializedObject(tower).FindProperty("crewmates"); // 공식 주소록의 동료 배열입니다.
            if (!Check(crew != null && crew.isArray && crew.arraySize == 5, "CallTowerManager has exactly five crewmates.", tower)) return; // 수가 틀리면 잘못 인덱싱하지 않습니다.
            HashSet<int> ids = new HashSet<int>(); // 같은 동료를 두 슬롯에 넣는 실수를 찾습니다.
            for (int i = 0; i < crew.arraySize; i++) // 다섯 배열 칸을 차례대로 검사합니다.
            {
                Transform mate = crew.GetArrayElementAtIndex(i).objectReferenceValue as Transform; // 해당 동료 물체의 위치 참조입니다.
                if (!Check(mate != null, "Crew slot " + i + " is assigned.", tower)) continue; // 비어 있으면 나머지 슬롯 검사는 계속합니다.
                Check(ids.Add(mate.GetInstanceID()), "Crew slot " + i + " is a distinct object.", mate); // HashSet.Add가 false면 중복입니다.
                MonoBehaviour behaviour = FindOnObject(mate.gameObject, "CrewmateBehavior"); // 상속한 이동 동료도 기본 동료로 인정합니다.
                if (!Check(behaviour != null, mate.name + " has CrewmateBehavior.", mate)) continue; // 통화 행동이 없는 물체는 오류입니다.
                RequireRefs(behaviour, "ctm", "callAudioCaller", "callAudioReceiver"); // 발신/응답 음원 두 개를 요구합니다.
                Check(ReadRef(behaviour, "ctm") == tower, mate.name + " uses the same call tower.", behaviour); // 다른 주소록을 가리키지 않게 합니다.
            }
        }

        static void CheckButtonsAndAnchors() // 새 UI만 검사하고 기존 보관 UI는 건드리지 않습니다.
        {
            int buttons = 0; // 실제 손 버튼 개수를 센 뒤 보고합니다.
            foreach (MonoBehaviour item in behaviours) // 모든 script를 하나씩 살펴봅니다.
            {
                if (item == null) continue; // missing script는 별도 함수가 보고합니다.
                string type = item.GetType().Name; // 별도 XR assembly 타입 의존성 없이 실제 클래스 이름을 읽습니다.
                if (type == "HandUIAnchor") // 손목 UI 추종 연결을 검사합니다.
                {
                    RequireRefs(item, "hands", "head", "content"); // 손 입력, 머리, 실제 자식 UI가 필요합니다.
                    Transform content = ReadRef(item, "content") as Transform; // 숨기거나 움직일 UI입니다.
                    if (content != null) Check(content != item.transform && !item.transform.IsChildOf(content), "HandUIAnchor content cannot disable its own anchor.", item); // 자기 자신/부모를 숨기면 재추적하지 못합니다.
                }
                if (type != "HandUIButton") continue; // 아래는 새 공통 버튼에만 적용합니다.
                buttons++; // 검사할 새 버튼 수입니다.
                RequireRefs(item, "button"); // 실제 기능 이벤트를 가진 UGUI 버튼이 필요합니다.
                Button button = ReadRef(item, "button") as Button; // Inspector의 같은 버튼을 읽습니다.
                Collider collider = item.GetComponent<Collider>(); // 손 ray/touch가 찾을 물리 영역입니다.
                Check(collider != null && collider.enabled, item.name + " has an enabled button collider.", item); // inactive 패널이어도 collider.enabled 설정은 켜져 있어야 합니다.
                Check(item.gameObject.layer == 5, item.name + " uses the UI layer (5).", item); // PhysicalUI를 숨기는 mud와 분리합니다.
                if (button == null) continue; // 이벤트를 읽을 대상이 없으면 다음 버튼으로 갑니다.
                Check(button.enabled, item.name + " has its Button component enabled.", button); // interactable과 컴포넌트 enabled는 별개입니다.
                int count = button.onClick.GetPersistentEventCount(); // Inspector에 저장된 클릭 연결 개수입니다.
                Check(count > 0, item.name + " has a persistent onClick callback.", button); // 보이지만 아무것도 하지 않는 버튼을 찾습니다.
                for (int i = 0; i < count; i++) // 연결된 이벤트를 하나씩 검사합니다.
                    Check(button.onClick.GetPersistentTarget(i) != null && !string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(i)), item.name + " callback " + i + " has a target and method.", button); // 비어 있는 콜백을 찾습니다.
                CheckAllowedWristHand(item); // 같은 손이 자기 손목을 누르지 않도록 검사합니다.
            }
            Check(buttons > 0, "Scene contains HandUIButton controls.", null); // 새 입력 경로가 빠진 Scene을 잡습니다.
            details.AppendLine("INFO: inspected " + buttons + " HandUIButton controls, including closed panels."); // 아직 닫힌 패널의 버튼도 포함한 수입니다.
        }

        static void CheckAllowedWristHand(MonoBehaviour button) // 손목/팔 UI는 반대 손만 허용해야 합니다.
        {
            SerializedProperty allowed = new SerializedObject(button).FindProperty("allowedHand"); // Either/Left/Right 열거형 값을 읽습니다.
            foreach (MonoBehaviour anchor in behaviours) // UI가 어느 손의 anchor에 속하는지 찾습니다.
            {
                if (anchor == null || anchor.GetType().Name != "HandUIAnchor") continue; // 손 anchor만 사용합니다.
                Transform content = ReadRef(anchor, "content") as Transform; // 그 anchor가 움직이는 자식 UI입니다.
                if (content == null || !button.transform.IsChildOf(content)) continue; // 이 버튼이 속하지 않은 UI는 건너뜁니다.
                SerializedProperty side = new SerializedObject(anchor).FindProperty("side"); // 0 왼손, 1 오른손입니다.
                if (side == null || allowed == null) continue; // 필드 누락은 다른 참조 검사와 코드 검토 대상입니다.
                int expected = side.enumValueIndex == 0 ? 2 : 1; // 왼손 anchor는 Right, 오른손 anchor는 Left를 요구합니다.
                Check(allowed.enumValueIndex == expected, button.name + " allows only the opposite hand.", button); // 자기 손목 클릭을 설정 단계에서 발견합니다.
            }
        }

        static void CheckHandsOnly() // 손 전용 시연과 충돌하는 활성 원본 경로를 찾습니다.
        {
            foreach (Transform item in transforms) // GameObject의 실제 activeInHierarchy를 확인합니다.
            {
                if (item.name == "Left Controller" || item.name == "Right Controller") // 원본 rig의 controller 루트 이름입니다.
                    Check(!item.gameObject.activeInHierarchy, item.name + " root stays inactive.", item); // 컨트롤러 모델/입력 루트가 켜지면 오류입니다.
            }
            foreach (MonoBehaviour item in behaviours) // 실제 실행할 컴포넌트만 중복 입력으로 봅니다.
            {
                if (item == null || !item.isActiveAndEnabled) continue; // inactive로 보관한 원본은 오류가 아닙니다.
                string type = item.GetType().Name; // 활성 클래스 이름입니다.
                bool oldMovement = type == "ThumbsUpAscend" || type == "StrokeZone" || InheritsNamed(item.GetType(), "LocomotionProvider"); // 별도로 rig를 움직이는 옛 경로입니다.
                if (oldMovement) Check(false, "Disable duplicate movement component: " + type, item); // SimpleMovement 이외 이동을 보고합니다.
                if (type == "XRInputModalityManager") Check(false, "XRInputModalityManager can re-enable controllers; keep it disabled.", item); // controller 자동 복귀를 막습니다.
                if (type == "MudPinchNavigator") Check(false, "Legacy MudPinchNavigator must stay disabled.", item); // 별도 pinch 선택 이벤트를 막습니다.
                if (type == "XRUIInputModule" || type == "InputSystemUIInputModule" || type == "StandaloneInputModule") // 새 UI가 직접 클릭을 소유하는 설계입니다.
                    Check(false, "Native UI input module is active alongside HandUIInteractor: " + type, item); // 같은 클릭의 두 입력 경로를 보고합니다.
            }
        }

        static void CheckMissingScripts() // Unity가 실제로 로드한 컴포넌트 상태를 사용합니다.
        {
            foreach (Transform item in transforms) // 각 GameObject를 한 번씩 검사합니다.
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject); // script 참조가 깨진 슬롯 수입니다.
                if (count == 0) continue; // 정상 객체는 추가 로그 없이 지나갑니다.
                if (item.gameObject.activeInHierarchy) Check(false, item.name + " has " + count + " missing scripts.", item); // 실행 중인 객체는 오류입니다.
                else Warn(item.name + " has " + count + " missing scripts in inactive archived content.", item); // 보관 객체는 별도 경고로 구분합니다.
            }
        }

        static MonoBehaviour OneActive(string typeName) // 실행 중인 특정 담당자가 하나인지 확인합니다.
        {
            MonoBehaviour found = null; // 첫 번째 발견한 컴포넌트입니다.
            int count = 0; // 활성 인스턴스 수입니다.
            foreach (MonoBehaviour item in behaviours) // 현재 Scene 목록만 순회합니다.
            {
                if (item == null || item.GetType().Name != typeName || !item.isActiveAndEnabled) continue; // 다른 타입과 꺼진 원본은 제외합니다.
                found = item; // 검사에 사용할 연결 대상을 저장합니다.
                count++; // 활성 수를 증가시킵니다.
            }
            Check(count == 1, "Exactly one active " + typeName + " (found " + count + ").", found); // 0개와 2개 이상을 모두 오류로 봅니다.
            return found; // 있으면 추가 연결 검사에 사용합니다.
        }

        static void RequireRefs(MonoBehaviour owner, params string[] fields) // 필수 참조 여러 개를 같은 규칙으로 읽습니다.
        {
            if (owner == null) return; // 컴포넌트 누락은 OneActive 등이 이미 보고했습니다.
            foreach (string field in fields) // 필요한 필드 이름마다 검사합니다.
                Check(ReadRef(owner, field) != null, owner.GetType().Name + "." + field + " is assigned.", owner); // Inspector의 None과 깨진 참조를 찾습니다.
        }

        static UnityEngine.Object ReadRef(MonoBehaviour owner, string field) // private serialized 참조도 읽는 작은 함수입니다.
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field); // 필드 이름으로 Inspector 저장값을 찾습니다.
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference ? property.objectReferenceValue : null; // 객체 참조일 때만 반환합니다.
        }

        static void RequireArray(MonoBehaviour owner, string field, int length) // 지정 길이와 각 객체 참조를 검사합니다.
        {
            SerializedProperty array = new SerializedObject(owner).FindProperty(field); // Inspector 배열을 읽습니다.
            if (!Check(array != null && array.isArray && array.arraySize == length, owner.GetType().Name + "." + field + " has " + length + " slots.", owner)) return; // 범위가 틀리면 순회하지 않습니다.
            for (int i = 0; i < array.arraySize; i++) // 각 슬롯의 객체가 빠지지 않았는지 봅니다.
                Check(array.GetArrayElementAtIndex(i).objectReferenceValue != null, field + "[" + i + "] is assigned.", owner); // 빈 연락처 버튼/글자를 찾습니다.
        }

        static int ScriptOrder(MonoBehaviour item) // Inspector에서 덮어쓴 실제 실행 순서를 읽습니다.
        {
            MonoScript script = MonoScript.FromMonoBehaviour(item); // 해당 C# 에셋을 얻습니다.
            int configured = script != null ? MonoImporter.GetExecutionOrder(script) : 0; // Project Settings의 순서 값을 읽습니다.
            if (configured != 0) return configured; // 별도로 저장한 순서가 있으면 코드의 기본값보다 우선합니다.
            DefaultExecutionOrder attribute = Attribute.GetCustomAttribute(item.GetType(), typeof(DefaultExecutionOrder)) as DefaultExecutionOrder; // Editor 목록에 표시되지 않는 코드의 기본 순서도 읽습니다.
            return attribute != null ? attribute.order : 0; // 기본 순서 선언이 없으면 보통 Update 순서인 0입니다.
        }

        static MonoBehaviour FindOnObject(GameObject target, string typeName) // 상속한 이동 동료도 찾을 수 있습니다.
        {
            foreach (MonoBehaviour item in target.GetComponents<MonoBehaviour>()) // 같은 물체의 스크립트를 검사합니다.
                if (item != null && InheritsNamed(item.GetType(), typeName)) return item; // 부모 타입 이름까지 비교합니다.
            return null; // 해당 행동을 찾지 못했습니다.
        }

        static bool InheritsNamed(Type type, string name) // 직접 타입 이름과 모든 부모 타입을 검사합니다.
        {
            while (type != null) // 상속 사슬이 끝날 때까지 반복합니다.
            {
                if (type.Name == name) return true; // 요청한 부모 또는 실제 타입을 찾았습니다.
                type = type.BaseType; // 한 단계 부모로 올라갑니다.
            }
            return false; // 요청한 타입의 계열이 아닙니다.
        }

        static Transform FindNamed(string name) // 현재 Scene 안에서 그룹 이름을 찾습니다.
        {
            foreach (Transform item in transforms) if (item.name == name) return item; // 다른 Scene이나 prefab 에셋은 검색하지 않습니다.
            return null; // 그룹이 없습니다.
        }

        static bool Check(bool condition, string message, UnityEngine.Object context) // 결과 집계를 한 곳에서 처리합니다.
        {
            if (condition) { passed++; return true; } // 통과는 수만 집계하여 Console을 과하게 늘리지 않습니다.
            errors++; // 실패 조건을 셉니다.
            details.AppendLine("ERROR: " + message); // 전체 보고서에 남깁니다.
            Debug.LogError("Hands Demo: " + message, context); // Console에서 클릭하면 관련 물체를 선택할 수 있습니다.
            return false; // 호출자는 필요하면 다음 계산을 중단합니다.
        }

        static void Warn(string message, UnityEngine.Object context) // inactive 보관 등의 경고는 오류와 구분합니다.
        {
            warnings++; // 경고 수를 셉니다.
            details.AppendLine("WARNING: " + message); // 상세 결과에 남깁니다.
            Debug.LogWarning("Hands Demo: " + message, context); // 관련 객체와 함께 경고를 표시합니다.
        }
    }
}
#endif // Editor 전용 코드 영역을 끝냅니다.
