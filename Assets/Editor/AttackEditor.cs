
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections;
using System.Drawing.Printing;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SearchService;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(Attack))]
public class AttackEditor : UnityEditor.Editor
{
    public float time;
    public static Color posKeyFrameColor = Color.red;
    public static Color bezierControlColor = Color.Lerp(Color.red, Color.yellow, 0.5f);
    public static Color selectedKeyFrameColor = Color.magenta;
    public static bool shouldDrawBezierControls = true;
    public static bool shouldDrawTimeControls = true;
    public static bool shouldDrawSpeedIndicators = true;
    public static float speedIndicatorSpacing = 0.1f;
    public static float speedIndicatorWidth = 0.1f;
    public bool isPlaying = false;
    public float playStartTime;
    public float playTimeStep;
    public Attack attack;
    public PlayerController dummy;
    public Vector2 previousMousePosition;
    public int nearestHandle = -1;

    private int posKeyPositionIndexOffset = 10;
    private int posKeyBeforeControlIndexOffset = 100;
    private int posKeyAfterControlIndexOffset = 100;

    private KeyFrame<PosKeyFrameData> lastSelectedlPosKeyFrame;

    private static bool showMenu = false;
    private static Vector2 menuPosition;
    private float keyFrameCreationTime = 1;

    private bool showFrameTimeEditor = false; // Flag to track window visibility

    bool isDragging = false;

    public override void OnInspectorGUI()
    {


        attack = (Attack)target;
        dummy = GameObject.FindGameObjectWithTag("Dummy").GetComponent<PlayerController>();



        time = EditorGUILayout.Slider(time, 0, attack.GetTotalDuration());
         
        
        //if (GUILayout.Button("Play"))
        //{
        //    isPlaying = true;
        //    playStartTime = Time.time;
        //    PlayAttack();
        //}
        attack.DisplayAtTime(dummy, time, Vector3.zero);




        posKeyFrameColor = EditorGUILayout.ColorField(posKeyFrameColor);
        bezierControlColor = EditorGUILayout.ColorField(bezierControlColor);
        selectedKeyFrameColor = EditorGUILayout.ColorField(selectedKeyFrameColor);
        shouldDrawBezierControls = EditorGUILayout.Toggle("Draw Bezier Controls", shouldDrawBezierControls);
        shouldDrawTimeControls = EditorGUILayout.Toggle("Draw Time Controls", shouldDrawTimeControls);
        shouldDrawSpeedIndicators = EditorGUILayout.Toggle("Draw Speed Indicators", shouldDrawSpeedIndicators);
        speedIndicatorSpacing = EditorGUILayout.FloatField("Speed Indicator Spacing", speedIndicatorSpacing);
        speedIndicatorSpacing = Mathf.Clamp(speedIndicatorSpacing, 0.01f, float.MaxValue);
        speedIndicatorWidth = EditorGUILayout.FloatField("Speed Indicator Width", speedIndicatorWidth);
        speedIndicatorWidth = Mathf.Clamp(speedIndicatorWidth, 0.01f, 1);


        if (GUILayout.Button("Create Hitbox KeyFrame"))
        {
            attack.AddHitboxKeyFrame();
        }


        DrawDefaultInspector();

    }


   

    private void OnEnable()
    {
        //SceneView.duringSceneGui += OnSceneupdate;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneupdate;
        SceneView.duringSceneGui -= OnSceneGUI;

    }

    

    private void OnSceneGUI(SceneView sceneView)
    {
        attack = (Attack)target;

        int hoverIndex = -1;
        hoverIndex = HandleUtility.nearestControl;






        DrawPosCurves();
        DrawBezierControls();
        DrawSpeedIndicators();

        HandleSceneRightClicks();

        if (showFrameTimeEditor)
        {
            Rect windowRect = new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100);
            GUI.Window(0, windowRect, DrawValueEditor, "Edit Value");
        }


        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            nearestHandle = HandleUtility.nearestControl;
            previousMousePosition = Event.current.mousePosition;

