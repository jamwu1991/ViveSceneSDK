

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Htc.Viveport.SDK
{
    public class DefaultNavMeshSettings : ScriptableObject
    {
        private const string RootName = "m_BuildSettings";

        private delegate void PropertyOperation(ref bool changeFlag, SerializedProperty src, SerializedProperty dst);

        private static readonly Dictionary<SerializedPropertyType, PropertyOperation> CopyOperations =
            new Dictionary<SerializedPropertyType, PropertyOperation>(2)
            {
                { SerializedPropertyType.Boolean, CopyBool },
                { SerializedPropertyType.Float, CopyFloat }
            };

        private static readonly Dictionary<SerializedPropertyType, PropertyOperation> CompareOperations =
            new Dictionary<SerializedPropertyType, PropertyOperation>(2)
            {
                { SerializedPropertyType.Boolean, CompareBool },
                { SerializedPropertyType.Float, CompareFloat }
            };

        /// <summary>
        /// Mirrors the fields in Navigation Window under the bake tab
        /// </summary>
        [Serializable]
        internal class BuildSettings
        {
            [SerializeField] public float agentRadius;
            [SerializeField] public float agentHeight;
            [SerializeField] public float agentSlope;
            [SerializeField] public float agentClimb;
            [SerializeField] public float ledgeDropHeight;
            [SerializeField] public float maxJumpAcrossDistance;
            [SerializeField] public float minRegionArea;
            [SerializeField] public bool manualCellSize;
            [SerializeField] public float cellSize;
            [SerializeField] public bool accuratePlacement;
        }

        [SerializeField, HideInInspector]
        private BuildSettings m_BuildSettings = new BuildSettings();
        internal BuildSettings Settings { get { return m_BuildSettings; } }
        
        internal static DefaultNavMeshSettings CreateFromSettings(SerializedObject src)
        {
            var dnms = CreateInstance<DefaultNavMeshSettings>();
            var dst = new SerializedObject(dnms);
            
            Copy(src, dst);
            
            return dnms;
        }

        public void ApplyToSettings(SerializedObject dst)
        {
            var src = new SerializedObject(this);

            Copy(src, dst);
        }

        private static void CopyFloat(ref bool isDirty, SerializedProperty src, SerializedProperty dst)
        {
            dst.floatValue = src.floatValue;
            isDirty = true;
        }

        private static void CompareFloat(ref bool isDifferent, SerializedProperty src, SerializedProperty dst)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            isDifferent |= dst.floatValue != src.floatValue;  // has to be Exact!
        }

        private static void CopyBool(ref bool isDirty, SerializedProperty src, SerializedProperty dst)
        {
            dst.boolValue = src.boolValue;
            isDirty = true;
        }

        private static void CompareBool(ref bool isDifferent, SerializedProperty src, SerializedProperty dst)
        {
            isDifferent |= dst.boolValue != src.boolValue;
        }
        
        private static void Copy(SerializedObject src, SerializedObject dst)
        {
            var isDirty = IterateProperties(src, dst, CopyOperations);
            
            if (!isDirty) return;

            dst.ApplyModifiedProperties();
            dst.Update();
            EditorUtility.SetDirty(dst.targetObject);
        }

        public bool IsDifferent(SerializedObject other)
        {
            var self = new SerializedObject(this);

            return IterateProperties(self, other, CompareOperations);
        }

        private static bool IterateProperties(SerializedObject src, SerializedObject dst,
            IDictionary<SerializedPropertyType, PropertyOperation> supportedOperations)
        {
            var changeFlag = false;

            var srcBuildSettings = src.FindProperty(RootName);
            var dstBuildSettings = dst.FindProperty(RootName);

            var srcProp = srcBuildSettings.Copy();
            srcProp.Next(true);
            while (true)
            {
                var propName = srcProp.name;
                SerializedProperty dstProp;
                try
                {
                    dstProp = dstBuildSettings.FindPropertyRelative(propName);
                    if (dstProp == null || dstProp.propertyType != srcProp.propertyType)
                    {
                        goto end;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    continue;
                }

                PropertyOperation op;
                if (supportedOperations.TryGetValue(srcProp.propertyType, out op))
                    op(ref changeFlag, srcProp, dstProp);
                
                end:
                if (!srcProp.Next(false))
                    break;
            }


            return changeFlag;
        }
    }


}