Shader "Gregs/Skybox/BlackHoleInSpace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.2, 1)

        _BlackHoleCenter ("Black Hole Center", Vector) = (0.5,0.5,0,0)
        _EventHorizonSize ("Event Horizon Size", Float) = 0.45
        _DistortionStrength ("Distortion Strength", Float) = 5.0
        _LensingZoneSize  ("Distortion Distance", Float) = 0.7
        _StarSizeIncreaseFactor ("Star Size Increase Factor", Float) = 9.0 // Factor by which star size increases within the lensing zone
        _SkewingFactor ("Skewing Factor",  Range(0, 1)) = 0.261// Factor by which UV coordinates are skewed within the lensing zone
        _ColorShiftIntensity ("Color Shift Intensity", Range(0, 1)) = 0.0
        _ShiftColor ("Shift Color", Color) = (1, 0, 0, 1) // Example: Shift towards red
       
        _RingColor ("Ring Color", Color) = (1, 1, 1, 1)
        _RingThickness ("Ring Thickness", Float) = 0.00
        _RingSize ("Ring Size", Range(0, 1)) = 0
        _RingColorBlendFactor ("Ring Color Blend Factor", Range(0, 1)) = 1
        _RingColorShiftIntensity ("Ring Color Shift Intensity", Range(0, 1)) = 1
        _RingBackgroundColor ("Ring Background Color", Color) = (0, 0, 0, 1)
        
        _StarsIntensity ("Stars Intensity", Float) = 2.7
        _NumStars ("Number of Stars", Range(0, 100)) = 100
        _StarSizeVariation ("Star Size Variation", Range(0, 1)) = 0.5
        _StarVisibilityThreshold ("Star Visibility Threshold", Range(0, 1)) = 0.8
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.9
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        
        _StarSize ("Star Size", Range(0, 100)) = 6.5
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
            float2 _BlackHoleCenter;
            float _EventHorizonSize;
            float _LensingZoneSize;
            float _StarSizeIncreaseFactor;
            float _SkewingFactor;
            float _DistortionStrength;
           
            float4 _RingColor;
            float _RingSize;
            float _RingColorBlendFactor;
            float _RingColorShiftIntensity;
            float4 _RingBackgroundColor;
            float _RingThickness;

            float _StarsIntensity;
            float _StarVisibilityThreshold;
            float4 _ShiftColor;
            int _NumStars;
            float _StarSizeVariation;
            float _ColorVariation;
            float _ColorShiftIntensity;
            fixed4 _StarColor;
            fixed4 _BackgroundColor;
            float _StarSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Calculate direction and distance from UV to the black hole center
                float2 direction = v.uv - _BlackHoleCenter;
                float distance = length(direction);
                float effectRadius = _EventHorizonSize;

                if (distance < effectRadius && distance > 0.0f)
                {
                    // Apply distortion effect
                    float distortion = (effectRadius  - distance) * _DistortionStrength;
                    direction = normalize(direction) * distortion;
                    o.vertex.xy -= direction ;
                }

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Simple hash function to generate star pattern
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 originalUV = i.uv;

                // Black Hole Distortion and Color Shift
                float2 toBlackHole = originalUV - _BlackHoleCenter;
                float distance = length(toBlackHole);
                float normalizedDistance = saturate(distance / _EventHorizonSize);
                float shiftIntensity = (1.0 - normalizedDistance) * _ColorShiftIntensity;
                fixed4 targetColor = lerp(_BackgroundColor, _ShiftColor, shiftIntensity);
                
               

                // Apply Black Hole Distortion to UV Coordinates
                if (distance < _EventHorizonSize)
                {
                    distance = pow(distance / _EventHorizonSize, _DistortionStrength) * _EventHorizonSize;
                    i.uv = _BlackHoleCenter + normalize(toBlackHole) * distance;
                }
                
                
            
                // Ring Effect Calculations (Using potentially distorted UVs)
                float2 toRingCenter = i.uv - _BlackHoleCenter;
                float ringDistance = length(toRingCenter);
                float innerBound = _RingSize;
                float outerBound = _RingSize + _RingThickness;
                float ringEffect = smoothstep(innerBound, outerBound, ringDistance) * (1.0 - smoothstep(outerBound, outerBound + _RingThickness, ringDistance));
                float ringIntensity = ringEffect * _RingColorShiftIntensity;
                fixed4 ringColor = lerp(_RingBackgroundColor, _RingColor, ringIntensity);


                float distanceToBlackHole = length(toBlackHole);
                // Check if within the lensing zone
                bool isInLensingZone = distanceToBlackHole > _EventHorizonSize && distanceToBlackHole < _LensingZoneSize;
               

                 // Calculate skewing effect
                float skewFactor = 1.0;
                float starSizeMultiplier = 1.0;
                if (isInLensingZone) {
                    float normalizedDistance = (distanceToBlackHole - _EventHorizonSize) / (_LensingZoneSize - _EventHorizonSize);
                    skewFactor = lerp(1, 2,1+ normalizedDistance); // Skew factor reversed, adjust as needed
                    starSizeMultiplier = lerp(_StarSizeIncreaseFactor, 1.0, normalizedDistance);
                }
                // Skewing UV coordinates for star rendering
                float2 skewedUV = i.uv;
                if (isInLensingZone) {
                    float angle = atan2(toBlackHole.y, toBlackHole.x) + 3.14159 * _SkewingFactor; // Perpendicular to the direction to black hole
                    skewedUV += float2(cos(angle), sin(angle)) * (1.0 - skewFactor) * _SkewingFactor; // Skew amount, adjust as needed
                }
            
                // Blend Black Hole and Ring Effects
                fixed4 finalColor = lerp(targetColor, ringColor, ringIntensity);
            

                
            // Distortion logic remains unchanged
            // Apply the calculated color shift to the star or background color
           
            // Calculate UV coordinates scaled by the desired number of stars to affect density
            float2 starCoord = skewedUV * _NumStars;
            // Calculate the center of each star cell
            float2 starCenter = floor(starCoord) + 0.5;
            // Determine if the current cell contains a star based on a hash function
            float hashValue = hash(starCenter); // Reuse the hash function defined earlier
            bool hasStar = hashValue > _StarVisibilityThreshold;
            // Simulating star size variation 
            if (hasStar)
            {
                 // Adjust star size based on the lensing effect
                 float effectiveStarSize = _StarSize * starSizeMultiplier;
                
                 effectiveStarSize = (hash(skewedUV / _NumStars + 0.1) * _StarSizeVariation + (1.0 - _StarSizeVariation)) * effectiveStarSize;
                // Distance from current point to the center of the star cell
                float2 toCenter = starCoord - starCenter;
                float distanceToCenter = length(toCenter);
                
                // Determine if the current pixel is within the radius of the star size
                bool isStar = distanceToCenter < (effectiveStarSize / _NumStars);
                // Calculate star brightness based on distance to center, creating a gradient effect
                float starBrightness = isStar ? (1.0 - distanceToCenter / (effectiveStarSize / _NumStars)) * _StarsIntensity : 0.0;
                

                // Apply star color and intensity
                fixed4 starColor = _StarColor * starBrightness;
                // Blend star color with background color
                return lerp(finalColor, starColor + finalColor, starBrightness);
            }
                      
        
        
        else
        {
            // Return background color for cells without a star
            return finalColor;
        }
                }
            ENDCG
        }
    }
}
