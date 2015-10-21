﻿//Shows the grayscale of the depth from the camera.
 
Shader "Custom/DepthShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

        Pass
        {
 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            uniform sampler2D _CameraDepthTexture; //the depth texture
			uniform sampler2D _CameraDepthNormalsTexture; //the depth texture

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 projPos : TEXCOORD1; //Screen position of pos
            };
 
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                o.projPos = ComputeScreenPos(o.pos);
 
                return o;
            }
 
            half4 frag(v2f i) : COLOR
            {

                //Grab the depth value from the depth texture
                //Linear01Depth restricts this value to [0, 1]
				float depth = Linear01Depth (tex2D(_CameraDepthTexture, i.projPos).r);
				//float depth = tex2D(_CameraDepthTexture, i.projPos).r;

				half4 c;
                c.r = depth;
				c.g = depth;
				c.b = depth;
				c.a = 1;
 
                return c;
            }
 
            ENDCG
        }
    }
    FallBack "VertexLit"
}