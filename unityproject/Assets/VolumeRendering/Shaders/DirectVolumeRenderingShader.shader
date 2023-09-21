Shader "VolumeRendering/DirectVolumeRenderingShader"
{
	Properties
	{
		_DataTex("Data Texture (Generated)", 3D) = "" {}
		_GradientTex("Gradient Texture (Generated)", 3D) = "" {}
		_NoiseTex("Noise Texture (Generated)", 2D) = "white" {}
		_TFTex("Transfer Function Texture (Generated)", 2D) = "" {}
		_MinVal("Min val", Range(0.0, 1.0)) = 0.0
		_MaxVal("Max val", Range(0.0, 1.0)) = 1.0
			//
			_LightPos("Spot Light Position", Vector) = (0.0,0.3,0.0,0.0)
			_LightLookDir("Spot Light Look Position", Vector) = (0.0,0.0,0.0,0.0)
			_LightIntensity("Spot Light Intensity", Range(0.0,5.0)) = 1.0
			_Ambient("Ambient Light Intensity", Range(0.0,1.0)) = 0.2
	}
		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 100
			Cull Front
			ZTest LEqual
			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM
				#pragma multi_compile_local MODE_DVR MODE_MIP MODE_SURF
				#pragma multi_compile_local __ TF2D_ON
				#pragma multi_compile_local __ CUTOUT_PLANE CUTOUT_BOX_INCL CUTOUT_BOX_EXCL
				#pragma multi_compile_local __ LIGHTING_ON 
				#pragma multi_compile_local __ ADV_LIGHTING_ON
				#pragma multi_compile_local __ CUTSHAPE_ON
				#pragma multi_compile_local __ CUTSHAPE_SEMITRANS
				#pragma multi_compile_local __ ERASER_ON
				#pragma multi_compile_local DEPTHWRITE_ON DEPTHWRITE_OFF
				#pragma multi_compile_local __ RAY_TERMINATE_ON
				#pragma multi_compile_local __ DVR_BACKWARD_ON
				#pragma multi_compile_local __ COLOR_TUNNEL_ON
				#pragma multi_compile_local __ PERSIST_COLOR_TUNNEL_ON
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				#define CUTOUT_ON CUTOUT_PLANE || CUTOUT_BOX_INCL || CUTOUT_BOX_EXCL

				struct vert_in
				{
					float4 vertex : POSITION;
					float4 normal : NORMAL;
					float2 uv : TEXCOORD0;

					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct frag_in
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float3 vertexLocal : TEXCOORD1;
					float3 normal : NORMAL;

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				struct frag_out
				{
					float4 colour : SV_TARGET;
	#if DEPTHWRITE_ON
					float depth : SV_DEPTH;
	#endif
				};

				sampler3D _DataTex;
				sampler3D _GradientTex;
				sampler2D _NoiseTex;
				sampler2D _TFTex;
	#if defined(ERASER_ON) || defined(PERSIST_COLOR_TUNNEL_ON)
				sampler3D _MaskTex;
	#endif


				float _MinVal;
				float _MaxVal;

				float4 _LightPos;
				float4 _LightLookDir;
				float _Ambient;
				float _LightIntensity;

#if defined(COLOR_TUNNEL_ON) || defined(PERSIST_COLOR_TUNNEL_ON)
				float _ColorTunnelRadius;
				float _MinColorTunnelVal;
				float _MaxColorTunnelVal;
				float3 _ColorTunnelLocCenter;
#endif

	#if CUTOUT_ON
				float4x4 _CrossSectionMatrix;
	#endif

				struct RayInfo
				{
					float3 startPos;
					float3 endPos;
					float3 direction;
					float2 aabbInters;
				};

				struct RaymarchInfo
				{
					RayInfo ray;
					int numSteps;
					float numStepsRecip;
					float stepSize;
				};

				float3 getViewRayDir(float3 vertexLocal)
				{
					if (unity_OrthoParams.w == 0)
					{
						// Perspective
						return normalize(ObjSpaceViewDir(float4(vertexLocal, 0.0f)));
					}
					else
					{
						// Orthographic
						float3 camfwd = mul((float3x3)unity_CameraToWorld, float3(0, 0, -1));
						float4 camfwdobjspace = mul(unity_WorldToObject, camfwd);
						return normalize(camfwdobjspace);
					}
				}

				// Find ray intersection points with axis aligned bounding box
				float2 intersectAABB(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax)
				{
					float3 tMin = (boxMin - rayOrigin) / rayDir;
					float3 tMax = (boxMax - rayOrigin) / rayDir;
					float3 t1 = min(tMin, tMax);
					float3 t2 = max(tMin, tMax);
					float tNear = max(max(t1.x, t1.y), t1.z);
					float tFar = min(min(t2.x, t2.y), t2.z);
					return float2(tNear, tFar);
				};

				// Get a ray for the specified fragment (back-to-front)
				RayInfo getRayBack2Front(float3 vertexLocal)
				{
					RayInfo ray;
					ray.direction = getViewRayDir(vertexLocal);
					ray.startPos = vertexLocal + float3(0.5f, 0.5f, 0.5f);
					// Find intersections with axis aligned boundinng box (the volume)
					ray.aabbInters = intersectAABB(ray.startPos, ray.direction, float3(0.0, 0.0, 0.0), float3(1.0f, 1.0f, 1.0));

					// Check if camera is inside AABB
					const float3 farPos = ray.startPos + ray.direction * ray.aabbInters.y - float3(0.5f, 0.5f, 0.5f);
					float4 clipPos = UnityObjectToClipPos(float4(farPos, 1.0f));
					ray.aabbInters += min(clipPos.w, 0.0);

					ray.endPos = ray.startPos + ray.direction * ray.aabbInters.y;
					return ray;
				}

				// Get a ray for the specified fragment (front-to-back)
				RayInfo getRayFront2Back(float3 vertexLocal)
				{
					RayInfo ray = getRayBack2Front(vertexLocal);
					ray.direction = -ray.direction;
					float3 tmp = ray.startPos;
					ray.startPos = ray.endPos;
					ray.endPos = tmp;
					return ray;
				}

				RaymarchInfo initRaymarch(RayInfo ray, int maxNumSteps)
				{
					RaymarchInfo raymarchInfo;
					raymarchInfo.stepSize = 1.732f/*greatest distance in box*/ / maxNumSteps;
					raymarchInfo.numSteps = (int)clamp(abs(ray.aabbInters.x - ray.aabbInters.y) / raymarchInfo.stepSize, 1, maxNumSteps);
					raymarchInfo.numStepsRecip = 1.0 / raymarchInfo.numSteps;
					return raymarchInfo;
				}

	#if defined(CUTSHAPE_ON) || defined(CUTSHAPE_SEMITRANS)
				#define MAX_MESH_TRIANGLES 100
				uniform float4 _MyTriangle[MAX_MESH_TRIANGLES * 3 + 1];
				float PointInOrOn(float3 P1, float3 P2, float3 A, float3 B)
				{
					float3 CP1 = cross(B - A, P1 - A);
					float3 CP2 = cross(B - A, P2 - A);
					return step(0.0, dot(CP1, CP2));
				}

				bool PointInTriangle(float3 px, float3 p0, float3 p1, float3 p2)
				{
					return
						PointInOrOn(px, p0, p1, p2) *
						PointInOrOn(px, p1, p2, p0) *
						PointInOrOn(px, p2, p0, p1);
				}


				float3 IntersectPlane(float3 origin, float3 dir, float3 p0, float3 p1, float3 p2)
				{
					float3 N = cross(p1 - p0, p2 - p0);
					float3 X = origin + dir * dot(p0 - origin, N) / dot(dir, N);

					return X;
				}
				bool IntersectTriangle(float3 origin, float3 dir, float3 p0, float3 p1, float3 p2)
				{
					float3 X = IntersectPlane(origin, dir, p0, p1, p2);
					if (dot(X - origin, dir) < 0) return false;
					return PointInTriangle(X, p0, p1, p2);
				}
				float3 IntersectTrianglePoint(float3 origin, float3 dir, float3 p0, float3 p1, float3 p2)
				{
					float3 X = IntersectPlane(origin, dir, p0, p1, p2);
					if (!PointInTriangle(X, p0, p1, p2)) return float3(-1.0f / 0.0f, -1.0f / 0.0f, -1.0f / 0.0f);
					return X;
				}
	#endif
	#if defined(ERASER_ON) || defined(PERSIST_COLOR_TUNNEL_ON)
				// Gets the mask at the specified position.
				float getMask(float3 pos) {
					return tex3Dlod(_MaskTex, float4(pos.x, pos.y, pos.z, 0.0f));
				}
	#endif

				// Gets the colour from a 1D Transfer Function (x = density)
				float4 getTF1DColour(float density)
				{
					return tex2Dlod(_TFTex, float4(density, 0.0f, 0.0f, 0.0f));
				}

				// Gets the colour from a 2D Transfer Function (x = density, y = gradient magnitude)
				float4 getTF2DColour(float density, float gradientMagnitude)
				{
					return tex2Dlod(_TFTex, float4(density, gradientMagnitude, 0.0f, 0.0f));
				}

				// Gets the density at the specified position
				float getDensity(float3 pos)
				{
					return tex3Dlod(_DataTex, float4(pos.x, pos.y, pos.z, 0.0f));
				}

				// Gets the gradient at the specified position
				float3 getGradient(float3 pos)
				{
					return tex3Dlod(_GradientTex, float4(pos.x, pos.y, pos.z, 0.0f)).rgb;
				}

				// Performs lighting calculations, and returns a modified colour.
				float3 calculateLighting(float3 col, float3 normal, float3 lightDir, float3 eyeDir, float specularIntensity)
				{
					float ndotl = max(lerp(0.0f, 1.5f, dot(normal, lightDir)), 0.5f); // modified, to avoid volume becoming too dark
					float3 diffuse = ndotl * col;
					float3 v = eyeDir;
					float3 r = normalize(reflect(-lightDir, normal));
					float rdotv = max(dot(r, v), 0.0);
					float3 specular = pow(rdotv, 32.0f) * float3(1.0f, 1.0f, 1.0f) * specularIntensity;
					return diffuse + specular;
				}

				// Performs Directional lighting calculations, and returns a modified colour.
				float3 calculateDirectionalLighting(float3 col, float3 normal, float3 lightDir, float3 voxelToLight, float3 viewDir, float3 lightDistance)
				{
					float k = max(dot(voxelToLight, lightDir),0.0); //Higher Offset, Narrower light
					//Calculate light radius in worldPos
					//unity_ObjectToWorld
					float light_radius = k * 1.73; //k*tan(60)
					float3 diffVec = voxelToLight - k * lightDir;
					float3 w_DiffVec = mul(unity_ObjectToWorld, diffVec);
					k = length(w_DiffVec);
					if (k > light_radius) return _Ambient * col;

					float diffuse = max(dot(normal, lightDir) ,0.0); //inside doesn't get diffuse lighting
					float3 reflectDir = normalize(reflect(-lightDir, normal));
					float rdotv = max(dot(reflectDir, viewDir), 0.0);
					float specular = pow(rdotv, 32.0f) * 0.5;
					float difSpecLight = specular + diffuse;
					difSpecLight = difSpecLight * _LightIntensity; // Brightness
					difSpecLight = difSpecLight * (min((light_radius - k) / light_radius, 1.0)); //Light Cone
					difSpecLight = difSpecLight / (lightDistance * lightDistance * lightDistance); //Reduce Energy by lightDistance^3
					return (_Ambient + difSpecLight) * col;
				}

				float3 calculateSpotLighting(float3 col, float3 normal, float3 lightDir, float3 viewDir, float3 lightDistance)
				{
					float diffuse = max(dot(normal, lightDir), 0.0); //inside doesn't get diffuse lighting
					float3 reflectDir = normalize(reflect(-lightDir, normal));
					float rdotv = max(dot(reflectDir, viewDir), 0.0);
					float specular = pow(rdotv, 8.0f) * _LightIntensity / lightDistance; // Energy decreased by lightDistance^3
					return (_Ambient + diffuse + specular) * col;
				}

				// Converts local position to depth value
				float localToDepth(float3 localPos)
				{
					float4 clipPos = UnityObjectToClipPos(float4(localPos, 1.0f));

	#if defined(SHADER_API_GLCORE) || defined(SHADER_API_OPENGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
					return (clipPos.z / clipPos.w) * 0.5 + 0.5;
	#else
					return clipPos.z / clipPos.w;
	#endif
				}

				bool IsCutout(float3 currPos)
				{
	#if CUTOUT_ON
					// Move the reference in the middle of the mesh, like the pivot
					float3 pos = currPos - float3(0.5f, 0.5f, 0.5f);

					// Convert from model space to plane's vector space
					float3 planeSpacePos = mul(_CrossSectionMatrix, float4(pos, 1.0f));

		#if CUTOUT_PLANE
					return planeSpacePos.z > 0.0f;
		#elif CUTOUT_BOX_INCL
					return !(planeSpacePos.x >= -0.5f && planeSpacePos.x <= 0.5f && planeSpacePos.y >= -0.5f && planeSpacePos.y <= 0.5f && planeSpacePos.z >= -0.5f && planeSpacePos.z <= 0.5f);
		#elif CUTOUT_BOX_EXCL
					return planeSpacePos.x >= -0.5f && planeSpacePos.x <= 0.5f && planeSpacePos.y >= -0.5f && planeSpacePos.y <= 0.5f && planeSpacePos.z >= -0.5f && planeSpacePos.z <= 0.5f;
		#endif
	#else
					return false;
	#endif
				}

#if defined(COLOR_TUNNEL_ON) || defined(PERSIST_COLOR_TUNNEL_ON)
				float distanceInWorldSpace(float3 localPos1, float3 localPos2, out float3 worldVec) {
					float3 vec = localPos1 - localPos2;
					worldVec = mul(unity_ObjectToWorld, vec);
					return length(worldVec);
				}
#endif

				frag_in vert_main(vert_in v)
				{
					frag_in o;

					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					o.vertexLocal = v.vertex;
					o.normal = UnityObjectToWorldNormal(v.normal);
					return o;
				}

				// Direct Volume Rendering
				frag_out frag_dvr(frag_in i)
				{
					#define MAX_NUM_STEPS 512
					#define OPACITY_THRESHOLD (1.0 - 1.0 / 255.0)

#ifdef DVR_BACKWARD_ON
					RayInfo ray = getRayBack2Front(i.vertexLocal);
#else
					RayInfo ray = getRayFront2Back(i.vertexLocal);
#endif
					RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);

#if defined(LIGHTING_ON) && defined(ADV_LIGHTING_ON)
					float3 lightDir = -normalize(_LightLookDir);
					float3 voxelToLight = float3(1.0f,0.0f,0.0f);
#elif defined(LIGHTING_ON)
					float3 lightDir = normalize(ObjSpaceViewDir(float4(float3(0.0f, 0.0f, 0.0f), 0.0f)));
#endif

					// Create a small random offset in order to remove artifacts
					ray.startPos += (2.0f * ray.direction * raymarchInfo.stepSize) * tex2D(_NoiseTex, float2(i.uv.x, i.uv.y)).r;

					float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);

#ifdef DVR_BACKWARD_ON
					float tDepth = 0.0f;
#else
					float tDepth = raymarchInfo.numStepsRecip * (raymarchInfo.numSteps - 1);
#endif
					// MY CODE
					// Find intersection from a curved shape and sort them

#if defined(CUTSHAPE_ON) || defined(CUTSHAPE_SEMITRANS)
#define INTERSECT_STEPS_ARR_SIZE 6
//Find 
					uint intersectSteps[INTERSECT_STEPS_ARR_SIZE];
					uint arrIdx = 0;
					for (uint idx = 0; idx < MAX_MESH_TRIANGLES * 3; idx += 3) {
						if (_MyTriangle[idx].x < -2.0f) break;
						float3 X = IntersectTrianglePoint(ray.startPos, ray.direction, _MyTriangle[idx], _MyTriangle[idx + 1], _MyTriangle[idx + 2]);
						if (X.x > -1000000.0f) {
							if (dot(X - ray.startPos, ray.direction) < 0.0f) {
								intersectSteps[arrIdx] = 0;
							}
							else {
								intersectSteps[arrIdx] = distance(X, ray.startPos) / raymarchInfo.stepSize;
							}
							arrIdx++;
							if (arrIdx >= INTERSECT_STEPS_ARR_SIZE) break;
						}
					}
					while (arrIdx < INTERSECT_STEPS_ARR_SIZE) {
						intersectSteps[arrIdx] = MAX_NUM_STEPS; // Any number bigger than MAX_NUM_STEPS - 1
						arrIdx++;
					}
					//Sort array from low to high
					for (arrIdx = 0; arrIdx < INTERSECT_STEPS_ARR_SIZE; arrIdx++) {
						uint lowest = intersectSteps[arrIdx];
						uint lowestIdx = arrIdx;
						for (uint idx = arrIdx + 1; idx < INTERSECT_STEPS_ARR_SIZE; idx++) {
							if (lowest > intersectSteps[idx]) {
								lowest = intersectSteps[idx];
								lowestIdx = idx;
							}
						}
						uint temp = intersectSteps[arrIdx];
						intersectSteps[arrIdx] = lowest;
						intersectSteps[lowestIdx] = temp;
					}

					//Render just inside the sections
					uint start = 0, finish = MAX_NUM_STEPS;
					uint iStep = 0;
					uint iSection = 0;
					bool outSide = false;

					//for (uint iSection = 0; iSection < INTERSECT_STEPS_ARR_SIZE - 1; iSection += 2) {
					while (iSection < INTERSECT_STEPS_ARR_SIZE - 1) {
#ifdef CUTSHAPE_SEMITRANS
						if (outSide) {
							iStep = finish;
						}
						start = intersectSteps[iSection];
						finish = intersectSteps[iSection + 1];
						// Create a semi-transparent areas outside the cutting shape
						if (iStep < start) {
							finish = start;
							start = iStep;
							outSide = true;
						}
						else {
							iSection = iSection + 2;
							outSide = false;
						}

#elif defined(CUTSHAPE_ON)
						start = intersectSteps[iSection];
						finish = intersectSteps[iSection + 1];
						outSide = false;
						iSection += 2;
#endif
						for (iStep = start; iStep < finish; iStep++)
						{
							//END OF MY CODE
#else
						for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
						{
#endif
							const float t = iStep * raymarchInfo.numStepsRecip;
							float3 currPos = lerp(ray.startPos, ray.endPos, t);

							// Perform slice culling (cross section plane)
#ifdef CUTOUT_ON
							if (IsCutout(currPos))
								continue;
#endif

#if defined(ERASER_ON) && !defined(CUTSHAPE_SEMITRANS)
							float density = getMask(currPos);
							if (density < -5.0f) {
								continue;
							}

							bool negativeCol = false;
							if (density < -2.99f) {
								density += 4.0f;
								negativeCol = true;
							}
							else if (density < -0.99f) {
								density += 2.0f;
								negativeCol = true;
							}

#elif defined(PERSIST_COLOR_TUNNEL_ON)
							// Persistent Colour Tunnelling
							float density = getMask(currPos);
							//For mix of earser & colour tunnelling
							if (density < 0.0f) {
								density = -1.0f * density - 1.0f;
								if(_MinColorTunnelVal > density || density > _MaxColorTunnelVal) {
									continue;
								}
							}

							// FOR Proper Persistent Colour Tunnelling
							/*if (density < 0.0f) {
								density = -1.0f * density - 1.0f;
							}*/
#else
							// Get the dansity/sample value of the current position
							/*const*/ float density = getDensity(currPos);
#endif

							// Colour Tunnelling
#if defined(COLOR_TUNNEL_ON) || defined(PERSIST_COLOR_TUNNEL_ON)
							float3 dirVec;
							float dist = distanceInWorldSpace(currPos, _ColorTunnelLocCenter, dirVec);
							if ((_MinColorTunnelVal > density || density > _MaxColorTunnelVal) && dist <= _ColorTunnelRadius) {
								continue;
							}
							else if (_ColorTunnelRadius < dist && dist < 2 * _ColorTunnelRadius) {
								dist = dist / _ColorTunnelRadius - 1;
								dist = 4 * dist;
								dist = sqrt(dist);
								currPos = normalize(dirVec) * dist * _ColorTunnelRadius;
								currPos = mul(unity_WorldToObject, currPos);
								currPos = currPos + _ColorTunnelLocCenter;
								density = getDensity(currPos);
								if (dist < 1 && _MinColorTunnelVal <= density && density <= _MaxColorTunnelVal) {
									continue;
								}
							}
#endif

							// Apply visibility window
							if (density < _MinVal || density > _MaxVal)
								continue;



							// Calculate gradient (needed for lighting and 2D transfer functions)
	#if defined(TF2D_ON) || defined(LIGHTING_ON)
							float3 gradient = getGradient(currPos);
	#endif

							// Apply transfer function
	#if TF2D_ON
							float mag = length(gradient) / 1.75f;
							float4 src = getTF2DColour(density, mag);
	#else
							float4 src = getTF1DColour(density);
	#endif

							// Apply lighting
	#if defined(LIGHTING_ON) && !defined(ADV_LIGHTING_ON)
		#if defined(DVR_BACKWARD_ON)
							src.rgb = calculateLighting(src.rgb, normalize(gradient), lightDir, ray.direction, 0.3f);
		#else
							src.rgb = calculateLighting(src.rgb, normalize(gradient), lightDir, -ray.direction, 0.3f);
		#endif
	#elif defined(LIGHTING_ON)
							voxelToLight = normalize(_LightPos - currPos);
		#if defined(DVR_BACKWARD_ON)
							src.rgb = calculateDirectionalLighting(src.rgb, normalize(gradient), lightDir, voxelToLight, ray.direction, distance(i.vertexLocal.xyz, _LightPos.xyz));
							//src.rgb = calculateSpotLighting(src.rgb, normalize(gradient), lightDir, rayDir, distance(i.vertexLocal.xyz, _LightPos.xyz));
		#else
							src.rgb = calculateDirectionalLighting(src.rgb, normalize(gradient), lightDir, voxelToLight, -ray.direction, distance(i.vertexLocal.xyz, _LightPos.xyz));
		#endif

	#endif

							#if defined(CUTSHAPE_SEMITRANS) && defined(ERASER_ON)
							if (outSide || density >= 1.0f) {
								src.a = 0.05f * src.a;
								iStep += 4;
							}
							#elif defined(CUTSHAPE_SEMITRANS)
								if (outSide) {
									src.a = 0.05f * src.a;
									iStep += 4;
								}
							#endif
							#ifdef ERASER_ON
							if (negativeCol) {
								src.rgb = float3(1.0f, 1.0f, 1.0f) - src.rgb;
							}
							#endif

							// Colour accumulation
#ifdef DVR_BACKWARD_ON
							col.rgb = src.a * src.rgb + (1.0f - src.a) * col.rgb;
							col.a = src.a + (1.0f - src.a) * col.a;

							// Optimisation: A branchless version of: if (src.a > 0.15f) tDepth = t;
							tDepth = max(tDepth, t * step(0.15, src.a));
#else
							src.rgb *= src.a;
							col = (1.0f - col.a) * src + col;

							if (col.a > 0.15 && t < tDepth) {
								tDepth = t;
							}
#endif

							// Early ray termination
#if !defined(DVR_BACKWARD_ON) && defined(RAY_TERMINATE_ON)
							if (col.a > OPACITY_THRESHOLD) {
								break;
							}
#endif
						}
	#if CUTSHAPE_ON
						}
	#endif
				// Write fragment output
				frag_out output;
				output.colour = col;
#if DEPTHWRITE_ON
				tDepth += (step(col.a, 0.0) * 1000.0); // Write large depth if no hit
				const float3 depthPos = lerp(ray.startPos, ray.endPos, tDepth) - float3(0.5f, 0.5f, 0.5f);
				output.depth = localToDepth(depthPos);
#endif
				return output;
			}

			// Maximum Intensity Projection mode
			frag_out frag_mip(frag_in i)
			{
#define MAX_NUM_STEPS 512

				RayInfo ray = getRayBack2Front(i.vertexLocal);
				RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);

				float maxDensity = 0.0f;
				float3 maxDensityPos = ray.startPos;
				for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
				{
					const float t = iStep * raymarchInfo.numStepsRecip;
					const float3 currPos = lerp(ray.startPos, ray.endPos, t);

#ifdef CUTOUT_ON
					if (IsCutout(currPos))
						continue;
#endif

					const float density = getDensity(currPos);
					if (density > maxDensity && density > _MinVal && density < _MaxVal)
					{
						maxDensity = density;
						maxDensityPos = currPos;
					}
				}

				// Write fragment output
				frag_out output;
				output.colour = float4(1.0f, 1.0f, 1.0f, maxDensity); // maximum intensity
#if DEPTHWRITE_ON
				output.depth = localToDepth(maxDensityPos - float3(0.5f, 0.5f, 0.5f));
#endif
				return output;
			}

			// Surface rendering mode
			// Draws the first point (closest to camera) with a density within the user-defined thresholds.
			frag_out frag_surf(frag_in i)
			{
#define MAX_NUM_STEPS 1024

				RayInfo ray = getRayFront2Back(i.vertexLocal);
				RaymarchInfo raymarchInfo = initRaymarch(ray, MAX_NUM_STEPS);

				// Create a small random offset in order to remove artifacts
				ray.startPos = ray.startPos + (2.0f * ray.direction * raymarchInfo.stepSize) * tex2D(_NoiseTex, float2(i.uv.x, i.uv.y)).r;

				float4 col = float4(0, 0, 0, 0);
				for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
				{
					const float t = iStep * raymarchInfo.numStepsRecip;
					const float3 currPos = lerp(ray.startPos, ray.endPos, t);

#ifdef CUTOUT_ON
					if (IsCutout(currPos))
						continue;
#endif

					const float density = getDensity(currPos);
					if (density > _MinVal && density < _MaxVal)
					{
						float3 normal = normalize(getGradient(currPos));
						col = getTF1DColour(density);
						col.rgb = calculateLighting(col.rgb, normal, -ray.direction, -ray.direction, 0.15);
						col.a = 1.0f;
						break;
					}
				}

				// Write fragment output
				frag_out output;
				output.colour = col;
#if DEPTHWRITE_ON

				const float tDepth = iStep * raymarchInfo.numStepsRecip + (step(col.a, 0.0) * 1000.0); // Write large depth if no hit
				output.depth = localToDepth(lerp(ray.startPos, ray.endPos, tDepth) - float3(0.5f, 0.5f, 0.5f));
#endif
				return output;
			}

			frag_in vert(vert_in v)
			{
				return vert_main(v);
			}

			frag_out frag(frag_in i)
			{
#if MODE_DVR
				return frag_dvr(i);
#elif MODE_MIP
				return frag_mip(i);
#elif MODE_SURF
				return frag_surf(i);
#endif
			}

			ENDCG
		}
	}
		}
