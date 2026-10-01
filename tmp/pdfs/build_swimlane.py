from __future__ import annotations

import math
import os
from pathlib import Path

from reportlab.lib.colors import HexColor, Color
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A3, landscape
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph


ROOT = Path(r"D:\WorkSpace\JayTom\JayTom.Dws")
OUTPUT = ROOT / "output" / "pdf" / "圆通速递_窄带矩阵分拣泳道图.pdf"

FONT_REGULAR = r"C:\Windows\Fonts\Deng.ttf"
FONT_BOLD = r"C:\Windows\Fonts\Dengb.ttf"

pdfmetrics.registerFont(TTFont("Deng", FONT_REGULAR))
pdfmetrics.registerFont(TTFont("DengBold", FONT_BOLD))
pdfmetrics.registerFontFamily("Deng", normal="Deng", bold="DengBold")


PURPLE = HexColor("#7553F6")
PURPLE_DARK = HexColor("#172235")
PURPLE_PALE = HexColor("#F2EEFF")
INK = HexColor("#182236")
MUTED = HexColor("#74849B")
LINE = HexColor("#CEDBEA")
BG = HexColor("#FFFFFF")
WHITE = HexColor("#FFFFFF")
TEAL = HexColor("#2A8F87")
TEAL_PALE = HexColor("#EAF7F4")
AMBER = HexColor("#D17C10")
AMBER_PALE = HexColor("#FFF4E6")
RED = HexColor("#E35757")
RED_PALE = HexColor("#FFF0EF")
BLUE = HexColor("#3F7EF4")


def para(text: str, size: float, color=INK, align=TA_LEFT, leading=None, bold=False):
    style = ParagraphStyle(
        name="p",
        fontName="DengBold" if bold else "Deng",
        fontSize=size,
        textColor=color,
        leading=leading or size * 1.35,
        alignment=align,
        spaceAfter=0,
        spaceBefore=0,
    )
    return Paragraph(text, style)


def draw_paragraph(c: Canvas, text: str, x: float, y_top: float, w: float, h: float,
                   size=9, color=INK, align=TA_LEFT, leading=None, bold=False):
    p = para(text, size, color, align, leading, bold)
    _, used_h = p.wrap(w, h)
    p.drawOn(c, x, y_top - used_h)
    return used_h


def round_rect(c: Canvas, x, y, w, h, fill, stroke=LINE, radius=8, stroke_width=0.8):
    c.setLineWidth(stroke_width)
    c.setStrokeColor(stroke)
    c.setFillColor(fill)
    c.roundRect(x, y, w, h, radius, fill=1, stroke=1)


def process_box(c: Canvas, x, y, w, h, title, detail="", fill=WHITE, accent=PURPLE, number=None):
    round_rect(c, x, y, w, h, fill, accent, 8, 0.9)
    tx = x + 14
    if number is not None:
        c.setFillColor(accent)
        c.circle(x + 20, y + h / 2, 11, fill=1, stroke=0)
        c.setFillColor(WHITE)
        c.setFont("DengBold", 8)
        c.drawCentredString(x + 20, y + h / 2 - 3, str(number))
        tx = x + 38
    draw_paragraph(c, title, tx, y + h - 9, w - (tx - x) - 10, 20, size=9.2, bold=True, leading=11)
    if detail:
        draw_paragraph(c, detail, tx, y + h - 25, w - (tx - x) - 10, h - 26, size=7.3, color=MUTED, leading=9)


def pill(c: Canvas, x, y, w, h, text, fill, color=WHITE, stroke=None, size=8, bold=True):
    round_rect(c, x, y, w, h, fill, stroke or fill, h / 2, 0.7)
    c.setFillColor(color)
    c.setFont("DengBold" if bold else "Deng", size)
    c.drawCentredString(x + w / 2, y + (h - size) / 2 + 1, text)


def arrow(c: Canvas, x1, y1, x2, y2, color=PURPLE, width=1.8, dashed=False, head=7):
    c.saveState()
    c.setStrokeColor(color)
    c.setFillColor(color)
    c.setLineWidth(width)
    if dashed:
        c.setDash(5, 3)
    c.line(x1, y1, x2, y2)
    angle = math.atan2(y2 - y1, x2 - x1)
    a1 = angle + math.pi * 0.82
    a2 = angle - math.pi * 0.82
    p1 = (x2 + head * math.cos(a1), y2 + head * math.sin(a1))
    p2 = (x2 + head * math.cos(a2), y2 + head * math.sin(a2))
    path = c.beginPath()
    path.moveTo(x2, y2)
    path.lineTo(*p1)
    path.lineTo(*p2)
    path.close()
    c.drawPath(path, fill=1, stroke=0)
    c.restoreState()


