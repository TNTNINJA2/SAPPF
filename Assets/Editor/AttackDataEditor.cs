using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static Codice.CM.WorkspaceServer.DataStore.WkTree.WriteWorkspaceTree;
using static PlasticPipe.Server.MonitorStats;

[CustomEditor(typeof(AttackSegmentData))]
public class AttackDataEditor : UnityEditor.Editor
{
    public AttackEditorData data;

    private AttackSegmentData attackData; // Reference to the edited object
    private int selectedFrameIndex = 0; // Currently viewed frame

    private AttackFrame clipBoardFrame;

    private AttackFrame selectedFrame
    {
        get
        {
            return attackData.frames[selectedFrameIndex];
        }
    }

    private int spritePaletteSize = 100;

    private Vector2 spritePaletteScrollPosition; // For scrolling if palette gets too large
    private bool showSpritePalette = true; // Whether to show/hide the palette

    private GUIStyle spriteButtonStyle; // Style for sprite buttons


    private float handleScalar
    {
        get
        {
            return SceneView.GetAllSceneCameras()[0].orthographicSize;
        }
    }

    private void OnEnable()
    {
        attackData = (AttackSegmentData)target;

        //SceneView.duringSceneGui += OnSceneupdate;
        SceneView.duringSceneGui += OnSceneGUI;



    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

    }

    private void OnSceneGUI(SceneView sceneView)
    {

        if (attackData.frames.Count > 0)
        {
            if (data.showAllFrames)
            {
                DrawLineFromOriginToFirstFrame();
                for (int frameIndex = 0; frameIndex < attackData.frames.Count; frameIndex++) //Loop for sprites
                {
                    AttackFrame frame = attackData.frames[frameIndex];

                    DrawFrameSpriteInWorldSpace(frameIndex);
                }
                for (int frameIndex = 0; frameIndex < attackData.frames.Count; frameIndex++) // Loop for Handles and Lines
                {
                    AttackFrame frame = attackData.frames[frameIndex];

                    DrawFramePositionHandle(frameIndex);

                    DrawLineToNextFrame(frameIndex);

                }
                DrawEndVelocityControl();
            }
            else
            {
                for (int frameIndex = 0; frameIndex < attackData.frames.Count; frameIndex++)
                {
                    int difference = frameIndex - selectedFrameIndex;
                    if (Mathf.Abs(difference) <= data.numberOfSurroundingFrames)
                    {
                        if (difference > 0)
                        {
                            DrawLineToNextFrame(frameIndex - 1);
                        }

                        if (difference < 0)
                        {
                            DrawLineToNextFrame(frameIndex);
                        } 
                        DrawFrameSpriteInWorldSpace(frameIndex);
                        if (frameIndex == 0)
                        {
                            DrawLineFromOriginToFirstFrame();
                        }

                        DrawHitboxesForFrame(frameIndex);

                    }
                }
                AttackFrame frame = attackData.frames[selectedFrameIndex];

                DrawFrameSpriteInWorldSpace(selectedFrameIndex);
                DrawFramePositionHandle(selectedFrameIndex);

                DrawHitboxesForFrame(selectedFrameIndex);


            }
        }

        if (Event.current.type == EventType.DragPerform)
        {
            HandleDragAndDropSprites();
        }

        if (Event.current.type == EventType.KeyDown)
        {
            CheckForKeyPresses();
        }



    }

    private void HandleDragAndDropSprites()
    {
        foreach (var draggedObject in DragAndDrop.objectReferences)
        {
            if (draggedObject is Sprite)
            {
                Undo.RecordObject(attackData, "Added Sprite KeyFrame");
                Undo.FlushUndoRecordObjects();

                Sprite draggedSprite = (Sprite)draggedObject;
                attackData.spritePalette.Add(draggedSprite);
                Event.current.Use();

            }
        }
    }

    private void CheckForKeyPresses()
    {
        if(Event.current.control && Event.current.keyCode == KeyCode.D)
        {
            DuplicateFrame(selectedFrameIndex);
            EditorUtility.SetDirty(target);
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.Delete)
        {
            DeleteFrame(selectedFrameIndex);
            EditorUtility.SetDirty(target);
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.RightArrow || (!Event.current.control && Event.current.keyCode == KeyCode.D))
        {
            selectedFrameIndex++;
            selectedFrameIndex = Mathf.Clamp(selectedFrameIndex, 0, attackData.frames.Count -1);
            EditorUtility.SetDirty(target);
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.LeftArrow || Event.current.keyCode == KeyCode.A)
        {
            selectedFrameIndex--;
            selectedFrameIndex = Mathf.Clamp(selectedFrameIndex, 0, attackData.frames.Count -1);
            EditorUtility.SetDirty(target);
            Event.current.Use();
        }
        if (Event.current.control && Event.current.keyCode == KeyCode.C)
        {
            clipBoardFrame = attackData.frames[selectedFrameIndex].Duplicate();
            Event.current.Use();
        }
        if (Event.current.control && Event.current.keyCode == KeyCode.V)
        {
            PasteFrame();
            EditorUtility.SetDirty(target);
            Event.current.Use();
        }
    }

