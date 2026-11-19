/* Pro Standard Assets 유래 원본 반사 코드의 동작은 변경하지 않고 한국어 해설만 덧붙였습니다. URP/Quest 호환 검증을 수행했다는 뜻은 아닙니다. */
using UnityEngine; // 위치, 회전, GameObject와 MonoBehaviour 등 Unity의 기본 기능을 가져옵니다.
using System.Collections; // IEnumerator 코루틴과 Hashtable 같은 컬렉션 이름을 사용할 수 있게 합니다.

// It's a Water script from Pro Standard Assets,
// just refraction stuff was removed.

[ExecuteInEditMode] // Make mirror live-update even when not in play mode | 원본처럼 Play 모드 밖에서도 반사 갱신 코드를 실행하도록 표시합니다.
public class WaterMirrorReflection : MonoBehaviour // Pro Standard Assets 유래의 원본 평면 반사 코드입니다. 저작 출처 주석을 보존합니다.
{
	//public int custom_scale = 1;
    public bool m_DisablePixelLights = true; // 원본에 남아 있는 픽셀 조명 옵션입니다. 아래 실제 적용 코드는 주석 처리되어 있습니다.
    public int m_TextureSize = 256; // 반사 그림을 담을 정사각 RenderTexture의 한 변 픽셀 수입니다.
    public float m_ClipPlaneOffset = 0.07f; // 반사 잘라내기 평면을 표면에서 조금 띄우는 거리입니다.
	public bool m_BackSide = false; // 반사 평면의 법선을 반대로 사용할지 정합니다.
    
    public LayerMask m_ReflectLayers = -1; // 반사에 포함할 레이어 비트이며 -1은 모든 비트가 켜진 값입니다.
        
    private Hashtable m_ReflectionCameras = new Hashtable(); // Camera -> Camera table | 원본 카메라→반사용 카메라의 대응표를 저장합니다.
    
    private RenderTexture m_ReflectionTexture = null; // 반사 결과를 담는 임시 GPU 텍스처입니다.
    private int m_OldReflectionTextureSize = 0; // 텍스처를 다시 만들 필요가 있는지 크기 변경을 비교합니다.
    
    private static bool s_InsideRendering = false; // 모든 인스턴스가 공유하는 재귀 반사 방지 표식입니다.

