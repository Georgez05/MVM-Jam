using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    #region Variables
    [SerializeField] private CinemachineCamera[] cameras;

    [Header("Jump/Fall Damping Attributes")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallPanTime = 0.35f;
    public float fallSpeedYDampingChangeThreshold = -15f;

    public bool isLerpingYDamping { get; private set; }
    public bool lerpedFromPlayerFalling { get; set; }

    private Coroutine panCameraCoroutine;

    private CinemachineCamera currentCamera;
    private CinemachinePositionComposer positionComposer;

    private float normYPanAmount;

    private Vector2 startingTreckedObjectOffset;
    #endregion

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].enabled)
            {
                // set the current active camera
                currentCamera = cameras[i];

                positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();

                normYPanAmount = positionComposer.Damping.y;
            }
        }

        // set the starting position of the target offfset
        startingTreckedObjectOffset = positionComposer.TargetOffset;
    }

    #region Lerp the Y Damping
    public void LerpYDamping(bool isPlayerFalling)
    {
        float start = positionComposer.Damping.y;
        float end = isPlayerFalling ? fallPanAmount : normYPanAmount;

        if (isPlayerFalling)
            lerpedFromPlayerFalling = true;

        isLerpingYDamping = true;

        LeanTween.value(start, end, fallPanTime)
            .setOnUpdate((float val) =>
            {
                Vector3 damping = positionComposer.Damping;
                damping.y = val;
                positionComposer.Damping = damping;
            })
            .setEase(LeanTweenType.easeInOutSine)
            .setOnComplete(() => isLerpingYDamping = false);
    }
    #endregion

    #region Pan Camera
    public void PanCameraOnContact(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        Vector2 endPos = Vector2.zero;
        Vector2 startingPos = Vector2.zero;

        if (!panToStartingPos)
        {
            // set the direction and distance
            switch (panDirection)
            {
                case PanDirection.up:
                    endPos = Vector2.up;
                    break;
                case PanDirection.down:
                    endPos = Vector2.down;
                    break;
                case PanDirection.left:
                    endPos = Vector2.right;
                    break;
                case PanDirection.right:
                    endPos = Vector2.left;
                    break;
            }
            endPos *= panDistance;

            startingPos = startingTreckedObjectOffset;

            endPos += startingPos;
        }
        else
        {
            startingPos = positionComposer.TargetOffset;
            endPos = startingTreckedObjectOffset;
        }

        float elapsedTime = 0f;
        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;

            Vector3 panLerp = Vector3.Lerp(startingPos, endPos, (elapsedTime / panTime));
            positionComposer.TargetOffset = panLerp;

            yield return null;
        }
    }
    #endregion

    #region Swap Camera
    public void SwapCamera(CinemachineCamera cameraFromLeft, CinemachineCamera cameraFromRight, Vector2 triggerExitDirection)
    {
        // if current camera is the camera from the left and our trigger exit direction was on the right
        if (currentCamera == cameraFromLeft && triggerExitDirection.x > 0f)
        {
            // activate the new camera
            cameraFromRight.enabled = true;

            // deactivate the old camera
            cameraFromLeft.enabled = false;

            // set the new camera as the current camera
            currentCamera = cameraFromRight;

            // update position composer
            positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();
        }

        // if current camera is the camera from the right and our trigger exit direction was on the left
        else if (currentCamera == cameraFromRight && triggerExitDirection.x < 0f)
        {
            // activate the new camera
            cameraFromLeft.enabled = true;

            // deactivate the old camera
            cameraFromRight.enabled = false;

            // set the new camera as the current camera
            currentCamera = cameraFromLeft;

            // update position composer
            positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();
        }
    }
    #endregion
}
