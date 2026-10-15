Shader "ForestExperience/Touch Foliage" {
 Properties {
  _BaseMap("Albedo",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
  _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=.65
  _Cutoff("Alpha cutoff",Range(0,1))=.4 _Smoothness("Smoothness",Range(0,1))=.15
  _Metallic("Metallic",Range(0,1))=0 _WindStrength("Wind strength",Float)=.025 _TouchStrength("Touch strength",Float)=1
  _Cull("Cull",Float)=0 _Surface("Surface",Float)=0 _Blend("Blend",Float)=0
  _SrcBlend("Src",Float)=1 _DstBlend("Dst",Float)=0 _ZWrite("ZWrite",Float)=1
  _SpecColor("Specular",Color)=(.1,.1,.1,1) _EmissionColor("Emission",Color)=(0,0,0,0)
  _EmissionMap("Emission map",2D)="white"{} _OcclusionMap("Occlusion",2D)="white"{} _OcclusionStrength("AO",Float)=1
  _MetallicGlossMap("Metallic",2D)="white"{} _SpecGlossMap("Specular",2D)="white"{}
  _DetailAlbedoMapScale("Detail",Float)=1 _DetailAlbedoMap("Detail map",2D)="linearGrey"{} _DetailNormalMap("Detail normal",2D)="bump"{} _DetailNormalMapScale("Detail normal scale",Float)=1 _DetailMask("Detail mask",2D)="white"{}
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Pass {
   Name "ForwardLit" Tags {"LightMode"="UniversalForwardOnly"}
   Cull Off ZWrite On
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex ForestVertex
   #pragma fragment LitPassFragment
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #define _ALPHATEST_ON 1
   #define _NORMALMAP 1
   #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
   float _WindStrength,_TouchStrength;float4 _ForestLeftHand,_ForestRightHand;
   Varyings ForestVertex(Attributes v){
    UNITY_SETUP_INSTANCE_ID(v);
    float3 w=TransformObjectToWorld(v.positionOS.xyz);
    float height=max(0,v.positionOS.y);float sway=sin(_Time.y*1.15+w.x*.9+w.z*.6)*_WindStrength*min(height,3);
    float3 delta=float3(sway,0,sway*.65);
    float4 hs[2]={_ForestLeftHand,_ForestRightHand};
    for(int i=0;i<2;i++){float3 away=w-hs[i].xyz;float influence=hs[i].w>0?saturate(1-length(away)/.42):0;float3 dir=normalize(away+float3(.001,0,.001));delta+=float3(dir.x,-.35,dir.z)*influence*min(height,.55)*_TouchStrength;}
    v.positionOS.xyz+=TransformWorldToObjectDir(delta,false);
    return LitPassVertex(v);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
 Fallback "Universal Render Pipeline/Lit"
}
