Shader"Custom/ColorSwapShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TargetColor ("Target Color", Color) = (1, 0, 0, 1) // Red
        _NewColor ("New Color", Color) = (0, 1, 0, 1) // Green
        _Threshold ("Threshold", Range(0, 1)) = 0.1 // Adjust for flexibility
    }SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

sampler2D _MainTex;
float4 _TargetColor;
float4 _NewColor;
float _Threshold;

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f vert(float4 vertex : POSITION)
{
    v2f o;
    o.pos = UnityObjectToClipPos(vertex);
    //o.uv = vertex.uv; // Pass UV coordinates to fragment shader
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    fixed4 col = tex2D(_MainTex, i.uv);
    return col;

                // Calculate color distance (you can use different metrics)
    float dist = distance(col, _TargetColor);

                // If the distance is below the threshold, swap the color
    if (dist < _Threshold)
    {
        col = _NewColor;
    }

    return col;
}

            ENDCG
        }
    }
}
