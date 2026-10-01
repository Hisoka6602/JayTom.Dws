from __future__ import annotations

import os
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from reportlab.lib.colors import HexColor
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A3, landscape
from reportlab.pdfgen.canvas import Canvas

import build_swimlane as b


ROOT = Path(r"D:\WorkSpace\JayTom\JayTom.Dws")
DETAIL_PDF = ROOT / "tmp" / "pdfs" / "detail_pages.pdf"
MERGED_PDF = ROOT / "tmp" / "pdfs" / "swimlane_merged.pdf"


def page_header(c: Canvas, title: str, subtitle: str, page_no: int, total_pages: int = 3):
    width, height = landscape(A3)
    c.setFillColor(b.BG)
    c.rect(0, 0, width, height, fill=1, stroke=0)
    c.setFillColor(b.INK)
    c.setFont("DengBold", 24)
    c.drawString(36, height - 47, title)
    c.setFillColor(b.MUTED)
    c.setFont("Deng", 8.5)
    c.drawString(38, height - 67, subtitle)
    b.pill(c, width - 170, height - 58, 134, 25, f"泳道详图 {page_no} / {total_pages}",
           HexColor("#EAF1F8"), color=b.INK, stroke=HexColor("#EAF1F8"), size=8.5)
    c.setFillColor(b.MUTED)
    c.setFont("Deng", 6.8)
    c.drawRightString(width - 38, height - 73, "内部资料 · 注意保密")
    c.setStrokeColor(b.LINE)
    c.setLineWidth(0.8)
    c.line(34, height - 88, width - 34, height - 88)
    return width, height


def page_footer(c: Canvas, page_no: int, total_pages: int = 3):
    width, _ = landscape(A3)
    c.setFillColor(b.MUTED)
    c.setFont("Deng", 6.8)
    c.drawString(34, 24, "依据：《圆通速递矩阵标准协议文档-窄带》（2026-08-24）｜ID：01 PLC，02 后台，03 DWS")
    c.setFillColor(HexColor("#E7EDF5"))
    c.setFont("DengBold", 15)
    c.drawRightString(width - 34, 38, "YTO")
    c.setFillColor(b.MUTED)
    c.setFont("Deng", 6.8)
    c.drawRightString(width - 34, 24, f"参考风格版 3.0  ·  {page_no} / {total_pages}")


def lane_grid(c: Canvas, lane_titles, body_bottom=108):
    width, height = landscape(A3)
    left, right = 34, width - 34
    header_top, header_h = height - 103, 43
    body_top = header_top - header_h
    lane_w = (right - left) / len(lane_titles)
    fills = [HexColor("#F8FBFE"), b.WHITE]
    centers = []
    for i, (title, sub) in enumerate(lane_titles):
        x = left + i * lane_w
        centers.append(x + lane_w / 2)
        c.setFillColor(fills[i % 2])
        c.setStrokeColor(b.LINE)
        c.rect(x, body_bottom, lane_w, body_top - body_bottom, fill=1, stroke=1)
        c.setFillColor(HexColor("#EAF1F8"))
        c.rect(x, header_top - header_h, lane_w, header_h, fill=1, stroke=0)
        accent = [b.TEAL, b.PURPLE, b.BLUE, b.AMBER, b.RED][i % 5]
        c.setFillColor(accent)
        c.rect(x, header_top - 4, lane_w, 4, fill=1, stroke=0)
        c.setFillColor(b.INK)
        c.setFont("DengBold", 10)
        c.drawCentredString(x + lane_w / 2, header_top - 17, title)
        c.setFillColor(b.MUTED)
        c.setFont("Deng", 6.7)
        c.drawCentredString(x + lane_w / 2, header_top - 31, sub)
    return left, right, body_top, body_bottom, lane_w, centers


def step_mark(c: Canvas, x: float, y: float, num: int, color=b.PURPLE):
    c.setFillColor(color)
    c.circle(x, y, 11, fill=1, stroke=0)
    c.setFillColor(b.WHITE)
    c.setFont("DengBold", 8)
    c.drawCentredString(x, y - 3, str(num))


def command_pill(c: Canvas, x, y, w, h, text, color, size=7.3):
    pale = (
        b.PURPLE_PALE if color == b.PURPLE else
        b.TEAL_PALE if color == b.TEAL else
        b.AMBER_PALE if color == b.AMBER else
        b.RED_PALE if color == b.RED else
        HexColor("#EDF3FF")
    )
    b.pill(c, x, y, w, h, text, pale, color=color, stroke=color, size=size)