    // This is called when it's known that the object will be rendered by some
    // camera. We render reflections and do other updates here.
    // Because the script executes in edit mode, reflections for the scene view
    // camera will just work!
    public void OnWillRenderObject() // 카메라가 이 렌더러를 그리려 할 때 호출되는 원본 콜백입니다.
    {
        if( !enabled || !GetComponent<Renderer>() || !GetComponent<Renderer>().sharedMaterial || !GetComponent<Renderer>().enabled ) // 스크립트/렌더러/재질이 유효하고 켜졌는지 확인합니다.
            return; // 현재 함수를 바로 끝내고 이번 프레임의 남은 계산을 하지 않습니다.
            
        Camera cam = Camera.current; // 현재 이 물체를 렌더링하는 카메라를 가져옵니다.
        if( !cam ) // 현재 카메라가 없으면 다음 return으로 중단합니다.
            return; // 현재 함수를 바로 끝내고 이번 프레임의 남은 계산을 하지 않습니다.
    
        // Safeguard from recursive reflections.        
        if( s_InsideRendering ) // 이미 반사를 렌더링 중이면 다시 반사에 들어가지 않습니다.
            return; // 현재 함수를 바로 끝내고 이번 프레임의 남은 계산을 하지 않습니다.
        s_InsideRendering = true; // 반사 작업을 시작했다고 표시합니다.
        
        Camera reflectionCamera; // 만들거나 찾아 받을 반사용 카메라 변수입니다.
        CreateMirrorObjects( cam, out reflectionCamera ); // 필요한 반사 텍스처와 카메라를 준비합니다.
		reflectionCamera.renderingPath = RenderingPath.Forward; // 원본대로 반사 카메라의 렌더링 경로를 Forward로 요청합니다.
        
        // find out the reflection plane: position and normal in world space
        Vector3 pos = transform.position; // 물 표면의 세계 위치입니다.
		Vector3 normal = transform.up; // 물 표면의 로컬 위축을 세계 법선으로 사용합니다.
		if(m_BackSide) normal = -normal; // 뒷면 옵션이면 법선 방향을 반대로 뒤집습니다.
		
        
        // Optionally disable pixel lights for reflection
        //int oldPixelLightCount = QualitySettings.pixelLightCount;
        //if( m_DisablePixelLights )
         //   QualitySettings.pixelLightCount = 0;
        
        //UpdateCameraModes( cam, reflectionCamera );
        
        // Render reflection
        // Reflect camera around reflection plane
        float d = -Vector3.Dot (normal, pos) - m_ClipPlaneOffset; // 평면식 n·x+d=0의 d를 표면 위치와 오프셋으로 계산합니다.
        Vector4 reflectionPlane = new Vector4 (normal.x, normal.y, normal.z, d); // 법선 세 성분과 d를 네 수로 묶습니다.
     
        Matrix4x4 reflection = Matrix4x4.zero; // 계산 결과를 채울 4×4 행렬을 준비합니다.
        CalculateReflectionMatrix (ref reflection, reflectionPlane); // ref로 같은 행렬을 전달해 평면 반사 행렬의 각 원소를 채웁니다.
        Vector3 oldpos = cam.transform.position; // 현재 카메라의 원래 세계 위치를 저장합니다.
        Vector3 newpos = reflection.MultiplyPoint( oldpos ); // 원래 카메라 위치를 평면 반대쪽으로 대칭 이동한 위치입니다.
        reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection; // 반사한 세계 좌표를 원래 카메라 기준으로 변환하는 행렬을 설정합니다.
    
        // Setup oblique projection matrix so that near plane is our reflection
        // plane. This way we clip everything below/above it for free.
        Vector4 clipPlane = CameraSpacePlane( reflectionCamera, pos, normal, 1.0f ); // 반사 표면의 절단 평면을 카메라 좌표로 변환합니다.
        Matrix4x4 projection = cam.projectionMatrix; // 원래 카메라의 투영 행렬을 복사합니다.
        CalculateObliqueMatrix (ref projection, clipPlane); // 카메라의 가까운 절단면을 기울어진 물 평면에 맞춥니다.
        reflectionCamera.projectionMatrix = projection; // 계산한 투영을 반사 카메라에 적용합니다.
        
        reflectionCamera.cullingMask = ~(1<<4) & m_ReflectLayers.value; // never render water layer | Water 레이어 4를 제외하고 지정한 레이어만 반사에 그립니다.
        reflectionCamera.targetTexture = m_ReflectionTexture; // 화면 대신 반사 텍스처에 결과를 그립니다.
        GL.invertCulling = true; // 거울 반전으로 뒤집힌 삼각형 앞/뒷면 판정을 반대로 바꿉니다.
        reflectionCamera.transform.position = newpos; // 반사 카메라를 대칭 위치로 놓습니다.
        Vector3 euler = cam.transform.eulerAngles; // 원래 카메라 회전각을 읽습니다.
        reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z); // 원본대로 x 각도는 0, y/z 각도는 원래 값을 사용합니다.
        reflectionCamera.Render(); // 원본 카메라 수동 렌더링 호출입니다. 실제 사용 중인 렌더 파이프라인 호환성은 별도 실행 확인이 필요합니다.
        reflectionCamera.transform.position = oldpos; // 원본대로 반사 카메라 위치를 원래 카메라 위치로 돌려놓습니다.
        GL.invertCulling = false; // 뒤집었던 삼각형 판정을 다시 끕니다.
        Material[] materials = GetComponent<Renderer>().sharedMaterials; // 이 렌더러의 공유 재질 목록을 읽습니다.
        foreach( Material mat in materials ) { // 각 공유 재질에 같은 반사 결과를 전달합니다.
            if( mat.HasProperty("_ReflectionTex") ) // 해당 재질이 반사 텍스처 속성을 가지고 있는지 확인합니다.
                mat.SetTexture( "_ReflectionTex", m_ReflectionTexture ); // 반사 결과를 재질의 해당 셰이더 속성에 넣습니다.
        }
        
        // Set matrix on the shader that transforms UVs from object space into screen
        // space. We want to just project reflection texture on screen.
        Matrix4x4 scaleOffset = Matrix4x4.TRS( // 이동·회전·크기 변환으로 화면 좌표를 텍스처 좌표에 맞출 행렬을 만듭니다.
            new Vector3(0.5f,0.5f,0.5f), Quaternion.identity, new Vector3(0.5f,0.5f,0.5f) ); // 0.5 이동과 0.5 배율을 사용하며 추가 회전은 없습니다.
        Vector3 scale = transform.lossyScale; // 부모까지 반영한 실제 세계 배율을 읽습니다.
        
