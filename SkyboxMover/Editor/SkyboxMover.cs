using UnityEditor;
using UnityEngine;

namespace ThibUnityTools.Editor
{
    public class SkyboxMover : EditorWindow
    {

        #region App hooks
        [MenuItem("Tools/Thib/Skybox Mover")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow(typeof(ObjectFitter));
            window.titleContent = new GUIContent("Thib - Skybox Mover");
            window.minSize = new Vector2(520, 600);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Transform", EditorStyles.boldLabel);
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();
        }
        #endregion App hooks
    }
}
