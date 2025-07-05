// VR�����ĵ�7.3.6_���ݾɰ�SDK��������������
using UnityEngine;
using Unity.XR.PXR;
using System.Collections.Generic;

public class HandVisualizer : MonoBehaviour
{
    public HandType handType;
    public GameObject jointPrefab;

    // ���ǽ��ٴ�ʹ���������Ϊ��ɿ��Ĳο���
    private Camera mainCamera;

    private HandJointLocations handJointLocations = new HandJointLocations();
    private List<GameObject> jointObjects = new List<GameObject>();

    void Start()
    {
        // �������������ȷ��������ȷ���
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
        // ����Ҳ����������ִ���κβ���
        if (mainCamera == null) return;

        bool success = PXR_HandTracking.GetJointLocations(handType, ref handJointLocations);

        if (success && handJointLocations.isActive > 0)
        {
            // ��ȡ������ڵ� XR Origin ���丸����� transform���������ǵġ���ҡ��������еĲο�ϵ��
            Transform playerOrigin = mainCamera.transform.parent;
            if (playerOrigin == null) // ������û�и����������������Ϊ�ο�
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

                    // ֻ�е�λ�ú���ת����Чʱ�����ǲŸ���
                    if (isPositionValid && isRotationValid)
                    {
                        jointObj.SetActive(true);

                        // a. ��PICO�ľֲ��������תת��ΪUnity����
                        Vector3 jointLocalPosition = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);
                        Quaternion jointLocalRotation = new Quaternion(joint.pose.Orientation.x, joint.pose.Orientation.y, joint.pose.Orientation.z, joint.pose.Orientation.w);

                        // b. ʹ�� TransformPoint �� TransformRotation ������ɿ�������ϵת��
                        //    ��Ὣ��������ԭ��ľֲ����꣬��ȷ��ת��������ռ���
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