    private void PasteFrame()
    {
        Undo.RecordObject(target, "Modified Frame Position");

        clipBoardFrame.position = attackData.frames[selectedFrameIndex].position;
        attackData.frames[selectedFrameIndex] = clipBoardFrame.Duplicate();
    }
    private void DuplicateFrame(int frameIndex)
    {
        Undo.RecordObject(target, "Modified Frame Position");
        AttackFrame newFrame = attackData.frames[frameIndex].Duplicate();
        attackData.frames.Insert(frameIndex + 1, newFrame);
        selectedFrameIndex++;
    }

    private void DeleteFrame(int frameIndex)
    {
        Undo.RecordObject(target, "Modified Frame Position");
        AttackFrame frame = attackData.frames[frameIndex];
        attackData.frames.RemoveAt(frameIndex);
    }


    private void DrawFramePositionHandle(int frameIndex)
    {
        AttackFrame frame = attackData.frames[frameIndex];

        EditorGUI.BeginChangeCheck();

        if (frame.hitboxes.Count > 0)
        {
            Handles.color = data.hitboxColor;
        } else
        {
            Handles.color = data.posKeyFrameColor;
        }
        if (frameIndex != selectedFrameIndex)
        {
            float fade = (data.unselectedFade + 1) / 2;
            Handles.color *= new Color(1, 1, 1, fade);
        }
        var handleRotation = Quaternion.identity;
        Vector3 newFramePosition = Handles.FreeMoveHandle(
            frame.position, ConvertHandleSize(0.07f), Vector3.zero, Handles.CircleHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Modified Frame Position");
            // Update the position offset in the AttackData
            frame.position = newFramePosition;
            attackData.frames[frameIndex] = frame;
            selectedFrameIndex = frameIndex;
            
            EditorUtility.SetDirty(target); // Mark the asset as dirty to save changes
        }
    }

    private void DrawFrameSpriteInWorldSpace(int frameIndex)
    {
        AttackFrame frame = attackData.frames[frameIndex];
        if (frame.sprite != null)
        {
            Handles.BeginGUI();

            Rect spriteRect = frame.sprite.rect;
            spriteRect.width /= frame.sprite.pixelsPerUnit;
            spriteRect.height /= frame.sprite.pixelsPerUnit;
            spriteRect.center = frame.position; 

            // Calc world rect to screen rect
            Vector2 minCornerScreenPos = HandleUtility.WorldToGUIPoint(spriteRect.min);
            Vector2 maxCornerScreenPos = HandleUtility.WorldToGUIPoint(spriteRect.max);

            Rect screenRect = new Rect();
            screenRect.min = new Vector2(minCornerScreenPos.x, maxCornerScreenPos.y);
            screenRect.max = new Vector2(maxCornerScreenPos.x, minCornerScreenPos.y);

            Texture2D texture = GetSlicedSpriteTexture(frame.sprite);
            if (frameIndex != selectedFrameIndex)
            {
                GUI.color = data.unselectedColor;
            } else
            {
                GUI.color = Color.white;
            }
            GUI.DrawTexture(screenRect, texture);

            Handles.EndGUI();
        }
    }

