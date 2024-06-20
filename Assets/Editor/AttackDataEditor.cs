using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static Codice.CM.WorkspaceServer.DataStore.WkTree.WriteWorkspaceTree;

[CustomEditor(typeof(AttackSegmentData))]
public class AttackDataEditor : UnityEditor.Editor
{
    public AttackEditorData data;

    private AttackSegmentData attackData; // Reference to the edited object
    private int selectedFrameIndex = 0; // Currently viewed frame

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
                for (int frameIndex = 0; frameIndex < attackData.frames.Count; frameIndex++)
                {
                    AttackFrame frame = attackData.frames[frameIndex];

                    DrawFramePositionHandle(frameIndex);

                    DrawFrameSpriteInWorldSpace(frameIndex);

                    DrawLineToNextFrame(frameIndex);

                }
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

                DrawFramePositionHandle(selectedFrameIndex);

                DrawFrameSpriteInWorldSpace(selectedFrameIndex);

                DrawHitboxesForFrame(selectedFrameIndex);
              
            }
        }

        if (Event.current.type == EventType.KeyDown)
        {
            CheckForKeyPresses();
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
    }
    private void DuplicateFrame(int frameIndex)
    {
        Undo.RecordObject(target, "Modified Frame Position");
        AttackFrame newFrame = attackData.frames[frameIndex].Duplicate();
        attackData.frames.Insert(frameIndex + 1, newFrame);
        Undo.RecordObject(target, "Modified Frame Position");
    }

    private void DeleteFrame(int frameIndex)
    {
        Undo.RecordObject(target, "Modified Frame Position");
        AttackFrame frame = attackData.frames[frameIndex];
        attackData.frames.RemoveAt(frameIndex);
        Undo.RecordObject(target, "Modified Frame Position");
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
            Handles.color *= data.unselectedColor;
        }
        var handleRotation = Quaternion.identity;
        Vector3 newFramePosition = Handles.FreeMoveHandle(
            frame.position, ConvertHandleSize(0.2f), Vector3.zero, Handles.CircleHandleCap);

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
        for (int i = 0; i < frame.hitboxes.Count; i++)
        {
            Rect hitbox = frame.hitboxes[i];

            if (frameIndex == selectedFrameIndex)
            {
                float handleSize = 0.05f;

                Handles.color = data.hitboxColor;
                EditorGUI.BeginChangeCheck();

                // Center
                hitbox.center = Handles.FreeMoveHandle(hitbox.center, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap);

                // Min and Max corners
                hitbox.min = Handles.FreeMoveHandle(hitbox.min, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap);
                hitbox.max = Handles.FreeMoveHandle(hitbox.max, ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap);

                // Other corners
                Vector2 newBottomRight = Handles.FreeMoveHandle(new Vector2(hitbox.xMax, hitbox.yMin), ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap);
                hitbox.xMax = newBottomRight.x;
                hitbox.yMin = newBottomRight.y;

                Vector2 newTopLeft = Handles.FreeMoveHandle(new Vector2(hitbox.xMin, hitbox.yMax), ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap);
                hitbox.xMin = newTopLeft.x;
                hitbox.yMax = newTopLeft.y;

                // Edges
                hitbox.xMin = Handles.FreeMoveHandle(new Vector2(hitbox.xMin, hitbox.y + hitbox.height * 0.5f), ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).x;
                hitbox.xMax = Handles.FreeMoveHandle(new Vector2(hitbox.xMax, hitbox.y + hitbox.height * 0.5f), ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).x;
                hitbox.yMin = Handles.FreeMoveHandle(new Vector2(hitbox.x + hitbox.width * 0.5f, hitbox.yMin), ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).y;
                hitbox.yMax = Handles.FreeMoveHandle(new Vector2(hitbox.x + hitbox.width * 0.5f, hitbox.yMax), ConvertHandleSize(handleSize), Vector3.zero, Handles.CircleHandleCap).y;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Modified Hitbox");

                    frame.hitboxes[i] = hitbox;
                    attackData.frames[selectedFrameIndex] = frame;
                    EditorUtility.SetDirty(target);
                }
                // Draw the hitbox outline
                Handles.DrawSolidRectangleWithOutline(hitbox, data.hitboxColor * new Color(1,1,1, data.hitboxOpacity), data.hitboxColor);

                //Draw Line to Frame Pos
                
                Handles.DrawLine(hitbox.center, frame.position);
            }
            else
            {
                Handles.color = Color.white;
                // Draw the hitbox outline
                Handles.DrawSolidRectangleWithOutline(hitbox, data.hitboxColor * data.unselectedColor * new Color(1, 1, 1, data.hitboxOpacity), data.hitboxColor * data.unselectedColor);

                //Draw Line to Frame Pos
                Handles.color = data.hitboxColor * data.unselectedColor;
                Handles.DrawLine(hitbox.center, frame.position);
            }
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

    private Vector2 ConvertScreenToWorldPos(Vector2 ScreenPos)
    {
        return SceneView.GetAllSceneCameras()[0].ScreenToWorldPoint(Vector2.Scale(new Vector2(1, -1), ScreenPos)
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("hitboxes"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frames").GetArrayElementAtIndex(selectedFrameIndex).FindPropertyRelative("hurtbox"));

            // Visualize the frame (optional)
            if (frameData.sprite != null)
            {
                // Calculate sprite size and position for visualization
                Texture2D spriteTexture = GetSlicedSpriteTexture(frameData.sprite);
                Rect spriteRect = new Rect(50, 150, spriteTexture.width, spriteTexture.height);
                GUI.DrawTexture(spriteRect, spriteTexture);

                // Draw hitboxes and hurtbox (you'll need to adjust colors/styles)
                Handles.color = Color.red; // Hitbox color
                foreach (Rect hitbox in frameData.hitboxes)
                {
                    Handles.DrawSolidRectangleWithOutline(hitbox, Color.clear, Color.red);
                }

                Handles.color = Color.blue; // Hurtbox color
                Handles.DrawSolidRectangleWithOutline(frameData.hurtbox, Color.clear, Color.blue);
            }
        }

        if (Event.current.type == EventType.KeyDown)
        {
            CheckForKeyPresses();
        }

        serializedObject.ApplyModifiedProperties(); // Apply changes to the object
    }

    private float ConvertHandleSize(float size)
    {
        float handleSize = Mathf.Min(size, size * handleScalar);
        return handleSize;
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
