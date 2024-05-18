using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using static PlasticPipe.Server.MonitorStats;
using UnityEngine.UIElements;

public class AttackEditorWindow : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;
    private Attack attack;
    private float scrollPos;
    private bool isSplitViewCollapsed;

    [MenuItem ( "Window/Attack Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<AttackEditorWindow>();
    }


    private void OnGUI()
    {
        rootVisualElement.Clear();

        if (Selection.activeObject is Attack)
        {
            GUILayout.BeginHorizontal();

            // Split the rect vertically
            var leftRect = new Rect(position.x, position.y, 250, position.height);
            var rightRect = new Rect(position.x + 250, position.y, position.width - 250, position.height);

            // Left Panel - Text Box
            GUILayout.BeginVertical("Left Side", GUILayout.MaxWidth(100), GUILayout.Height(100));
            {
                GUILayout.Label("Enter Text:");
                string text = GUILayout.TextField(""); // Get user input
            }
            GUILayout.EndVertical();


            // Right Panel - Rectangle
            GUILayout.BeginVertical("Right Side", GUILayout.MaxWidth(100), GUILayout.Height(100));
            {
                GUI.backgroundColor = Color.red; // Set desired color
                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(50));
                GUI.backgroundColor = Color.blue; // Set desired color
                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(50));
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        
    }

}
