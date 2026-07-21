Shader "Custom/LeftAndRightToSideBySide" {
  Properties {
    _MainTex("Left", 2D) = "white" {}
    _RightTex("Right", 2D) = "white" {}
  }

  CGINCLUDE

#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

  struct vert_to_frag {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
  };

  sampler2D _MainTex;
  sampler2D _RightTex;

  vert_to_frag vert(appdata v) {
	vert_to_frag output;
	output.uv = v.uv;
	output.vertex = UnityObjectToClipPos(v.vertex);
    return output;
  }

  fixed4 frag_left_and_right_to_side_by_side(vert_to_frag input) : SV_Target {
    if (input.uv.x < 0.5) {
      fixed2 uv = fixed2(input.uv.x * 2, input.uv.y);
      return tex2D(_MainTex, uv);

    } else {
      fixed2 uv = fixed2((input.uv.x - 0.5) * 2, input.uv.y);
      return tex2D(_RightTex, uv);
    }
  }

  ENDCG

  SubShader {
    Blend Off ZTest Always ZWrite Off Cull Off Lighting Off

	Pass {
      CGPROGRAM
#pragma vertex vert
#pragma fragment frag_left_and_right_to_side_by_side
      ENDCG
    }
  }
}