            // Undo
            Undo.RegisterCompleteObjectUndo(attack, "HandleTransform");
            Undo.FlushUndoRecordObjects();
        }
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            nearestHandle = -1;
            previousMousePosition = Vector3.zero;
            RecalculatePosKeyFrameIndices();
        }
        HandlePosKeyFrames(hoverIndex);
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            previousMousePosition = Event.current.mousePosition;
        }
    }

    private void DrawSpeedIndicators() {
        float time = 0;
        float maxTime = attack.GetTotalDuration();
        while (time < maxTime)
        {
            Vector2 pos = attack.GetPosAtTime(dummy, time, Vector3.zero);
            Vector2 normalizedVelocity = attack.GetVelocityAtTime(dummy, time, Vector3.zero).normalized;
            Vector2 perpendicular = new Vector2(normalizedVelocity.y, -normalizedVelocity.x);
            Vector2 p1 = pos + perpendicular * speedIndicatorWidth;
            Vector2 p2 = pos - perpendicular * speedIndicatorWidth;

            Handles.DrawLine(p1, p2, 0.1f);
            time += speedIndicatorSpacing;
        }
    }

    void DrawValueEditor(int windowID)
    {
        // Layout elements for editing the value
        GUILayout.Label("Current Value: " + keyFrameCreationTime);
        keyFrameCreationTime = EditorGUILayout.FloatField(keyFrameCreationTime, GUILayout.Width(100));

        // Button to confirm and close window
        if (GUILayout.Button("Create keyframe"))
        {
            CreateNewPoskeyFrame();
            showFrameTimeEditor = false;
        }

        // Button to cancel (optional)
        if (GUILayout.Button("Cancel"))
        {
          showFrameTimeEditor = false;
        }

        GUI.DragWindow(); // Allow dragging the window
    }

    private void DrawPosCurves()
    {
        Handles.color = Color.red;
        for (int i = 0; i < attack.posKeyFrames.Count - 1; i++)
        {
            KeyFrame<PosKeyFrameData> posKeyFrame1 = attack.posKeyFrames[i];
            KeyFrame<PosKeyFrameData> posKeyFrame2 = attack.posKeyFrames[i + 1];


            Handles.DrawBezier(posKeyFrame1.data.pos, posKeyFrame2.data.pos,
                posKeyFrame1.data.afterBezierControlPoint, posKeyFrame2.data.beforeBezierControlPoint,
                Color.red, Texture2D.whiteTexture, 1);
        }

    }

    private void DrawBezierControls()
    {
        if (shouldDrawBezierControls)
        {
            for (int i = 0; i < attack.posKeyFrames.Count; i++)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];

                Handles.color = bezierControlColor;
                Handles.DrawLine(posKeyFrame.data.pos, posKeyFrame.data.beforeBezierControlPoint);
                Handles.DrawLine(posKeyFrame.data.pos, posKeyFrame.data.afterBezierControlPoint);
            }
        }
    }





    private void HandlePosKeyFrames(int hoverIndex)
    {
        for (int i = 0; i < attack.posKeyFrames.Count; i++)
        {
            int index = posKeyPositionIndexOffset + i;
            KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];
            Handles.color = hoverIndex == index ? selectedKeyFrameColor : posKeyFrameColor;
            CreateHandleCap(index, posKeyFrame.data.pos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (nearestHandle == index)
            {
                lastSelectedlPosKeyFrame = posKeyFrame;

                if (i != 0 && (Event.current.type == EventType.MouseDrag && Event.current.button == 0))
                {
                    Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition);
                    Debug.Log(move);
                    posKeyFrame.data.pos += new Vector2(move.x, -move.y);
                    posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
                    posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);


                }
            }

            if (shouldDrawTimeControls) HandlePosKeyFrameTime(i);
        }

        if (shouldDrawBezierControls)
        {
            for (int i = 0; i < attack.posKeyFrames.Count; i++)
            {
                DrawBezierControlsForPoint(i, hoverIndex, attack.posKeyFrames[i]);
            }
        }

        if (Event.current.type == EventType.MouseDrag) SceneView.RepaintAll();


    }

    private void HandlePosKeyFrameTime(int posKeyFrameIndex)
    {
        KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[posKeyFrameIndex];

        Vector3 position = posKeyFrame.data.pos;

        // 2. Draw the Input Field and Handle Events
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(HandleUtility.WorldToGUIPoint(position), Vector2.one * 150));

        EditorGUI.BeginChangeCheck(); // Start checking for changes

        float newValue = EditorGUILayout.FloatField("Float Value: " + posKeyFrame.time, posKeyFrame.time);
        if (posKeyFrameIndex != 0) posKeyFrame.time = newValue; // Make first frame uneditable

        if (EditorGUI.EndChangeCheck()) // Check if the value changed
        {
            Undo.RecordObject(target, "Modified Float Value");
            posKeyFrame.time = Mathf.Clamp(posKeyFrame.time, 0, float.MaxValue);
        }

        // Check for mouse events within the GUI area


        


        GUILayout.EndArea();
        Handles.EndGUI();

        // Prevent object movement when editing the float value
        if (isDragging)
        {
            Tools.current = Tool.None; // Temporarily disable the current tool
        }
    }

    private void RecalculatePosKeyFrameIndices()
    {
        bool swapped = true;
        while (swapped) {
            swapped = false;
            for (int i = 0; i < attack.posKeyFrames.Count - 1; i++)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame1 = attack.posKeyFrames[i];
                KeyFrame<PosKeyFrameData> posKeyFrame2 = attack.posKeyFrames[i + 1];
                if (posKeyFrame1.time > posKeyFrame2.time) {
                    attack.posKeyFrames[i] = posKeyFrame2;
                    attack.posKeyFrames[i + 1] = posKeyFrame1;
                    swapped = true;
                }
            }
            
        }
    }

    private void HandleSceneRightClicks()
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            menuPosition = Event.current.mousePosition;
            showMenu = true;
            Event.current.Use();

        }

        // Draw the context menu if needed
        if (showMenu)
        {
            // Create a new generic menu
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create PosKeyFrame"), false, OpenFrameTimeEditorWindow);
            menu.AddItem(new GUIContent("Delete last selected PosKeyFrame"), false, DeleteSelectedPosKeyFrame);


            // Show the menu at the mouse position
            menu.ShowAsContext();
            showMenu = false;
        }
    }


    private void OpenFrameTimeEditorWindow()
    {
        keyFrameCreationTime = attack.posKeyFrames[attack.posKeyFrames.Count - 1].time + 0.1f;
        showFrameTimeEditor = true;
    }

    private void CreateNewPoskeyFrame()
    {
        // Undo
        Undo.RegisterCompleteObjectUndo(attack, "HandleTransform");
        Undo.FlushUndoRecordObjects();

        Vector2 pos = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(menuPosition * new Vector2(1, -1)
            + new Vector2(0, SceneView.GetAllSceneCameras()[0].pixelHeight));
        Vector2 afterBezierControlPoint = pos + Vector2.right;
        Vector2 beforeBezierControlPoint = pos + Vector2.left;
        float time = keyFrameCreationTime;
        attack.AddPosKeyFrame(time, pos, afterBezierControlPoint, beforeBezierControlPoint);



    }
    private void DeleteSelectedPosKeyFrame()
    {
        if (lastSelectedlPosKeyFrame != null) { attack.posKeyFrames.Remove(lastSelectedlPosKeyFrame); }
        else { Debug.Log("Keyframe is null :("); }
        Debug.Log("Trying to delete " + lastSelectedlPosKeyFrame.time);
        
    }

    private void DrawBezierControlsForPoint(int i, int hoverIndex, KeyFrame<PosKeyFrameData> posKeyFrame)
    {
        int index = i + posKeyPositionIndexOffset;
        int beforeControlIndex = index + posKeyBeforeControlIndexOffset;
        int afterControlIndex = beforeControlIndex + posKeyAfterControlIndexOffset;
        Handles.color = (hoverIndex == index || hoverIndex == beforeControlIndex) ? selectedKeyFrameColor : bezierControlColor;
        CreateHandleCap(beforeControlIndex, posKeyFrame.data.beforeBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

        Handles.color = (hoverIndex == index || hoverIndex == afterControlIndex) ? selectedKeyFrameColor : bezierControlColor;
        CreateHandleCap(afterControlIndex, posKeyFrame.data.afterBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == beforeControlIndex)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition); posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
        }
        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == afterControlIndex)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition); posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);
        }
    }

    void CreateHandleCap(int id, Vector3 position, Quaternion rotation, float size, EventType eventType)
    {
        Handles.DotHandleCap(id, position, rotation, size, eventType);
    }


    public void OnSceneupdate(SceneView sceneView)
    {
        attack = (Attack)target;


        // Set handle color






        for (int i = 0; i < attack.hitboxKeyFrames.Count; i++)
        {
            KeyFrame<HitboxKeyFrameData> hitboxKeyFrame = attack.hitboxKeyFrames[i];
            Vector3 pos = hitboxKeyFrame.data.pos;
            Quaternion rotation = Quaternion.identity;
            Vector3 size = hitboxKeyFrame.data.size;
            pos = Handles.PositionHandle(pos, rotation);
            size = Handles.ScaleHandle(size, pos, rotation);
            Handles.DrawSolidRectangleWithOutline(new Rect(pos, size), Color.red, Color.red);

            hitboxKeyFrame.data.pos = pos;
            hitboxKeyFrame.data.size = size;

        }
    }
}
