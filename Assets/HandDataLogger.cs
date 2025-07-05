// Plan B: ʹ��Unity��׼��־ϵͳ

using UnityEngine;
using Unity.XR.PXR; // ȷ��PICO�ĺ��������ռ䱻����

public class HandDataLogger : MonoBehaviour
{
    // ��Plan B�У����ǲ�����Ҫ�Զ���LOG_TAG��������һ��ǰ׺��������־��ʶ��
    private const string LOG_PREFIX = "MyPICOHandData_Output :::";

    // ���ڽ����ֲ����ݵı��������ֲ���
    private HandJointLocations leftHandJointLocations = new HandJointLocations();
    private HandJointLocations rightHandJointLocations = new HandJointLocations();

    void Update()
    {
        // ÿһ֡���������ݻ�ȡ����
        LogHandJointsData(HandType.HandLeft, ref leftHandJointLocations);
        LogHandJointsData(HandType.HandRight, ref rightHandJointLocations);
    }

    /// <summary>
    /// ʹ�ùٷ�API��ȡ���ݣ���ͨ����׼��UnityEngine.Debug.Log�������
    /// </summary>
    private void LogHandJointsData(HandType hand, ref HandJointLocations jointData)
    {
        // ����PICO�ٷ�API��ȡ�ֲ��ؽڵ�λ��
        bool success = PXR_HandTracking.GetJointLocations(hand, ref jointData);

        // ���ؼ��߼����ƹ�isActive��飬ֱ�Ӵ����������õ�����������
        if (success)
        {
            // ��������26���ؽڵ�
            for (int i = 0; i < jointData.jointCount; i++)
            {
                var joint = jointData.jointLocations[i];

                // ֻ����λ��������Ч�Ĺؽڵ�
                if ((joint.locationStatus & HandLocationStatus.PositionValid) != 0)
                {
                    // ��PICO������ṹת��ΪUnity��Vector3
                    Vector3 position = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);

                    // ׼������������Ҫ�������־��Ϣ
                    string logMessage = $"{LOG_PREFIX} [{hand}] Joint ID: {i}, Pos: {position.ToString("F3")}";

                    // �����ġ�ʹ�����׼��UnityEngine.Debug.Log����ӡ
                    UnityEngine.Debug.Log(logMessage);
                }
            }
        }
    }
}