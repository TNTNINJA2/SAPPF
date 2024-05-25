
using System.Collections;
using System.Drawing.Printing;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
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
    public bool isPlaying = false;
    public float playStartTime;
    public float playTimeStep;
    public Attack attack;
    public PlayerController dummy;
    public Vector2 previousMousePosition;
    public int nearestHandle = -1;

    private static bool showMenu = false;
    private static Vector2 menuPosition;
    private float keyFrameCreationTime = 1;

    private bool showFrameTimeEditor = false; // Flag to track window visibility

    private EditorCoroutine playCoroutine;

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


        if (GUILayout.Button("Create Hitbox KeyFrame"))
        {
            attack.AddHitboxKeyFrame();
        }


        DrawDefaultInspector();

    }


    public void PlayAttack()
    {
        if (isPlaying)
        {
           
            EditorCoroutineUtility.StopCoroutine(playCoroutine);
        }
        else
        {
            isPlaying= true;
            playCoroutine = EditorCoroutineUtility.StartCoroutine(StepPlayAttack(), attack);
        }
    }

    IEnumerator StepPlayAttack()
    {
        float lastTime = 0;

        while (isPlaying)
        {
            time = Time.time - playStartTime;
            Debug.Log("IsPlaying");
            while (time > lastTime + playTimeStep)
            {
                if (time > attack.GetTotalDuration()) isPlaying = false;
                attack.DisplayAtTime(dummy, time, Vector3.zero);
                lastTime = time;
                Debug.Log("Is Time Stepping");


                EditorUtility.SetDirty(this);
            }
            

            yield return null;
        }
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


        int index = 10;

        DrawPosCurves();
        DrawBezierControls();

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
        }
        HandlePosKeyFrames(hoverIndex, ref index);
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            previousMousePosition = Event.current.mousePosition;
        }
    }

    void DrawValueEditor(int windowID)
    {
        // Layout elements for editing the value
        GUILayout.Label("Current Value: " + keyFrameCreationTime);
        keyFrameCreationTime = EditorGUILayout.FloatField(keyFrameCreationTime, GUILayout.Width(100));
        Debug.Log(keyFrameCreationTime);

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

    private void HandlePosKeyFrames(int hoverIndex, ref int index)
    {



        for (int i = 0; i < attack.posKeyFrames.Count; i++)
        {
            index++;
            KeyFrame<PosKeyFrameData> poskeyFrame = attack.posKeyFrames[i];
            Handles.color = hoverIndex == index ? selectedKeyFrameColor : posKeyFrameColor;
            CreateHandleCap(index, poskeyFrame.data.pos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (i != 0 && (Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == index)
            {
                Vector2 move = Camera.main.ScreenToWorldPoint(Event.current.mousePosition) - Camera.main.ScreenToWorldPoint(previousMousePosition);
                Debug.Log(move);
                KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];
                posKeyFrame.data.pos += new Vector2(move.x, -move.y);
                posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
                posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);

            }

            if (i != 0 && (Event.current.type == EventType.MouseDown && Event.current.button == 1))
            {
                Debug.Log("pressedbutton");
                if (nearestHandle == index)
                {

                    KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];
                    Debug.Log("left clicked: " + posKeyFrame);

                }
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
                menu.AddItem(new GUIContent("Option 2"), false, Option2);
                

                // Show the menu at the mouse position
                menu.ShowAsContext();
                showMenu = false;
            }
        }

        if (shouldDrawBezierControls)
        {
            for (int i = 0; i < attack.posKeyFrames.Count; i++)
            {
                index++;
                DrawBezierControlsForPoint(index, hoverIndex, attack.posKeyFrames[i]);
                index++;
            }
        }

        if (Event.current.type == EventType.MouseDrag) SceneView.RepaintAll();


    }
    private void GetAndSetKeyFrameCreationTime(object userData)
    {
        keyFrameCreationTime = EditorGUILayout.FloatField(new GUIContent(""), keyFrameCreationTime); // No label needed here
    }

    private void OpenFrameTimeEditorWindow()
    {
        showFrameTimeEditor = true;

    }

    private void CreateNewPoskeyFrame()
    {

        Vector2 pos = Camera.main.ScreenToWorldPoint(menuPosition);
        Vector2 afterBezierControlPoint = pos + Vector2.right;
        Vector2 beforeBezierControlPoint = pos + Vector2.left;
        float time = keyFrameCreationTime;
        attack.AddPosKeyFrame(time, pos, afterBezierControlPoint, beforeBezierControlPoint);

    }
    private void Option2()
    {
        Debug.Log("option 2");
    }

    private void DrawBezierControlsForPoint(int index, int hoverIndex, KeyFrame<PosKeyFrameData> posKeyFrame)
    {
        Handles.color = hoverIndex == index ? selectedKeyFrameColor : bezierControlColor;
        CreateHandleCap(index, posKeyFrame.data.beforeBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);
        CreateHandleCap(index + 1, posKeyFrame.data.afterBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == index)
        {
            Vector2 move = Camera.main.ScreenToWorldPoint(Event.current.mousePosition) - Camera.main.ScreenToWorldPoint(previousMousePosition);
            posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
        }
        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == index + 1)
        {
            Vector2 move = Camera.main.ScreenToWorldPoint(Event.current.mousePosition) - Camera.main.ScreenToWorldPoint(previousMousePosition);
            posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);
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
