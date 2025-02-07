using System.Collections.Generic;
using UnityEngine;

public class MoveNetAnimator : MonoBehaviour
{
    public static MoveNetAnimator Instance { get; private set; }
    public Transform head, leftShoulder, rightShoulder, leftElbow, rightElbow, leftHand, rightHand;
    public Transform leftHip, rightHip, leftKnee, rightKnee, leftFoot, rightFoot;

    private Dictionary<int, Transform> keypointMapping;

    private void Start()
    {
        // Map MoveNet keypoints to Unity bones
        keypointMapping = new Dictionary<int, Transform>
        {
            {0, head},
            {5, leftShoulder}, {6, rightShoulder},
            {7, leftElbow}, {8, rightElbow},
            {9, leftHand}, {10, rightHand},
            {11, leftHip}, {12, rightHip},
            {13, leftKnee}, {14, rightKnee},
            {15, leftFoot}, {16, rightFoot}
        };
    }
    private void Awake()
    {
        // Ensure only one instance of MoveNetAnimator exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateAllRotations();
    }

    /// <summary>
    /// Converts 2D keypoints (normalized) to world space coordinates
    /// </summary>
    private Vector3 Convert2DTo3D(Vector2 keypoint2D, float depth = 5.0f)
    {
        Vector3 screenPosition = new Vector3(
            keypoint2D.x * Screen.width,
            (1 - keypoint2D.y) * Screen.height,
            depth
        );

        return Camera.main.ScreenToWorldPoint(screenPosition);
    }

    /// <summary>
    /// Updates the character's bone positions based on MoveNet keypoints
    /// </summary>
    public void UpdatePose(Vector2[] keypoints2D, float depth = 5.0f)
    {
        for (int i = 0; i < keypoints2D.Length; i++)
        {
            if (keypointMapping.ContainsKey(i) && keypointMapping[i] != null)
            {
                Vector3 targetPosition = Convert2DTo3D(keypoints2D[i], depth);
                keypointMapping[i].position = Vector3.Lerp(keypointMapping[i].position, targetPosition, Time.deltaTime * 10);
            }
        }
    }

    /// <summary>
    /// Updates bone rotations for a natural look
    /// </summary>
    private void UpdateBoneRotation(Transform bone, Vector3 startPos, Vector3 endPos)
    {
        if (bone == null) return;

        Vector3 direction = (endPos - startPos).normalized;
        if (direction.sqrMagnitude > 0.0001f)
        {
            bone.rotation = Quaternion.Slerp(bone.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10);
        }
    }

    /// <summary>
    /// Updates all major limb rotations
    /// </summary>
    private void UpdateAllRotations()
    {
        if (leftShoulder != null && leftElbow != null) UpdateBoneRotation(leftShoulder, leftShoulder.position, leftElbow.position);
        if (leftElbow != null && leftHand != null) UpdateBoneRotation(leftElbow, leftElbow.position, leftHand.position);

        if (rightShoulder != null && rightElbow != null) UpdateBoneRotation(rightShoulder, rightShoulder.position, rightElbow.position);
        if (rightElbow != null && rightHand != null) UpdateBoneRotation(rightElbow, rightElbow.position, rightHand.position);

        if (leftHip != null && leftKnee != null) UpdateBoneRotation(leftHip, leftHip.position, leftKnee.position);
        if (leftKnee != null && leftFoot != null) UpdateBoneRotation(leftKnee, leftKnee.position, leftFoot.position);

        if (rightHip != null && rightKnee != null) UpdateBoneRotation(rightHip, rightHip.position, rightKnee.position);
        if (rightKnee != null && rightFoot != null) UpdateBoneRotation(rightKnee, rightKnee.position, rightFoot.position);
    }
}
