
using System.Drawing.Printing;
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
    public Attack attack;
    public PlayerController dummy;
    public Vector2 previousMousePosition;
    public int nearestHandle = -1;

    public override void OnInspectorGUI()
    {
        attack = (Attack)target;


        time = EditorGUILayout.Slider(time, 0, attack.GetTotalDuration());
        posKeyFrameColor = EditorGUILayout.ColorField(posKeyFrameColor);
        bezierControlColor = EditorGUILayout.ColorField(bezierControlColor);
        selectedKeyFrameColor = EditorGUILayout.ColorField(selectedKeyFrameColor);
        shouldDrawBezierControls = EditorGUILayout.Toggle("Draw Bezier Controls", shouldDrawBezierControls);

        dummy = GameObject.FindGameObjectWithTag("Dummy").GetComponent<PlayerController>();

        if (GUILayout.Button("Create Hitbox KeyFrame"))
        {
            attack.AddHitboxKeyFrame();
        }


        DrawDefaultInspector();
        attack.DisplayAtTime(dummy, time, Vector3.zero);

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

            if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == index)
            {
                Vector2 move = Camera.main.ScreenToWorldPoint(Event.current.mousePosition) - Camera.main.ScreenToWorldPoint(previousMousePosition);
                Debug.Log(move);
                KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];
                posKeyFrame.data.pos += new Vector2(move.x, -move.y);
                posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
                posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);

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