def down_connector(c: Canvas, x, y1, y2, color=LINE):
    arrow(c, x, y1, x, y2, color=color, width=1.1, head=5)


def message(c: Canvas, x1, x2, y, cmd, title, detail, reverse=False, color=PURPLE):
    if reverse:
        arrow(c, x2, y, x1, y, color=color, width=2.2, head=8)
        cx = (x1 + x2) / 2
    else:
        arrow(c, x1, y, x2, y, color=color, width=2.2, head=8)
        cx = (x1 + x2) / 2
    pale = PURPLE_PALE if color == PURPLE else TEAL_PALE if color == TEAL else WHITE
    pill(c, cx - 32, y + 8, 64, 18, cmd, pale, color=color, stroke=color, size=8)
    draw_paragraph(c, title, cx - 88, y - 6, 176, 18, size=8, align=TA_CENTER, bold=True, leading=9)
    draw_paragraph(c, detail, cx - 105, y - 22, 210, 16, size=6.8, color=MUTED, align=TA_CENTER, leading=8)


def control_card(c: Canvas, x, y, w, h, title, cmd_text, body, accent, pale):
    round_rect(c, x, y, w, h, WHITE, LINE, 8, 0.8)
    c.setFillColor(pale)
    c.roundRect(x, y + h - 27, w, 27, 8, fill=1, stroke=0)
    c.rect(x, y + h - 27, w, 8, fill=1, stroke=0)
    c.setFillColor(accent)
    c.circle(x + 16, y + h - 13.5, 4.5, fill=1, stroke=0)
    c.setFillColor(INK)
    c.setFont("DengBold", 9)
    c.drawString(x + 28, y + h - 17, title)
    pill(c, x + w - 69, y + h - 22, 57, 17, cmd_text, WHITE, color=accent, stroke=accent, size=7.2)
    draw_paragraph(c, body, x + 12, y + h - 37, w - 24, h - 39, size=7.2, color=MUTED, leading=9)


