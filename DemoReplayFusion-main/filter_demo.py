import json
import sys
import os
from datetime import datetime
from collections import defaultdict

def log(msg):
    print(f"[{datetime.now().strftime('%H:%M:%S')}] {msg}")

def main():
    if len(sys.argv) < 2:
        log("用法: 把 .json 文件拖到 filter_demo.py 上")
        input("按回车退出...")
        return

    input_path = sys.argv[1]

    if not os.path.exists(input_path):
        log(f"文件不存在: {input_path}")
        input("按回车退出...")
        return

    base_name = os.path.splitext(os.path.basename(input_path))[0]
    output_dir = os.path.dirname(input_path)
    output_path = os.path.join(output_dir, f"{base_name}_clean.json")

    log(f"输入: {input_path}")
    log(f"文件大小: {os.path.getsize(input_path) / 1024 / 1024:.1f} MB")

    with open(input_path, 'r', encoding='utf-8') as f:
        raw_text = f.read()

    log("正在搜索包含 frame, tick, roundNumber 的数组...")

    candidates = []
    i = 0
    while i < len(raw_text):
        if raw_text[i] == '[':
            depth = 1
            j = i + 1
            while j < len(raw_text) and depth > 0:
                if raw_text[j] == '[':
                    depth += 1
                elif raw_text[j] == ']':
                    depth -= 1
                j += 1
            chunk = raw_text[i:j]
            if '"frame"' in chunk and '"tick"' in chunk and '"roundNumber"' in chunk:
                candidates.append(chunk)
            i = j
        else:
            i += 1

    log(f"找到 {len(candidates)} 个候选数组")

    if not candidates:
        log("❌ 未找到包含 frame, tick, roundNumber 的数组")
        input("按回车退出...")
        return

    # 选元素最多的
    best_data = []
    best_count = 0
    for chunk in candidates:
        try:
            arr = json.loads(chunk)
            if isinstance(arr, list) and len(arr) > best_count:
                if arr and isinstance(arr[0], dict):
                    if 'x' in arr[0] and 'y' in arr[0] and 'steamId' in arr[0]:
                        best_data = arr
                        best_count = len(arr)
        except:
            pass

    if not best_data:
        log("❌ 未找到包含 x, y, steamId 的有效玩家数组")
        input("按回车退出...")
        return

    log(f"选定数组: {best_count} 个元素")

    # 转换字段
    clean_players = []
    for item in best_data:
        if not isinstance(item, dict):
            continue
        if 'x' not in item or 'y' not in item or 'steamId' not in item:
            continue

        x = float(item.get('x', 0))
        y = float(item.get('y', 0))
        z = float(item.get('z', 0))
        yaw = float(item.get('yaw', 0))

        converted = {
            'name': str(item.get('name', '')),
            'steamId': str(item.get('steamId', '')),
            'side': int(item.get('side', 0)),
            'isAlive': bool(item.get('isAlive', True)),
            'health': int(item.get('health', 100)),
            'armor': int(item.get('armor', 0)),
            'hasHelmet': bool(item.get('hasHelmet', False)),
            'hasBomb': bool(item.get('hasBomb', False)),
            'hasDefuseKit': bool(item.get('hasDefuseKit', False)),
            'isDucking': bool(item.get('isDucking', False)),
            'isScoping': bool(item.get('isScoping', False)),
            'isDefusing': bool(item.get('isDefusing', False)),
            'isPlanting': bool(item.get('isPlanting', False)),
            'isGrabbingHostage': bool(item.get('isGrabbingHostage', False)),
            'money': int(item.get('money', 0)),
            'origin': [x, y, z],
            'viewAngle': [0.0, yaw, 0.0],
            'weaponName': str(item.get('activeWeaponName', '')),
            'equipments': item.get('equipments', []),
            'grenades': item.get('grenades', []),
            'pistols': item.get('pistols', []),
            'smgs': item.get('smgs', []),
            'rifles': item.get('rifles', []),
            'heavy': item.get('heavy', []),
            'flashDurationRemaining': float(item.get('flashDurationRemaining', 0))
        }
        clean_players.append((int(item.get('tick', 0)), int(item.get('frame', 0)), int(item.get('roundNumber', 0)), converted))

    log(f"有效玩家帧: {len(clean_players)}")

    # 按 tick 合并玩家
    tick_groups = defaultdict(list)
    for tick, frame, round_num, player in clean_players:
        tick_groups[tick].append((frame, round_num, player))

    clean_frames = []
    for tick, players in tick_groups.items():
        frame_num = players[0][0]
        round_num = players[0][1]
        player_list = [p[2] for p in players]
        clean_frames.append({
            'frame': frame_num,
            'tick': tick,
            'roundNumber': round_num,
            'players': player_list
        })

    clean_frames.sort(key=lambda f: f['tick'])

    log(f"合并后帧数: {len(clean_frames)}")

    # 按回合分组
    rounds = defaultdict(int)
    for f in clean_frames:
        rounds[f['roundNumber']] += 1
    log("回合分布:")
    for r in sorted(rounds):
        log(f"  回合 {r}: {rounds[r]} 帧")

    # 保存
    log(f"正在保存: {output_path}")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(clean_frames, f, separators=(',', ':'))

    output_size = os.path.getsize(output_path) / 1024 / 1024
    log(f"✅ 完成！输出: {output_path} ({output_size:.1f} MB)")
    input("按回车退出...")

if __name__ == "__main__":
    main()