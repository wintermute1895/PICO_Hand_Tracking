// VR开发文档7.3.6_兼容旧版SDK的最终坐标修正
using UnityEngine;
using Unity.XR.PXR;
using System.Collections.Generic;

public class HandVisualizer : MonoBehaviour
{
    public HandType handType;
    public GameObject jointPrefab;

    // 我们将再次使用主相机作为最可靠的参考点
    private Camera mainCamera;

    private HandJointLocations handJointLocations = new HandJointLocations();
    private List<GameObject> jointObjects = new List<GameObject>();

    void Start()
    {
        // 查找主相机，并确保它被正确标记
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            UnityEngine.Debug.LogError("HandVisualizer: Main Camera not found! Please ensure your camera is tagged as 'MainCamera'.");
            return;
        }

        if (jointPrefab == null)
        {
            UnityEngine.Debug.LogError("HandVisualizer: Joint Prefab is not assigned!");
            return;
        }

        for (int i = 0; i < 26; i++)
        {
            GameObject jointObj = Instantiate(jointPrefab, this.transform);
            jointObj.name = $"{handType}_Joint_{i}";
            jointObj.SetActive(false);
            jointObjects.Add(jointObj);
        }
    }

    void Update()
    {
        // 如果找不到相机，则不执行任何操作
        if (mainCamera == null) return;

        bool success = PXR_HandTracking.GetJointLocations(handType, ref handJointLocations);

        if (success && handJointLocations.isActive > 0)
        {
            // 获取相机所在的 XR Origin 或其父对象的 transform。这是我们的“玩家”在世界中的参考系。
            Transform playerOrigin = mainCamera.transform.parent;
            if (playerOrigin == null) // 如果相机没有父对象，则以相机自身为参考
            {
                playerOrigin = mainCamera.transform;
            }

            for (int i = 0; i < handJointLocations.jointCount; i++)
            {
                if (i < jointObjects.Count)
                {
                    var joint = handJointLocations.jointLocations[i];
                    GameObject jointObj = jointObjects[i];
                    bool isPositionValid = (joint.locationStatus & HandLocationStatus.PositionValid) != 0;
                    bool isRotationValid = (joint.locationStatus & HandLocationStatus.OrientationValid) != 0;

                    // 只有当位置和旋转都有效时，我们才更新
                    if (isPositionValid && isRotationValid)
                    {
                        jointObj.SetActive(true);

                        // a. 将PICO的局部坐标和旋转转换为Unity类型
                        Vector3 jointLocalPosition = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);
                        Quaternion jointLocalRotation = new Quaternion(joint.pose.Orientation.x, joint.pose.Orientation.y, joint.pose.Orientation.z, joint.pose.Orientation.w);

                        // b. 使用 TransformPoint 和 TransformRotation 进行最可靠的坐标系转换
                        //    这会将相对于玩家原点的局部坐标，正确地转换到世界空间中
                        jointObj.transform.position = playerOrigin.TransformPoint(jointLocalPosition);
                        jointObj.transform.rotation = playerOrigin.transform.rotation * jointLocalRotation;
                    }
                    else
                    {
                        jointObj.SetActive(false);
                    }
                }
            }
        }
        else
        {
            foreach (var jointObj in jointObjects)
            {
                jointObj.SetActive(false);
            }
        }
    }
}