// VR开发7.3.11_绕过isActive检查的UDP发送器
using UnityEngine;
using Unity.XR.PXR;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic; // 需要List来动态添加有效关节点

public class HandDataUDPSender : MonoBehaviour
{
    [Header("Network Settings")]
    public string pcIpAddress = "192.168.208.15"; // PC的IP（注意是PC的而不是PICO设备的，两个设备要处于同一局域网下，用ADB看PICO的IP)
    public int port = 9999; //这个脚本里可以添加一个逻辑处理Unity里IP输入前后的空格，但我暂时懒得写了，在Unity里只要改外部的ip就可以，不用每次打包都改代码里的ip，代码里的只是一个示例

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private Camera mainCamera;

    private HandJointLocations leftHandData = new HandJointLocations();
    private HandJointLocations rightHandData = new HandJointLocations();

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("UDP_SENDER_LOG: Main Camera not found!");
            return;
        }

        try
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(pcIpAddress), port);
            udpClient = new UdpClient();
            Debug.Log($"UDP_SENDER_LOG: Client initialized. Target: {pcIpAddress}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP_SENDER_LOG: Error initializing UDP client: {e.Message}");
        }
    }

    void Update()
    {
        if (udpClient == null || mainCamera == null) return;

        ProcessAndSendHandData(HandType.HandLeft, ref leftHandData);
        ProcessAndSendHandData(HandType.HandRight, ref rightHandData);
    }

    private void ProcessAndSendHandData(HandType hand, ref HandJointLocations jointLocations)
    {
        bool success = PXR_HandTracking.GetJointLocations(hand, ref jointLocations);

        // 【核心修改】现在只检查API调用是否成功，不再检查isActive > 0
        if (success)
        {
            Transform playerOrigin = mainCamera.transform.parent ?? mainCamera.transform;

            // 使用List来动态存储有效的关节点数据
            List<JointData> validJoints = new List<JointData>();

            for (int i = 0; i < jointLocations.jointCount; i++)
            {
                var joint = jointLocations.jointLocations[i];
                bool isPositionValid = (joint.locationStatus & HandLocationStatus.PositionValid) != 0;
                bool isRotationValid = (joint.locationStatus & HandLocationStatus.OrientationValid) != 0;

                // 只处理数据有效的关节点
                if (isPositionValid && isRotationValid)
                {
                    // --- 使用最终验证过的坐标变换逻辑 ---
                    Vector3 jointLocalPosition = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);
                    Quaternion jointLocalRotation = new Quaternion(joint.pose.Orientation.x, joint.pose.Orientation.y, joint.pose.Orientation.z, joint.pose.Orientation.w);

                    jointLocalPosition.z = -jointLocalPosition.z;
                    jointLocalRotation.x = -jointLocalRotation.x;
                    jointLocalRotation.y = -jointLocalRotation.y;

                    Vector3 worldPosition = playerOrigin.TransformPoint(jointLocalPosition);
                    Quaternion worldRotation = playerOrigin.rotation * jointLocalRotation;

                    // 将有效的关节点数据添加到List中
                    validJoints.Add(new JointData
                    {
                        id = i,
                        posX = worldPosition.x,
                        posY = worldPosition.y,
                        posZ = worldPosition.z,
                        rotX = worldRotation.x,
                        rotY = worldRotation.y,
                        rotZ = worldRotation.z,
                        rotW = worldRotation.w
                    });
                }
            }

            // 只有当至少有一个有效关节点时才发送数据包
            if (validJoints.Count > 0)
            {
                HandDataPacket packet = new HandDataPacket
                {
                    hand = hand.ToString(),
                    // 绕过了isActive，但还是把它的原始值发出去，方便PC端了解情况
                    isActive = jointLocations.isActive > 0,
                    // confidence = jointLocations.trackingConfidence, // 3.2.0版本的SDK里没有这个，先注释掉了
                    joints = validJoints.ToArray() // 将List转换为数组
                };

                try
                {
                    string json = JsonUtility.ToJson(packet);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    udpClient.Send(data, data.Length, remoteEndPoint);

                    Debug.Log($"UDP_SENDER_LOG: Sent {data.Length} bytes for {hand} hand with {validJoints.Count} valid joints. (isActive was {jointLocations.isActive})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"UDP_SENDER_LOG: SEND FAILED for {hand}: {e.ToString()}");
                }
            }
            else
            {
                // 即使调用成功，但没有任何一个关节点是有效的
                Debug.LogWarning($"UDP_SENDER_LOG: GetJointLocations for {hand} succeeded, but no valid joints found.");
            }
        }
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            Debug.Log("UDP_SENDER_LOG: UDP Client closed.");
        }
    }
}