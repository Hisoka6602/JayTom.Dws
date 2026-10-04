"""使用现有泳道图的矢量样式，展开密集触发、迟到响应及固定位置消费。"""

from __future__ import annotations

import json
from pathlib import Path

from reportlab.lib.colors import HexColor
from reportlab.lib.enums import TA_LEFT
from reportlab.pdfgen.canvas import Canvas

import build_dws_swimlane as d


ROOT = Path(r"D:/WorkSpace/JayTom/JayTom.Dws")
OUT = ROOT / "output/pdf/JayTomDWS_密集包裹延迟相机响应垂直泳道链路.pdf"
QA = ROOT / "tmp/pdfs/dws_dense"
d.H = 6810.0
d.LANE_BOTTOM = 5520.0


def chart(c):
    """从上到下保留时间轴，分开触发次序、结果次序与业务输出。"""
    for i, (title, subtitle, accent, pale, _) in enumerate(d.LANES):
        x = d.LEFT + i * d.LANE_W
        c.setFillColor(HexColor(pale))
        c.setStrokeColor(d.RULE)
        c.setLineWidth(1)
        c.rect(x, d.H - d.LANE_BOTTOM, d.LANE_W, d.LANE_BOTTOM - d.LANE_TOP, fill=1, stroke=1)
        c.setFillColor(HexColor(accent))
        c.rect(x, d.H - d.LANE_TOP - 180, d.LANE_W, 180, fill=1, stroke=0)
        d.paragraph(c, title, x + 8, d.LANE_TOP + 54, d.LANE_W - 16, 58, 23, d.WHITE, True,
                    leading=31, tag=f"lane:{i}")
        d.paragraph(c, subtitle, x + 10, d.LANE_TOP + 117, d.LANE_W - 20, 44, 14, d.WHITE,
                    leading=20, tag=f"lane:{i}.subtitle")

    triggers = []
    for letter, tick, top, number, queue in [
        ("A", "0", 780, 1, "[ A ]"),
        ("B", "300", 1000, 4, "[ A, B ]"),
        ("C", "600", 1220, 7, "[ A, B, C ]"),
    ]:
        plc = d.box(f"trigger-{letter}", 1, top, 186, f"t = {tick} ms\n小车 {letter} 触发",
                    f"发送本车 Id / 创建指令\n同次触发相机拍照\n空车也产生一次触发", number)
        photo = d.box(f"photo-{letter}", 2, top, 186, f"拍照 / 解码 {letter}",
                      "相机内部识别中\n处理耗时可变\n最终结果尚未返回", number + 1)
        slot = d.box(f"slot-{letter}", 3, top, 186, f"创建 {letter} 并保留位置",
                     f"原车 Id + 创建时间\n独立包裹 Timestamp\n待消费队列：{queue}", number + 2)
        d.arrow(c, [plc.east, photo.west])
        row = top - 24
        d.arrow(c, [plc.north, (plc.cx, row), (slot.cx, row), slot.north],
                label=f"创建指令 {letter}", label_at=(688, row))
        triggers.append(slot)
        d.paragraph(c, f"{tick} ms", 24, top + 54, 100, 28, 18, d.MUTED, True,
                    leading=25, tag=f"time:{letter}")
    for a, b in zip(triggers, triggers[1:]):
        d.down(c, a, b)

    d.box("waiting-note", 5, 835, 496, "C 已创建，A 仍在等待", 
          "程序已经收到 A、B、C 三次创建。\n相机结果还没有消费 A 的位置。\n\n相机的第 1 个最终结果仍属于 A；\n第 2 个属于 B；第 3 个属于 C。\n\n本机次序是保序消费依据，\n不冒充相机与小车的硬件序号映射。\n相机响应变慢只会增加等待位置，\n不能把结果交给最新创建的 C。", span=3)

    ra = d.box("result-A", 2, 1550, 226, "t = 650 ms\nA 的最终结果到达", 
               "有效条码 A0001\n示例 ScanTime = 200 ms\n这时 C 已经创建\n第 1 次结果进入组包", 10)
    pa = d.box("pair-A", 3, 1550, 208, "确定 A 的固定位置", 
               "第 1 结果 → 第 1 触发位\n先取出原车 A 的位置\n剩余位置：[ B, C ]\n不按返回时刻搜索 C", 11)
    d.arrow(c, [ra.east, pa.west])
    d.paragraph(c, "650 ms", 24, 1630, 100, 28, 18, d.MUTED, True, leading=25, tag="time:RA")
    d.box("identity-note", 5, 1550, 208, "关键：消费位置，随后只校验原车", 
          "原车查找使用已取出的创建时间。\nA 过期、移除、过滤或校验失败，都不改为 B / C。\n这次 A 的结果与 A 的位置一起结束；后续结果继续原次序。", span=3)
    win = d.box("window-A", 3, 1820, 250, "只校验 A 的窗口", 
                "窗口开启：50-500 ms\n示例时间差 200 ms 通过\n比较 ScanTime - CreateTime\n不直接用 650 ms 返回耗时", 12)
    fail = d.box("window-reject", 1, 1820, 250, "失败也只关闭 A", 
                 "若实际时间差 = 650 ms\n则超过 500 ms 上限\n或 A 已过期 / 被移除\n拒绝本结果，B、C 继续\n不生成业务完成输出", warning=True)
    bind = d.box("bind-A", 3, 2100, 190, "绑定 A0001 → A", 
                 "原车 Id、Timestamp 不变\n保留相机与采集时间\nA 齐套后独立发布完成", 13)
    d.down(c, pa, win)
    d.arrow(c, [win.west, fail.east], d.RED, True, "原车失效 / 越界", (685, 1880))
    d.down(c, win, bind, "原车通过", d.GREEN)
    d.box("timestamp-note", 5, 1850, 395, "650 ms 到达，不等于时间差 650 ms", 
          "例：A 创建于 0 ms，采集时间标为 200 ms，\n识别结果到 650 ms 才回调。\n窗口时间差仍为 200 ms。\n\n如果 SDK 把回调时间写入 ScanTime，\n或设备时钟存在偏差，窗口判断会不同。\n先看实际事件时间字段，再判断是否超窗。\n源码不会自动修正设备时钟偏差。", span=3)

    previous_pair = bind
    for letter, tick, scan, top, ordinal, number in [
        ("B", "950", "500", 2340, 2, 14),
        ("C", "1250", "800", 2570, 3, 16),
    ]:
        result = d.box(f"result-{letter}", 2, top, 200, f"t = {tick} ms\n{letter} 的结果到达", 
                       f"第 {ordinal} 个最终结果\n采集时间 {scan} ms\n有效码 / NoRead 均算一次", number)
        paired = d.box(f"pair-{letter}", 3, top, 190, f"只消费原车 {letter}", 
                       f"第 {ordinal} 结果 → 第 {ordinal} 触发位\n通过窗口才绑定原车\n{('队列剩余：[ C ]' if letter == 'B' else '本组三个位置已消费')}\n前车失败也不顺延", number + 1)
        d.arrow(c, [result.east, paired.west])
        d.down(c, previous_pair, paired)
        if letter == "B":
            d.arrow(c, [fail.south, (fail.cx, 2310), (paired.cx, 2310), paired.north],
                    d.RED, True, "A 结束，B 仍对应 B", (685, 2310))
        previous_pair = paired
        d.paragraph(c, f"{tick} ms", 18, top + 57, 106, 28, 17, d.MUTED, True,
                    leading=25, tag=f"time:R{letter}")

    fusion = d.box("fusion", 3, 2920, 238, "每个原车的融合 / 过滤", 
                   "单相机直接处理\n多相机各自第 N 次成组\n有效码优先于 NoRead\n冲突 / 缺帧仅关闭原组\n迟到帧不重新开启原组", 18)
    collect = d.box("camera-fusion", 2, 2920, 218, "多相机 / 其他输入", 
                    "各相机最终结果必返保序\n按相机内部次数汇合\n重量等按现场配置获取\n不开称重时使用默认值")
    nr = d.box("noread-note", 5, 2920, 218, "NoRead 开关的实际含义", 
               "开启：消费原车，保留 NoRead 绑定 / 完成，按正常配置规则分拣。\n关闭：同样消费原车与该结果，随后丢弃业务输出。\nNoRead 过滤配置继续有效；开启不代表所有空读都保留。", span=3)
    discard = d.box("discard", 1, 3230, 220, "消费原车后丢弃", 
                    "NoRead 关闭 / 被过滤\n多相机冲突 / 融合缺帧\n本车不显示、不上传\n不写包裹业务主记录\n不下发落格；保留诊断日志", warning=True)
    fields = d.box("fields", 3, 3230, 220, "原车数据绑定并齐套", 
                   "选中的条码 / NoRead\n原车 Id + Timestamp\n相机、扫码、创建信息\n重量、体积模型齐备\n只发布一次完成事件", 19)
    d.arrow(c, [collect.east, fusion.west])
    d.down(c, fusion, fields, "允许保留且原车通过", d.GREEN)
    d.arrow(c, [fusion.south, (fusion.cx, 3180), (discard.cx, 3180), discard.north],
            d.RED, True, "过滤 / 丢弃仅影响本车", (685, 3180))

    complete = d.box("completed", 3, 3620, 172, "A 齐套即独立完成", 
                     "A 的完成事件并行分发\n后半段展示 A 的业务链路\n实际不等待 B、C 返回", 20)
    ui = d.box("ui", 0, 3900, 200, "显示 A 的完成结果", 
               "原车 Id 与条码\nNoRead 保留时展示\n界面独立通知更新\n数据查询按积压让步", 21)
    upload = d.box("upload", 4, 3900, 200, "上传 / 查询 A", 
                   "无损工作队列\n携带原车身份与条码\n响应可先后不同\n回流仍属原请求 A", 22)
    persist = d.box("persist", 7, 3900, 200, "落库 / 存图并行", 
                    "主记录关联包裹标识\n创建指令、图片元数据\n存图独立队列执行\nPLC 无需等待", 23)
    api = d.box("api", 5, 4170, 200, "返回 A 的业务响应", 
                "目标信息或失败\n响应可晚于 B 返回\n仍保留原请求身份", 24)
    d.down(c, fields, complete)
    for target in [ui, upload, persist]:
        d.arrow(c, [complete.south, (complete.cx, 3850), (target.cx, 3850), target.north])
    d.labels.append(("A 完成后并行继续；B、C 独立处理", 1270, 3850, d.ARROW))
    d.arrow(c, [upload.south, (upload.cx, 4134), (api.cx, 4134), api.north],
            label="HTTP 查询", label_at=(1238, 4134))

    response = d.box("api-response", 4, 4470, 240, "响应回流仍携带 A 身份", 
                     "原车 Id、创建时间\nTimestamp、条码、ScanTime\n校验身份、重复与车号复用\n不按 HTTP 完成顺序选车")
    rules = d.box("rules", 6, 4470, 240, "为 A 选择落格", 
                  "正常：规则目标口\nNoRead：正常规则\n失败：配置异常口\n无异常口则记录未下发\n身份失效禁止错车回复", 25)
    send = d.box("send", 6, 4750, 174, "编码并回复 A", 
                 "格口 + 原车 A 的 Id\n同一连接顺序发送\n延时及写入前复核身份", 26)
    plc = d.box("plc-command", 1, 4750, 174, "PLC 接收 A 格口", 
                "正常口或配置异常口\n根据 A 的机械位置执行\n不会因 C 较新而改发 C", 27)
    d.arrow(c, [api.south, (api.cx, 4380), (response.cx, 4380), response.north],
            label="A 的业务响应", label_at=(1240, 4370))
    d.arrow(c, [response.east, rules.west], label="原请求身份", label_at=(1350, response.cy))
    d.down(c, rules, send, "有效身份、格口与绑定", d.GREEN)
    d.arrow(c, [send.west, plc.east], label="原车 A 的 Id + 目标格口", label_at=(1050, 4810))

    fall = d.box("plc-fall", 1, 5100, 188, "PLC 回执原车 A", 
                 "真实回执进入连接\n按配置更新 / 移除\n已发送不等于已落格", 28)
    confirm = d.box("confirm", 6, 5100, 200, "确认 A 的格口", 
                    "发送后记理论格口\n首次回执记物理格口\n重复回执忽略\n60 秒无回执软件补记", 29)
    final = d.box("closed", 7, 5330, 176, "更新 A 的完整记录", 
                  "指令、上传、图片、格口\n关联包裹 Timestamp\n回传按接口配置执行", 30)
    d.down(c, plc, fall)
    d.arrow(c, [fall.east, (574, fall.cy), (574, 5060), (confirm.cx, 5060), confirm.north],
            label="回执对应原车 A", label_at=(1000, 5060))
    d.arrow(c, [confirm.south, (confirm.cx, 5304), (final.cx, 5304), final.north])
    d.box("fall-timeout-note", 3, 5100, 188, "软件补记须与真实回执区分", 
          "60 秒未收到回执时，当前代码沿用理论口补记正常落格。\n新增日志以 isSynthetic=true 标明；这不代表 PLC 已确认。", span=3, warning=True)

    for top, text in [
        (690, "阶段 1 | 0-600 ms：A、B、C 密集触发，创建与拍照并行，结果尚未返回"),
        (1460, "阶段 2 | C 已创建才返回 A：固定次序消费，窗口只校验原车，后车不顺延"),
        (2820, "阶段 3 | 每个原车的通用处理：多相机融合、NoRead / 过滤、窗口与齐套"),
        (3520, "阶段 4 | A 一旦齐套即可继续：界面、API、持久化与存图并行"),
        (4388, "阶段 5 | API 可乱序完成：响应保留原车身份，选择目标口或配置异常口"),
        (4990, "阶段 6 | 向 PLC 回复原车 A，区分写入成功、真实落格及软件补记"),
    ]:
        d.stage(c, top, text)
    overflows = []
    for b in d.BOXES.values():
        try:
            d.draw_box(c, b)
        except ValueError as error:
            overflows.append(str(error))
    if overflows:
        raise ValueError("\n".join(overflows))
    for text, x, y, color in d.labels:
        d.label(c, text, x, y, color)


