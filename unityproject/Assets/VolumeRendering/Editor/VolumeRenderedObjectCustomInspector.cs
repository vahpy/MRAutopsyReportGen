using UnityEngine;
using UnityEditor;

namespace UnityVolumeRendering
{
    [CustomEditor(typeof(VolumeRenderedObject))]
    public class VolumeRenderedObjectCustomInspector : Editor
    {
        bool otherSettings = false;
        public override void OnInspectorGUI()
        {
            VolumeRenderedObject volrendObj = (VolumeRenderedObject)target;

            // Render mode
            RenderMode oldRenderMode = volrendObj.GetRenderMode();
            RenderMode newRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Render mode", oldRenderMode);
            if (newRenderMode != oldRenderMode)
                volrendObj.SetRenderMode(newRenderMode);

            // Lighting settings
            if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                volrendObj.SetLightingEnabled(GUILayout.Toggle(volrendObj.GetLightingEnabled(), "Enable lighting"));
            else
                volrendObj.SetLightingEnabled(false);

            // Advanced Lighting settings
            if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                volrendObj.SetAdvancedLightingEnabled(GUILayout.Toggle(volrendObj.GetAdvancedLightingEnabled(), "Enable advanced lighting"));
            else
                volrendObj.SetAdvancedLightingEnabled(false);
            // Cutting Shape settings
            if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                volrendObj.SetCutShapeEnabled(GUILayout.Toggle(volrendObj.GetCutShapeEnabled(), "Enable cut shape"));
            else
                volrendObj.SetCutShapeEnabled(false);
            // Cutting Shape Semi Transparent settings
            if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                volrendObj.SetCutShapeSemiTransparentEnabled(GUILayout.Toggle(volrendObj.GetCutShapeSemiTransparentEnabled(), "Enable Semi Transparent"));
            else
                volrendObj.SetCutShapeSemiTransparentEnabled(false);
            // Erasesr settings
            if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                volrendObj.SetEraserEnabled(GUILayout.Toggle(volrendObj.GetEraserEnabled(), "Enable eraser"));
            else
                volrendObj.SetEraserEnabled(false);
            // New Mask Texture
            if (GUILayout.Button("Create New Mask"))
            {
                volrendObj.MakeMaskTextureNull();
            }

            // Visibility window
            Vector2 visibilityWindow = volrendObj.GetVisibilityWindow();
            EditorGUILayout.MinMaxSlider("Visible value range", ref visibilityWindow.x, ref visibilityWindow.y, 0.0f, 1.0f);
            EditorGUILayout.Space();
            volrendObj.SetVisibilityWindow(visibilityWindow);

            //Color Tunneling
            volrendObj.SetColorTunnelingEnabled(GUILayout.Toggle(volrendObj.GetColorTunnelingEnabled(), "Enable Color Tunneling"));
            volrendObj.SetPersistColorTunnelingEnabled(GUILayout.Toggle(volrendObj.GetPersistColorTunnelingEnabled(), "Enable Persistent Color Tunneling"));
            //if (volrendObj.GetPersistColorTunnelRunner() == null)
            //{
            //    EditorGUIUtility.ShowObjectPicker<PersistColorTunnelRunner>(volrendObj.GetPersistColorTunnelRunner(), true, "color", 0);
            //    volrendObj.SetPersistColorTunnelRunner((PersistColorTunnelRunner)EditorGUIUtility.GetObjectPickerObject());
            //}
            if (volrendObj.GetColorTunnelingEnabled())
            {
                //range
                Vector2 colorRange = volrendObj.GetColorTunnelRange();
                EditorGUILayout.MinMaxSlider("Visible value range", ref colorRange.x, ref colorRange.y, 0.0f, 1.0f);
                EditorGUILayout.Space();
                volrendObj.SetColorTunnelRange(colorRange.x,colorRange.y);
                //radius
                volrendObj.SetColorTunnelRadius(EditorGUILayout.FloatField("Tunnel Radius:",volrendObj.GetColorTunnelRadius()));
            }

            // Transfer function type
            TFRenderMode tfMode = (TFRenderMode)EditorGUILayout.EnumPopup("Transfer function type", volrendObj.GetTransferFunctionMode());
            if (tfMode != volrendObj.GetTransferFunctionMode())
                volrendObj.SetTransferFunctionMode(tfMode);

            // Show TF button
            if (GUILayout.Button("Edit transfer function"))
            {
                if (tfMode == TFRenderMode.TF1D)
                    TransferFunctionEditorWindow.ShowWindow();
                else
                    TransferFunction2DEditorWindow.ShowWindow();
            }


            // Other settings for direct volume rendering
            if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
            {
                GUILayout.Space(10);
                otherSettings = EditorGUILayout.Foldout(otherSettings, "Other Settings");
                if (otherSettings)
                {
                    // Temporary back-to-front rendering option
                    volrendObj.SetDVRBackwardEnabled(GUILayout.Toggle(volrendObj.GetDVRBackwardEnabled(), "Enable Back-to-Front Direct Volume Rendering"));

                    // Early ray termination for Front-to-back DVR
                    if (!volrendObj.GetDVRBackwardEnabled())
                    {
                        volrendObj.SetRayTerminationEnabled(GUILayout.Toggle(volrendObj.GetRayTerminationEnabled(), "Enable early ray termination"));
                    }
                }
            }
        }
    }
}
