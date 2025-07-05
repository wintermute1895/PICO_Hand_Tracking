import socket
import json

# 与Unity中设置的IP和端口保持一致
# IP设为 '0.0.0.0' 表示监听本机所有网络接口
UDP_IP = "0.0.0.0" 
UDP_PORT = 9999

# 创建socket对象
sock = socket.socket(socket.AF_INET, # Internet
                     socket.SOCK_DGRAM) # UDP

# 绑定IP和端口
sock.bind((UDP_IP, UDP_PORT))

print(f"UDP Receiver started. Listening on port {UDP_PORT}...")
print("Press Ctrl+C to stop.")

try:
    while True:
        # 使用足够大的缓冲区接收数据
        data, addr = sock.recvfrom(8192) 
        
        try:
            # 解码并解析JSON字符串
            json_string = data.decode('utf-8')
            hand_data = json.loads(json_string)
            
            # 获取基本信息
            hand_type = hand_data.get('hand', 'Unknown')
            is_active = hand_data.get('isActive', False)
            
            # 打印一个清晰的头部，表明是哪只手的数据
            print(f"\n--- Received data for {hand_type} Hand (IsActive: {is_active}) ---")
            
            # --- 核心修改部分：遍历并打印所有关节点 ---
            joints = hand_data.get('joints')
            if joints:
                print(f"  Received {len(joints)} valid joints:")
                for joint in joints:
                    # 确保关节点数据本身不是null
                    if joint:
                        # 使用.get()安全地获取数据，避免因缺少键而报错
                        joint_id = joint.get('id', -1)
                        pos_x = joint.get('posX', 0.0)
                        pos_y = joint.get('posY', 0.0)
                        pos_z = joint.get('posZ', 0.0)
                        rot_x = joint.get('rotX', 0.0)
                        rot_y = joint.get('rotY', 0.0)
                        rot_z = joint.get('rotZ', 0.0)
                        rot_w = joint.get('rotW', 1.0)
                        
                        # 使用格式化字符串打印，保持对齐和可读性
                        print(f"    ID: {joint_id:02d} | Pos: ({pos_x:7.3f}, {pos_y:7.3f}, {pos_z:7.3f}) | Rot: ({rot_x:7.3f}, {rot_y:7.3f}, {rot_z:7.3f}, {rot_w:7.3f})")
            else:
                print("  No joint data found in this packet.")
            # --- 修改结束 ---

        except (UnicodeDecodeError, json.JSONDecodeError) as e:
            print(f"Error decoding packet: {e} | Raw data length: {len(data)}")
        except Exception as e:
            print(f"An unexpected error occurred: {e}")

except KeyboardInterrupt:
    print("\nReceiver stopped by user.")
finally:
    # 确保程序退出时关闭socket
    sock.close()
    print("Socket closed.")