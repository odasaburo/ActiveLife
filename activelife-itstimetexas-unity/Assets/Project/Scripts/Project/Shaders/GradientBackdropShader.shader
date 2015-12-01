Shader "Custom/GradientBackdropShader" {
	Properties {
		_StartColor ("Start Color", Color) = (1.0,1.0,1.0,1.0)
		_EndColor ("End Color", Color) = (1.0,1.0,1.0,1.0)
		_Scale ("Scale", Float) = 1
	}
	SubShader {
		BindChannels {
			Bind "vertex", vertex
			Bind "color", color
		}
		
		Pass {}
			
	}
}