def draw_document():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    width, height = landscape(A3)
    c = Canvas(str(OUTPUT), pagesize=(width, height))
    c.setTitle("圆通速递窄带矩阵分拣交互泳道图")
    c.setAuthor("OpenAI Codex")
    c.setSubject("依据圆通速递矩阵标准协议文档-窄带整理")

    # Canvas background and header, following the reference document's airy appendix style.
    c.setFillColor(BG)
    c.rect(0, 0, width, height, fill=1, stroke=0)
    c.setFillColor(INK)
    c.setFont("DengBold", 24)
    c.drawString(36, height - 49, "圆通速递 · 窄带矩阵分拣交互泳道图")
    c.setFont("Deng", 8.5)
    c.setFillColor(MUTED)
    c.drawString(38, height - 68, "核心业务流 + 并行控制通道｜条码威视 / DWS 作为服务端，PLC 作为客户端")
    pill(c, width - 170, height - 58, 134, 25, "流程总览 1 / 3", HexColor("#EAF1F8"), color=INK,
         stroke=HexColor("#EAF1F8"), size=8.5)
    c.setFillColor(MUTED)
    c.setFont("Deng", 6.8)
    c.drawRightString(width - 38, height - 73, "内部资料 · 注意保密")
    c.setStrokeColor(LINE)
    c.setLineWidth(0.7)
    c.line(36, height - 82, width - 36, height - 82)

    # Legend.
    ly = height - 91
    c.setFillColor(MUTED)
    c.setFont("Deng", 7.6)
    c.drawString(38, ly + 3, "图例")
    pill(c, 73, ly - 4, 70, 18, "主业务报文", PURPLE_PALE, color=PURPLE, stroke=PURPLE, size=7.2)
    pill(c, 150, ly - 4, 70, 18, "设备动作", TEAL_PALE, color=TEAL, stroke=TEAL, size=7.2)
    pill(c, 227, ly - 4, 70, 18, "异常 / 兜底", AMBER_PALE, color=AMBER, stroke=AMBER, size=7.2)
    c.setFillColor(MUTED)
    c.setFont("Deng", 7.2)
    c.drawRightString(width - 36, ly + 3, "时间自上而下｜端口 3001：触发号、格口下发；端口 3000：其余协议")

    # Swimlane geometry.
    left = 34
    right = width - 34
    phase_w = 112
    swim_left = left + phase_w
    swim_right = right
    lane_w = (swim_right - swim_left) / 4
    header_top = height - 111
    header_h = 42
    body_top = header_top - header_h
    body_bottom = 205
    lane_titles = [
        ("现场 / 包裹", "流程触发与落格结果"),
        ("窄带 PLC", "触发、跟车与硬件控制"),
        ("条码威视 / DWS", "读码、路由与协议服务"),
        ("分拣机构 / 滑槽", "执行分拣与落格感知"),
    ]
    lane_colors = [HexColor("#F8FBFE"), WHITE, HexColor("#F8FBFE"), WHITE]

    # Phase rail.
    c.setFillColor(HexColor("#EAF1F8"))
    c.roundRect(left, header_top - header_h, phase_w - 8, header_h, 8, fill=1, stroke=0)
    c.setFillColor(INK)
    c.setFont("DengBold", 9)
    c.drawCentredString(left + (phase_w - 8) / 2, header_top - 25, "业务阶段")

    phase_sections = [
        (body_top - 4, 500, "A", "识别与绑定", PURPLE),
        (500, 363, "B", "路由与确认", BLUE),
        (363, body_bottom + 4, "C", "执行与闭环", TEAL),
    ]
    for top, bottom, letter, label, color in phase_sections:
        round_rect(c, left, bottom, phase_w - 8, top - bottom, WHITE, LINE, 7, 0.8)
        c.setFillColor(color)
        c.circle(left + 26, (top + bottom) / 2 + 8, 12, fill=1, stroke=0)
        c.setFillColor(WHITE)
        c.setFont("DengBold", 9)
        c.drawCentredString(left + 26, (top + bottom) / 2 + 5, letter)
        draw_paragraph(c, label, left + 44, (top + bottom) / 2 + 17, phase_w - 56, 32, size=8, bold=True, leading=10)

    # Lanes and headers.
    for i, ((title, sub), fill) in enumerate(zip(lane_titles, lane_colors)):
        x = swim_left + i * lane_w
        c.setFillColor(fill)
        c.setStrokeColor(LINE)
        c.rect(x, body_bottom, lane_w, body_top - body_bottom, fill=1, stroke=1)
        c.setFillColor(HexColor("#EAF1F8"))
        c.rect(x, header_top - header_h, lane_w, header_h, fill=1, stroke=0)
        accent = [TEAL, PURPLE, BLUE, AMBER][i]
        c.setFillColor(accent)
        c.rect(x, header_top - 4, lane_w, 4, fill=1, stroke=0)
        c.setFillColor(INK)
        c.setFont("DengBold", 10.5)
        c.drawCentredString(x + lane_w / 2, header_top - 17, title)
        c.setFillColor(MUTED)
        c.setFont("Deng", 6.8)
        c.drawCentredString(x + lane_w / 2, header_top - 31, sub)

    centers = [swim_left + lane_w * (i + 0.5) for i in range(4)]
    box_w = lane_w - 36

    # Core business flow.
    process_box(c, centers[0] - box_w / 2, 626, box_w, 48,
                "包裹进入六面扫", "触发光电，流程开始 / 开始读码", fill=PURPLE_PALE, accent=PURPLE, number=1)
    down_connector(c, centers[0], 626, 607, LINE)
    arrow(c, centers[0], 607, centers[1] - box_w / 2 - 6, 607, color=LINE, width=1.2, head=6)

    process_box(c, centers[1] - box_w / 2, 580, box_w, 50,
                "生成触发号并绑定小车", "触发号 0-65535 循环；可带头/尾部标志", fill=WHITE, accent=PURPLE, number=2)

    message(c, centers[1] + 18, centers[2] - 18, 548,
            "0x68", "PLC → DWS：发送触发号", "设备台号 + 触发号 + 可选扩展字段｜端口 3001", color=PURPLE)
    down_connector(c, centers[2], 540, 523, LINE)

    process_box(c, centers[2] - box_w / 2, 468, box_w, 54,
                "读码、查询路由、生成格口", "以触发号关联当前货物；无可用格口时格口号 = 0x00", fill=WHITE, accent=BLUE, number=3)

    message(c, centers[1] + 18, centers[2] - 18, 435,
            "0x67", "DWS → PLC：格口下发", "条码 + 三段码 + 格口号 + 触发号 + 扩展字段｜端口 3001", reverse=True, color=PURPLE)
    down_connector(c, centers[1], 427, 410, LINE)

    process_box(c, centers[1] - box_w / 2, 366, box_w, 42,
                "接收并校验格口指令", "保留条码、触发号与格口关联", fill=WHITE, accent=PURPLE, number=4)

    message(c, centers[1] + 18, centers[2] - 18, 343,
            "0x64", "PLC → DWS：格口回复", "设备台号 + 条码 + 格口号｜端口 3000", color=PURPLE)

    process_box(c, centers[1] - box_w / 2, 276, box_w, 44,
                "跟车定位并触发分拣", "按触发号定位小车，到达动作点后驱动机构", fill=TEAL_PALE, accent=TEAL, number=5)
    arrow(c, centers[1] + box_w / 2, 298, centers[3] - box_w / 2, 298, color=TEAL, width=2.2, head=8)
    pill(c, (centers[1] + centers[3]) / 2 - 45, 307, 90, 18, "硬件动作", TEAL_PALE,
         color=TEAL, stroke=TEAL, size=7.5)

    process_box(c, centers[3] - box_w / 2, 274, box_w, 48,
                "执行分拣并感知落格", "包裹落入目标滑槽；产生成功 / 失败结果", fill=TEAL_PALE, accent=TEAL, number=6)
    arrow(c, centers[3], 274, centers[1], 257, color=LINE, width=1.1, dashed=True, head=6)

    message(c, centers[1] + 18, centers[2] - 18, 237,
            "0x66", "PLC → DWS：落格回传并闭环", "成功 / 失败 + 原因（摆轮、间距、迷失、PLC、锁格）｜端口 3000", color=TEAL)

    # Bottom control area.
    panel_y = 47
    panel_h = 140
    round_rect(c, left, panel_y, right - left, panel_h, WHITE, LINE, 10, 0.9)
    c.setFillColor(PURPLE_DARK)
    c.setFont("DengBold", 10.5)
    c.drawString(left + 16, panel_y + panel_h - 22, "并行控制与运维通道")
    c.setFillColor(MUTED)
    c.setFont("Deng", 7.2)
    c.drawString(left + 153, panel_y + panel_h - 21, "与主业务流并行，不改变上方时序")

    gap = 10
    card_x0 = left + 14
    card_y = panel_y + 14
    card_h = 94
    card_w = (right - left - 28 - gap * 3) / 4
    control_card(c, card_x0, card_y, card_w, card_h,
                 "链路心跳", "0x60 / 0x62",
                 "PLC 每 10 秒发送心跳；DWS 收到后回复。连续 3 次未收到时，DWS 提示链路异常并主动断开，PLC 启动重连。",
                 PURPLE, PURPLE_PALE)
    control_card(c, card_x0 + (card_w + gap), card_y, card_w, card_h,
                 "设备状态上报", "0x61",
                 "PLC 在首次建立连接、开/关机状态变化或设备故障时，上报线体速度、系统状态、模式及摆轮信息。",
                 BLUE, HexColor("#EDF5FD"))
    control_card(c, card_x0 + 2 * (card_w + gap), card_y, card_w, card_h,
                 "格口与模式控制", "0x6B · 0x71/70",
                 "PLC 上报锁格/解锁，DWS 据此避开不可用格口；DWS 下发进/出港模式 0x71，PLC 以 0x70 确认。",
                 AMBER, AMBER_PALE)
    control_card(c, card_x0 + 3 * (card_w + gap), card_y, card_w, card_h,
                 "报警与业务解锁", "0x73 · 0x75",
                 "严重错误时 DWS 下发亮灯/停机；PDA 绑格口与包签后，经总包网关→DWS→PLC 自动解锁格口。",
                 RED, RED_PALE)

    # Footer.
    c.setFillColor(MUTED)
    c.setFont("Deng", 6.8)
    c.drawString(left, 25, "依据：《圆通速递矩阵标准协议文档-窄带》（2026-08-24）｜说明：ID 03 的“条码威视 / DWS / 上位机”合并展示；概览页省略的字段详见第 2-3 页。")
    c.setFillColor(HexColor("#E7EDF5"))
    c.setFont("DengBold", 15)
    c.drawRightString(right, 38, "YTO")
    c.setFillColor(MUTED)
    c.setFont("Deng", 6.8)
    c.drawRightString(right, 25, "参考风格版 3.0  ·  1 / 3")

    c.showPage()
    c.save()


if __name__ == "__main__":
    draw_document()
    print(OUTPUT)
