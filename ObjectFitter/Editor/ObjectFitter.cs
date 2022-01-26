using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThibUnityTools.Editor
{
    public class ObjectFitter : EditorWindow
    {
        [Serializable]
        private struct BoundingAxisRecord
        {
            public MeshFilter AxisA;
            public MeshFilter AxisB;

            public BoundingAxisRecord(MeshFilter axisA = null, MeshFilter axisB = null)
            {
                AxisA = axisA;
                AxisB = axisB;
            }
        }

        [NonSerialized] private string fResultMessage = string.Empty;
        [NonSerialized] private bool fResultSuccess = true;

        private bool fEnableAxisX = true;
        private bool fEnableAxisY = true;
        private bool fEnableAxisZ = true;
        private bool fCenterObject = true;
        private bool fScaleObject = true;
        private MeshFilter fObjectToFit = null;
        private BoundingAxisRecord fBoundingObjectsX = new BoundingAxisRecord();
        private BoundingAxisRecord fBoundingObjectsY = new BoundingAxisRecord();
        private BoundingAxisRecord fBoundingObjectsZ = new BoundingAxisRecord();

        #region App hooks
        [MenuItem("Tools/Thib/Object Fitter")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow(typeof(ObjectFitter));
            window.titleContent = new GUIContent("Thib - Object Fitter");
            window.minSize = new Vector2(520, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            fCenterObject = EditorGUILayout.Toggle("Center object", fCenterObject);
            fScaleObject = EditorGUILayout.Toggle("Scale object", fScaleObject);
            GUILayout.Label("Enabled axis", EditorStyles.label);
            EditorGUILayout.BeginHorizontal();
            float origWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20f;
            fEnableAxisX = EditorGUILayout.Toggle("X", fEnableAxisX, GUILayout.ExpandWidth(false));
            EditorGUILayout.Space(20f, false);
            fEnableAxisY = EditorGUILayout.Toggle("Y", fEnableAxisY, GUILayout.ExpandWidth(false));
            EditorGUILayout.Space(20f, false);
            fEnableAxisZ = EditorGUILayout.Toggle("Z", fEnableAxisZ, GUILayout.ExpandWidth(false));
            EditorGUIUtility.labelWidth = origWidth;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Object To Fit", EditorStyles.boldLabel);
            fObjectToFit = (MeshFilter)EditorGUILayout.ObjectField(fObjectToFit, typeof(MeshFilter), true);
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Bounding Objects", EditorStyles.boldLabel);

            if (fEnableAxisX)
            {
                GUILayout.Label("X-axis bounding objects", EditorStyles.label);
                fBoundingObjectsX.AxisA = (MeshFilter)EditorGUILayout.ObjectField(fBoundingObjectsX.AxisA, typeof(MeshFilter), true);
                fBoundingObjectsX.AxisB = (MeshFilter)EditorGUILayout.ObjectField(fBoundingObjectsX.AxisB, typeof(MeshFilter), true);
            }

            if (fEnableAxisY)
            {
                GUILayout.Label("Y-axis bounding objects", EditorStyles.label);
                fBoundingObjectsY.AxisA = (MeshFilter)EditorGUILayout.ObjectField(fBoundingObjectsY.AxisA, typeof(MeshFilter), true);
                fBoundingObjectsY.AxisB = (MeshFilter)EditorGUILayout.ObjectField(fBoundingObjectsY.AxisB, typeof(MeshFilter), true);
            }

            if (fEnableAxisZ)
            {
                GUILayout.Label("Z-axis bounding objects", EditorStyles.label);
                fBoundingObjectsZ.AxisA = (MeshFilter)EditorGUILayout.ObjectField(fBoundingObjectsZ.AxisA, typeof(MeshFilter), true);
                fBoundingObjectsZ.AxisB = (MeshFilter)EditorGUILayout.ObjectField(fBoundingObjectsZ.AxisB, typeof(MeshFilter), true);
            }

            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Fit!", EditorStyles.toolbarButton))
                ProcessFitting();

            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(fResultMessage, fResultSuccess ? MessageType.Info : MessageType.Error);
        }
        #endregion App hooks

        #region Tool methods
        private void ProcessFitting()
        {
            if (fObjectToFit == null)
            {
                fResultMessage = "Error!\nObject to fit is null.";
                fResultSuccess = false;
                return;
            }

            if (fEnableAxisX && (fBoundingObjectsX.AxisA == null || fBoundingObjectsX.AxisB == null))
            {
                fResultMessage = "Error!\nOne of the X-bounding objects is null.";
                fResultSuccess = false;
                return;
            }
            
            if (fEnableAxisY && (fBoundingObjectsY.AxisA == null || fBoundingObjectsY.AxisB == null))
            {
                fResultMessage = "Error!\nOne of the Y-bounding objects is null.";
                fResultSuccess = false;
                return;
            }

            if (fEnableAxisZ && (fBoundingObjectsZ.AxisA == null || fBoundingObjectsZ.AxisB == null))
            {
                fResultMessage = "Error!\nOne of the Z-bounding objects is null.";
                fResultSuccess = false;
                return;
            }

            fResultMessage = string.Empty;
            var newPos = fCenterObject ? CalculateNewCenter() : fObjectToFit.gameObject.transform.localPosition;
            var newScale = fScaleObject ? CalculateNewScale() : fObjectToFit.gameObject.transform.localScale;
            FitObject(newPos, newScale);
        }

        private Vector3 CalculateNewCenter()
        {
            var pos = fObjectToFit.gameObject.transform.localPosition;
            float adjustedAPos;
            float adjustedBPos;

            if (fEnableAxisX)
            {
                if (fBoundingObjectsX.AxisA.transform.localPosition.x > fBoundingObjectsX.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosX(fBoundingObjectsX.AxisA, true);
                    adjustedBPos = GetAdjustedPosX(fBoundingObjectsX.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosX(fBoundingObjectsX.AxisA, false);
                    adjustedBPos = GetAdjustedPosX(fBoundingObjectsX.AxisB, true);
                }

                pos.x = adjustedAPos + (adjustedBPos - adjustedAPos) / 2;
            }

            if (fEnableAxisY)
            {
                if (fBoundingObjectsY.AxisA.transform.localPosition.x > fBoundingObjectsY.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosY(fBoundingObjectsY.AxisA, true);
                    adjustedBPos = GetAdjustedPosY(fBoundingObjectsY.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosY(fBoundingObjectsY.AxisA, false);
                    adjustedBPos = GetAdjustedPosY(fBoundingObjectsY.AxisB, true);
                }

                pos.y = adjustedAPos + (adjustedBPos - adjustedAPos) / 2;
            }

            if (fEnableAxisZ)
            {
                if (fBoundingObjectsZ.AxisA.transform.localPosition.x > fBoundingObjectsZ.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisA, true);
                    adjustedBPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisA, false);
                    adjustedBPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisB, true);
                }

                pos.z = adjustedAPos + (adjustedBPos - adjustedAPos) / 2;
            }

            return pos;
        }

        private Vector3 CalculateNewScale()
        {
            Vector3 scale = fObjectToFit.gameObject.transform.localScale;
            Bounds objBounds = fObjectToFit.sharedMesh.bounds;
            float adjustedAPos;
            float adjustedBPos;
            float gap;

            if (fEnableAxisX)
            {
                if (fBoundingObjectsX.AxisA.transform.localPosition.x > fBoundingObjectsX.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosX(fBoundingObjectsX.AxisA, true);
                    adjustedBPos = GetAdjustedPosX(fBoundingObjectsX.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosX(fBoundingObjectsX.AxisA, false);
                    adjustedBPos = GetAdjustedPosX(fBoundingObjectsX.AxisB, true);
                }

                gap = Math.Abs(adjustedAPos - adjustedBPos);
                scale.x = 1.0f * (gap / objBounds.size.x);
            }

            if (fEnableAxisY)
            {
                if (fBoundingObjectsY.AxisA.transform.localPosition.y > fBoundingObjectsY.AxisB.transform.localPosition.y)
                {
                    adjustedAPos = GetAdjustedPosY(fBoundingObjectsY.AxisA, true);
                    adjustedBPos = GetAdjustedPosY(fBoundingObjectsY.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosY(fBoundingObjectsY.AxisA, false);
                    adjustedBPos = GetAdjustedPosY(fBoundingObjectsY.AxisB, true);
                }

                gap = Math.Abs(adjustedAPos - adjustedBPos);
                scale.y = 1.0f * (gap / objBounds.size.y);
            }

            if (fEnableAxisZ)
            {
                if (fBoundingObjectsZ.AxisA.transform.localPosition.z > fBoundingObjectsZ.AxisB.transform.localPosition.z)
                {
                    adjustedAPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisA, true);
                    adjustedBPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisA, false);
                    adjustedBPos = GetAdjustedPosZ(fBoundingObjectsZ.AxisB, true);
                }

                gap = Math.Abs(adjustedAPos - adjustedBPos);
                scale.z = 1.0f * (gap / objBounds.size.z);
            }

            return scale;
        }

        private float GetAdjustedPosX(MeshFilter meshFilter, bool aIndNeg)
        {
            Bounds bounds = GetScaledBounds(meshFilter);
            return aIndNeg
                ? meshFilter.transform.localPosition.x - bounds.extents.x
                : meshFilter.transform.localPosition.x + bounds.extents.x;
        }

        private float GetAdjustedPosY(MeshFilter meshFilter, bool aIndNeg)
        {
            Bounds bounds = GetScaledBounds(meshFilter);
            return aIndNeg
                ? meshFilter.transform.localPosition.y - bounds.extents.y
                : meshFilter.transform.localPosition.y + bounds.extents.y;
        }

        private float GetAdjustedPosZ(MeshFilter meshFilter, bool aIndNeg)
        {
            Bounds bounds = GetScaledBounds(meshFilter);
            return aIndNeg
                ? meshFilter.transform.localPosition.z - bounds.extents.z
                : meshFilter.transform.localPosition.z + bounds.extents.z;
        }

        private static Bounds GetScaledBounds(MeshFilter aFilter)
        {
            Vector3 scale = aFilter.gameObject.transform.localScale;
            var result = aFilter != null ? aFilter.sharedMesh.bounds : new Bounds();
            var scaledMin = result.min;
            scaledMin.Scale(scale);
            result.min = scaledMin;
            var scaledMax = result.max;
            scaledMax.Scale(scale);
            result.max = scaledMax;
            return result;
        }

        private void FitObject(Vector3 aNewPos, Vector3 aNewScale)
        {
            Undo.RecordObject(fObjectToFit.gameObject.transform, "Fit object to bounds");
            fObjectToFit.gameObject.transform.localPosition = aNewPos;
            fObjectToFit.gameObject.transform.localScale = aNewScale;
            // Notice that if the call to RecordPrefabInstancePropertyModifications is not present,
            // all changes to scale will be lost when saving the Scene, and reopening the Scene
            // would revert the scale back to its previous value.
            PrefabUtility.RecordPrefabInstancePropertyModifications(fObjectToFit.gameObject.transform);

            fResultMessage += "All done!";
            fResultSuccess = true;
        }
        #endregion Tool methods
    }
}
