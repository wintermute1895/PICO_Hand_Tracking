// VR�����ĵ�7.3.11_�ƹ�isActive����UDP������
using UnityEngine;
using Unity.XR.PXR;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic; // ��ҪList����̬�����Ч�ؽڵ�

public class HandDataUDPSender : MonoBehaviour
{
    [Header("Network Settings")]
    public string pcIpAddress = "192.168.208.15"; // ���ٴ�ȷ��������PC��IP
    public int port = 9999;

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

        // �������޸ġ���������ֻ���API�����Ƿ�ɹ������ټ��isActive > 0
        if (success)
        {
            Transform playerOrigin = mainCamera.transform.parent ?? mainCamera.transform;

            // ʹ��List����̬�洢��Ч�Ĺؽڵ�����
            List<JointData> validJoints = new List<JointData>();

            for (int i = 0; i < jointLocations.jointCount; i++)
            {
                var joint = jointLocations.jointLocations[i];
                bool isPositionValid = (joint.locationStatus & HandLocationStatus.PositionValid) != 0;
                bool isRotationValid = (joint.locationStatus & HandLocationStatus.OrientationValid) != 0;

                // ������Ȼֻ����������Ч�Ĺؽڵ�
                if (isPositionValid && isRotationValid)
                {
                    // --- ʹ������������֤��������任�߼� ---
                    Vector3 jointLocalPosition = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);
                    Quaternion jointLocalRotation = new Quaternion(joint.pose.Orientation.x, joint.pose.Orientation.y, joint.pose.Orientation.z, joint.pose.Orientation.w);

                    jointLocalPosition.z = -jointLocalPosition.z;
                    jointLocalRotation.x = -jointLocalRotation.x;
                    jointLocalRotation.y = -jointLocalRotation.y;

                    Vector3 worldPosition = playerOrigin.TransformPoint(jointLocalPosition);
                    Quaternion worldRotation = playerOrigin.rotation * jointLocalRotation;

                    // ����Ч�Ĺؽڵ�������ӵ�List��
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

            // ֻ�е�������һ����Ч�ؽڵ�ʱ�����ǲŷ������ݰ�
            if (validJoints.Count > 0)
            {
                HandDataPacket packet = new HandDataPacket
                {
                    hand = hand.ToString(),
                    // ��ʹ�����ƹ���isActive��������Ȼ������ԭʼֵ����ȥ������PC���˽����
                    isActive = jointLocations.isActive > 0,
                    // confidence = jointLocations.trackingConfidence, // ���SDK�������Ա������ȡ��ע��
                    joints = validJoints.ToArray() // ��Listת��Ϊ����
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
                // ��ʹ���óɹ�����û���κ�һ���ؽڵ�����Ч��
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