using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace ThibObjectFitter.Editor
{
    public class ObjectFitter : EditorWindow
    {
        [Flags]
        private enum FittingAxis : short
        {
            X = 0x01,
            Y = 0x02,
            Z = 0x04,
            XY = 0x08,
            XZ = 0x10,
            YZ = 0x20,
            XYZ = 0x40
        }

        private readonly static FittingAxis CXPositions = FittingAxis.X | FittingAxis.XY | FittingAxis.XZ | FittingAxis.XYZ;
        private readonly static FittingAxis CYPositions = FittingAxis.Y | FittingAxis.XY | FittingAxis.YZ | FittingAxis.XYZ;
        private readonly static FittingAxis CZPositions = FittingAxis.Z | FittingAxis.XZ | FittingAxis.YZ | FittingAxis.XYZ;

        [Serializable]
        struct BoundingAxisRecord
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

        private FittingAxis fAxis = FittingAxis.XYZ;
        private bool fCenterObject = true;
        private bool fScaleObject = true;
        private MeshFilter fObjectToFit = null;
        private readonly List<BoundingAxisRecord> fBoundingObjects = new List<BoundingAxisRecord>(3) {
            new BoundingAxisRecord(),
            new BoundingAxisRecord(),
            new BoundingAxisRecord()
        };

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

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            fAxis = (FittingAxis)EditorGUILayout.EnumPopup("Fitting Axes", fAxis);
            fCenterObject = EditorGUILayout.Toggle("Center object", fCenterObject);
            fScaleObject = EditorGUILayout.Toggle("Scale object", fScaleObject);
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Object To Fit", EditorStyles.boldLabel);
            fObjectToFit = (MeshFilter)EditorGUILayout.ObjectField(fObjectToFit, typeof(MeshFilter), true);
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Bounding Objects", EditorStyles.boldLabel);
            int i = 0;
            BoundingAxisRecord rec;

            if (fAxis == FittingAxis.X || fAxis == FittingAxis.XY || fAxis == FittingAxis.XZ || fAxis == FittingAxis.XYZ)
            {
                GUILayout.Label("X-axis bounding objects", EditorStyles.label);
                rec = fBoundingObjects[i];
                rec.AxisA = (MeshFilter)EditorGUILayout.ObjectField(rec.AxisA, typeof(MeshFilter), true);
                rec.AxisB = (MeshFilter)EditorGUILayout.ObjectField(rec.AxisB, typeof(MeshFilter), true);
                fBoundingObjects[i++] = rec;
            }

            if (fAxis == FittingAxis.Y || fAxis == FittingAxis.XY || fAxis == FittingAxis.YZ || fAxis == FittingAxis.XYZ)
            {
                GUILayout.Label("Y-axis bounding objects", EditorStyles.label);
                rec = fBoundingObjects[i];
                rec.AxisA = (MeshFilter)EditorGUILayout.ObjectField(rec.AxisA, typeof(MeshFilter), true);
                rec.AxisB = (MeshFilter)EditorGUILayout.ObjectField(rec.AxisB, typeof(MeshFilter), true);
                fBoundingObjects[i++] = rec;
            }

            if (fAxis == FittingAxis.Z || fAxis == FittingAxis.XZ || fAxis == FittingAxis.YZ || fAxis == FittingAxis.XYZ)
            {
                GUILayout.Label("Z-axis bounding objects", EditorStyles.label);
                rec = fBoundingObjects[i];
                rec.AxisA = (MeshFilter)EditorGUILayout.ObjectField(rec.AxisA, typeof(MeshFilter), true);
                rec.AxisB = (MeshFilter)EditorGUILayout.ObjectField(rec.AxisB, typeof(MeshFilter), true);
                fBoundingObjects[i++] = rec;
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

            switch (fAxis)
            {
                case FittingAxis.X:
                case FittingAxis.Y:
                case FittingAxis.Z:
                    {
                        if (fBoundingObjects[0].AxisA == null || fBoundingObjects[0].AxisB == null)
                        {
                            fResultMessage = "Error!\nBounding object is null.";
                            fResultSuccess = false;
                            return;
                        }

                        break;
                    }
                case FittingAxis.XY:
                case FittingAxis.XZ:
                case FittingAxis.YZ:
                    {
                        if (fBoundingObjects[0].AxisA == null || fBoundingObjects[0].AxisB == null ||
                            fBoundingObjects[1].AxisA == null || fBoundingObjects[1].AxisB == null)
                        {
                            fResultMessage = "Error!\nOne of the bounding objects is null.";
                            fResultSuccess = false;
                            return;
                        }

                        break;
                    }
                case FittingAxis.XYZ:
                    {
                        if (fBoundingObjects[0].AxisA == null || fBoundingObjects[0].AxisB == null ||
                            fBoundingObjects[1].AxisA == null || fBoundingObjects[1].AxisB == null ||
                            fBoundingObjects[2].AxisA == null || fBoundingObjects[2].AxisB == null)
                        {
                            fResultMessage = "Error!\nOne of the bounding objects is null.";
                            fResultSuccess = false;
                            return;
                        }

                        break;
                    }
            }

            fResultMessage = string.Empty;
            var newPos = fCenterObject ? CalculateNewCenter() : fObjectToFit.gameObject.transform.localPosition;
            var newScale = fScaleObject ? CalculateNewScale() : fObjectToFit.gameObject.transform.localScale;
            FitObject(newPos, newScale);
        }

        private Vector3 CalculateNewCenter()
        {
            var pos = fObjectToFit.gameObject.transform.localPosition;
            int i = 0;
            BoundingAxisRecord rec;
            float adjustedAPos;
            float adjustedBPos;

            if (CXPositions.HasFlag(fAxis))
            {
                rec = fBoundingObjects[i++];

                if (rec.AxisA.transform.localPosition.x > rec.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosX(rec.AxisA, true);
                    adjustedBPos = GetAdjustedPosX(rec.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosX(rec.AxisA, false);
                    adjustedBPos = GetAdjustedPosX(rec.AxisB, true);
                }

                pos.x = adjustedAPos + (adjustedBPos - adjustedAPos) / 2;
            }

            if (CYPositions.HasFlag(fAxis))
            {
                rec = fBoundingObjects[i++];

                if (rec.AxisA.transform.localPosition.x > rec.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosY(rec.AxisA, true);
                    adjustedBPos = GetAdjustedPosY(rec.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosY(rec.AxisA, false);
                    adjustedBPos = GetAdjustedPosY(rec.AxisB, true);
                }

                pos.y = adjustedAPos + (adjustedBPos - adjustedAPos) / 2;
            }

            if (CZPositions.HasFlag(fAxis))
            {
                rec = fBoundingObjects[i++];

                if (rec.AxisA.transform.localPosition.x > rec.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosZ(rec.AxisA, true);
                    adjustedBPos = GetAdjustedPosZ(rec.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosZ(rec.AxisA, false);
                    adjustedBPos = GetAdjustedPosZ(rec.AxisB, true);
                }

                pos.z = adjustedAPos + (adjustedBPos - adjustedAPos) / 2;
            }

            return pos;
        }

        private Vector3 CalculateNewScale()
        {
            Vector3 scale = fObjectToFit.gameObject.transform.localScale;
            Bounds objBounds = fObjectToFit.sharedMesh.bounds;
            int i = 0;
            BoundingAxisRecord rec;
            float adjustedAPos;
            float adjustedBPos;
            float gap;

            if (CXPositions.HasFlag(fAxis))
            {
                rec = fBoundingObjects[i++];

                if (rec.AxisA.transform.localPosition.x > rec.AxisB.transform.localPosition.x)
                {
                    adjustedAPos = GetAdjustedPosX(rec.AxisA, true);
                    adjustedBPos = GetAdjustedPosX(rec.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosX(rec.AxisA, false);
                    adjustedBPos = GetAdjustedPosX(rec.AxisB, true);
                }

                gap = Math.Abs(adjustedAPos - adjustedBPos);
                scale.x = 1.0f * (gap / objBounds.size.x);
            }

            if (CYPositions.HasFlag(fAxis))
            {
                rec = fBoundingObjects[i++];

                if (rec.AxisA.transform.localPosition.y > rec.AxisB.transform.localPosition.y)
                {
                    adjustedAPos = GetAdjustedPosY(rec.AxisA, true);
                    adjustedBPos = GetAdjustedPosY(rec.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosY(rec.AxisA, false);
                    adjustedBPos = GetAdjustedPosY(rec.AxisB, true);
                }

                gap = Math.Abs(adjustedAPos - adjustedBPos);
                scale.y = 1.0f * (gap / objBounds.size.y);
            }

            if (CZPositions.HasFlag(fAxis))
            {
                rec = fBoundingObjects[i++];

                if (rec.AxisA.transform.localPosition.z > rec.AxisB.transform.localPosition.z)
                {
                    adjustedAPos = GetAdjustedPosZ(rec.AxisA, true);
                    adjustedBPos = GetAdjustedPosZ(rec.AxisB, false);
                }
                else
                {
                    adjustedAPos = GetAdjustedPosZ(rec.AxisA, false);
                    adjustedBPos = GetAdjustedPosZ(rec.AxisB, true);
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