		Matrix4x4 mtx = transform.localToWorldMatrix * Matrix4x4.Scale( new Vector3(1.0f/scale.x, 1.0f/scale.y, 1.0f/scale.z) ); // 물체 변환에 역배율을 곱해 배율 효과를 제거하는 원본 행렬입니다.
        //Matrix4x4 mtx = transform.localToWorldMatrix * Matrix4x4.Scale( new Vector3(custom_scale/scale.x, custom_scale/scale.y, custom_scale/scale.z) );
		
		mtx = scaleOffset * cam.projectionMatrix * cam.worldToCameraMatrix * mtx; // 물체→세계→카메라→투영→텍스처 순서의 변환을 합성합니다.
        foreach( Material mat in materials ) { // 각 공유 재질에 같은 반사 결과를 전달합니다.
            mat.SetMatrix( "_ProjMatrix", mtx ); // 합성 행렬을 재질의 셰이더 속성으로 전달합니다.
        }
        
        // Restore pixel light count
        //if( m_DisablePixelLights )
        //    QualitySettings.pixelLightCount = oldPixelLightCount;
        
        s_InsideRendering = false; // 정상 경로의 반사 작업이 끝났다고 표시합니다.
    }
    
    
    // Cleanup all the objects we possibly have created
    void OnDisable() // 컴포넌트를 껐을 때 연결과 임시 객체를 정리하는 함수입니다.
    {
        if( m_ReflectionTexture ) { // 반사 텍스처가 남아 있으면 정리합니다.
            DestroyImmediate( m_ReflectionTexture ); // 편집 모드에서도 임시 반사 텍스처를 즉시 제거합니다.
            m_ReflectionTexture = null; // 제거한 텍스처의 참조를 비웁니다.
        }
        foreach( DictionaryEntry kvp in m_ReflectionCameras ) // 대응표에 저장된 각 반사 카메라를 살펴봅니다.
            DestroyImmediate( ((Camera)kvp.Value).gameObject ); // 값을 Camera로 해석하고 그 임시 GameObject를 제거합니다.
        m_ReflectionCameras.Clear(); // 정리한 카메라 목록도 비웁니다.
    }
    
    
    private void UpdateCameraModes( Camera src, Camera dest ) // 원본 카메라 설정을 복사하는 보조 함수입니다. 현재 호출은 주석 처리되어 있습니다.
    {
        if( dest == null ) // 복사 대상 카메라가 없으면 중단합니다.
            return; // 현재 함수를 바로 끝내고 이번 프레임의 남은 계산을 하지 않습니다.
        // set camera to clear the same way as current camera
        dest.clearFlags = src.clearFlags; // 배경을 지우는 방법을 원래 카메라와 같게 합니다.
        dest.backgroundColor = src.backgroundColor; // 배경색을 복사합니다.
        if( src.clearFlags == CameraClearFlags.Skybox ) // 원래 카메라가 하늘 상자로 배경을 그리는 경우입니다.
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox; // 원래 카메라의 Skybox 컴포넌트를 찾습니다. as는 실패하면 null입니다.
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox; // 반사 카메라의 Skybox 컴포넌트를 찾습니다.
            if( !sky || !sky.material ) // 원래 하늘 상자나 그 재질이 없다면 복사할 수 없습니다.
            {
                mysky.enabled = false; // 반사 카메라의 하늘 상자를 끕니다.
            }
            else // 바로 앞 조건이 거짓인 경우의 처리를 시작합니다.
            {
                mysky.enabled = true; // 원본 하늘 상자를 사용할 수 있어 반사 카메라에서도 켭니다.
                mysky.material = sky.material; // 같은 하늘 재질을 사용합니다.
            }
        }
        // update other values to match current camera.
        // even if we are supplying custom camera&projection matrices,
        // some of values are used elsewhere (e.g. skybox uses far plane)
        dest.farClipPlane = src.farClipPlane; // 먼 거리 절단면을 복사합니다.
        dest.nearClipPlane = src.nearClipPlane; // 가까운 거리 절단면을 복사합니다.
        dest.orthographic = src.orthographic; // 직교/원근 카메라 방식을 복사합니다.
        dest.fieldOfView = src.fieldOfView; // 원근 카메라의 시야각을 복사합니다.
        dest.aspect = src.aspect; // 화면 가로세로 비율을 복사합니다.
        dest.orthographicSize = src.orthographicSize; // 직교 카메라의 표시 크기를 복사합니다.
    }
    
    // On-demand create any objects we need
    private void CreateMirrorObjects( Camera currentCamera, out Camera reflectionCamera ) // 필요할 때만 텍스처/카메라를 만들고 out으로 반사 카메라를 돌려줍니다.
    {
        reflectionCamera = null; // 아직 카메라를 찾지 않은 상태로 시작합니다.
        
        // Reflection render texture
        if( !m_ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize ) // 텍스처가 없거나 크기 설정이 바뀌면 새로 만듭니다.
        {
            if( m_ReflectionTexture ) // 기존 텍스처가 있다면 먼저 제거합니다.
                DestroyImmediate( m_ReflectionTexture ); // 편집 모드에서도 임시 반사 텍스처를 즉시 제거합니다.
            m_ReflectionTexture = new RenderTexture( m_TextureSize, m_TextureSize, 16 ); // 정사각 컬러 텍스처와 16비트 깊이 버퍼를 요청합니다.
            m_ReflectionTexture.name = "__MirrorReflection" + GetInstanceID(); // 물체 고유 번호를 붙여 임시 텍스처를 구분합니다.
            m_ReflectionTexture.isPowerOfTwo = true; // 원본의 2의 거듭제곱 크기 플래그 설정을 유지합니다.
            m_ReflectionTexture.hideFlags = HideFlags.DontSave; // 임시 텍스처를 Scene 파일에 저장하지 않게 합니다.
            m_OldReflectionTextureSize = m_TextureSize; // 현재 만든 크기를 기록해 다음 호출과 비교합니다.
        }
        
        // Camera for reflection
        reflectionCamera = m_ReflectionCameras[currentCamera] as Camera; // 원래 카메라에 대응하는 반사 카메라를 표에서 찾습니다.
        if( !reflectionCamera ) // catch both not-in-dictionary and in-dictionary-but-deleted-GO | 표에 없거나 이미 제거된 카메라이면 새로 만듭니다.
        {
            GameObject go = new GameObject( "Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox) ); // 식별 이름을 붙인 임시 물체에 Camera와 Skybox를 함께 만듭니다.
            reflectionCamera = go.GetComponent<Camera>(); // 방금 만든 물체에서 반사 카메라를 가져옵니다.
            reflectionCamera.enabled = false; // 자동 렌더링을 끄고 위 Render 호출로만 그립니다.
            reflectionCamera.transform.position = transform.position; // 임시 카메라의 초기 위치를 반사 표면 위치에 맞춥니다.
            reflectionCamera.transform.rotation = transform.rotation; // 초기 회전도 반사 표면에 맞춥니다.
            reflectionCamera.gameObject.AddComponent<FlareLayer>(); // 원본의 레거시 플레어 레이어 컴포넌트 추가를 보존합니다.
            go.hideFlags = HideFlags.HideAndDontSave; // 임시 물체를 일반 Hierarchy 표시와 Scene 저장에서 제외합니다.
            m_ReflectionCameras[currentCamera] = reflectionCamera; // 다음에 재사용하도록 대응표에 저장합니다.
        }        
    }
    
    // Extended sign: returns -1, 0 or 1 based on sign of a
    private static float sgn(float a) // 수의 부호를 -1/0/+1로 돌려주는 보조 함수입니다.
    {
        if (a > 0.0f) return 1.0f; // 양수이면 +1을 돌려줍니다.
        if (a < 0.0f) return -1.0f; // 음수이면 -1을 돌려줍니다.
        return 0.0f; // 0이면 그대로 0을 돌려줍니다.
    }
    
    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign) // 세계 위치/법선의 평면을 카메라 기준 평면으로 바꿉니다.
    {
        Vector3 offsetPos = pos + normal * m_ClipPlaneOffset; // 법선 방향으로 절단면 오프셋만큼 이동한 위치입니다.
        Matrix4x4 m = cam.worldToCameraMatrix; // 세계→카메라 좌표 변환 행렬입니다.
        Vector3 cpos = m.MultiplyPoint( offsetPos ); // 평면 위 점을 카메라 기준 위치로 바꿉니다.
        Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign; // 법선을 카메라 방향으로 변환하고 길이와 방향 부호를 맞춥니다.
        return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) ); // 카메라 기준 평면식의 법선과 d를 반환합니다.
    }
    
    // Adjusts the given projection matrix so that near plane is the given clipPlane
    // clipPlane is given in camera space. See article in Game Programming Gems 5 and
    // http://aras-p.info/texts/obliqueortho.html
    private static void CalculateObliqueMatrix (ref Matrix4x4 projection, Vector4 clipPlane) // 기울어진 가까운 절단면에 맞게 전달받은 투영 행렬을 직접 수정합니다.
    {
        Vector4 q = projection.inverse * new Vector4( // 클립 공간의 모서리 점을 역투영해 카메라 공간으로 바꿉니다.
            sgn(clipPlane.x), // 절단면 x 성분의 부호로 모서리의 x 방향을 고릅니다.
            sgn(clipPlane.y), // 절단면 y 성분의 부호로 모서리의 y 방향을 고릅니다.
            1.0f, // 모서리의 z 성분에 1을 넣습니다.
            1.0f // 동차 좌표 w에 1을 넣습니다.
        ); // 위 Vector4 구성과 역투영 계산을 마칩니다.
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot (clipPlane, q))); // 절단 평면과 모서리의 내적을 이용해 투영에 맞는 배율을 계산합니다.
        // third row = clip plane - fourth row
        projection[2] = c.x - projection[3]; // 열 우선 저장 행렬의 세 번째 행 x 원소를 절단면에 맞춥니다.
        projection[6] = c.y - projection[7]; // 세 번째 행 y 원소를 같은 방식으로 수정합니다.
        projection[10] = c.z - projection[11]; // 세 번째 행 z 원소를 수정합니다.
        projection[14] = c.w - projection[15]; // 세 번째 행 w 원소를 수정합니다.
    }

    // Calculates reflection matrix around the given plane
    private static void CalculateReflectionMatrix (ref Matrix4x4 reflectionMat, Vector4 plane) // 평면 n·x+d=0에 대해 점을 대칭 이동할 4×4 행렬을 채웁니다.
    {
        reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]); // 반사행렬 0행 0열에 I−2nnᵀ의 해당 값을 넣어 x 성분이 x에 미치는 영향을 정합니다.
        reflectionMat.m01 = (   - 2F*plane[0]*plane[1]); // 반사행렬 0행 1열에 I−2nnᵀ의 해당 값을 넣어 y 성분이 x에 미치는 영향을 정합니다.
        reflectionMat.m02 = (   - 2F*plane[0]*plane[2]); // 반사행렬 0행 2열에 I−2nnᵀ의 해당 값을 넣어 z 성분이 x에 미치는 영향을 정합니다.
        reflectionMat.m03 = (   - 2F*plane[3]*plane[0]); // 평면의 d 오프셋이 x 위치에 만드는 -2d·n 성분을 넣습니다.

        reflectionMat.m10 = (   - 2F*plane[1]*plane[0]); // 반사행렬 1행 0열에 I−2nnᵀ의 해당 값을 넣어 x 성분이 y에 미치는 영향을 정합니다.
        reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]); // 반사행렬 1행 1열에 I−2nnᵀ의 해당 값을 넣어 y 성분이 y에 미치는 영향을 정합니다.
        reflectionMat.m12 = (   - 2F*plane[1]*plane[2]); // 반사행렬 1행 2열에 I−2nnᵀ의 해당 값을 넣어 z 성분이 y에 미치는 영향을 정합니다.
        reflectionMat.m13 = (   - 2F*plane[3]*plane[1]); // 평면의 d 오프셋이 y 위치에 만드는 -2d·n 성분을 넣습니다.
    
        reflectionMat.m20 = (   - 2F*plane[2]*plane[0]); // 반사행렬 2행 0열에 I−2nnᵀ의 해당 값을 넣어 x 성분이 z에 미치는 영향을 정합니다.
        reflectionMat.m21 = (   - 2F*plane[2]*plane[1]); // 반사행렬 2행 1열에 I−2nnᵀ의 해당 값을 넣어 y 성분이 z에 미치는 영향을 정합니다.
        reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]); // 반사행렬 2행 2열에 I−2nnᵀ의 해당 값을 넣어 z 성분이 z에 미치는 영향을 정합니다.
        reflectionMat.m23 = (   - 2F*plane[3]*plane[2]); // 평면의 d 오프셋이 z 위치에 만드는 -2d·n 성분을 넣습니다.

        reflectionMat.m30 = 0F; // 동차 좌표의 마지막 행을 0로 두어 일반 평면 반사 변환을 완성합니다.
        reflectionMat.m31 = 0F; // 동차 좌표의 마지막 행을 0로 두어 일반 평면 반사 변환을 완성합니다.
        reflectionMat.m32 = 0F; // 동차 좌표의 마지막 행을 0로 두어 일반 평면 반사 변환을 완성합니다.
        reflectionMat.m33 = 1F; // 동차 좌표의 마지막 행을 1로 두어 일반 평면 반사 변환을 완성합니다.
    }
}
 
