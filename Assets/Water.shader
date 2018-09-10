Shader "Custom/Water" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		
		// FlowMap doesn't need a separate UV tiling and offset so we give it the NoScaleOffset attribute. 
		// The default is that there is no flow, which corresponds to a black texture.
		// Additionally we store noise in the A channel.
		[NoScaleOffset] _FlowMap("Flow (RG, A noise)", 2D) = "black" {}
		
		// Besides always offsetting the UV of A and B by half a unit, it is also possible to offset the UV per phase.
		// That will cause the animation to change over time, so it takes longer before it loops back to the exact same state.
		_UJump("U jump per phase", Range(-0.25, 0.25)) = 0.25
		_VJump("V jump per phase", Range(-0.25, 0.25)) = 0.25

		// We cannot rely on the main tiling and offset of the surface shader, because that also affects the flow map.
		_Tiling("Tiling", Float) = 1

		_WaterFogColor("Water Fog Color", Color) = (0, 0, 0, 0)
		_WaterFogDensity("Water Fog Density", Range(0, 2)) = 0.1
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		// Changing Queue to Transparent moves the shader to the transparent rendering queue, 
		// now being drawn after all opaque geometry has been rendered.
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 200

		// GrabPass adds an extra step in the rendering pipeline. Just before the water gets drawn, 
		// what's rendered up to this points gets copied to a grab-pass texture. 
		// This happens each time something that uses our water shader gets rendered. 
		// We can reduce this to a single extra draw by giving the grabbed texture an explicit name.
		GrabPass { "_WaterBackground" }

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows finalcolor:ResetAlpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "Flow.cginc"

		sampler2D _MainTex, _CameraDepthTexture, _WaterBackground, _FlowMap;
		float4 _CameraDepthTexture_TexelSize;
		float3 _WaterFogColor;
		float _WaterFogDensity, _UJump, _VJump, _Tiling;

		struct Input {
			float2 uv_MainTex;
			// To sample the depth texture, we need the screen - space coordinates of the current fragment. 
			// We can retrieve those by adding a float4 screenPos field to our surface shader's input structure.
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void ResetAlpha(Input IN, SurfaceOutputStandard o, inout fixed4 color) {
			color.a = 1;
		}

		float3 ColorBelowWater(float4 screenPos) {
			// We have to divide XY by W to get the final depth texture coordinates.
			float2 uv = screenPos.xy / screenPos.w;

			// To guard against upside-down results check whether the texel size of the camera depth texture is negative in the V dimension. 
			// If so, invert the V coordinate. We only have to check this on platforms that work with top-to-bottom coordinates. 
			// In those cases, UNITY_UV_STARTS_AT_TOP is defined as 1.
			#if UNITY_UV_STARTS_AT_TOP
				if (_CameraDepthTexture_TexelSize.y < 0) {
					uv.y = 1 - uv.y;
				}
			#endif

			// Now we can sample the background depth via the SAMPLE_DEPTH_TEXTURE macro, 
			// and then convert the raw value to the linear depth via the LinearEyeDepth function.
			float backgroundDepth =	LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));

			// We need to know the distance between the water and the screen. 
			// We find it by taking the Z component of screenPos which is the interpolated clip space depth 
			// and converting it to linear depth via the UNITY_Z_0_FAR_FROM_CLIPSPACE macro.
			float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(screenPos.z);

			// The underwater depth is found by subtracting the surface depth from the background depth.
			float depthDifference = backgroundDepth - surfaceDepth;

			// Add a variable for this texture, then sample it using the same UV coordinates that we used to sample the depth texture.
			float3 backgroundColor = tex2D(_WaterBackground, uv).rgb;

			// We'll use simple exponential fog.
			float fogFactor = exp2(-_WaterFogDensity * depthDifference);
			return lerp(_WaterFogColor, backgroundColor, fogFactor);
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float2 flowVector = tex2D(_FlowMap, IN.uv_MainTex).rg 
				* 2 - 1; // the vector is encoded the same way as in a normal map. We have to manually decode it

			// water albedo color - 4E83A9

			float noise = tex2D(_FlowMap, IN.uv_MainTex).a;
			float time = _Time.y + noise; // the current time in Unity is available via _Time.y
			float2 jump = float2(_UJump, _VJump);
			float3 uvw = FlowUVW(IN.uv_MainTex, flowVector, time, jump, _Tiling);
			
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D(_MainTex, uvw.xy) 
				* uvw.z 
				* _Color;

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;

			// water transparency
			o.Albedo = (c.rgb + ColorBelowWater(IN.screenPos)) / 3;
			
			// We must add the underwater color to the surface lighting, which we can do by using it as the emissive color. 
			// But we must modulate this by the water's albedo. The more opaque it is, the less we see of the background.
			o.Emission = ColorBelowWater(IN.screenPos) * (1 - c.a);
		}
		ENDCG
	}

	// To eliminate the shadows of the main directional light, remove the fallback.
	// FallBack "Diffuse"
}
