Shader "HCI/HandPointer" { Properties { _Color ("Color", Color) = (0.6,1,0.75,1) _UseVertexColor ("Use Vertex Color", Float) = 1 } SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" } Pass { ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; UNITY_VERTEX_OUTPUT_STEREO };
CBUFFER_START(UnityPerMaterial) half4 _Color; float _UseVertexColor; CBUFFER_END
Varyings vert(Attributes v) { Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.color=v.color; return o; }
half4 frag(Varyings i) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return _Color * lerp(half4(1,1,1,1), i.color, _UseVertexColor); }
ENDHLSL } } }
