Shader "Custom/Invisible"
{
    SubShader
    {
        Tags {"Queue" = "Transparent" "VRCFallback" = "UnlitTransparent" }
        Lighting Off
		ZWrite Off
        Pass
        {
            ColorMask 0
        }
    }
}

