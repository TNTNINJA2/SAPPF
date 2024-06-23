
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using Unity.Profiling;
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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(Attack))]
public class AttackEditor : UnityEditor.Editor
{
    public AttackEditorData data;
    public int frame;

    

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
    private int spriteKeyIndexOffset = 100;
    private int hitboxKeyIndexOffset = 1000;
    private int posTimelineIndexOffset = 100;
    private int hitboxTimelineIndexOffset = 100;
    private int spriteTimelineIndexOffset = 200;

    private KeyFrame<PosKeyFrameData> lastSelectedPosKeyFrame;
    private KeyFrame<SpriteKeyFrameData> lastSelectedSpriteKeyFrame;
    private KeyFrame<HitboxKeyFrameData> lastSelectedHitboxKeyFrame;

    private static bool showMenu = false;
    private static Vector2 menuPosition;
    private int keyFrameCreationFrame = 1;
    private int hitboxFrameCreationTime = 1;
    private int hitboxFrameCreationLength = 1;

    private bool posFrameCreationWindow = false; // Flag to track window visibility
    private bool hitboxFrameCreationWindow = false; // Flag to track window visibility

    bool isDragging = false;

    Vector2 dragOffset;

    public override void OnInspectorGUI()
    {

        attack = (Attack)target;
        dummy = GameObject.FindGameObjectWithTag("Dummy").GetComponent<PlayerController>();



        frame = (int)EditorGUILayout.Slider(frame, 0, attack.GetTotalDuration());
         

        attack.DisplayAtFrame(dummy, frame, Vector3.zero);

        data.posKeyFrameColor = EditorGUILayout.ColorField("Pos Keys", data.posKeyFrameColor);
        data.bezierControlColor = EditorGUILayout.ColorField("Bezier Controls", data.bezierControlColor);
        data.spriteKeyFrameColor = EditorGUILayout.ColorField("Sprite Keys", data.spriteKeyFrameColor);
        data.speedIndicatorColor = EditorGUILayout.ColorField("Speed Indicators", data.speedIndicatorColor);
        data.hitboxColor = EditorGUILayout.ColorField("Hitboxes", data.hitboxColor);
        data.timelineColor = EditorGUILayout.ColorField("Timeline", data.timelineColor);
        data.selectedKeyFrameColor = EditorGUILayout.ColorField("Selected Keys", data.selectedKeyFrameColor);

        data.shouldDrawBezierControls = EditorGUILayout.Toggle("Draw Bezier Controls", data.shouldDrawBezierControls);
        data.shouldDrawTimeControls = EditorGUILayout.Toggle("Draw Time Controls", data.shouldDrawTimeControls);
        data.shouldDrawSpriteTimeControls = EditorGUILayout.Toggle("Draw Sprite Time Controls", data.shouldDrawSpriteTimeControls);
        data.shouldDrawSpeedIndicators = EditorGUILayout.Toggle("Draw Speed Indicators", data.shouldDrawSpeedIndicators);
        data.shouldDrawHitboxes = EditorGUILayout.Toggle("Draw hitboxes", data.shouldDrawHitboxes);
        data.shouldDrawSecondaryHitboxes = EditorGUILayout.Toggle("Draw secondary hitboxes", data.shouldDrawSecondaryHitboxes);

        data.speedIndicatorSpacing = EditorGUILayout.FloatField("Speed Indicator Spacing", data.speedIndicatorSpacing);
        data.speedIndicatorSpacing = Mathf.Clamp(data.speedIndicatorSpacing, 0.01f, float.MaxValue);
        data.speedIndicatorWidth = EditorGUILayout.FloatField("Speed Indicator Width", data.speedIndicatorWidth);
        data.speedIndicatorWidth = Mathf.Clamp(data.speedIndicatorWidth, 0.01f, 1);

        data.timelineHeight = EditorGUILayout.FloatField("Time Line Pos", data.timelineHeight);



        DrawDefaultInspector();

    }


   

    private void OnEnable()
    {
        //SceneView.duringSceneGui += OnSceneupdate;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

    }

    

