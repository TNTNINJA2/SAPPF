
using System.Drawing.Printing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[CustomEditor(typeof(Attack))]
public class AttackEditor : UnityEditor.Editor
{
    public float time;
    public Attack attack;
    public PlayerController dummy;
    public override void OnInspectorGUI()
    {
        attack = (Attack)target;


        time = EditorGUILayout.Slider(time, 0, attack.GetTotalDuration());

        dummy = GameObject.FindGameObjectWithTag("Dummy").GetComponent<PlayerController>();


        DrawDefaultInspector();
        attack.DisplayAtTime(dummy, time, Vector3.zero);

    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneupdate;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneupdate;
    }


    public void OnSceneupdate(SceneView sceneView)
    {
        attack = (Attack)target;

        // Set handle color
        Handles.color = Color.red;
        for (int i = 0; i < attack.posKeyFrames.Count - 1; i++)
        {
            KeyFrame<PosKeyFrameData> posKeyFrame1 = attack.posKeyFrames[i];
            KeyFrame<PosKeyFrameData> posKeyFrame2 = attack.posKeyFrames[i + 1];


            Handles.DrawBezier(posKeyFrame1.data.pos, posKeyFrame2.data.pos, 
                posKeyFrame1.data.afterBezierControlPoint, posKeyFrame2.data.beforeBezierControlPoint, 
                Color.red, Texture2D.whiteTexture, 1);
        }

        for (int i = 0; i < attack.posKeyFrames.Count; i++)
        {
            KeyFrame<PosKeyFrameData> posKeyFrame = attack.posKeyFrames[i];

            
            Vector2 newPos = Handles.DoPositionHandle(posKeyFrame.data.pos, Quaternion.identity);
            Vector2 deltaPos = newPos - posKeyFrame.data.pos;

            posKeyFrame.data.beforeBezierControlPoint += deltaPos;
            posKeyFrame.data.afterBezierControlPoint += deltaPos;
            posKeyFrame.data.pos = newPos; 

            posKeyFrame.data.beforeBezierControlPoint = Handles.DoPositionHandle(posKeyFrame.data.beforeBezierControlPoint, Quaternion.identity);
            posKeyFrame.data.afterBezierControlPoint = Handles.DoPositionHandle(posKeyFrame.data.afterBezierControlPoint, Quaternion.identity);

            Handles.color = Color.green;
            Handles.DrawLine(posKeyFrame.data.pos, posKeyFrame.data.beforeBezierControlPoint);
            Handles.DrawLine(posKeyFrame.data.pos, posKeyFrame.data.afterBezierControlPoint);
            
        }

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