def detail_arrow(c: Canvas, x1, x2, y, cmd, direction, fields, port, color=b.PURPLE, reverse=False):
    if reverse:
        b.arrow(c, x2, y, x1, y, color=color, width=2.2, head=8)
    else:
        b.arrow(c, x1, y, x2, y, color=color, width=2.2, head=8)
    cx = (x1 + x2) / 2
    pale = b.PURPLE_PALE if color == b.PURPLE else b.TEAL_PALE if color == b.TEAL else HexColor("#EDF3FF")
    b.pill(c, cx - 31, y + 18, 62, 18, cmd, pale, color=color, stroke=color, size=7.8)
    b.pill(c, cx + 37, y + 18, 72, 18, f"端口 {port}", b.WHITE, color=color,
           stroke=b.LINE, size=6.7)
    b.draw_paragraph(c, direction, cx - 115, y + 7, 230, 15, size=8, align=TA_CENTER, bold=True, leading=9)
    b.draw_paragraph(c, fields, cx - 180, y - 7, 360, 28, size=6.8, color=b.MUTED,
                     align=TA_CENTER, leading=8)


def diamond(c: Canvas, cx, cy, w, h, text, fill=b.AMBER_PALE, stroke=b.AMBER):
    p = c.beginPath()
    p.moveTo(cx, cy + h / 2)
    p.lineTo(cx + w / 2, cy)
    p.lineTo(cx, cy - h / 2)
    p.lineTo(cx - w / 2, cy)
    p.close()
    c.setFillColor(fill)
    c.setStrokeColor(stroke)
    c.setLineWidth(1)
    c.drawPath(p, fill=1, stroke=1)
    b.draw_paragraph(c, text, cx - w * 0.30, cy + 5, w * 0.60, 18, size=7.5,
                     align=TA_CENTER, bold=True, leading=8)


def mini_rule(c: Canvas, x, y, w, title, body, accent, pale):
    b.round_rect(c, x, y, w, 48, b.WHITE, b.LINE, 7, 0.8)
    c.setFillColor(pale)
    c.roundRect(x, y + 29, w, 19, 7, fill=1, stroke=0)
    c.rect(x, y + 29, w, 6, fill=1, stroke=0)
    c.setFillColor(accent)
    c.setFont("DengBold", 7.5)
    c.drawString(x + 10, y + 35, title)
    b.draw_paragraph(c, body, x + 10, y + 24, w - 20, 22, size=6.4, color=b.MUTED, leading=7.5)


