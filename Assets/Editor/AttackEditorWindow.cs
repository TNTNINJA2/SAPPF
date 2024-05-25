using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using static PlasticPipe.Server.MonitorStats;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class AttackEditorWindow : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;
    private Attack attack;
    private float scrollPos;
    private bool isSplitViewCollapsed;

    private bool shouldDisplaySpriteKeyFrames;

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
            attack = Selection.activeObject as Attack;
            GUILayout.BeginHorizontal();

            // Split the rect vertically
            var leftRect = new Rect(position.x, position.y, 250, position.height);
            var rightRect = new Rect(position.x + 250, position.y, position.width - 250, position.height);

            // Left Panel - Text Box
            GUILayout.BeginVertical("Left Side", GUILayout.MaxWidth(100), GUILayout.Height(100));
            {
                GUILayout.Label("Enter Text:");
                string text = GUILayout.TextField(""); // Get user input
                shouldDisplaySpriteKeyFrames = EditorGUILayout.Toggle("Sprites", shouldDisplaySpriteKeyFrames);
            }
            GUILayout.EndVertical();


            // Right Panel - Rectangle
            GUILayout.BeginVertical("Right Side", GUILayout.MaxWidth(100), GUILayout.Height(100));
            {
                if (shouldDisplaySpriteKeyFrames && attack != null)
                {
                    GUILayout.BeginHorizontal("");
                    foreach (KeyFrame<SpriteKeyFrameData> spriteKeyFrame in attack.spriteKeyFrames)
                    {
                        Texture2D displayTexture = GetTextureFromRect(spriteKeyFrame.data.sprite.texture, spriteKeyFrame.data.sprite.rect);
                        //displayTexture.Reinitialize(32, 32);
                        //displayTexture.Apply();
                        
                        GUILayout.Box(displayTexture, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
                    }
                    GUILayout.EndHorizontal();
                }
                GUI.backgroundColor = Color.red; // Set desired color
                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(50));
                GUI.backgroundColor = Color.blue; // Set desired color
                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(50));
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        
    }

    private Texture2D GetTextureFromRect(Texture2D sourceTexture, Rect selectionRect)
    {
        Texture2D newTexture = new Texture2D((int)selectionRect.width, (int)selectionRect.height);
        Debug.Log("xMin: " + selectionRect.xMin + " width: " + selectionRect.width + " yMin: " + selectionRect.yMin + " height: " + selectionRect.height);
        Color[] sourcePixels = sourceTexture.GetPixels((int)selectionRect.xMin, (int)selectionRect.yMin,
                                               (int)selectionRect.width, (int)selectionRect.height);

        newTexture.SetPixels(sourcePixels);
        newTexture.Apply();

        return newTexture;
    } 

}
