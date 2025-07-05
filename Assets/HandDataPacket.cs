// HandDataPacket.cs
using System;

// ��Щ [Serializable] ��ǩ����Unity��JsonUtility�ܹ�������Щ��Ĺؼ�
[Serializable]
public class HandDataPacket
{
    public string hand; // "Left" �� "Right"
    public bool isActive;
    public float confidence;
    public JointData[] joints;
}

[Serializable]
public class JointData
{
    public int id;
    // λ��
    public float posX, posY, posZ;
    // ��ת����Ԫ����
    public float rotX, rotY, rotZ, rotW;
}