def draw_core_detail(c: Canvas):
    width, height = page_header(
        c,
        "核心分拣业务 - 详细泳道",
        "从包裹触发到落格结果闭环｜展开命令字、触发条件、字段与异常分支",
        2,
    )
    lane_titles = [
        ("现场 / 包裹", "光电触发与货物状态"),
        ("窄带 PLC", "触发号、跟车及设备控制"),
        ("条码威视 / DWS", "读码、路由和协议服务"),
        ("分拣机构 / 滑槽", "动作执行与落格感知"),
    ]
    left, right, body_top, body_bottom, lane_w, centers = lane_grid(c, lane_titles, body_bottom=112)
    box_w = lane_w - 38

    # Row bands and sequence markers.
    row_bounds = [(676, 610), (610, 532), (532, 460), (460, 384), (384, 312), (312, 228), (228, 112)]
    row_colors = [HexColor("#F8FBFE"), b.WHITE, HexColor("#F8FBFE"), b.WHITE,
                  HexColor("#F8FBFE"), b.WHITE, HexColor("#F2FAF8")]
    for (top, bottom), fill in zip(row_bounds, row_colors):
        c.setFillColor(fill)
        c.rect(left, bottom, right - left, top - bottom, fill=1, stroke=0)
        c.setStrokeColor(b.LINE)
        c.setLineWidth(0.45)
        c.line(left, bottom, right, bottom)
    for i, y in enumerate([646, 570, 496, 421, 349, 271, 177], start=1):
        step_mark(c, left + 16, y, i, b.TEAL if i >= 6 else b.PURPLE)

    # 1. Trigger and bind.
    b.process_box(c, centers[0] - box_w / 2, 623, box_w, 47,
                  "包裹触发六面扫光电", "流程开始，设备进入读码窗口", fill=b.PURPLE_PALE, accent=b.PURPLE)
    b.arrow(c, centers[0] + box_w / 2, 646, centers[1] - box_w / 2, 646,
            color=b.LINE, width=1.3, head=6)
    b.process_box(c, centers[1] - box_w / 2, 618, box_w, 54,
                  "生成触发号并绑定小车", "0-65535 循环；记录设备台号与触发位置；动态秤场景可带开始/结束标志",
                  fill=b.WHITE, accent=b.PURPLE)

    # 2. Trigger message.
    detail_arrow(c, centers[1] + 25, centers[2] - 25, 574, "0x68",
                 "PLC → DWS：触发号发送",
                 "设备台号(1B)｜触发号(2B)｜可选触发位置｜可选扩展字段；尾部触发不支持扩展字段",
                 "3001")

    # 3. Read and decide.
    b.process_box(c, centers[2] - box_w / 2, 477, box_w, 48,
                  "关联触发号、读码、查询格口方案", "以触发号匹配当前货物；结合条码、三段码及锁格状态进行路由",
                  fill=b.WHITE, accent=b.BLUE)
    diamond(c, centers[2], 468, 98, 34, "有可用格口？")
    command_pill(c, centers[2] + 54, 458, 50, 17, "否：0x00", b.AMBER, size=6.8)

    # 4. Chute assignment.
    detail_arrow(c, centers[1] + 25, centers[2] - 25, 423, "0x67",
                 "DWS → PLC：下发格口",
                 "台号｜条码长度+条码｜'|'｜三段码长度+三段码｜'|'｜格口｜'|'｜触发号｜可选扩展字段",
                 "3001", reverse=True)

    # 5. Ack.
    b.process_box(c, centers[1] - box_w / 2, 328, box_w, 48,
                  "接收、校验并保存关联", "保存条码、触发号、格口与设备台号；用于后续跟车和问题定位",
                  fill=b.WHITE, accent=b.PURPLE)
    detail_arrow(c, centers[1] + 25, centers[2] - 25, 321, "0x64",
                 "PLC → DWS：格口接收回复",
                 "设备台号(固定 0x01)｜条码长度｜条码｜'|'｜格口号",
                 "3000")

    # 6. Execute.
    b.process_box(c, centers[1] - box_w / 2, 247, box_w, 52,
                  "跟车定位、到点触发", "根据触发号定位当前小车；到达动作位置后输出分拣控制",
                  fill=b.TEAL_PALE, accent=b.TEAL)
    b.arrow(c, centers[1] + box_w / 2, 273, centers[3] - box_w / 2, 273,
            color=b.TEAL, width=2.2, head=8)
    command_pill(c, (centers[1] + centers[3]) / 2 - 39, 282, 78, 18, "驱动分拣", b.TEAL, size=7.2)
    b.process_box(c, centers[3] - box_w / 2, 247, box_w, 52,
                  "机构动作，包裹落入滑槽", "现场传感器形成落格结果；成功或失败均进入结果回传",
                  fill=b.TEAL_PALE, accent=b.TEAL)

    # 7. Result and failure branch.
    detail_arrow(c, centers[1] + 25, centers[2] - 25, 190, "0x66",
                 "PLC → DWS：落格结果回传",
                 "台号｜条码长度+条码｜'|'｜格口｜'|'｜结果｜'|'｜失败原因｜可选首/尾车范围",
                 "3000", color=b.TEAL)
    b.process_box(c, centers[2] - box_w / 2, 124, box_w, 48,
                  "记录分拣闭环", "0x01 成功；0x00 失败时按原因进入设备排查、间距优化、迷失追踪或锁格处理",
                  fill=b.WHITE, accent=b.TEAL)
    b.process_box(c, centers[3] - box_w / 2, 124, box_w, 48,
                  "失败原因映射", "0x01 摆轮故障｜0x02 间距过小｜0x03 分拣迷失｜0x04 PLC 故障｜0x05 锁格",
                  fill=b.RED_PALE, accent=b.RED)

    # Protocol frame strip.
    c.setFillColor(b.WHITE)
    c.setStrokeColor(b.LINE)
    c.roundRect(left, 49, right - left, 48, 8, fill=1, stroke=1)
    b.pill(c, left + 12, 64, 74, 18, "统一报文结构", HexColor("#EAF1F8"), color=b.INK,
           stroke=b.LINE, size=7.2)
    b.draw_paragraph(c,
                     "报文头 0x0A 0x0B  →  消息长度(2B)  →  发送方ID  →  接收方ID  →  命令字  →  不定长报文内容  →  报文尾 0x0D 0x0A",
                     left + 98, 82, right - left - 114, 22, size=8.2, color=b.INK, leading=10)
    b.draw_paragraph(c, "消息长度范围 1-1024 字节；方向中的 DWS / 条码威视 / 上位机均按发送或接收方 ID 03 处理。",
                     left + 98, 64, right - left - 114, 16, size=6.8, color=b.MUTED, leading=8)
    page_footer(c, 2)
    c.showPage()


