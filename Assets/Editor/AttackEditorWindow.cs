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
            attack = (Attack)Selection.activeObject;

            // Clear previous elements (optional, but recommended)
            rootVisualElement.Clear();


            // Create TwoPaneSplitView
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Vertical);
            rootVisualElement.Add(splitView);

            // Left Panel - Text Box
            var leftPanel = new VisualElement();
            TextField textField = new TextField();
            textField.label = "Enter Text:";
            leftPanel.Add(textField);
            splitView.Add(leftPanel);

            // Right Panel - Rectangle
            var rightPanel = new VisualElement();
            var rectangle = new VisualElement();
            rectangle.style.backgroundColor = Color.red; // Set desired color
            rectangle.style.width = 100;
            rectangle.style.height = 50;
            rightPanel.Add(rectangle);
            splitView.Add(rightPanel);
        }

        // Ensure valid target
        if (attack == null)
        {
            return;
        }
        

        

    }
}