    private void DrawHitboxesForFrame(int frameIndex)
    {
        AttackFrame frame = attackData.frames[frameIndex];
        for (int hitboxIndex = 0; hitboxIndex < frame.hitboxes.Count; hitboxIndex++)
        {
            Rect hitboxRect = frame.hitboxes[hitboxIndex].rect;

            if (frameIndex == selectedFrameIndex)
            {
                float handleSize = 0.05f;

                Handles.color = data.hitboxColor;
                EditorGUI.BeginChangeCheck();

                // Center
                hitboxRect.center = Handles.FreeMoveHandle(hitboxRect.center + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap) - (Vector3)frame.position;

                // Min and Max corners
                hitboxRect.min = Handles.FreeMoveHandle(hitboxRect.min + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap) - (Vector3)frame.position;
                hitboxRect.max = Handles.FreeMoveHandle(hitboxRect.max + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap) - (Vector3)frame.position;

                // Other corners
                Vector2 newBottomRight = Handles.FreeMoveHandle(new Vector2(hitboxRect.xMax, hitboxRect.yMin) + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap) - (Vector3)frame.position;
                hitboxRect.xMax = newBottomRight.x;
                hitboxRect.yMin = newBottomRight.y;

                Vector2 newTopLeft = Handles.FreeMoveHandle(new Vector2(hitboxRect.xMin, hitboxRect.yMax) + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap) - (Vector3)frame.position;
                hitboxRect.xMin = newTopLeft.x; 
                hitboxRect.yMax = newTopLeft.y;

                // Edges
                hitboxRect.xMin = Handles.FreeMoveHandle(new Vector2(hitboxRect.xMin, hitboxRect.y + hitboxRect.height * 0.5f) + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).x - frame.position.x;
                hitboxRect.xMax = Handles.FreeMoveHandle(new Vector2(hitboxRect.xMax, hitboxRect.y + hitboxRect.height * 0.5f) + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).x - frame.position.x;
                hitboxRect.yMin = Handles.FreeMoveHandle(new Vector2(hitboxRect.x + hitboxRect.width * 0.5f, hitboxRect.yMin) + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).y - frame.position.y;
                hitboxRect.yMax = Handles.FreeMoveHandle(new Vector2(hitboxRect.x + hitboxRect.width * 0.5f, hitboxRect.yMax) + frame.position, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).y - frame.position.y;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Modified Hitbox");
                    Hitbox newHitbox = frame.hitboxes[hitboxIndex];
                    newHitbox.rect = hitboxRect;
                    frame.hitboxes[hitboxIndex] = newHitbox;
                    attackData.frames[selectedFrameIndex] = frame;
                    EditorUtility.SetDirty(target);
                }
                // Draw the hitbox outline
                Rect visualRect = hitboxRect;
                visualRect.center += frame.position;
                Handles.DrawSolidRectangleWithOutline(visualRect, data.hitboxColor * new Color(1,1,1, data.hitboxOpacity), data.hitboxColor);

                //Draw Line to Frame Pos
                
                Handles.DrawLine(visualRect.center, frame.position);

                DrawHitboxLaunchArrow(frameIndex, hitboxIndex);
            }
            else
            {
                Handles.color = Color.white;
                // Draw the hitbox outline
                Rect visualRect = hitboxRect;
                visualRect.center += frame.position;
                Handles.DrawSolidRectangleWithOutline(visualRect, data.hitboxColor * data.unselectedColor * new Color(1, 1, 1, data.hitboxOpacity), data.hitboxColor * data.unselectedColor);

                //Draw Line to Frame Pos
                Handles.color = data.hitboxColor * data.unselectedColor;
                Handles.DrawLine(visualRect.center, frame.position);
            }
        }
    }

    private void DrawHitboxLaunchArrow(int frameIndex, int hitboxIndex)
    {
        AttackFrame frame = attackData.frames[frameIndex];
        Hitbox hitbox = frame.hitboxes[hitboxIndex];


        // Draw and update launch direction arrow handle
        EditorGUI.BeginChangeCheck();
        Vector2 newLaunchDirection = Handles.FreeMoveHandle(frame.position + hitbox.rect.center + hitbox.hitVector, 1f, Vector3.zero, Handles.ArrowHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Modified Hitbox Launch Direction");
            hitbox.hitVector = newLaunchDirection - hitbox.rect.center - frame.position;
            frame.hitboxes[hitboxIndex] = hitbox;
            attackData.frames[frameIndex] = frame;
            EditorUtility.SetDirty(target);
        }

        if (hitbox.hitVector != Vector2.zero)
        {
            // Draw launch direction arrow 
            Handles.color = Color.green;
            Handles.ArrowHandleCap(0, frame.position + hitbox.rect.center, Quaternion.LookRotation(hitbox.hitVector, Vector3.back), hitbox.hitVector.magnitude * 0.87f, EventType.Repaint);
            // ... 
        }
    }
    
    private void DrawLineToNextFrame(int frameIndex)
    {
        if (frameIndex + 1 < attackData.frames.Count)
        {
            Handles.color = Color.white;
            Handles.DrawLine(attackData.frames[frameIndex].position, attackData.frames[frameIndex + 1].position);
        }
    }

    private void DrawLineFromOriginToFirstFrame()
    {

        Handles.color = Color.white;
        Handles.DrawLine(Vector3.zero, attackData.frames[0].position);

    }

    private void DrawEndVelocityControl()
    {

        Handles.color = Color.white;
        AttackFrame lastFrame = attackData.frames[attackData.frames.Count - 1];
        Handles.DrawLine(lastFrame.position,lastFrame.position + attackData.endVelocity);

        EditorGUI.BeginChangeCheck();


        var handleRotation = Quaternion.FromToRotation(Vector3.zero, attackData.endVelocity);
        Vector2 newEndVelocity = Handles.FreeMoveHandle(
            lastFrame.position + attackData.endVelocity, ConvertHandleSize(0.7f), Vector3.zero, Handles.ArrowHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Modified Frame Position");
            // Update the position offset in the AttackData
            attackData.endVelocity = newEndVelocity - lastFrame.position;

            EditorUtility.SetDirty(target); // Mark the asset as dirty to save changes
        }

    }

    private Vector2 ConvertScreenToWorldPos(Vector2 ScreenPos)
    {
        return SceneScreenToWorldPoint(Vector2.Scale(new Vector2(1, -1), ScreenPos)
                        + new Vector2(0, SceneView.GetAllSceneCameras()[0].pixelHeight));
    }

    public override void OnInspectorGUI()
    {

        serializedObject.Update(); // Sync editor with object

        // Display basic attack properties
        EditorGUILayout.PropertyField(serializedObject.FindProperty("frames"), true);

        data.showAllFrames = EditorGUILayout.Toggle("Show All Frames", data.showAllFrames);
        data.numberOfSurroundingFrames = EditorGUILayout.IntSlider("Surrounding Frames", data.numberOfSurroundingFrames, 0, 5);


        data.posKeyFrameColor = EditorGUILayout.ColorField("Pos Keys", data.posKeyFrameColor);
        data.hitboxColor = EditorGUILayout.ColorField("Hitboxes", data.hitboxColor);
        data.unselectedFade = EditorGUILayout.Slider("Unselected Fade", data.unselectedFade, 0, 1);
        data.hitboxOpacity = EditorGUILayout.Slider("Hitbox Opacity", data.hitboxOpacity, 0, 1);


        // Frame Selection and Visualization
        EditorGUILayout.Space();
        selectedFrameIndex = EditorGUILayout.IntSlider("Selected Frame", selectedFrameIndex, 0, attackData.frames.Count - 1);

        if (attackData.frames.Count > 0)
        {
            AttackFrame frameData = attackData.frames[selectedFrameIndex];

            // Display frame details (position, sprite, hitboxes, hurtbox)
            EditorGUILayout.LabelField("Frame Details", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("pauseDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("controlsPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("isHoldFrame"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("hitboxes"), true);
            if (GUILayout.Button("Create Hitbox")) CreateHitbox();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("hurtbox"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("transitions"));


        }
        DrawSpritePalette();

        if (Event.current.type == EventType.KeyDown)
        {
            CheckForKeyPresses();
        }

        serializedObject.ApplyModifiedProperties(); // Apply changes to the object
    }

    private void CreateHitbox()
    {
        Undo.RecordObject(target, "Modified Frame Position");
        selectedFrame.hitboxes.Add(new Hitbox());
        Debug.Log("Creating hitbox for frame: " + selectedFrameIndex);
    }

    private void DrawSpritePalette()
    {
        EditorGUILayout.BeginVertical();
        spritePaletteSize = EditorGUILayout.IntSlider("Sprite Palette Size", spritePaletteSize, 10, 500);

        float availableWidth = EditorGUIUtility.currentViewWidth; // Get the available width of the inspector
        float usedWidth = spritePaletteSize;

        spriteButtonStyle = new GUIStyle(GUI.skin.button); // Base style on default button


        // Style for the background (border)
        GUIStyle backgroundStyle = new GUIStyle();
        backgroundStyle.normal.background = Texture2D.grayTexture; // Or use a custom border texture
        backgroundStyle.border = new RectOffset(2, 2, 2, 2); // Adjust border thickness as needed

        // Combine the image and background styles
        spriteButtonStyle.normal.background = backgroundStyle.normal.background;
        spriteButtonStyle.border = backgroundStyle.border;

        spriteButtonStyle.stretchWidth = true; // Stretch to fit width
        spriteButtonStyle.stretchHeight = true; // Stretch to fit height
        spriteButtonStyle.fixedHeight = spritePaletteSize;
        spriteButtonStyle.fixedWidth = spritePaletteSize;



        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < attackData.spritePalette.Count; i++)
        {

            if (GUILayout.Button(GetSlicedSpriteTexture(attackData.spritePalette[i]), spriteButtonStyle, GUILayout.Width(spritePaletteSize), GUILayout.Height(spritePaletteSize)))
            {
                Undo.RecordObject(attackData, "Changed Sprite of Frame");
                selectedFrame.sprite = attackData.spritePalette[i];
                EditorUtility.SetDirty(target);
            }

            // Calculate used width and check for line break
            Rect buttonRect = GUILayoutUtility.GetLastRect(); // Get the rect of the last button
            usedWidth += spritePaletteSize; // Add button width and potential spacing
            if (usedWidth > availableWidth)
            {
                EditorGUILayout.EndHorizontal(); // End the current line if exceeded available width
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(availableWidth)); // Start a new line
                usedWidth = spritePaletteSize; // Reset used width for the new line
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    

    private float ConvertHandleSize(float size)
    {
        float handleSize = Mathf.Min(size, size * handleScalar);
        return handleSize;
    } 

    private Vector2 SceneScreenToWorldPoint(Vector2 pos)
    {
        return Vector2.Scale(new Vector2(1, -1), SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(pos));
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
