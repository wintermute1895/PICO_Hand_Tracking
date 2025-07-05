// VR开发7.3.9_最终简化修正：仅反转Z轴
using UnityEngine;
using Unity.XR.PXR;
using System.Collections.Generic;

public class HandVisualizer : MonoBehaviour
{
    public HandType handType;
    public GameObject jointPrefab;

    private Camera mainCamera;
    private HandJointLocations handJointLocations = new HandJointLocations();
    private List<GameObject> jointObjects = new List<GameObject>();

    void Start()
    {
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
        if (mainCamera == null) return;

        bool success = PXR_HandTracking.GetJointLocations(handType, ref handJointLocations);

        if (success && handJointLocations.isActive > 0)
        {
            Transform playerOrigin = mainCamera.transform.parent;
            if (playerOrigin == null)
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

                    if (isPositionValid && isRotationValid)
                    {
                        jointObj.SetActive(true);

                        // a. 将PICO的局部坐标和旋转转换为Unity类型
                        Vector3 jointLocalPosition = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);
                        Quaternion jointLocalRotation = new Quaternion(joint.pose.Orientation.x, joint.pose.Orientation.y, joint.pose.Orientation.z, joint.pose.Orientation.w);

                        // b. 【最终关键修正】只反转Z轴，来将坐标系从背后翻转到身前，同时进行一次镜像操作
                        jointLocalPosition.z = -jointLocalPosition.z;

                        // c. 由于只进行了一次镜像，旋转也需要进行镜像补偿。
                        //    在四元数中，对一个轴进行镜像，等价于将其余两个轴的旋转分量取反。
                        //    这里镜像了Z轴，所以需要反转X和Y的旋转分量。
                        jointLocalRotation.x = -jointLocalRotation.x;
                        jointLocalRotation.y = -jointLocalRotation.y;

                        // d. 使用Unity标准函数进行世界坐标转换
                        jointObj.transform.position = playerOrigin.TransformPoint(jointLocalPosition);
                        jointObj.transform.rotation = playerOrigin.rotation * jointLocalRotation;
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