def notes(c):
    """补充三种窗口结论、保序条件及现场排查字段。"""
    d.stage(c, 5600, "补充 | 相同的 650 ms 回调，窗口结论取决于实际 ScanTime")
    widths = [405, 545, 820]
    rows = [
        ("现场事件", "当前窗口比较", "处理结论（50-500 ms 示例）"),
        ("A 创建 0 ms；650 ms 收到结果", "ScanTime = 200 ms；差值 200 ms", "消费 A，窗口通过，条码仍绑定 A；C 的位置保持独立。"),
        ("A 创建 0 ms；650 ms 收到结果", "ScanTime = 650 ms；差值 650 ms", "消费 A，拒绝本车结果；B、C 后续继续按自己的位置处理。"),
        ("A 的活动包裹已经移除 / 过期", "原车占位仍保留到结果消费", "A 的结果结束于 A 的位置；不会寻找仍存活的 B / C。"),
    ]
    top = 5690
    for row_index, row in enumerate(rows):
        height = 58 if row_index == 0 else 76
        x = d.LEFT
        for col_index, text in enumerate(row):
            fill = HexColor("#3D6078") if row_index == 0 else (d.WHITE if row_index % 2 else HexColor("#EFF5FA"))
            c.setFillColor(fill)
            c.setStrokeColor(d.RULE)
            c.setLineWidth(0.8)
            c.rect(x, d.H - top - height, widths[col_index], height, fill=1, stroke=1)
            d.paragraph(c, text, x + 16, top + 14, widths[col_index] - 32, height - 22,
                        17 if row_index else 19, d.WHITE if row_index == 0 else d.INK,
                        row_index == 0, TA_LEFT, 23, tag=f"table:{row_index}:{col_index}")
            x += widths[col_index]
        top += height
    d.rr(c, d.LEFT, 6030, d.TOTAL, 170, d.RED_LIGHT, HexColor("#EA6368"), 18, 1.5)
    d.paragraph(c, "防错位保证的前提：同次触发、每次最终必返、每台相机严格保序", d.LEFT + 24,
                6046, d.TOTAL - 48, 35, 22, d.RED, True, TA_LEFT, 30)
    d.paragraph(c, "有效下位机创建不受普通创建防抖间隔限制；指令解析失败或占位创建被拒绝时，仍保留本次消费位置。\n单包窗口拒绝、NoRead 丢弃及融合失败都只结束对应原车；显式停止运行才重置本轮消费状态。\n没有硬件关联标识时，若现场改为漏结果或乱序返回，单靠这些队列无法可靠恢复对应关系。",
                d.LEFT + 26, 6093, d.TOTAL - 52, 95, 18, d.INK, align=TA_LEFT, leading=26)
    d.paragraph(c, "排查密集场景的落盘证据", d.LEFT, 6250, d.TOTAL, 38, 24, d.INK, True,
                TA_LEFT, 32)
    for top, text in [
        (6303, "先按 packageTimestamp 找一次包裹；再核对 cartId、createdAt / creationTimeTicks、triggerSequence、cameraOrdinal。车号循环复用时不要只按 Id 判断。"),
        (6340, "主要阶段：trigger.slot.queued → camera.result.ordered → camera.paired → barcode.assignment.selected / camera.pair.rejected → package.completed / package.discarded。"),
        (6377, "后续阶段：api.query.completed → sorting.reply.queued → plc.send.started / plc.send.failed → sorting.reply.sent → plc.fall.received / plc.fall.timeout。"),
        (6414, "observedAt 是窗口使用的 SDK 扫码时间；processingAt 是消费处理时刻。日志同时记录窗口边界、时间差、原车身份、拒绝原因与队列水位。"),
    ]:
        d.paragraph(c, text, d.LEFT, top, d.TOTAL, 31, 17, d.MUTED, align=TA_LEFT, leading=24)
    d.paragraph(c, "代码核对：YunShanPackageBackgroundService / PackageSessionRegistry；SubmitApiBackgroundService；DefaultSortingService / DefaultSortingConnectionService；PackageExitUpdateBackgroundService。",
                d.LEFT, 6500, d.TOTAL, 57, 16, d.MUTED, align=TA_LEFT, leading=23)
    c.setStrokeColor(d.RULE)
    c.line(d.LEFT, d.H - 6630, d.RIGHT, d.H - 6630)
    d.paragraph(c, "JayTom DWS 当前工作区 | 密集包裹与延迟相机响应 | 1 / 1", d.LEFT, 6660,
                1250, 35, 18, d.MUTED, align=TA_LEFT, leading=25)
    c.setFillColor(HexColor("#E3E8ED"))
    c.setFont("Helvetica-BoldOblique", 52)
    c.drawRightString(d.RIGHT, d.H - 6755, "JayTom DWS")


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    c = Canvas(str(OUT), pagesize=(d.W, d.H), pageCompression=1)
    c.setTitle("JayTom DWS 密集包裹延迟相机响应垂直泳道链路")
    c.setAuthor("JayTom DWS")
    c.setSubject("C 触发后才收到 A 的结果：固定原车消费、窗口、NoRead、融合、上传与 PLC 落格")
    c.setFillColor(d.BG)
    c.rect(0, 0, d.W, d.H, fill=1, stroke=0)
    d.paragraph(c, "JayTom DWS：密集包裹延迟相机响应链路", d.LEFT, 84, 1490, 72,
                36, d.INK, True, TA_LEFT, 48)
    d.rr(c, 1675, 79, 225, 65, HexColor("#EAF3FF"), HexColor("#75A6EF"), 26, 1.4)
    d.paragraph(c, "单页垂直泳道", 1690, 98, 195, 34, 21, HexColor("#215F9E"), True, leading=29)
    d.paragraph(c, "300 ms 连续触发 | C 已创建才返回 A 的条码 | 2026-10-04 当前工作区实现",
                d.LEFT, 163, d.TOTAL, 52, 22, d.MUTED, align=TA_LEFT, leading=30)
    d.rr(c, d.LEFT, 240, d.TOTAL, 122, d.RED_LIGHT, HexColor("#F18D92"), 18, 1.2)
    d.paragraph(c, "场景前提", d.LEFT + 28, 266, 130, 38, 22, d.RED, True, TA_LEFT, 30)
    d.paragraph(c, "创建包裹与拍照来自同次物理触发；每次必返最终结果，且每台相机严格按触发顺序返回。\n图中毫秒数是相对时间示例；窗口另按 SDK ScanTime 比较，设备时钟可能存在差异。",
                d.LEFT + 176, 263, d.TOTAL - 210, 82, 20, d.INK, align=TA_LEFT, leading=29)
    chart(c)
    notes(c)
    d.validate_layout()
    c.showPage()
    c.save()
    (QA / "layout.json").write_text(json.dumps(d.layout, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(OUT), "pages": 1, "lanes": 8, "numbered_steps": 30,
                      "size_points": [d.W, d.H], "bytes": OUT.stat().st_size}, ensure_ascii=False))


if __name__ == "__main__":
    main()
