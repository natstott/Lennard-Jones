Shader "Molecule" { // StructuredBuffer + SurfaceShader

   Properties {
		_Color ("Color", Color) = (1,1,0,1)

	}

   SubShader {
 
		CGPROGRAM
        #include "UnityCG.cginc"


        struct appdata_custom {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 tangent : TANGENT;
 
            uint id : SV_VertexID;
            uint inst : SV_InstanceID;

            UNITY_VERTEX_INPUT_INSTANCE_ID
         };
		struct Input {
			float3 worldPos;
		};

		fixed4 _Color;
 
        #pragma multi_compile __ FRAME_INTERPOLATION
        #pragma surface surf Standard vertex:vert nolightmap
       #pragma instancing_options procedural:setup

        float3 _BoidPosition;
        float _BoidSize;


         #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct Boid
            {
                float3 position;
                float3 direction;
                float3 velocity;
                float3 acceleration;
                float boidsize;
                float boidmass;

            };

            StructuredBuffer<Boid> boidBuffer; 

         #endif
  
        void vert(inout appdata_custom v)
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

                v.vertex = _BoidSize *v.vertex;
                v.vertex.xyz += _BoidPosition;
            #endif
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                _BoidPosition = boidBuffer[unity_InstanceID].position;
                _BoidSize= boidBuffer[unity_InstanceID].boidsize;
             //   _LookAtMatrix = look_at_matrix(_BoidPosition, _BoidPosition + (boidBuffer[unity_InstanceID].direction * -1), float3(0.0, 1.0, 0.0));

  
            #endif
        }
 
         void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
         }
 
         ENDCG
   }
}