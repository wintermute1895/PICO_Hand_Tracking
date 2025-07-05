// HandDataPacket.cs
using System;

// 这些 [Serializable] 标签是让Unity的JsonUtility能够处理这些类的关键
[Serializable]
public class HandDataPacket
{
    public string hand; // "Left" 或 "Right"
    public bool isActive;
    public float confidence;
    public JointData[] joints;
}

[Serializable]
public class JointData
{
    public int id;
    // 位置
    public float posX, posY, posZ;
    // 旋转（四元数）
    public float rotX, rotY, rotZ, rotW;
}