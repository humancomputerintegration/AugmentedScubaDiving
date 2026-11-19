using UnityEditor; // Inspector에서 사용자 설정을 보여 주는 기능이다.
using UnityEditor.UI; // 원래 Image Inspector를 그대로 사용하기 위해 가져온다.

[CustomEditor(typeof(CircleGraphic))] // CircleGraphic을 선택했을 때 사용할 Inspector다.
[CanEditMultipleObjects] // 같은 종류의 버튼을 여러 개 골라도 함께 색과 둥근 정도를 조정할 수 있다.
public class CircleGraphicEditor : ImageEditor // 기본 Image 설정 아래에 필요한 항목만 더한다.
{
    public override void OnInspectorGUI() // Inspector를 그릴 때 Unity가 호출한다.
    {
        base.OnInspectorGUI(); // 기존 Image의 색과 Raycast Target 같은 항목을 먼저 보여 준다.
        serializedObject.Update(); // 현재 버튼의 저장된 값을 읽는다.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("roundness")); // 볼록한 느낌의 세기를 0부터 1까지 조절한다.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("phoneIcon")); // 수화기 그림을 이 원 안에 그릴지 고른다.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("symbolColor")); // 수화기 그림의 색을 고른다.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rimWidth")); // 단색 원에 쓰던 기존 테두리 설정도 남겨 둔다.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rimColor")); // 기존 테두리의 색을 보여 준다.
        serializedObject.ApplyModifiedProperties(); // 바꾼 값만 Unity 오브젝트에 저장한다.
    }
}
