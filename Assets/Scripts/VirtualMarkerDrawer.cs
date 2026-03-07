using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit.Input;

public class VirtualMarkerDrawer : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float pinchThreshold = 0.02f; // Distance in meters for pinch detection
    [SerializeField] private float eraserThreshold = 0.02f; // index-middle distance for erase gesture

    private HandsAggregatorSubsystem handsSubsystem;
    private List<GameObject> markers = new List<GameObject>();
    private LineRenderer lineRenderer;
    private bool wasPinching = false;
    private bool wasErasing = false;

    private void Start()
    {
        // grab the MRTK hand tracking subsystem
        handsSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();

        // make sure pinch state reflect current hand so we don't spawn a starting point
        wasPinching = IsPinching(XRNode.LeftHand) || IsPinching(XRNode.RightHand);
        wasErasing = IsEraserPinch(XRNode.LeftHand) || IsEraserPinch(XRNode.RightHand);

        // Create LineRenderer for drawing lines
        GameObject lineObject = new GameObject("LineRenderer");
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        bool isPinching = IsPinching(XRNode.LeftHand) || IsPinching(XRNode.RightHand);
        bool isErasing = IsEraserPinch(XRNode.LeftHand) || IsEraserPinch(XRNode.RightHand);

        // if eraser just began, pop last marker
        if (isErasing && !wasErasing)
        {
            RemoveLastMarker();
        }

        // pinch for new point
        if (isPinching && !wasPinching)
        {
            Vector3 pinchPosition = GetPinchPosition();
            PlaceMarker(pinchPosition);
        }

        wasPinching = isPinching;
        wasErasing = isErasing;
    }

    private bool IsPinching(XRNode node)
    {
        if (handsSubsystem == null) return false;
        if (!handsSubsystem.TryGetEntireHand(node, out IReadOnlyList<HandJointPose> joints))
            return false;

        var thumbPose = joints[(int)TrackedHandJoint.ThumbTip];
        var indexPose = joints[(int)TrackedHandJoint.IndexTip];
        float distance = Vector3.Distance(thumbPose.Position, indexPose.Position);
        return distance < pinchThreshold;
    }

    // eraser gesture: index + middle fingers together
    private bool IsEraserPinch(XRNode node)
    {
        if (handsSubsystem == null) return false;
        if (!handsSubsystem.TryGetEntireHand(node, out IReadOnlyList<HandJointPose> joints))
            return false;

        var indexPose = joints[(int)TrackedHandJoint.IndexTip];
        var middlePose = joints[(int)TrackedHandJoint.MiddleTip];
        float dist = Vector3.Distance(indexPose.Position, middlePose.Position);
        return dist < eraserThreshold;
    }

    private Vector3 GetPinchPosition()
    {
        if (IsPinching(XRNode.LeftHand) &&
            handsSubsystem.TryGetEntireHand(XRNode.LeftHand, out IReadOnlyList<HandJointPose> leftJoints))
        {
            return leftJoints[(int)TrackedHandJoint.IndexTip].Position;
        }
        else if (IsPinching(XRNode.RightHand) &&
                 handsSubsystem.TryGetEntireHand(XRNode.RightHand, out IReadOnlyList<HandJointPose> rightJoints))
        {
            return rightJoints[(int)TrackedHandJoint.IndexTip].Position;
        }
        return Vector3.zero;
    }

    private void PlaceMarker(Vector3 position)
    {
        GameObject marker = Instantiate(markerPrefab, position, Quaternion.identity);
        markers.Add(marker);

        UpdateLineRenderer();
        UpdateAngleLabels();
    }

    private void RemoveLastMarker()
    {
        if (markers.Count == 0) return;
        Destroy(markers[markers.Count - 1]);
        markers.RemoveAt(markers.Count - 1);
        UpdateLineRenderer();
        UpdateAngleLabels();
    }

    private void UpdateLineRenderer()
    {
        lineRenderer.positionCount = markers.Count;
        for (int i = 0; i < markers.Count; i++)
        {
            lineRenderer.SetPosition(i, markers[i].transform.position);
        }
    }

    private void UpdateAngleLabels()
    {
        // clear all texts
        for (int i = 0; i < markers.Count; i++)
        {
            var txt = markers[i].GetComponentInChildren<TextMesh>();
            if (txt != null) txt.text = string.Empty;
        }

        // compute angle at each internal vertex
        for (int i = 1; i < markers.Count - 1; i++)
        {
            Vector3 prev = markers[i - 1].transform.position;
            Vector3 curr = markers[i].transform.position;
            Vector3 next = markers[i + 1].transform.position;

            float angle = Vector3.Angle(prev - curr, next - curr);
            var txt = markers[i].GetComponentInChildren<TextMesh>();
            if (txt != null)
            {
                txt.text = angle.ToString("F1") + "°";
                // orient text toward camera
                if (Camera.main != null)
                    txt.transform.rotation = Quaternion.LookRotation(txt.transform.position - Camera.main.transform.position);
            }
        }
    }
}