
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections;
using System.Drawing.Printing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public static Color spriteKeyFrameColor = Color.cyan;
    public static Color speedIndicatorColor = Color.yellow;
    public static Color selectedKeyFrameColor = Color.magenta;
    public static bool shouldDrawBezierControls = true;
    public static bool shouldDrawTimeControls = true;
    public static bool shouldDrawSpriteTimeControls = true;
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
    private int spriteKeyIndexOffset = 100;

    private KeyFrame<PosKeyFrameData> lastSelectedPosKeyFrame;
    private KeyFrame<SpriteKeyFrameData> lastSelectedSpriteKeyFrame;

    private static bool showMenu = false;
    private static Vector2 menuPosition;
    private float keyFrameCreationTime = 1;

    private bool showFrameTimeEditor = false; // Flag to track window visibility

    bool isDragging = false;

    Vector2 dragOffset;

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




        posKeyFrameColor = EditorGUILayout.ColorField("Pos Keys", posKeyFrameColor);
        bezierControlColor = EditorGUILayout.ColorField("Bezier Controls", bezierControlColor);
        spriteKeyFrameColor = EditorGUILayout.ColorField("Sprite Keys", spriteKeyFrameColor);
        speedIndicatorColor = EditorGUILayout.ColorField("Speed Indicators", speedIndicatorColor);
        selectedKeyFrameColor = EditorGUILayout.ColorField("Selected Keys", selectedKeyFrameColor);
        shouldDrawBezierControls = EditorGUILayout.Toggle("Draw Bezier Controls", shouldDrawBezierControls);
        shouldDrawTimeControls = EditorGUILayout.Toggle("Draw Time Controls", shouldDrawTimeControls);
        shouldDrawSpriteTimeControls = EditorGUILayout.Toggle("Draw Sprite Time Controls", shouldDrawSpriteTimeControls);
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

        if (Event.current.type == EventType.DragPerform)
        {
            HandleDragAndDropSprites();
        }


        DrawPosCurves();
        DrawBezierControls();

        if (shouldDrawSpriteTimeControls) DrawSpriteTimeControls();
        if (shouldDrawSpeedIndicators) DrawSpeedIndicators();

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
                newSpriteKeyFrame.time = attack.spriteKeyFrames[attack.spriteKeyFrames.Count - 1].time + 0.1f;
                attack.spriteKeyFrames.Add(newSpriteKeyFrame);
                Event.current.Use();



            }
        }
    }

    private void DrawSpriteTimeControls()
    {
        for (int i = 0; i < attack.spriteKeyFrames.Count; i++)
        {

            KeyFrame<SpriteKeyFrameData> spriteKeyFrame = attack.spriteKeyFrames[i];
            Vector3 spritePos = attack.GetPosAtTime(dummy, spriteKeyFrame.time, Vector3.zero);
            int index = i + posKeyPositionIndexOffset + posKeyBeforeControlIndexOffset + posKeyAfterControlIndexOffset + spriteKeyIndexOffset;


            //Handles.DrawSolidDisc(spritePos, Vector3.back, 0.1f);

            if (Event.current.type == EventType.Repaint)
            {
                Texture2D texture = GetSlicedSpriteTexture(spriteKeyFrame.data.sprite);

                Handles.color = new Color(1, 1, 1, 0.5f) * (nearestHandle == index ? selectedKeyFrameColor : spriteKeyFrameColor);
                CreateDotHandleCap(index, spritePos, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

                Handles.BeginGUI();

                Vector2 guiPosition = HandleUtility.WorldToGUIPoint(spritePos);
                Rect handleRect = new Rect(guiPosition - new Vector2(texture.width / 2, texture.height / 2),
                    new Vector2(texture.width, texture.height));
                GUI.DrawTexture(handleRect, texture);

                if (GUI.Button(handleRect, "", GUIStyle.none))
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


                    Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition);
                    move.y *= -1;

                    Vector2 velocity = attack.GetVelocityAtTime(dummy, spriteKeyFrame.time);
                    if (velocity.x == 0) velocity.x = 0.0001f;

                    float slope = velocity.y / velocity.x;

                    Vector2 projectedMove = new Vector2((slope * move.y + move.x) / (slope * slope + 1), (slope * slope * move.y + slope * move.x) / (slope * slope + 1));

                    float direction = Vector2.Dot(velocity.normalized, projectedMove.normalized);
                    float timeChange = direction * (projectedMove / velocity).magnitude;

                    Debug.Log(projectedMove);

                    spriteKeyFrame.time += timeChange;
                }


            }
        }
    }

    private void DrawSpeedIndicators() {
        float time = 0;
        float maxTime = attack.GetTotalDuration();
        Handles.color = speedIndicatorColor;
        while (time < maxTime)
        {
            Vector2 pos = attack.GetPosAtTime(dummy, time, Vector3.zero);
            Vector2 normalizedVelocity = attack.GetVelocityAtTime(dummy, time).normalized;
            Vector2 perpendicular = new Vector2(normalizedVelocity.y, -normalizedVelocity.x);
            Vector2 p1 = pos + perpendicular * speedIndicatorWidth;
            Vector2 p2 = pos - perpendicular * speedIndicatorWidth;

            Handles.DrawLine(p1, p2, 0.1f);
            Handles.DrawLine(pos, pos + normalizedVelocity);
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

        Vector3 position = posKeyFrame.data.pos + Vector2.up * 0.5f;

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
            menu.AddItem(new GUIContent("Delete last selected SpriteKeyFrame"), false, DeleteSelectedSpriteKeyFrame);


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
        if (lastSelectedPosKeyFrame != null) { attack.posKeyFrames.Remove(lastSelectedPosKeyFrame); }
        else { Debug.Log("Keyframe is null :("); }
        Debug.Log("Trying to delete " + lastSelectedPosKeyFrame.time);
        
    }
    private void DeleteSelectedSpriteKeyFrame()
    {
        if (lastSelectedSpriteKeyFrame != null) { attack.spriteKeyFrames.Remove(lastSelectedSpriteKeyFrame); }
        else { Debug.Log("Keyframe is null :("); }

    }

    private void DrawBezierControlsForPoint(int i, int hoverIndex, KeyFrame<PosKeyFrameData> posKeyFrame)
    {
        int index = i + posKeyPositionIndexOffset;
        int beforeControlIndex = index + posKeyBeforeControlIndexOffset;
        int afterControlIndex = beforeControlIndex + posKeyAfterControlIndexOffset;
        Handles.color = (hoverIndex == index || hoverIndex == beforeControlIndex) ? selectedKeyFrameColor : bezierControlColor;
        CreateDotHandleCap(beforeControlIndex, posKeyFrame.data.beforeBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

        Handles.color = (hoverIndex == index || hoverIndex == afterControlIndex) ? selectedKeyFrameColor : bezierControlColor;
        CreateDotHandleCap(afterControlIndex, posKeyFrame.data.afterBezierControlPoint, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.1f, Event.current.type);

        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == beforeControlIndex)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition); posKeyFrame.data.beforeBezierControlPoint += new Vector2(move.x, -move.y);
        }
        if ((Event.current.type == EventType.MouseDrag && Event.current.button == 0) && nearestHandle == afterControlIndex)
        {
            Vector2 move = SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Event.current.mousePosition) - SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(previousMousePosition); posKeyFrame.data.afterBezierControlPoint += new Vector2(move.x, -move.y);
        }
    }

    void CreateDotHandleCap(int id, Vector3 position, Quaternion rotation, float size, EventType eventType)
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

    private Texture2D GetTextureFromSprite(Sprite sprite)
    {
        return GetTextureFromRect(sprite.texture, sprite.rect);
    }

    public static Texture2D GetSpriteTexture(Sprite sprite)
    {
        Texture2D originalTexture = sprite.texture;
        Rect textureRect = sprite.textureRect;

        // Adjust textureRect to account for atlas packing
        textureRect.x += sprite.rect.x;
        textureRect.y += sprite.rect.y;

        // Create a new Texture2D to store the extracted portion
        Texture2D extractedTexture = new Texture2D((int)textureRect.width, (int)textureRect.height);

        // Read the pixels from the original texture within the sprite's rectangle
        extractedTexture.ReadPixels(textureRect, 0, 0);
        extractedTexture.Apply(); // Apply changes to the extracted texture

        return extractedTexture;
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
