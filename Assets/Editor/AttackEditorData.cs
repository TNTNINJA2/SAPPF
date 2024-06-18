using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackEditorData", menuName = "ScriptableObjects/Attack Editor Data", order = 1)]

public class AttackEditorData : ScriptableObject
{ 
    public Color posKeyFrameColor = Color.red;
    public Color bezierControlColor = Color.Lerp(Color.red, Color.yellow, 0.5f);
    public Color spriteKeyFrameColor = Color.cyan;
    public Color speedIndicatorColor = Color.yellow;
    public Color hitboxColor = Color.blue;
    public Color timelineColor = Color.green;
    public Color selectedKeyFrameColor = Color.magenta;
    public bool shouldDrawBezierControls = true;
    public bool shouldDrawTimeControls = true;
    public bool shouldDrawSpriteTimeControls = true;
    public bool shouldDrawSpeedIndicators = true;
    public bool shouldDrawHitboxes = true;
    public bool shouldDrawSecondaryHitboxes = true;
    public float speedIndicatorSpacing = 0.2f;
    public float speedIndicatorWidth = 0.1f;
    public float timelineHeight = -2;
    public float timelineBeginX = 0;
    public float timelineEndX = 5;

    public float posTimelineOffset = -1;
    public float spriteTimelineOffset = -2;
    public float hitboxTimelineOffset = -3;
    public float hitboxTimelineVerticalSpacing = -1;
}
