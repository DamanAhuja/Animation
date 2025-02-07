using Cysharp.Threading.Tasks;
using UnityEngine;
using TensorFlowLite.MoveNet;
using TextureSource;

[RequireComponent(typeof(VirtualTextureSource))]
public class MoveNetMultiPoseSample : MonoBehaviour
{
    [SerializeField]
    MoveNetMultiPose.Options options = default;

    [SerializeField]
    private RectTransform cameraView = null;

    [SerializeField]
    private bool runBackground = false;

    [SerializeField, Range(0, 1)]
    private float threshold = 0.3f;

    private MoveNetMultiPose moveNet;
    private MoveNetPoseWithBoundingBox[] poses;
    private MoveNetDrawer drawer;

    private UniTask<bool> task;

    private void Start()
    {
        moveNet = new MoveNetMultiPose(options);
        drawer = new MoveNetDrawer(Camera.main, cameraView);

        if (TryGetComponent(out VirtualTextureSource source))
        {
            source.OnTexture.AddListener(OnTextureUpdate);
        }
    }

    private void OnDestroy()
    {
        if (TryGetComponent(out VirtualTextureSource source))
        {
            source.OnTexture.RemoveListener(OnTextureUpdate);
        }
        moveNet?.Dispose();
        drawer?.Dispose();
    }

    private void Update()
    {
        if (poses != null)
        {
            foreach (var pose in poses)
            {
                drawer.DrawPose(pose, threshold);
            }
        }
        if (poses != null && poses.Length > 0)
        {
            MoveNetPoseWithBoundingBox mainPose = poses[0]; // First detected person

            Vector2[] keypoints2D = new Vector2[MoveNetPose.JOINT_COUNT];

            for (int i = 0; i < MoveNetPose.JOINT_COUNT; i++)
            {
                keypoints2D[i] = new Vector2(mainPose[i].x, mainPose[i].y);
            }

            MoveNetAnimator.Instance.UpdatePose(keypoints2D); // Send to animator
        }
    }

    private void OnTextureUpdate(Texture texture)
    {
        if (runBackground)
        {
            if (task.Status.IsCompleted())
            {
                task = InvokeAsync(texture);
            }
        }
        else
        {
            Invoke(texture);
        }
    }

    private void Invoke(Texture texture)
    {
        moveNet.Run(texture);
        poses = moveNet.GetResults();
    }

    private async UniTask<bool> InvokeAsync(Texture texture)
    {
        await moveNet.RunAsync(texture, destroyCancellationToken);
        poses = moveNet.GetResults();
        return true;
    }
}
