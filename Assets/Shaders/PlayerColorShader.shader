Shader"Unlit/PlayerColorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _SkinBaseColor ("Skin Base Color", float) = 1
        _SkinNewColor ("Skin New Color", Color) = (1, 1, 1, 1)
        _OutlineBaseColor ("Outline Base Color", float) = 1
        _OutlineNewColor ("Outline New Color", Color) = (1, 1, 1, 1)
        _StreakBaseColor ("Streak Base Color", float) = 1
        _StreakNewColor ("Streak New Color", Color) = (1, 1, 1, 1)

        _TargetColor ("Target Color", Color) = (1, 0, 0, 1) // Red
        _NewColor ("New Color", Color) = (0, 1, 0, 1) // Green
        _Threshold ("Threshold", Range(0, 1)) = 0.1 // Adjust for flexibility
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            float _Threshold;

            float4 _NewColor;
            float _SkinBaseColor;
            float4 _SkinNewColor;
            float _OutlineBaseColor;
            float4 _OutlineNewColor;
            float _StreakBaseColor;
            float4 _StreakNewColor;

            fixed4 frag (v2f i) : SV_Target
{
                // sample the texture
    fixed4 col = tex2D(_MainTex, i.uv);
    
                //if (col.r == _SkinBaseColor)
                //{
                //    col = _SkinNewColor;
                //}
                //if (col.r == _OutlineBaseColor)
                //{
                //    col = _OutlineNewColor;
                //}
                //if (col.r == _StreakBaseColor)
                //{
                //    col = _StreakNewColor;
                //}
                //if (col.r == _SkinBaseColor)
                //{
                //    col = _SkinNewColor;
                //}
    
                if (_SkinBaseColor == 0)
                {
                    col = (0, 1, 1, 1);
                }
                if (_SkinBaseColor == 2)
                {
                    col = (0, 1, 1, 1);
                }
                if (_SkinBaseColor == 2)
                {
                    col = (1, 0, 1, 1);
                }
                if (_SkinBaseColor == 3)
                {
                    col = (1, 1, 0, 1);
                }

        return col;
}
            ENDCG
        }
    }
}