    private void OnSceneGUI(SceneView sceneView)
    {
        attack = (Attack)target;

        int hoverIndex = -1;
        hoverIndex = HandleUtility.nearestControl;

        if (Event.current.type == EventType.DragPerform)
        {
            HandleDragAndDropSprites();
        }


        DrawPosCurves();
        DrawBezierControls();

        DrawTimelineControls();

        if (data.shouldDrawSpriteTimeControls) DrawSpriteTimeControls();
        if (data.shouldDrawSpeedIndicators) DrawSpeedIndicators();
        if (data.shouldDrawHitboxes) HandleHitBoxFrames();

        HandleSceneRightClicks();

        if (posFrameCreationWindow)
        {
            Rect windowRect = new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100);
            GUI.Window(0, windowRect, DrawPosCreator, "Edit Value");
        }
        if (hitboxFrameCreationWindow)
        {
            Rect windowRect = new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100);
            GUI.Window(1, windowRect, DrawHitboxCreator, "Edit Value");
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
            RecalculateSpriteKeyFrameIndices();
        }
        HandlePosKeyFrames(hoverIndex);
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            previousMousePosition = Event.current.mousePosition;
        }
    }

    private void HandleDragAndDropSprites()
    {
        foreach (var draggedObject in DragAndDrop.objectReferences)
        {
            if (draggedObject is Sprite)
            {
                Undo.RecordObject(attack, "Added Sprite KeyFrame");
                Undo.FlushUndoRecordObjects();

                Sprite draggedSprite = (Sprite)draggedObject;
                KeyFrame<SpriteKeyFrameData> newSpriteKeyFrame = new KeyFrame<SpriteKeyFrameData>();
                newSpriteKeyFrame.data.sprite = draggedSprite;
                if (attack.spriteKeyFrames.Count != 0)
                    newSpriteKeyFrame.frame = attack.spriteKeyFrames[attack.spriteKeyFrames.Count - 1].frame + 1;
                else
                    newSpriteKeyFrame.frame = 0;
                attack.spriteKeyFrames.Add(newSpriteKeyFrame);
                Event.current.Use();



            }
        }
    }

    private void DrawTimelineControls()
    {
        Vector2 startTimelinePos = new Vector2(data.timelineBeginX, data.timelineHeight);
        int index = 1;

        Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.timelineColor);
        CreateDotHandleCap(index, startTimelinePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);


        if (nearestHandle == index && Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition);
            data.timelineBeginX += move.x;
            data.timelineEndX += move.x;
            data.timelineHeight += move.y * -1;
        }

        Vector2 endTimelinePos = new Vector2(data.timelineEndX, data.timelineHeight);
        index = 2;
        
        Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.timelineColor);
        CreateDotHandleCap(index, endTimelinePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);


        if (nearestHandle == index && Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition);
            data.timelineEndX += move.x;
        }

        DrawPosTimeline();
        DrawSpriteTimeline();
        DrawHitboxTimeline();
    }
    private void DrawPosTimeline()
    {
        for (int i = 0; i < attack.posKeyFrames.Count; i++)
        {

            KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];
            int index = i + posKeyPositionIndexOffset + posKeyAfterControlIndexOffset + posKeyBeforeControlIndexOffset + spriteKeyIndexOffset + posTimelineIndexOffset;
            float keyFramePosX = (float)posKeyFrame.frame / attack.GetTotalDuration() * (data.timelineEndX - data.timelineBeginX) + data.timelineBeginX;
            Vector2 pos = new Vector2(keyFramePosX, data.timelineHeight + data.posTimelineOffset);

            Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.posKeyFrameColor);
            CreateDotHandleCap(index, pos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);


            if (nearestHandle == index && Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                lastSelectedPosKeyFrame = posKeyFrame;

                float mouseWorldPosX = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition).x;
                posKeyFrame.frame = CalculateFrameFromTimelinePos(mouseWorldPosX);
            }

        }

    }

    private void DrawSpriteTimeline()
    {
        for (int i = 0; i < attack.spriteKeyFrames.Count; i++)
        {

            KeyFrame<SpriteKeyFrameData> spriteKeyFrame = attack.spriteKeyFrames[i];
            int index = i + posKeyPositionIndexOffset + posKeyAfterControlIndexOffset + posKeyBeforeControlIndexOffset + spriteKeyIndexOffset + posTimelineIndexOffset + spriteTimelineIndexOffset;
            float keyFramePosX = CalculateTimelineXFromFrame(spriteKeyFrame.frame);
            Vector2 pos = new Vector2(keyFramePosX, data.timelineHeight + data.spriteTimelineOffset);

            if (Event.current.type == EventType.Repaint)
            {
                Texture2D texture = GetSlicedSpriteTexture(spriteKeyFrame.data.sprite);

                Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.spriteKeyFrameColor);
                CreateDotHandleCap(index, pos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);


                Handles.BeginGUI();

                Rect spriteRect = spriteKeyFrame.data.sprite.rect;
                spriteRect.width /= spriteKeyFrame.data.sprite.pixelsPerUnit;
                spriteRect.height /= spriteKeyFrame.data.sprite.pixelsPerUnit;
                spriteRect.center = pos;

                // Calc world rect to screen rect
                Vector2 minCornerScreenPos = HandleUtility.WorldToGUIPoint(spriteRect.min);
                Vector2 maxCornerScreenPos = HandleUtility.WorldToGUIPoint(spriteRect.max);

                Rect screenRect = new Rect();
                screenRect.min = new Vector2(minCornerScreenPos.x, maxCornerScreenPos.y);
                screenRect.max = new Vector2(maxCornerScreenPos.x, minCornerScreenPos.y);


                GUI.DrawTexture(screenRect, texture);


                if (GUI.Button(screenRect, "", GUIStyle.none))
                {
                    // Handle click interaction here.
                }

                Handles.EndGUI();
            }

            Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.spriteKeyFrameColor);
            CreateDotHandleCap(index, pos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);


            if (nearestHandle == index && Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                lastSelectedSpriteKeyFrame = spriteKeyFrame;

                float mouseWorldPosX = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition).x;

                spriteKeyFrame.frame = CalculateFrameFromTimelinePos(mouseWorldPosX);
            }

            

        }

    }

    private void DrawHitboxTimeline()
    {
        for (int i = 0; i < attack.hitboxKeyFrames.Count; i++)
        {

            KeyFrame<HitboxKeyFrameData> hitboxKeyFrame = attack.hitboxKeyFrames[i];
            int index = 2 * i + posKeyPositionIndexOffset + posKeyAfterControlIndexOffset + posKeyBeforeControlIndexOffset + spriteKeyIndexOffset + posTimelineIndexOffset + spriteTimelineIndexOffset + hitboxTimelineIndexOffset;
            float keyFramePosX = CalculateTimelineXFromFrame(hitboxKeyFrame.frame);

            // Start point
            Vector2 pos1 = new Vector2(keyFramePosX, data.timelineHeight + data.hitboxTimelineOffset + i * data.hitboxTimelineVerticalSpacing);

            Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor);
            CreateDotHandleCap(index, pos1, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (nearestHandle == index && Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                lastSelectedHitboxKeyFrame = hitboxKeyFrame;

                float mouseWorldPosX = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Vector2.Scale(new Vector2(1, -1), Event.current.mousePosition)
                        + new Vector2(0, SceneView.GetAllSceneCameras()[0].pixelHeight)).x;

                hitboxKeyFrame.frame = CalculateFrameFromTimelinePos(mouseWorldPosX);



            }

            //End point
            index++;

            keyFramePosX = CalculateTimelineXFromFrame(hitboxKeyFrame.frame + hitboxKeyFrame.data.length);

            Vector2 pos2 = new Vector2(keyFramePosX, data.timelineHeight + data.hitboxTimelineOffset + i * data.hitboxTimelineVerticalSpacing);

            Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor);
            CreateDotHandleCap(index, pos2, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (nearestHandle == index && Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                lastSelectedHitboxKeyFrame = hitboxKeyFrame;

                float mouseWorldPosX = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Vector2.Scale(new Vector2(1, -1), Event.current.mousePosition)
                        + new Vector2(0, SceneView.GetAllSceneCameras()[0].pixelHeight)).x;

                hitboxKeyFrame.data.length =  Mathf.Clamp(CalculateFrameFromTimelinePos(mouseWorldPosX) - hitboxKeyFrame.frame,
                    0, attack.GetTotalDuration() - hitboxKeyFrame.frame);
            }

            Handles.color = data.hitboxColor;
            Handles.DrawLine(pos1, pos2);

        }

    }

    private int CalculateFrameFromTimelinePos(float mouseWorldPosX)
    {
        float timelinePercent = (mouseWorldPosX - data.timelineBeginX) / (data.timelineEndX - data.timelineBeginX);
        int frame = (int)(timelinePercent * attack.GetTotalDuration());
        frame = Mathf.Clamp(frame, 0, int.MaxValue);
        return frame;
    }

    private float CalculateTimelineXFromFrame(int frame)
    {
        return (float)frame / (float)attack.GetTotalDuration() * (data.timelineEndX - data.timelineBeginX) + data.timelineBeginX;
    }



    private void DrawSpriteTimeControls()
    {
        for (int i = 0; i < attack.spriteKeyFrames.Count; i++)
        {

            KeyFrame<SpriteKeyFrameData> spriteKeyFrame = attack.spriteKeyFrames[i];
            Vector3 spritePos = attack.GetPosAtFrame(dummy, spriteKeyFrame.frame, Vector3.zero);
            int index = i + posKeyPositionIndexOffset + posKeyBeforeControlIndexOffset + posKeyAfterControlIndexOffset + spriteKeyIndexOffset;



            if (Event.current.type == EventType.Repaint)
            {
                Texture2D texture = GetSlicedSpriteTexture(spriteKeyFrame.data.sprite);

                Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? data.selectedKeyFrameColor : data.spriteKeyFrameColor);
                CreateDotHandleCap(index, spritePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);


                Handles.BeginGUI();

                Rect spriteRect = spriteKeyFrame.data.sprite.rect;
                spriteRect.width /= spriteKeyFrame.data.sprite.pixelsPerUnit;
                spriteRect.height /= spriteKeyFrame.data.sprite.pixelsPerUnit;
                spriteRect.center = spritePos;

                // Calc world rect to screen rect
                Vector2 minCornerScreenPos = HandleUtility.WorldToGUIPoint(spriteRect.min);
                Vector2 maxCornerScreenPos = HandleUtility.WorldToGUIPoint(spriteRect.max);

                Rect screenRect = new Rect();
                screenRect.min = new Vector2(minCornerScreenPos.x, maxCornerScreenPos.y);
                screenRect.max = new Vector2(maxCornerScreenPos.x, minCornerScreenPos.y);
                
                
                GUI.DrawTexture(screenRect, texture);


                if (GUI.Button(screenRect, "", GUIStyle.none))
                {
                    // Handle click interaction here.
                }

                Handles.EndGUI();
            }
            else if (Event.current.type == EventType.Layout)
            {
                CreateDotHandleCap(index, spritePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);
            }
            if (nearestHandle == index)
            {
                lastSelectedSpriteKeyFrame = spriteKeyFrame;

                

                if (i != 0 && (Event.current.type == EventType.MouseDrag && Event.current.button == 0))
                {
                    Vector2 mouseWorldPos = GetMouseWorldPos();

                    int nearestFrame = FindNearestFrameToPos(mouseWorldPos);

                    if (nearestFrame != -1) {
                        spriteKeyFrame.frame = nearestFrame;
                    }
                    // Vector2 move =  - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition);
                    // move.y *= -1;

                    // Vector2 velocity = attack.GetVelocityAtFrame(dummy, spriteKeyFrame.frame);
                    // if (velocity.x == 0) velocity.x = 0.0001f;

                    // float slope = velocity.y / velocity.x;

                    // Vector2 projectedMove = new Vector2((slope * move.y + move.x) / (slope * slope + 1), (slope * slope * move.y + slope * move.x) / (slope * slope + 1));

                    // float direction = Vector2.Dot(velocity.normalized, projectedMove.normalized);
                    // float timeChange = direction * (projectedMove / velocity).magnitude;

                    // Debug.Log(projectedMove);

                    // spriteKeyFrame.frame += timeChange;
                }


            }
        }
    }

    private Vector2 GetMouseWorldPos() {
        return SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Vector2.Scale(new Vector2(1, -1), Event.current.mousePosition)
                        + new Vector2(0, SceneView.GetAllSceneCameras()[0].pixelHeight));
    }

    private int FindNearestFrameToPos(Vector2 worldPos)
    {
        float minSqrDistance = float.MaxValue;
        int bestFrame = -1;
        for (int frame = 0; frame < attack.GetTotalDuration(); frame++)
        {
            Vector2 potentialNewPos = attack.GetPosAtFrame(dummy, frame, Vector2.zero);
            float sqrDistance = (worldPos - potentialNewPos).sqrMagnitude;

            Handles.DrawLine(worldPos, potentialNewPos);
            if (sqrDistance < minSqrDistance)
            {
                bestFrame = frame;
                minSqrDistance = sqrDistance;
            }
        }
        return bestFrame;
    }

   

    private void DrawSpeedIndicators() {
        int frame = 0;
        int maxFrame = attack.GetTotalDuration();
        Handles.color = data.speedIndicatorColor;
        while (frame < maxFrame)
        {
            Vector2 pos = attack.GetPosAtFrame(dummy, frame, Vector3.zero);
            Vector2 normalizedVelocity = attack.GetVelocityAtFrame(dummy, frame).normalized;
            Vector2 perpendicular = new Vector2(normalizedVelocity.y, -normalizedVelocity.x);
            Vector2 p1 = pos + perpendicular * data.speedIndicatorWidth;
            Vector2 p2 = pos - perpendicular * data.speedIndicatorWidth;

            Handles.DrawLine(p1, p2, 0.1f);
            frame ++;
        }
    }

    void DrawPosCreator(int windowID)
    {
        // Layout elements for editing the value
        GUILayout.Label("Current Value: " + keyFrameCreationFrame);
        keyFrameCreationFrame = EditorGUILayout.IntField(keyFrameCreationFrame, GUILayout.Width(100));

        // Button to confirm and close window
        if (GUILayout.Button("Create keyframe"))
        {
            CreateNewPosKeyFrame();
            posFrameCreationWindow = false;
        }

        // Button to cancel (optional)
        if (GUILayout.Button("Cancel"))
        {
          posFrameCreationWindow = false;
        }

        GUI.DragWindow(); // Allow dragging the window
    }
    void DrawHitboxCreator(int windowID)
    {
        // Layout elements for editing the value
        GUILayout.Label("Current Value: " + hitboxFrameCreationTime);
        keyFrameCreationFrame = EditorGUILayout.IntField(hitboxFrameCreationTime, GUILayout.Width(100));

        // Button to confirm and close window
        if (GUILayout.Button("Create keyframe"))
        {
            CreateNewHitboxKeyFrame();
            hitboxFrameCreationWindow = false;
        }

        // Button to cancel (optional)
        if (GUILayout.Button("Cancel"))
        {
            hitboxFrameCreationWindow = false;
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
        if (data.shouldDrawBezierControls)
        {
            for (int i = 0; i < attack.posKeyFrames.Count; i++)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];

                Handles.color = data.bezierControlColor;
                if (i != 0)
                Handles.DrawLine(posKeyFrame.data.pos, posKeyFrame.data.beforeBezierControlPoint);
                if (i != attack.posKeyFrames.Count - 1)
                    Handles.DrawLine(posKeyFrame.data.pos, posKeyFrame.data.afterBezierControlPoint);
            }
        }
    }


    private void HandleHitBoxFrames()
    {
        for (int i = 0; i < attack.hitboxKeyFrames.Count; i++)
        {
            int index = 11 * i + posKeyPositionIndexOffset + posKeyAfterControlIndexOffset + posKeyBeforeControlIndexOffset + spriteKeyIndexOffset + hitboxKeyIndexOffset;
            KeyFrame<HitboxKeyFrameData> hitboxKeyFrame = attack.hitboxKeyFrames[i];

            Vector2 currentAttackPos = attack.GetPosAtFrame(dummy, hitboxKeyFrame.frame, Vector2.zero);

            float handleSize = 0.03f;

            // Draw the rectangle
            Handles.color = data.hitboxColor; // Example color
            Handles.DrawWireCube(hitboxKeyFrame.data.rect.position + hitboxKeyFrame.data.rect.size * 0.5f + currentAttackPos, hitboxKeyFrame.data.rect.size); // Use half size and center for drawing
            int timeStep = 0;
            while (data.shouldDrawSecondaryHitboxes && timeStep < hitboxKeyFrame.data.length)
            {
                timeStep++; // increment by one frame
                Vector2 currentPos = attack.GetPosAtFrame(dummy, hitboxKeyFrame.frame + timeStep, Vector2.zero);
                Handles.DrawWireCube(hitboxKeyFrame.data.rect.position + hitboxKeyFrame.data.rect.size * 0.5f + currentPos, hitboxKeyFrame.data.rect.size); // Use half size and center for drawing

            }

            #region BoxHandles

            // Bottom left corner
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            Vector2 handlePos = new Vector2(hitboxKeyFrame.data.rect.xMin, hitboxKeyFrame.data.rect.yMin) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    Debug.Log(move);
                    hitboxKeyFrame.data.rect.xMin += move.x;
                    hitboxKeyFrame.data.rect.yMin += move.y;
                }
            }
            index++;

            // Bottom edge
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2((hitboxKeyFrame.data.rect.xMax + hitboxKeyFrame.data.rect.xMin) * 0.5f, hitboxKeyFrame.data.rect.yMin) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.yMin += move.y;
                }
            }
            index++;

            // Bottom right corner
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2(hitboxKeyFrame.data.rect.xMax, hitboxKeyFrame.data.rect.yMin) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1,-1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.xMax += move.x;
                    hitboxKeyFrame.data.rect.yMin += move.y;
                }
            }
            index++;

            // Left Edge
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2(hitboxKeyFrame.data.rect.xMin, (hitboxKeyFrame.data.rect.yMin + hitboxKeyFrame.data.rect.yMax) * 0.5f) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.xMin += move.x;
                }
            }
            index++;

            // Middle
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2((hitboxKeyFrame.data.rect.xMin + hitboxKeyFrame.data.rect.xMax) * 0.5f, (hitboxKeyFrame.data.rect.yMin + hitboxKeyFrame.data.rect.yMax) * 0.5f) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.x += move.x;
                    hitboxKeyFrame.data.rect.y += move.y;
                }
            }
            index++;

            // Right Edge
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2(hitboxKeyFrame.data.rect.xMax, (hitboxKeyFrame.data.rect.yMin + hitboxKeyFrame.data.rect.yMax) * 0.5f) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.xMax += move.x;
                }
            }
            index++;

            // Top Left Corner
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2(hitboxKeyFrame.data.rect.xMin, hitboxKeyFrame.data.rect.yMax) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.xMin += move.x;
                    hitboxKeyFrame.data.rect.yMax += move.y;
                }
            }
            index++;

            // Top Edge
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2((hitboxKeyFrame.data.rect.xMin + hitboxKeyFrame.data.rect.xMax) * 0.5f, hitboxKeyFrame.data.rect.yMax) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.yMax += move.y;
                }
            }
            index++;

            // Top Right Corner
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = new Vector2(hitboxKeyFrame.data.rect.xMax, hitboxKeyFrame.data.rect.yMax) + currentAttackPos;
            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), handleSize, Event.current.type);

            if (nearestHandle == index)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 move = Vector2.Scale(new Vector2(1, -1), (SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition)));
                    hitboxKeyFrame.data.rect.xMax += move.x;
                    hitboxKeyFrame.data.rect.yMax += move.y;
                }
            }
            index++;

            #endregion

            // First time handle
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = currentAttackPos;

            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (nearestHandle == index)
            {
                lastSelectedHitboxKeyFrame = hitboxKeyFrame;
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {

                    Vector2 mouseWorldPos = GetMouseWorldPos();
                    int bestFrame = FindNearestFrameToPos(mouseWorldPos);

                    if (bestFrame != -1) {
                        hitboxKeyFrame.frame = bestFrame;
                    }
                }
            }
            index++;

            // Second Time handle
            Handles.color = nearestHandle == index ? data.selectedKeyFrameColor : data.hitboxColor;
            handlePos = attack.GetPosAtFrame(dummy, hitboxKeyFrame.frame + hitboxKeyFrame.data.length, Vector2.zero);

            CreateDotHandleCap(index, handlePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (nearestHandle == index)
            {
                lastSelectedHitboxKeyFrame = hitboxKeyFrame;
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    Vector2 mouseWorldPos = GetMouseWorldPos();
                    int bestFrame = FindNearestFrameToPos(mouseWorldPos);

                    if (bestFrame != -1)
                    {
                        hitboxKeyFrame.data.length = bestFrame - hitboxKeyFrame.frame;
                    }

                }
            }
            index++;

        }
    }


    private void HandlePosKeyFrames(int hoverIndex)
    {
        for (int i = 0; i < attack.posKeyFrames.Count; i++)
        {
            int index = posKeyPositionIndexOffset + i;
            KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];
            Handles.color = hoverIndex == index ? data.selectedKeyFrameColor : data.posKeyFrameColor;
            CreateDotHandleCap(index, posKeyFrame.data.pos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

            if (nearestHandle == index)
            {
                lastSelectedPosKeyFrame = posKeyFrame;

                if (i != 0 && (Event.current.type == EventType.MouseDrag && Event.current.button == 0))
                {
                    Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition);
                    Debug.Log(move);
                    posKeyFrame.data.pos += new Vector2(move.x, -move.y);
                    posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
                    posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);


                }
            }

            if (data.shouldDrawTimeControls) HandlePosKeyFrameTime(i);
        }

        if (data.shouldDrawBezierControls)
        {
            for (int i = 0; i < attack.posKeyFrames.Count; i++)
            {
                DrawBezierControlsForPoint(i, hoverIndex, attack.posKeyFrames[i]);
            }
        }

        if (Event.current.type == EventType.MouseDrag) SceneView.RepaintAll();


    }
    private void DrawBezierControlsForPoint(int i, int hoverIndex, KeyFrame<PosKeyFrameData> posKeyFrame)
    {
        int index = i + posKeyPositionIndexOffset;
        int beforeControlIndex = index + posKeyBeforeControlIndexOffset;
        int afterControlIndex = beforeControlIndex + posKeyAfterControlIndexOffset;
        if (i != 0)
        {
            Handles.color = (hoverIndex == index || hoverIndex == beforeControlIndex) ? data.selectedKeyFrameColor : data.bezierControlColor;
            CreateDotHandleCap(beforeControlIndex, posKeyFrame.data.beforeBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);
        }

        if (i != attack.posKeyFrames.Count - 1)
        {
            Handles.color = (hoverIndex == index || hoverIndex == afterControlIndex) ? data.selectedKeyFrameColor : data.bezierControlColor;
            CreateDotHandleCap(afterControlIndex, posKeyFrame.data.afterBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);
        }

        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == beforeControlIndex)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition); posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
        }
        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == afterControlIndex)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition); posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);
        }
    }

    private void HandlePosKeyFrameTime(int posKeyFrameIndex)
    {
        KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[posKeyFrameIndex];

        Vector3 position = posKeyFrame.data.pos + Vector2.up * 0.5f;

        // 2. Draw the Input Field and Handle Events
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(HandleUtility.WorldToGUIPoint(position), Vector2.one * 150));

        EditorGUI.BeginChangeCheck(); // Start checking for changes

        int newFrame = EditorGUILayout.IntField("Frame: " + posKeyFrame.frame, posKeyFrame.frame);
        newFrame = Mathf.Clamp(newFrame, 1, int.MaxValue);
        if (posKeyFrameIndex != 0) // Make first frame uneditable
        {
            int previousPosKeyFrameTime = attack.posKeyFrames[posKeyFrameIndex - 1].frame;
            if (previousPosKeyFrameTime < newFrame)
            {
                for (int i = 0; i < attack.spriteKeyFrames.Count; i++)
                {
                    KeyFrame<SpriteKeyFrameData> spriteKeyFrame = attack.spriteKeyFrames[i];
                    if (spriteKeyFrame.frame > previousPosKeyFrameTime && spriteKeyFrame.frame < posKeyFrame.frame)
                    {
                        float spriteTimePercentage = (spriteKeyFrame.frame - previousPosKeyFrameTime) / (posKeyFrame.frame - previousPosKeyFrameTime);
                        int newSpriteTime = (int)spriteTimePercentage * (newFrame - previousPosKeyFrameTime) + previousPosKeyFrameTime;
                        //spriteKeyFrame.frame = newSpriteTime;
                    }
                }

                for (int i = 0; i < attack.hitboxKeyFrames.Count; i++)
                {
                    KeyFrame<HitboxKeyFrameData> hitboxKeyFrame = attack.hitboxKeyFrames[i];
                    if (hitboxKeyFrame.frame > previousPosKeyFrameTime && hitboxKeyFrame.frame < posKeyFrame.frame)
                    {
                        //Start time
                        float hitboxTimeStartPercentage = (hitboxKeyFrame.frame - previousPosKeyFrameTime) / (posKeyFrame.frame - previousPosKeyFrameTime);
                        int newHitboxStartTime = (int)hitboxTimeStartPercentage * (newFrame - previousPosKeyFrameTime) + previousPosKeyFrameTime;
                        //hitboxKeyFrame.frame = newHitboxStartTime;

                        //End time / length
                        float hitboxTimeEndPercentage = (hitboxKeyFrame.frame + hitboxKeyFrame.data.length - previousPosKeyFrameTime) / (posKeyFrame.frame - previousPosKeyFrameTime);
                        int newHitboxEndTime = (int)hitboxTimeEndPercentage * (newFrame - previousPosKeyFrameTime) + previousPosKeyFrameTime;
                        //hitboxKeyFrame.data.length = newHitboxEndTime - hitboxKeyFrame.frame;
                    }
                }
            }

            if (attack.posKeyFrames.Count > posKeyFrameIndex + 1)
            {
                int nextPosKeyFrameTime = attack.posKeyFrames[posKeyFrameIndex + 1].frame;
                if (nextPosKeyFrameTime > newFrame)
                {
                    for (int i = 0; i < attack.spriteKeyFrames.Count; i++)
                    {
                        KeyFrame<SpriteKeyFrameData> spriteKeyFrame = attack.spriteKeyFrames[i];
                        if (spriteKeyFrame.frame < nextPosKeyFrameTime && spriteKeyFrame.frame > posKeyFrame.frame)
                        {
                            float spriteTimePercentage = (spriteKeyFrame.frame - posKeyFrame.frame) / (nextPosKeyFrameTime - posKeyFrame.frame);
                            int newSpriteTime = (int)spriteTimePercentage * (nextPosKeyFrameTime - newFrame) + newFrame;
                            Debug.Log(newSpriteTime);
                            //spriteKeyFrame.frame = newSpriteTime;
                        }
                    }

                    for (int i = 0; i < attack.hitboxKeyFrames.Count; i++)
                    {
                        KeyFrame<HitboxKeyFrameData> hitboxKeyFrame = attack.hitboxKeyFrames[i];
                        if (hitboxKeyFrame.frame < nextPosKeyFrameTime && hitboxKeyFrame.frame > posKeyFrame.frame)
                        {
                            //Start time
                            float hitboxTimeStartPercentage = (hitboxKeyFrame.frame - posKeyFrame.frame) / (nextPosKeyFrameTime - posKeyFrame.frame );
                            int newHitboxStartTime = (int)hitboxTimeStartPercentage * (nextPosKeyFrameTime - newFrame) + posKeyFrame.frame;
                            //hitboxKeyFrame.frame = newHitboxStartTime;

                            //End time / length
                            float hitboxTimeEndPercentage = ((hitboxKeyFrame.frame + hitboxKeyFrame.data.length) - posKeyFrame.frame) / (nextPosKeyFrameTime - posKeyFrame.frame);
                            int newHitboxEndTime = (int)hitboxTimeEndPercentage * (nextPosKeyFrameTime - newFrame ) + posKeyFrame.frame;
                            //hitboxKeyFrame.data.length = newHitboxEndTime - hitboxKeyFrame.frame;
                        }
                    }
                }

            }

            posKeyFrame.frame = newFrame; 
        }

        if (EditorGUI.EndChangeCheck()) // Check if the value changed
        {
            Undo.RecordObject(target, "Modified Time Value");
            posKeyFrame.frame = Mathf.Clamp(posKeyFrame.frame, 0, int.MaxValue);
        }
        


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
                if (posKeyFrame1.frame > posKeyFrame2.frame) {
                    attack.posKeyFrames[i] = posKeyFrame2;
                    attack.posKeyFrames[i + 1] = posKeyFrame1;
                    swapped = true;
                }
            }
            
        }
    }
    private void RecalculateSpriteKeyFrameIndices()
    {
        bool swapped = true;
        while (swapped)
        {
            swapped = false;
            for (int i = 0; i < attack.spriteKeyFrames.Count - 1; i++)
            {
                KeyFrame<SpriteKeyFrameData> spriteKeyFrame1 = attack.spriteKeyFrames[i];
                KeyFrame<SpriteKeyFrameData> spriteKeyFrame2 = attack.spriteKeyFrames[i + 1];
                if (spriteKeyFrame1.frame > spriteKeyFrame2.frame)
                {
                    attack.spriteKeyFrames[i] = spriteKeyFrame2;
                    attack.spriteKeyFrames[i + 1] = spriteKeyFrame1;
                    swapped = true;
                }
            }

        }
    }

    #region Right Click Menu

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
            menu.AddItem(new GUIContent("Create PosKeyFrame"), false, OpenPosFrameCreationWindow);
            menu.AddItem(new GUIContent("Create HitboxKeyFrame"), false, OpenHitboxFrameCreationWindow);
            menu.AddItem(new GUIContent("Delete last selected PosKeyFrame"), false, DeleteSelectedPosKeyFrame);
            menu.AddItem(new GUIContent("Delete last selected SpriteKeyFrame"), false, DeleteSelectedSpriteKeyFrame);
            menu.AddItem(new GUIContent("Delete last selected HitboxKeyFrame"), false, DeleteSelectedHitboxKeyFrame);


            // Show the menu at the mouse position
            menu.ShowAsContext();
            showMenu = false;
        }
    }


    private void OpenPosFrameCreationWindow()
    {
        if (attack.posKeyFrames.Count > 0)
        {
            keyFrameCreationFrame = attack.posKeyFrames[attack.posKeyFrames.Count - 1].frame + 1;
        }
        else keyFrameCreationFrame = 0;
        posFrameCreationWindow = true;
    }

    private void OpenHitboxFrameCreationWindow()
    {
        hitboxFrameCreationWindow = true;
    }

    private void CreateNewPosKeyFrame()
    {
        // Undo
        Undo.RegisterCompleteObjectUndo(attack, "HandleTransform");
        Undo.FlushUndoRecordObjects();

        Vector2 pos = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(menuPosition * new Vector2(1, -1)
            + new Vector2(0, SceneView.GetAllSceneCameras()[0].pixelHeight));
        Vector2 afterBezierControlPoint = pos + Vector2.right;
        Vector2 beforeBezierControlPoint = pos + Vector2.left;
        int frame = keyFrameCreationFrame;
        attack.AddPosKeyFrame(frame, pos, beforeBezierControlPoint, afterBezierControlPoint);
    }

    private void CreateNewHitboxKeyFrame()
    {
        // Undo
        Undo.RegisterCompleteObjectUndo(attack, "HandleTransform");
        Undo.FlushUndoRecordObjects();

        attack.AddHitboxKeyFrame(hitboxFrameCreationTime, hitboxFrameCreationLength);
    }
    private void DeleteSelectedPosKeyFrame()
    {
        if (lastSelectedPosKeyFrame != null) { attack.posKeyFrames.Remove(lastSelectedPosKeyFrame); }
        else { Debug.Log("Keyframe is null :("); }
        Debug.Log("Trying to delete " + lastSelectedPosKeyFrame.frame);
        
    }
    private void DeleteSelectedSpriteKeyFrame()
    {
        if (lastSelectedSpriteKeyFrame != null) { attack.spriteKeyFrames.Remove(lastSelectedSpriteKeyFrame); }
        else { Debug.Log("Keyframe is null :("); }

    }

    private void DeleteSelectedHitboxKeyFrame()
    {
        if (lastSelectedHitboxKeyFrame != null) { attack.hitboxKeyFrames.Remove(lastSelectedHitboxKeyFrame); }
        else { Debug.Log("Keyframe is null :("); }

    }

    #endregion

   

    void CreateDotHandleCap(int id, Vector3 position, Quaternion rotation, float size, EventType eventType)
    {
        Handles.DotHandleCap(id, position, rotation, size, eventType);
    }


    Texture2D GetSlicedSpriteTexture(Sprite sprite)
    {
        Rect rect = sprite.rect;
        Texture2D slicedTex = new Texture2D((int)rect.width, (int)rect.height);
        slicedTex.filterMode = sprite.texture.filterMode;

        slicedTex.SetPixels(0, 0, (int)rect.width, (int)rect.height, sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
        slicedTex.Apply();

        return slicedTex;
    }
}
