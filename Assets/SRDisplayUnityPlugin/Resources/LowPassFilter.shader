Shader "uLowPassFilter/LowPassFilter"
{
  Properties
  {
    _MainTex("Texture", 2D) = "white" {}
  }

    SubShader
  {
    Cull Off ZWrite Off ZTest Always

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

      v2f vert(appdata v)
      {
          v2f o;
          o.vertex = UnityObjectToClipPos(v.vertex);
          o.uv = v.uv;
          return o;
      }

      sampler2D _MainTex;
      uniform float _TexWidth;
      int _is9tap; //0(false) or 1(true)

      fixed4 frag(v2f i) : SV_Target
      {

        if (_is9tap == 1)
        {
          float4 l4 = tex2D(_MainTex, float2(i.uv.x - (4 / _TexWidth), i.uv.y)) * (-8.2844764696373547e-18);
          float4 l3 = tex2D(_MainTex, float2(i.uv.x - (3 / _TexWidth), i.uv.y)) * (-6.9433820583776326e-02);
          float4 l2 = tex2D(_MainTex, float2(i.uv.x - (2 / _TexWidth), i.uv.y)) * (1.6568952939274709e-17);
          float4 l1 = tex2D(_MainTex, float2(i.uv.x - (1 / _TexWidth), i.uv.y)) * (3.1245219262699347e-01);
          float4 c = tex2D(_MainTex, float2(i.uv.x, i.uv.y)) * (5.1396325591356573e-01);
          float4 r1 = tex2D(_MainTex, float2(i.uv.x + (1 / _TexWidth), i.uv.y)) * (3.1245219262699347e-01);
          float4 r2 = tex2D(_MainTex, float2(i.uv.x + (2 / _TexWidth), i.uv.y)) * (1.6568952939274709e-17);
          float4 r3 = tex2D(_MainTex, float2(i.uv.x + (3 / _TexWidth), i.uv.y)) * (-6.9433820583776326e-02);
          float4 r4 = tex2D(_MainTex, float2(i.uv.x + (4 / _TexWidth), i.uv.y)) * (-8.2844764696373547e-18);

          return l4 + l3 + l2 + l1 + c + r1 + r2 + r3 + r4;
        }
        else
        {
          float4 l4 = tex2D(_MainTex, float2(i.uv.x - (4 / _TexWidth), i.uv.y)) * (0);
          float4 l3 = tex2D(_MainTex, float2(i.uv.x - (3 / _TexWidth), i.uv.y)) * (-3.5322906942948783e-02);
          float4 l2 = tex2D(_MainTex, float2(i.uv.x - (2 / _TexWidth), i.uv.y)) * (-6.9232897608179605e-02);
          float4 l1 = tex2D(_MainTex, float2(i.uv.x - (1 / _TexWidth), i.uv.y)) * (+1.4078332945667019e-01);
          float4 c = tex2D(_MainTex, float2(i.uv.x, i.uv.y)) * (+4.6377247509445824e-01);
          float4 r1 = tex2D(_MainTex, float2(i.uv.x + (1 / _TexWidth), i.uv.y)) * (+4.6377247509445824e-01);
          float4 r2 = tex2D(_MainTex, float2(i.uv.x + (2 / _TexWidth), i.uv.y)) * (+1.4078332945667019e-01);
          float4 r3 = tex2D(_MainTex, float2(i.uv.x + (3 / _TexWidth), i.uv.y)) * (-6.9232897608179605e-02);
          float4 r4 = tex2D(_MainTex, float2(i.uv.x + (4 / _TexWidth), i.uv.y)) * (-3.5322906942948783e-02);

          return l4 + l3 + l2 + l1 + c + r1 + r2 + r3 + r4;
        }

      }
      ENDCG
    }
  }
}