﻿Shader "PlanetaryTerrain/PlanetFadeShaderBump" {
	Properties {
		
		_Tex1 ("Texture 0 (Beach)", 2D) = "white" {}
		_Nor1 ("Normal", 2D) = "bump" {}
		_Glossiness1 ("Smoothness", Range(0,1)) = 0.5
		_Metallic1 ("Metallic", Range(0,1)) = 0.0
		_TexScale1 ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex2 ("Texture 1", 2D) = "white" {}
		_Nor2 ("Normal", 2D) = "bump" {}
		_Glossiness2 ("Smoothness", Range(0,1)) = 0.5
		_Metallic2 ("Metallic", Range(0,1)) = 0.0
		_TexScale2 ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex3 ("Texture 2", 2D) = "white" {}
		_Nor3 ("Normal", 2D) = "bump" {}
		_Glossiness3 ("Smoothness", Range(0,1)) = 0.5
		_Metallic3 ("Metallic", Range(0,1)) = 0.0
		_TexScale3 ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex4 ("Texture 3", 2D) = "white" {}
		_Nor4 ("Normal", 2D) = "bump" {}
		_Glossiness4 ("Smoothness", Range(0,1)) = 0.5
		_Metallic4 ("Metallic", Range(0,1)) = 0.0
		_TexScale4 ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex5 ("Texture 4", 2D) = "white" {}
		_Nor5 ("Normal", 2D) = "bump" {}
		_Glossiness5 ("Smoothness", Range(0,1)) = 0.5
		_Metallic5 ("Metallic", Range(0,1)) = 0.0
		_TexScale5 ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex6 ("Texture 5 (Mountains)", 2D) = "white" {}
		_Nor6 ("Normal", 2D) = "bump" {}
		_Glossiness6 ("Smoothness", Range(0,1)) = 0.5
		_Metallic6 ("Metallic", Range(0,1)) = 0.0
		_TexScale6 ("Texture Scale", Float) = 1

		[Space(50)]
		[Header(Textures faded to when far away)]
		[Space]

		_Tex1Fade ("Texture 0 Fade", 2D) = "white" {}
		_Nor1Fade ("Normal", 2D) = "bump" {}
		_TexScale1Fade ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex2Fade ("Texture 1 Fade", 2D) = "white" {}
		_Nor2Fade ("Normal", 2D) = "bump" {}
		_TexScale2Fade ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex3Fade ("Texture 2 Fade", 2D) = "white" {}
		_Nor3Fade ("Normal", 2D) = "bump" {}
		_TexScale3Fade ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex4Fade ("Texture 3 Fade", 2D) = "white" {}
		_Nor4Fade ("Normal", 2D) = "bump" {}
		_TexScale4Fade ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex5Fade ("Texture 4 Fade", 2D) = "white" {}
		_Nor5Fade ("Normal", 2D) = "bump" {}
		_TexScale5Fade ("Texture Scale", Float) = 1
		[Space(20)]
		_Tex6Fade ("Texture 5 Fade", 2D) = "white" {}
		_Nor6Fade ("Normal", 2D) = "bump" {}
		_TexScale6Fade ("Texture Scale", Float) = 1

		[Space(50)]

		_FadeRangeMin ("Fade Start", Float) = 1
		_FadeRangeMax ("Fade End", Float) = 1

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		#define USING_6_TEXTURES
		#define f

		#pragma surface surf Standard vertex:vert
		#pragma target 3.0

		
		UNITY_DECLARE_TEX2D(_Tex1);
		UNITY_DECLARE_TEX2D(_Tex2);
		UNITY_DECLARE_TEX2D(_Tex3);
		UNITY_DECLARE_TEX2D(_Tex4);
		UNITY_DECLARE_TEX2D(_Tex5);
		UNITY_DECLARE_TEX2D(_Tex6);

		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor1);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor2);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor3);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor4);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor5);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor6);

		UNITY_DECLARE_TEX2D(_Tex1Fade);
		UNITY_DECLARE_TEX2D(_Tex2Fade);
		UNITY_DECLARE_TEX2D(_Tex3Fade);
		UNITY_DECLARE_TEX2D(_Tex4Fade);
		UNITY_DECLARE_TEX2D(_Tex5Fade);
		UNITY_DECLARE_TEX2D(_Tex6Fade);

		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor1Fade);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor2Fade);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor3Fade);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor4Fade);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor5Fade);
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Nor6Fade);

		float _TexScale1;
		float _TexScale2;
		float _TexScale3;
		float _TexScale4;
		float _TexScale5;
		float _TexScale6;

		float _TexScale1Fade;
		float _TexScale2Fade;
		float _TexScale3Fade;
		float _TexScale4Fade;
		float _TexScale5Fade;
		float _TexScale6Fade;

		float _FadeRangeMin;
		float _FadeRangeMax;


		struct Input {

			float2 uv_Tex1;
			float2 uv4_Tex2;
			float4 color: COLOR;
			float4 screenPos;
			half fade;
		};

		half _Glossiness1;
		half _Metallic1;

		half _Glossiness2;
		half _Metallic2;

		half _Glossiness3;
		half _Metallic3;

		half _Glossiness4;
		half _Metallic4;

		half _Glossiness5;
		half _Metallic5;

		half _Glossiness6;
		half _Metallic6;
		

		void vert (inout appdata_full v, out Input data) {
			
      		UNITY_INITIALIZE_OUTPUT(Input,data);
      		float pos = length(UnityObjectToViewPos(v.vertex).xyz);
      		float diff = _FadeRangeMax - _FadeRangeMin;
      		float invDiff = -1.0 / diff;
      		data.fade = clamp ((_FadeRangeMax - pos) * invDiff, 0.0, 1.0);
    	}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			
			fixed4 c1 = lerp(UNITY_SAMPLE_TEX2D(_Tex1, IN.uv_Tex1 * _TexScale1), UNITY_SAMPLE_TEX2D(_Tex1Fade, IN.uv_Tex1 * _TexScale1Fade), IN.fade);
			fixed4 c2 = lerp(UNITY_SAMPLE_TEX2D(_Tex2, IN.uv_Tex1 * _TexScale2), UNITY_SAMPLE_TEX2D(_Tex2Fade, IN.uv_Tex1 * _TexScale2Fade), IN.fade);
			fixed4 c3 = lerp(UNITY_SAMPLE_TEX2D(_Tex3, IN.uv_Tex1 * _TexScale3), UNITY_SAMPLE_TEX2D(_Tex3Fade, IN.uv_Tex1 * _TexScale3Fade), IN.fade);
			fixed4 c4 = lerp(UNITY_SAMPLE_TEX2D(_Tex4, IN.uv_Tex1 * _TexScale4), UNITY_SAMPLE_TEX2D(_Tex4Fade, IN.uv_Tex1 * _TexScale4Fade), IN.fade);
			fixed4 c5 = lerp(UNITY_SAMPLE_TEX2D(_Tex5, IN.uv_Tex1 * _TexScale5), UNITY_SAMPLE_TEX2D(_Tex5Fade, IN.uv_Tex1 * _TexScale5Fade), IN.fade);
			fixed4 c6 = lerp(UNITY_SAMPLE_TEX2D(_Tex6, IN.uv_Tex1 * _TexScale6), UNITY_SAMPLE_TEX2D(_Tex6Fade, IN.uv_Tex1 * _TexScale6Fade), IN.fade);

			o.Albedo = IN.color.r * c1 + IN.color.g * c2 + IN.color.b * c3 + IN.color.a * c4 + IN.uv4_Tex2.x * c5 + IN.uv4_Tex2.y * c6;

			o.Metallic = IN.color.r * _Metallic1 + IN.color.g * _Metallic2 + IN.color.b * _Metallic3 + IN.color.a * _Metallic4 + IN.uv4_Tex2.x * _Metallic5 + IN.uv4_Tex2.y * _Metallic6;
			o.Smoothness = IN.color.r * _Glossiness1 + IN.color.g * _Glossiness2 + IN.color.b * _Glossiness3 + IN.color.a * _Glossiness4 + IN.uv4_Tex2.x * _Glossiness5 + IN.uv4_Tex2.y * _Glossiness6;
			
			fixed3 n1 = lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor1, _Tex1, IN.uv_Tex1 * _TexScale1)), UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor1Fade, _Tex1Fade, IN.uv_Tex1 * _TexScale1Fade)), IN.fade);
			fixed3 n2 = lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor2, _Tex2, IN.uv_Tex1 * _TexScale2)), UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor2Fade, _Tex2Fade, IN.uv_Tex1 * _TexScale2Fade)), IN.fade);
			fixed3 n3 = lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor3, _Tex3, IN.uv_Tex1 * _TexScale3)), UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor3Fade, _Tex3Fade, IN.uv_Tex1 * _TexScale3Fade)), IN.fade);
			fixed3 n4 = lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor4, _Tex4, IN.uv_Tex1 * _TexScale4)), UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor4Fade, _Tex4Fade, IN.uv_Tex1 * _TexScale4Fade)), IN.fade);
			fixed3 n5 = lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor5, _Tex5, IN.uv_Tex1 * _TexScale5)), UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor5Fade, _Tex5Fade, IN.uv_Tex1 * _TexScale5Fade)), IN.fade);
			fixed3 n6 = lerp(UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor6, _Tex6, IN.uv_Tex1 * _TexScale6)), UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_Nor6Fade, _Tex6Fade, IN.uv_Tex1 * _TexScale6Fade)), IN.fade);
		
			o.Normal = IN.color.r * n1 + IN.color.g * n2 + IN.color.b * n3 + IN.color.a * n4 + IN.uv4_Tex2.x * n5 + IN.uv4_Tex2.y * n6;

		}
		ENDCG
	}
	FallBack "Diffuse"
}
