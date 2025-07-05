// Plan B: 使用Unity标准日志系统

using UnityEngine;
using Unity.XR.PXR; // 确保PICO的核心命名空间被引用

public class HandDataLogger : MonoBehaviour
{
    // 在Plan B中，我们不再需要自定义LOG_TAG，但保留一个前缀方便在日志里识别
    private const string LOG_PREFIX = "MyPICOHandData_Output :::";

    // 用于接收手部数据的变量，保持不变
    private HandJointLocations leftHandJointLocations = new HandJointLocations();
    private HandJointLocations rightHandJointLocations = new HandJointLocations();

    void Update()
    {
        // 每一帧都调用数据获取函数
        LogHandJointsData(HandType.HandLeft, ref leftHandJointLocations);
        LogHandJointsData(HandType.HandRight, ref rightHandJointLocations);
    }

    /// <summary>
    /// 使用官方API获取数据，并通过标准的UnityEngine.Debug.Log进行输出
    /// </summary>
    private void LogHandJointsData(HandType hand, ref HandJointLocations jointData)
    {
        // 调用PICO官方API获取手部关节点位置
        bool success = PXR_HandTracking.GetJointLocations(hand, ref jointData);

        // 【关键逻辑】绕过isActive检查，直接处理我们能拿到的所有数据
        if (success)
        {
            // 遍历所有26个关节点
            for (int i = 0; i < jointData.jointCount; i++)
            {
                var joint = jointData.jointLocations[i];

                // 只处理位置数据有效的关节点
                if ((joint.locationStatus & HandLocationStatus.PositionValid) != 0)
                {
                    // 将PICO的坐标结构转换为Unity的Vector3
                    Vector3 position = new Vector3(joint.pose.Position.x, joint.pose.Position.y, joint.pose.Position.z);

                    // 准备好我们最终要输出的日志信息
                    string logMessage = $"{LOG_PREFIX} [{hand}] Joint ID: {i}, Pos: {position.ToString("F3")}";

                    // 【核心】使用最标准的UnityEngine.Debug.Log来打印
                    UnityEngine.Debug.Log(logMessage);
                }
            }
        }
    }
}