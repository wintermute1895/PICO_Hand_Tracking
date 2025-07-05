// VR����7.3.9_���ռ�����������תZ��
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

                        // a. ��PICO�ľֲ��������תת��ΪUnity����
                        Vector3 jointLocalPosition = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);
                        Quaternion jointLocalRotation = new Quaternion(joint.pose.Orientation.x, joint.pose.Orientation.y, joint.pose.Orientation.z, joint.pose.Orientation.w);

                        // b. �����չؼ�������ֻ��תZ�ᣬ��������ϵ�ӱ���ת����ǰ��ͬʱ����һ�ξ������
                        jointLocalPosition.z = -jointLocalPosition.z;

                        // c. ����ֻ������һ�ξ�����תҲ��Ҫ���о��񲹳���
                        //    ����Ԫ���У���һ������о��񣬵ȼ��ڽ��������������ת����ȡ����
                        //    ���ﾵ����Z�ᣬ������Ҫ��תX��Y����ת������
                        jointLocalRotation.x = -jointLocalRotation.x;
                        jointLocalRotation.y = -jointLocalRotation.y;

                        // d. ʹ��Unity��׼����������������ת��
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