def phase_label(c: Canvas, x, y_top, y_bottom, letter, title, color):
    b.round_rect(c, x, y_bottom, 92, y_top - y_bottom, b.WHITE, b.LINE, 7, 0.8)
    c.setFillColor(color)
    c.circle(x + 23, (y_top + y_bottom) / 2 + 7, 11, fill=1, stroke=0)
    c.setFillColor(b.WHITE)
    c.setFont("DengBold", 8)
    c.drawCentredString(x + 23, (y_top + y_bottom) / 2 + 4, letter)
    b.draw_paragraph(c, title, x + 40, (y_top + y_bottom) / 2 + 15, 45, 28,
                     size=7.4, bold=True, leading=9)


def draw_control_detail(c: Canvas):
    width, height = page_header(
        c,
        "链路、状态与控制 - 详细泳道",
        "心跳重连、设备状态、锁格/模式、报警停机及 PDA 自动解锁",
        3,
    )
    phase_w = 104
    left, right = 34 + phase_w, width - 34
    header_top, header_h = height - 103, 43
    body_top, body_bottom = header_top - header_h, 106
    lanes = [
        ("PDA", "绑格口与包签"),
        ("总包网关", "业务指令转发"),
        ("条码威视 / DWS", "服务端与路由状态"),
        ("窄带 PLC", "客户端与硬件控制"),
        ("现场设备", "LED、停机与解锁"),
    ]
    lane_w = (right - left) / len(lanes)
    centers = []
    for i, (title, sub) in enumerate(lanes):
        x = left + i * lane_w
        centers.append(x + lane_w / 2)
        c.setFillColor(HexColor("#F8FBFE") if i % 2 == 0 else b.WHITE)
        c.setStrokeColor(b.LINE)
        c.rect(x, body_bottom, lane_w, body_top - body_bottom, fill=1, stroke=1)
        c.setFillColor(HexColor("#EAF1F8"))
        c.rect(x, header_top - header_h, lane_w, header_h, fill=1, stroke=0)
        accent = [b.PURPLE, b.BLUE, b.TEAL, b.AMBER, b.RED][i]
        c.setFillColor(accent)
        c.rect(x, header_top - 4, lane_w, 4, fill=1, stroke=0)
        c.setFillColor(b.INK)
        c.setFont("DengBold", 9.4)
        c.drawCentredString(x + lane_w / 2, header_top - 17, title)
        c.setFillColor(b.MUTED)
        c.setFont("Deng", 6.4)
        c.drawCentredString(x + lane_w / 2, header_top - 31, sub)
    c.setFillColor(HexColor("#EAF1F8"))
    c.roundRect(34, header_top - header_h, phase_w - 8, header_h, 8, fill=1, stroke=0)
    c.setFillColor(b.INK)
    c.setFont("DengBold", 8.5)
    c.drawCentredString(34 + (phase_w - 8) / 2, header_top - 25, "控制阶段")
    phase_label(c, 34, body_top - 4, 405, "A", "链路与状态", b.PURPLE)
    phase_label(c, 34, 405, 234, "B", "运行控制", b.BLUE)
    phase_label(c, 34, 234, body_bottom + 4, "C", "业务解锁", b.TEAL)

    # Section backgrounds.
    for top, bottom, fill in [(body_top, 405, HexColor("#F8FBFE")), (405, 234, b.WHITE), (234, body_bottom, HexColor("#F2FAF8"))]:
        c.setFillColor(fill)
        c.rect(left, bottom, right - left, top - bottom, fill=1, stroke=0)
    for x in [left + i * lane_w for i in range(len(lanes) + 1)]:
        c.setStrokeColor(b.LINE)
        c.line(x, body_bottom, x, body_top)

    # A1. Connection.
    b.process_box(c, centers[3] - lane_w * 0.39, 624, lane_w * 0.78, 46,
                  "PLC 发起 TCP 连接", "PLC 为客户端；DWS 为服务端", fill=b.WHITE, accent=b.PURPLE)
    b.arrow(c, centers[3] - 12, 615, centers[2] + 12, 615, color=b.PURPLE, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 - 51, 624, 102, 18, "建立 3000 / 3001", b.PURPLE, size=6.9)

    # A2. Heartbeat pair.
    b.arrow(c, centers[3] - 12, 562, centers[2] + 12, 562, color=b.PURPLE, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 - 31, 571, 62, 18, "0x60", b.PURPLE)
    b.draw_paragraph(c, "PLC 每 10 秒发送：线体号 + 系统状态 + 系统模式", centers[2] - 74, 556, lane_w * 2 - 22, 18,
                     size=6.7, color=b.MUTED, align=TA_CENTER, leading=8)
    b.arrow(c, centers[2] + 12, 528, centers[3] - 12, 528, color=b.BLUE, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 - 31, 537, 62, 18, "0x62", b.BLUE)
    b.draw_paragraph(c, "DWS 收到心跳后立即回复同类状态字段", centers[2] - 74, 522, lane_w * 2 - 22, 18,
                     size=6.7, color=b.MUTED, align=TA_CENTER, leading=8)

    # A3. Timeout / reconnect and state report.
    diamond(c, centers[2], 482, 104, 34, "3 次未收到？", b.RED_PALE, b.RED)
    b.process_box(c, centers[2] - lane_w * 0.42, 444, lane_w * 0.84, 34,
                  "DWS 提示链路异常并主动断开", "重连成功后清除提示", fill=b.RED_PALE, accent=b.RED)
    b.process_box(c, centers[3] - lane_w * 0.42, 444, lane_w * 0.84, 34,
                  "PLC 启动重连机制", "恢复 3000 / 3001 通道", fill=b.AMBER_PALE, accent=b.AMBER)
    b.arrow(c, centers[2] + 48, 461, centers[3] - 48, 461, color=b.AMBER, width=1.6, head=6)
    b.arrow(c, centers[3] - 12, 421, centers[2] + 12, 421, color=b.BLUE, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 - 31, 430, 62, 18, "0x61", b.BLUE)
    b.draw_paragraph(c, "首次连接、开/关机状态变化或故障时：线体号 + 速度 + 系统状态/模式 + 摆轮信息",
                     centers[2] - 84, 415, lane_w * 2 - 2, 20, size=6.3, color=b.MUTED, align=TA_CENTER, leading=7.4)

    # B1. Lock/unlock.
    b.arrow(c, centers[3] - 12, 382, centers[2] + 12, 382, color=b.AMBER, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 - 31, 391, 62, 18, "0x6B", b.AMBER)
    b.draw_paragraph(c, "PLC → DWS：锁格状态 0x01 / 解锁状态 0x02 + 格口号；DWS 更新可分配格口池",
                     centers[2] - 84, 376, lane_w * 2 - 2, 20, size=6.4, color=b.MUTED, align=TA_CENTER, leading=7.5)

    # B2. Mode switch pair.
    b.arrow(c, centers[2] + 12, 337, centers[3] - 12, 337, color=b.BLUE, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 - 67, 346, 60, 18, "0x71", b.BLUE, size=7.2)
    b.draw_paragraph(c, "进港 0x01 / 出港 0x02", centers[2] - 20, 331, lane_w * 2 - 90, 15,
                     size=6.4, color=b.MUTED, align=TA_CENTER, leading=7)
    b.arrow(c, centers[3] - 12, 308, centers[2] + 12, 308, color=b.PURPLE, width=2, head=8)
    command_pill(c, (centers[2] + centers[3]) / 2 + 7, 317, 60, 18, "0x70", b.PURPLE, size=7.2)
    b.draw_paragraph(c, "PLC 回执当前模式", centers[2] - 20, 302, lane_w * 2 - 90, 15,
                     size=6.4, color=b.MUTED, align=TA_CENTER, leading=7)

    # B3. Alarm and stop.
    b.arrow(c, centers[2] + 12, 261, centers[3] - 12, 261, color=b.RED, width=2, head=8)
    b.arrow(c, centers[3] + 12, 261, centers[4] - 12, 261, color=b.RED, width=2, head=8)
    command_pill(c, centers[3] - 31, 270, 62, 18, "0x73", b.RED)
    b.process_box(c, centers[4] - lane_w * 0.40, 239, lane_w * 0.80, 42,
                  "LED 报警 / 停机", "灯：0x01 无操作，0x02 报警；停机：0x01 无，0x02 停机；错误号默认 0x01",
                  fill=b.RED_PALE, accent=b.RED)

    # C. PDA auto-unlock.
    b.process_box(c, centers[0] - lane_w * 0.41, 165, lane_w * 0.82, 48,
                  "绑定格口与包签", "PDA 操作完成后自动发起解锁", fill=b.TEAL_PALE, accent=b.TEAL)
    b.arrow(c, centers[0] + lane_w * 0.41, 189, centers[1] - lane_w * 0.41, 189, color=b.TEAL, width=2, head=7)
    b.process_box(c, centers[1] - lane_w * 0.41, 165, lane_w * 0.82, 48,
                  "转发业务指令", "总包网关 → DWS", fill=b.WHITE, accent=b.TEAL)
    b.arrow(c, centers[1] + lane_w * 0.41, 189, centers[2] - lane_w * 0.41, 189, color=b.TEAL, width=2, head=7)
    b.process_box(c, centers[2] - lane_w * 0.41, 165, lane_w * 0.82, 48,
                  "生成解锁控制", "校验后向 PLC 下发格口号", fill=b.WHITE, accent=b.TEAL)
    b.arrow(c, centers[2] + lane_w * 0.41, 189, centers[3] - lane_w * 0.41, 189, color=b.TEAL, width=2, head=7)
    command_pill(c, (centers[2] + centers[3]) / 2 - 31, 198, 62, 18, "0x75", b.TEAL)
    b.process_box(c, centers[3] - lane_w * 0.41, 165, lane_w * 0.82, 48,
                  "接收 2 字节格口号", "执行硬件解锁", fill=b.WHITE, accent=b.TEAL)
    b.arrow(c, centers[3] + lane_w * 0.41, 189, centers[4] - lane_w * 0.41, 189, color=b.TEAL, width=2, head=7)
    b.process_box(c, centers[4] - lane_w * 0.41, 165, lane_w * 0.82, 48,
                  "格口解锁", "恢复可用状态", fill=b.TEAL_PALE, accent=b.TEAL)

    # Bottom rule cards.
    rule_y = 49
    gap = 9
    rule_w = (right - 34 - gap * 3) / 4
    mini_rule(c, 34, rule_y, rule_w, "心跳状态值", "1 开机｜2 关机｜3 急停｜4 待机｜5 运行中｜6 启动中；模式 0 正常 / 1 其他。", b.PURPLE, b.PURPLE_PALE)
    mini_rule(c, 34 + rule_w + gap, rule_y, rule_w, "摆轮故障码", "1 过载｜2 过流｜3 缺相｜4 短路｜5 通讯故障。", b.BLUE, HexColor("#EDF5FD"))
    mini_rule(c, 34 + 2 * (rule_w + gap), rule_y, rule_w, "锁格影响", "锁格后 DWS 应从格口分配池剔除该格口，并可分配同目的地的其他格口。", b.AMBER, b.AMBER_PALE)
    mini_rule(c, 34 + 3 * (rule_w + gap), rule_y, rule_w, "报警原则", "0x73 面向较严重错误；包含 LED 操作、停机操作和错误编号。", b.RED, b.RED_PALE)
    page_footer(c, 3)
    c.showPage()


def build_detail_pdf():
    DETAIL_PDF.parent.mkdir(parents=True, exist_ok=True)
    width, height = landscape(A3)
    c = Canvas(str(DETAIL_PDF), pagesize=(width, height))
    c.setTitle("圆通速递窄带矩阵分拣交互泳道图 - 详细页")
    c.setAuthor("OpenAI Codex")
    draw_core_detail(c)
    draw_control_detail(c)
    c.save()


def merge_pdfs():
    # Rebuild the overview page, then append the two detailed pages.
    b.draw_document()
    build_detail_pdf()
    writer = PdfWriter()
    for path in (b.OUTPUT, DETAIL_PDF):
        reader = PdfReader(str(path))
        for page in reader.pages:
            writer.add_page(page)
    writer.add_metadata({
        "/Title": "圆通速递窄带矩阵分拣交互泳道图",
        "/Subject": "参考《底扫前置方案》视觉风格的三页详细泳道图",
        "/Author": "OpenAI Codex",
    })
    with open(MERGED_PDF, "wb") as stream:
        writer.write(stream)
    os.replace(MERGED_PDF, b.OUTPUT)
    print(b.OUTPUT)


if __name__ == "__main__":
    merge_pdfs()
