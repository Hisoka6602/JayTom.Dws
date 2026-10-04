"""按照用户参考图绘制当前 DWS 下位机创建至落格的单页垂直泳道图。"""

from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path
from xml.sax.saxutils import escape

from reportlab.lib.colors import Color, HexColor
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph


ROOT = Path(r"D:/WorkSpace/JayTom/JayTom.Dws")
OUT = ROOT / "output/pdf/JayTomDWS_下位机触发至落格垂直泳道全流程.pdf"
QA = ROOT / "tmp/pdfs/dws_current"
W, H = 1980.0, 8300.0
LEFT, RIGHT = 130.0, 1900.0
TOTAL = RIGHT - LEFT
LANE_W = TOTAL / 8
LANE_TOP, LANE_BOTTOM = 450.0, 6460.0
NODE_W = LANE_W - 28

pdfmetrics.registerFont(TTFont("YaHei", r"C:/Windows/Fonts/msyh.ttc", subfontIndex=0))
pdfmetrics.registerFont(TTFont("YaHeiBold", r"C:/Windows/Fonts/msyhbd.ttc", subfontIndex=0))
pdfmetrics.registerFontFamily("YaHei", normal="YaHei", bold="YaHeiBold")

INK = HexColor("#18324A")
MUTED = HexColor("#637E95")
BG = HexColor("#F7FAFC")
RULE = HexColor("#D3DEE8")
ARROW = HexColor("#6B8498")
STAGE = HexColor("#EAF1F6")
STAGE_LINE = HexColor("#ADC2D4")
RED = HexColor("#DC3E42")
RED_LIGHT = HexColor("#FFF3F3")
GREEN = HexColor("#16975D")
WHITE = HexColor("#FFFFFF")

LANES = [
    ("操作界面", "配置、启动与结果显示", "#2563EB", "#F5F9FF", "#EFF6FF"),
    ("下位机 / PLC", "小车触发与机械落格", "#B7860B", "#FFFCF3", "#FFFBEB"),
    ("相机 / 采集设备", "拍照识别与可选称重", "#C026D3", "#FEF8FF", "#FCF2FF"),
    ("DWS 组包", "占位、融合与原车绑定", "#0F9F9A", "#F3FEFC", "#ECFDFB"),
    ("上传服务", "接口查询与落格回传", "#1E8E5A", "#F4FBF7", "#ECF8F1"),
    ("业务 API", "外部业务响应与接收", "#7C3AED", "#FAF8FF", "#F5F3FF"),
    ("分拣 / 协议服务", "格口规则、指令与确认", "#0284C7", "#F6FBFF", "#F0F9FF"),
    ("持久化 / 存图", "包裹、指令、格口与图片", "#EA6A0A", "#FFFAF4", "#FFF7ED"),
]

layout: list[dict] = []
labels: list[tuple] = []


def rr(c: Canvas, x, top, width, height, fill, stroke=None, radius=18, weight=1.5):
    """用顶部坐标绘制与参考图一致的圆角框。"""
    c.setFillColor(fill)
    c.setStrokeColor(stroke or fill)
    c.setLineWidth(weight)
    c.roundRect(x, H - top - height, width, height, radius, fill=1, stroke=int(stroke is not None))


def paragraph(c, text, x, top, width, available, size=19, color=INK, bold=False,
              align=TA_CENTER, leading=None, tag=None):
    """绘制可搜索的嵌入中文文字，并校验文字高度。"""
    style = ParagraphStyle(
        "dws", fontName="YaHeiBold" if bold else "YaHei", fontSize=size,
        leading=leading or size * 1.38, textColor=color, alignment=align,
        wordWrap="CJK", splitLongWords=True, spaceAfter=0, spaceBefore=0,
    )
    p = Paragraph(escape(text).replace("\n", "<br/>"), style)
    _, used = p.wrap(width, available)
    if used > available + 0.1:
        raise ValueError(f"文字超出布局: {tag or text[:20]}, {used:.1f}>{available:.1f}")
    p.drawOn(c, x, H - top - used)
    layout.append({"kind": "text", "tag": tag or text[:24], "x": x, "top": top,
                   "w": width, "h": used, "size": size})
    return used


@dataclass
class Box:
    """维护步骤边界，避免箭头穿过活动框。"""
    key: str
    lane: int
    top: float
    height: float
    title: str
    detail: str
    number: int | None = None
    span: int = 1
    warning: bool = False

    @property
    def x(self):
        return LEFT + self.lane * LANE_W + 14

    @property
    def width(self):
        return LANE_W * self.span - 28

    @property
    def cx(self):
        return self.x + self.width / 2

    @property
    def cy(self):
        return self.top + self.height / 2

    @property
    def bottom(self):
        return self.top + self.height

    @property
    def east(self):
        return (self.x + self.width, self.cy)

    @property
    def west(self):
        return (self.x, self.cy)

    @property
    def south(self):
        return (self.cx, self.bottom)

    @property
    def north(self):
        return (self.cx, self.top)


BOXES: dict[str, Box] = {}


def box(key, lane, top, height, title, detail, number=None, span=1, warning=False):
    """登记普通步骤或红色条件分支。"""
    b = Box(key, lane, top, height, title, detail, number, span, warning)
    if b.x < LEFT or b.x + b.width > RIGHT or b.bottom > LANE_BOTTOM:
        raise ValueError(f"活动框超出泳道: {key}")
    BOXES[key] = b
    return b


def draw_box(c, b):
    """按泳道颜色绘制编号与正文，保留与参考图相同的角标。"""
    accent = RED if b.warning else HexColor(LANES[b.lane][2])
    fill = RED_LIGHT if b.warning else HexColor(LANES[b.lane][4])
    rr(c, b.x, b.top, b.width, b.height, fill, accent, radius=18, weight=2.2)
    title_top = b.top + (47 if b.number is not None else 19)
    title_size = 18.2 if b.span == 1 else 21.0
    if b.number is not None:
        c.setFillColor(accent)
        c.circle(b.x + 27, H - b.top - 27, 19, fill=1, stroke=0)
        c.setFillColor(WHITE)
        c.setFont("YaHeiBold", 15)
        c.drawCentredString(b.x + 27, H - b.top - 32.5, str(b.number))
    used = paragraph(c, b.title, b.x + 12, title_top, b.width - 24, b.height - 58,
                     title_size, accent if b.warning else INK, True, leading=24 if b.span == 1 else 25,
                     tag=b.key + ".title")
    detail_top = title_top + used + 7
    paragraph(c, b.detail, b.x + 12, detail_top, b.width - 24, b.bottom - detail_top - 12,
              16.5 if b.span == 1 else 18, INK, False, leading=20 if b.span == 1 else 25,
              tag=b.key + ".detail")
    layout.append({"kind": "box", "tag": b.key, "x": b.x, "top": b.top,
                   "w": b.width, "h": b.height, "lane": b.lane, "number": b.number})


def arrow(c, points, color=ARROW, dashed=False, label=None, label_at=None, width=1.9):
    """绘制直角折线与箭头，标签最后绘制，保证标签背景洁净。"""
    c.saveState()
    c.setStrokeColor(color)
    c.setFillColor(color)
    c.setLineWidth(width)
    if dashed:
        c.setDash(7, 5)
    path = c.beginPath()
    path.moveTo(points[0][0], H - points[0][1])
    for x, y in points[1:]:
        path.lineTo(x, H - y)
    c.drawPath(path)
    c.setDash()
    px, py = points[-2]
    ex, ey = points[-1]
    angle = math.atan2(ey - py, ex - px)
    length, half = 11, 5.6
    bx, by = ex - length * math.cos(angle), ey - length * math.sin(angle)
    left = (bx + half * math.sin(angle), by - half * math.cos(angle))
    right = (bx - half * math.sin(angle), by + half * math.cos(angle))
    head = c.beginPath()
    head.moveTo(ex, H - ey)
    head.lineTo(left[0], H - left[1])
    head.lineTo(right[0], H - right[1])
    head.close()
    c.drawPath(head, fill=1, stroke=0)
    c.restoreState()
    if label:
        labels.append((label, *label_at, color))


def down(c, a, b, label=None, color=ARROW, tag_y=None):
    """连接同一泳道内的两个相邻步骤。"""
    arrow(c, [a.south, b.north], color=color, label=label,
          label_at=(a.cx, tag_y or (a.bottom + b.top) / 2) if label else None)


def label(c, text, cx, cy, color=ARROW, size=15):
    """绘制白底的小型数据标签，模拟参考图的接口说明气泡。"""
    width = pdfmetrics.stringWidth(text, "YaHei", size) + 25
    height = 28
    x, top = cx - width / 2, cy - height / 2
    if x < LEFT or x + width > RIGHT:
        raise ValueError(f"箭头标签超出主版面: {text}")
    rr(c, x, top, width, height, WHITE, Color(color.red, color.green, color.blue, alpha=0.55), 9, 0.8)
    c.setFont("YaHei", size)
    c.setFillColor(color)
    c.drawCentredString(cx, H - cy - 5.1, text)
    layout.append({"kind": "label", "tag": text, "x": x, "top": top, "w": width, "h": height})


def stage(c, top, text):
    """绘制横贯八泳道的阶段分隔条。"""
    rr(c, LEFT, top, TOTAL, 50, STAGE, STAGE_LINE, 19, 1.2)
    paragraph(c, text, LEFT + 26, top + 10, TOTAL - 52, 34, 21, HexColor("#3D6078"),
              True, TA_LEFT, 29, tag="stage:" + text)


def main_chart(c):
    """按实际代码展开创建、融合、上传、规则、协议发送与回执六阶段。"""
    for i, (title, subtitle, color, pale, _) in enumerate(LANES):
        x = LEFT + i * LANE_W
        c.setFillColor(HexColor(pale))
        c.setStrokeColor(RULE)
        c.setLineWidth(1)
        c.rect(x, H - LANE_BOTTOM, LANE_W, LANE_BOTTOM - LANE_TOP, fill=1, stroke=1)
        c.setFillColor(HexColor(color))
        c.rect(x, H - LANE_TOP - 180, LANE_W, 180, fill=1, stroke=0)
        paragraph(c, title, x + 8, LANE_TOP + 54, LANE_W - 16, 58, 23, WHITE, True, leading=31,
                  tag=f"lane:{i + 1}")
        paragraph(c, subtitle, x + 10, LANE_TOP + 117, LANE_W - 20, 44, 14, WHITE,
                  leading=20, tag=f"lane:{i + 1}.subtitle")

    n1 = box("01", 0, 780, 178, "选择并启动本轮运行", "下位机创建包裹\nNoRead、融合、窗口\n接口、格口及移除方式\n均按现场配置生效", 1)
    n2 = box("02", 1, 780, 178, "同一次物理触发", "小车到达拍照位置\n发送本车创建指令\n同时触发相机拍照\n空车也占一次触发", 2)
    n3 = box("03", 2, 780, 178, "接收触发并拍照识别", "曝光、拍照与解码\n处理耗时可以不同\n每次触发最终必返回\n且按触发顺序返回", 3)
    n4 = box("04", 6, 1040, 188, "接收并解析创建指令", "连接服务按协议解码\n得到小车 Id、原文\n记录通讯接收时间\n转交 DWS 关键队列", 4)
    n5 = box("05", 2, 1040, 188, "相机 SDK 最终回调", "有效条码 / NoRead\n过滤也保留消费结果\n带相机编号、扫码时间\n设备服务统一转交", 5)
    n6 = box("06", 3, 1300, 190, "建立小车包裹占位", "Id = 下位机小车号\n创建时刻 = 指令时间\n每次创建保留消费位\n创建失败也占一个位", 6)
    n7 = box("07", 3, 1640, 190, "原车位置与结果配对", "一份最终结果对应\n一辆原始小车\n相机先回则暂存\n不凭响应时间选后车", 7)
    n8 = box("08", 3, 1900, 195, "按同次触发完成融合", "单相机直接处理\n多相机各自第 N 次\n最终结果组成第 N 组\n到齐或融合期限结束", 8)
    n9 = box("09", 3, 2180, 215, "融合与空读判定", "有效码优先于 NoRead\n不同有效码视为冲突\n全空读按开关处理\n过滤仍消费原车位置", 9)
    discard = box("discard", 1, 2180, 215, "消费后丢弃本车", "NoRead 关闭 / 过滤\n融合冲突 / 结果未到齐\n不显示包裹结果\n不上传、不主记录落库\n不下发本包裹落格指令", warning=True)
    box("order-note", 5, 1835, 330, "关联依据：内部保序计数", "没有可靠的相机触发序号\n与小车 Id 直接映射。\n本轮创建次序与每台相机\n最终结果次序一对一对应。\n\n单包裹超时不清空整条队列；\n显式停机才重置本轮消费状态。", span=2)

    n10 = box("10", 3, 2550, 200, "窗口只校验原小车", "开启：比较本车创建\n与本次扫码的时间差\n关闭：按触发次序绑定\n原车已移除也不顺延", 10)
    window_fail = box("window-fail", 1, 2550, 200, "校验失败仅关闭原车", "窗口越界 / 原车过期\n本次结果不移交下一辆\n如窗口 50-500ms\n本车扫码时间差 600ms\n只影响 A，后车继续", warning=True)
    n11 = box("11", 3, 2810, 185, "把识别数据绑定原车", "条码 / NoRead + Id\n相机编号、扫码时间\n接管本次识别原图\n按配置等待重量数据", 11)
    weight = box("weight", 2, 2810, 185, "可选重量及外部数据", "采集设备提供称重等\n未用称重则补默认值\n体积有默认模型\n条码和重量可以先后到")
    n12 = box("12", 3, 3070, 185, "模型齐全后完成包裹", "条码、重量、体积模型\n完成事件只发布一次\n独立队列分发通知\n创建指令同时进入业务", 12)

    n13 = box("13", 0, 3420, 180, "界面显示完成结果", "显示原车 Id 与条码\nNoRead 开启时保留\n相机、重量等正常展示\n界面通知单独处理", 13)
    n14 = box("14", 4, 3420, 195, "完成结果进入上传", "进入无损工作队列\n条码、Id、创建时间\n扫码时刻及业务数据\n并发查询，上限 600\n不等待界面或存图", 14)
    n15 = box("15", 7, 3420, 195, "持久化与存图并行", "异步保存包裹主记录\n创建指令和图片元数据\n图片独立排队保存\n按已有存图配置执行", 15)
    n16 = box("16", 5, 3670, 180, "处理并返回业务响应", "接收条码及业务参数\n按所选接口执行业务\n返回目标信息或错误\nHTTP 结果对应原请求", 16)
    n17 = box("17", 4, 3920, 195, "发布原车 API 结果", "Id、创建时刻、时间戳\n条码、ScanTime\n与上传响应一起发布\n并发请求可乱序完成", 17)

    n18 = box("18", 6, 4280, 200, "校验 API 响应身份", "仅 API 响应分拣模式\n核对 Id、创建时刻\n时间戳与条码一致\n扫码时刻也须一致\n重复响应只消费一次", 18)
    api_fail = box("api-fail", 4, 4280, 200, "身份失效禁止下发", "包裹已移除 / Id 复用\n创建信息或条码不一致\n重复 API 响应\n拒绝生成本次落格指令", warning=True)
    n19 = box("19", 6, 4540, 200, "按响应规则选择格口", "匹配字符串 / JSON\n确认物理格口有效\n锁格按配置找备用口\n无命中走配置异常口", 19)
    abnormal = box("abnormal", 3, 4540, 200, "配置异常分拣分支", "网络超时、规则未命中、无有效格口或无指令\n有可用异常口及绑定才执行异常格口下发\n没有对应配置时记录异常，不生成有效指令", span=2, warning=True)
    n20 = box("20", 6, 4800, 195, "构造落格指令与附件", "目标格口 + 原车 Id\n原车创建、扫码信息\n读取绑定协议与间隔\n延时结束再次核对身份", 20)
    n21 = box("21", 6, 5060, 195, "协议编码并下发指令", "同一连接有序发送\n按配置写 TCP 或串口\n每条发送前复核原车\n启用时校验协议应答", 21)
    n22 = box("22", 1, 5060, 195, "接收指令并机械落格", "解析格口和原小车号\n按设备机械位置执行\n返回落格 / 移除 / 异常\n回执格式由协议决定", 22)

    n23 = box("23", 6, 5460, 190, "记录发送与理论格口", "每条发送成功均通知\n全部写入才标记已发\n理论格口启动确认计时\n尚不等于设备确认落格", 23)
    n24 = box("24", 3, 5460, 190, "接收落格及移除回执", "连接解码后按车 Id 查\n获取原车 Timestamp\n发布回执指令事件\n移除方式按配置执行", 24)
    box("cleanup", 0, 5460, 260, "活动包裹按配置清理", "下位机移除：处理移除回执后清理。\n填充完整信息：存图已转交、指令已发\n且全景图就绪等条件满足后可先清理。\n\n回执按 Id 查询时，须仍有活动包裹；\n查不到原车会记录匹配失败。", span=2)
    n25 = box("25", 6, 5730, 200, "生成物理格口事件", "PLC 首次回执完成确认\n60s 未回则软件补记\n保留完成标记防重复\n理论与物理状态分开", 25)
    timeout = box("fall-timeout", 3, 5730, 230, "当前代码的 60 秒补记分支", "理论格口登记后 60 秒没有落格回执：\n沿用理论格口，软件补记正常落格\n发布 PhysicalExit 事件进入后续回传\n迟到或重复的真实回执不再覆盖\n这是软件推定，不代表设备已确认。", span=2, warning=True)
    n26 = box("26", 4, 6010, 185, "按接口形成落格回传", "包裹、格口、回执与图\n按所选接口二次提交\n接口不支持时不执行\n需要图片时按接口等待", 26)
    n27 = box("27", 7, 6010, 185, "更新包裹全链路记录", "上传结果、指令及格口\n理论 / 物理分别保存\n按 Timestamp 关联\n持久化失败进入重试", 27)
    n28 = box("28", 5, 6280, 145, "接收落格业务回传", "按接口接收状态与图片\n形成业务记录闭环", 28)

    arrow(c, [n1.east, n2.west], label="运行后", label_at=(352, 754))
    arrow(c, [n2.east, n3.west], label="同一次触发", label_at=(573, 754))
    arrow(c, [n2.south, (n2.cx, 981), (n4.cx, 981), n4.north],
          label="创建指令：小车 Id + 指令原文", label_at=(1200, 981))
    down(c, n3, n5, "异步识别完成", tag_y=1003)
    arrow(c, [n4.south, (n4.cx, 1266), (n6.cx, 1266), n6.north],
          label="创建事件进入关键线程", label_at=(1242, 1266))
    down(c, n6, n7)
    arrow(c, [n5.south, (n5.cx, 1620), (n7.cx, 1620), n7.north],
          label="最终结果统一转交", label_at=(686, 1513))
    down(c, n7, n8, "按次序保留位置")
    down(c, n8, n9, "到齐 / 融合截止")
    arrow(c, [n9.west, discard.east], RED, True, "关闭 NoRead 或需丢弃", (685, 2246))
    down(c, n9, n10, "保留有效码或允许的 NoRead", GREEN, 2422)
    arrow(c, [n10.west, window_fail.east], RED, True, "原车校验失败", (685, 2608))
    down(c, n10, n11, "原车校验通过", GREEN)
    arrow(c, [weight.east, n11.west])
    down(c, n11, n12, "数据齐全")
    for target in [n13, n14, n15]:
        arrow(c, [n12.south, (n12.cx, 3297), (target.cx, 3297), target.north])
    labels.append(("完成事件并行分发", 1360, 3297, ARROW))
    arrow(c, [n14.south, (n14.cx, 3642), (n16.cx, 3642), n16.north],
          label="HTTP 查询", label_at=(1238, 3642))
    arrow(c, [n16.south, (n16.cx, 3880), (n17.cx, 3880), n17.north],
          label="业务响应 / 失败状态", label_at=(1238, 3880))
    arrow(c, [n17.south, (n17.cx, 4147), (n18.cx, 4147), n18.north],
          label="携带完整原车身份的响应事件", label_at=(1407, 4147))
    arrow(c, [n18.west, api_fail.east], RED, True, "身份不符 / 重复", (1346, 4353))
    down(c, n18, n19, "身份校验通过", GREEN)
    arrow(c, [n19.west, abnormal.east], RED, True, "异常分拣", (1346, 4609))
    down(c, n19, n20, "有效格口及绑定指令", GREEN)
    arrow(c, [abnormal.south, (abnormal.cx, 4771), (n20.cx, 4771), n20.north],
          RED, True, "存在可用异常口及指令", (1346, 4771))
    down(c, n20, n21, "发送前再复核")
    arrow(c, [n21.west, n22.east], label="原车 Id + 目标格口（按配置协议编码）", label_at=(1000, 5130))
    down(c, n21, n23, "全部指令写入后更新状态", tag_y=5324)
    arrow(c, [n22.south, (n22.cx, 5324), (n24.cx, 5324), n24.north],
          label="落格 / 移除 / 异常回执", label_at=(695, 5324))
    arrow(c, [n24.south, (n24.cx, 5686), (n25.cx, 5686), n25.north],
          label="回执携带原车 Timestamp", label_at=(1300, 5672))
    arrow(c, [n23.south, (n23.cx, 5715), (timeout.cx, 5715), timeout.north],
          RED, True, "60 秒未收到真实落格回执", (1300, 5715))
    arrow(c, [timeout.east, (1346, timeout.cy), (1346, n25.cy), n25.west],
          RED, True, "软件补记正常落格", (1346, 5798))
    for target in [n26, n27]:
        arrow(c, [n25.south, (n25.cx, 5971), (target.cx, 5971), target.north])
    labels.append(("格口更新 / 回执事件", 1640, 5971, ARROW))
    arrow(c, [n26.south, (n26.cx, 6236), (n28.cx, 6236), n28.north],
          label="按所选接口二次回传", label_at=(1238, 6236))

    for top, text in [
        (690, "阶段 1 | 同一物理触发、异步拍照与小车占位"),
        (1550, "阶段 2 | 按触发次序关联、融合与 NoRead 输出判定"),
        (2460, "阶段 3 | 原车窗口校验、条码绑定与包裹完成"),
        (3330, "阶段 4 | 完成事件并行分发、接口查询与响应回流"),
        (4180, "阶段 5 | API 身份校验、格口规则、协议编码与原车落格下发"),
        (5370, "阶段 6 | 理论格口、真实回执 / 软件补记、持久化与业务回传"),
    ]:
        stage(c, top, text)
    for b in BOXES.values():
        draw_box(c, b)
    for text, x, y, color in labels:
        label(c, text, x, y, color)


def boundary_table(c):
    """补充接口传输内容和实际运行边界，保持参考图三列表结构。"""
    paragraph(c, "A. 链路边界（当前工作区实现）", LEFT, 6620, 720, 38,
              24, INK, True, TA_LEFT, 32)
    paragraph(c, "主线按下位机创建 + API 响应分拣展开；接口、协议及移除方式以现场配置为准", LEFT + 650,
              6627, TOTAL - 650, 25, 15, MUTED, align=TA_LEFT, leading=21)
    cols = [350.0, 580.0, TOTAL - 930.0]
    rows = [
        ("PLC → 相机 / DWS", "同一触发；小车 Id、创建指令原文、通讯接收时间", "拍照与创建由同一物理触发产生；发送与回调时间戳可不同。"),
        ("相机 / SDK → DWS", "有效条码、NoRead 或过滤消费结果；相机编号、扫码时间、帧信息与图片", "每台相机最终必返且保序；多相机按各自第 N 次结果融合。"),
        ("DWS → 界面 / 业务消费者", "完成包裹、原车身份、重量体积、创建指令、图片转交消息", "通知独立分发；NoRead 关闭或被过滤时消费后丢弃，不产生包裹业务完成输出。"),
        ("上传服务 ↔ 业务 API", "条码、重量与接口业务参数；返回内容、成功失败状态及原请求身份", "当前查询并发上限 600；响应可乱序，但回流保留完整原车身份。"),
        ("分拣 / 协议 → PLC", "匹配格口、绑定指令与原车附件；协议编码后的 TCP / 串口报文", "核对身份后按连接有序发送；延时结束和每条实际写入前再次核对。"),
        ("PLC 回执 → 格口更新", "落格 / 移除 / 异常回执；关联原车 Timestamp、理论与物理格口", "发送成功记录理论格口；首次真实回执确认；60 秒无回执由软件补记正常落格。"),
        ("格口更新 → 数据 / 回传", "指令、上传结果、物理格口、图片元数据及接口所需落格业务数据", "按 Timestamp 关联历史；二次回传受接口实现约束，不同接口字段与图片等待策略不同。"),
        ("活动会话 → 清理 / 停机", "按移除方式清理包裹；显式停止后结束本轮触发消费计数", "完整信息模式可能先于回执清理；下位机回执按 Id 查询时须仍能找到原车。"),
    ]
    top, head_h, row_h = 6668.0, 44.0, 60.0
    rr(c, LEFT, top, TOTAL, head_h, HexColor("#3F637A"), None, 9)
    x = LEFT
    for text, width in zip(["对接链路", "主要传递内容", "责任与边界"], cols):
        paragraph(c, text, x + 10, top + 9, width - 20, 28, 17, WHITE, True, leading=23)
        x += width
    for index, row in enumerate(rows):
        row_top = top + head_h + index * row_h
        c.setFillColor(WHITE if index % 2 == 0 else HexColor("#F1F6FA"))
        c.setStrokeColor(STAGE_LINE)
        c.setLineWidth(0.7)
        c.rect(LEFT, H - row_top - row_h, TOTAL, row_h, fill=1, stroke=1)
        x = LEFT
        for column, text in enumerate(row):
            if column:
                c.line(x, H - row_top, x, H - row_top - row_h)
            paragraph(c, text, x + 10, row_top + 9, cols[column] - 20, row_h - 15,
                      15.5, INK, column == 0, TA_LEFT, 20.5,
                      tag=f"table:{index + 1}.{column + 1}")
            x += cols[column]


def compact_timing(c):
    """用参考图的双行蛇形卡片补充单包裹因果时序。"""
    paragraph(c, "B. 单包裹时序（因果主线）", LEFT, 7224, 630, 38, 24, INK, True, TA_LEFT, 32)
    paragraph(c, "相机结果可早到；接口响应可乱序；创建与最终识别按同一触发配对，后续按原车身份关联", LEFT + 605,
              7231, TOTAL - 605, 25, 15, MUTED, align=TA_LEFT, leading=21)
    timing = [
        (1, 1, "下位机 / PLC", "发送小车创建指令\n同次触发启动拍照"),
        (2, 3, "DWS 组包", "建立原车占位\n保存一次消费位置"),
        (3, 2, "相机 / SDK", "最终条码 / NoRead\n按触发顺序返回"),
        (4, 3, "DWS 组包", "同次触发组帧融合\n窗口和 NoRead 判定"),
        (5, 3, "DWS 组包", "绑定原车并完成\n发布完成事件"),
        (6, 4, "上传服务", "独立并发请求\n查询目标业务信息"),
        (7, 5, "业务 API", "返回业务响应\n属于原查询请求"),
        (8, 6, "分拣服务", "核对原车完整身份\n只消费一次响应"),
        (9, 6, "分拣 / 协议", "匹配目标格口\n协议编码并下发"),
        (10, 1, "下位机 / PLC", "执行机械落格\n回传实际状态"),
        (11, 6, "落格确认", "接收首次真实回执\n或 60s 软件补记"),
        (12, 7, "数据 / 业务回传", "更新格口与回执\n按所选接口回传"),
    ]
    gap = 42.0
    width = (TOTAL - 5 * gap) / 6
    tops = [7300.0, 7480.0]
    height = 126.0
    positions = []
    for index in range(12):
        row = index // 6
        column = index % 6 if row == 0 else 5 - index % 6
        positions.append((LEFT + column * (width + gap), tops[row]))
    for i in range(11):
        x1, y1 = positions[i]
        x2, y2 = positions[i + 1]
        if y1 == y2:
            forward = x2 > x1
            arrow(c, [(x1 + (width if forward else 0), y1 + height / 2),
                      (x2 + (0 if forward else width), y2 + height / 2)])
        else:
            arrow(c, [(x1 + width / 2, y1 + height), (x2 + width / 2, y2)])
    for (number, lane, title, detail), (x, top) in zip(timing, positions):
        color = HexColor(LANES[lane][2])
        rr(c, x, top, width, height, HexColor(LANES[lane][4]), color, 17, 2)
        c.setFillColor(color)
        c.circle(x + 26, H - top - 24, 17, fill=1, stroke=0)
        c.setFont("YaHeiBold", 14)
        c.setFillColor(WHITE)
        c.drawCentredString(x + 26, H - top - 29.3, str(number))
        paragraph(c, title, x + 51, top + 15, width - 60, 28, 15, color, True, TA_LEFT, 21)
        paragraph(c, detail, x + 12, top + 56, width - 24, height - 62, 17, INK, True, leading=24)


def document(c):
    """完成题头、主图、边界表、时序及可核对的源码出处。"""
    c.setFillColor(BG)
    c.rect(0, 0, W, H, fill=1, stroke=0)
    paragraph(c, "JayTom DWS：下位机触发至落格完整垂直泳道链路", LEFT, 72, 1410, 100,
              42, INK, True, TA_LEFT, 56)
    rr(c, 1651, 79, 249, 65, HexColor("#EAF3FF"), HexColor("#75A6EF"), 26, 1.4)
    paragraph(c, "单页完整链路", 1671, 99, 209, 32, 22, HexColor("#215F9E"), True, leading=30)
    paragraph(c, "当前工作区代码 · 2026-10-04 | 创建 → 识别 → 融合 / 绑定 → 上传 → 指令落格 → 回执闭环", LEFT,
              160, TOTAL, 52, 22, MUTED, align=TA_LEFT, leading=30)
    rr(c, LEFT, 240, TOTAL, 122, RED_LIGHT, HexColor("#F18D92"), 18, 1.2)
    paragraph(c, "运行前提", LEFT + 28, 265, 125, 40, 22, RED, True, TA_LEFT, 30)
    paragraph(c, "创建与拍照来自同一物理触发；每台相机每次触发最终必返回一次（有效码 / NoRead / 过滤），并按触发顺序返回。\n图中按“下位机创建包裹 + API 响应分拣”展开；接口、融合超时、赋值窗口、格口协议及移除方式以现场配置为准。",
              LEFT + 176, 263, TOTAL - 210, 82, 20, INK, align=TA_LEFT, leading=29)
    main_chart(c)
    stage(c, 6550, "补充 | 链路边界、单包裹时序与防错位规则")
    boundary_table(c)
    compact_timing(c)
    rr(c, LEFT, 7670, TOTAL, 132, RED_LIGHT, HexColor("#EA6368"), 18, 1.7)
    paragraph(c, "NoRead / 超时分支：先消费原车，再决定输出；单包失败不顺延到下一辆", LEFT + 20, 7684,
              TOTAL - 40, 31, 20, RED, True, leading=28)
    paragraph(c, "NoRead 关闭：消费后丢弃，不产生包裹业务完成输出；NoRead 开启：正常绑定、完成、上传，并按配置参与分拣，NoRead 过滤继续有效。\n窗口校验 ScanTime 与原车 CreateTime 之差：50-500ms 下时间差 600ms 只关闭原车。原车过期仍保留消费位置；融合失败也只影响对应原车。",
              LEFT + 28, 7724, TOTAL - 56, 66, 18, INK, align=TA_LEFT, leading=25)
    paragraph(c, "源码核对入口（均为当前工作区实际注册与调用链）", LEFT, 7845, TOTAL, 40,
              22, INK, True, TA_LEFT, 30)
    for top, text in [
        (7892, "注册：HostedWorkflowRegistration.cs；组包 / 融合：YunShanPackageBackgroundService.cs；会话：PackageSessionRegistry.cs；相机统一事件：DefaultDeviceService.cs"),
        (7924, "查询 / 落格回传：SubmitApiBackgroundService.cs；API 规则：ApiSortingRuleEvaluator.cs；原车身份与格口分拣：DefaultSortingService.cs"),
        (7956, "连接收发：DefaultSortingConnectionService.cs；落格事件：PackageExitUpdateBackgroundService.cs；60 秒补记：FallConfirmationTimeoutTracker.cs"),
        (7988, "持久化 / 存图：DataProcessingBackgroundService.cs、SaveImageBackgroundService.cs；以上流程为代码梳理，不代表已进行现场硬件联调。"),
    ]:
        paragraph(c, text, LEFT, top, TOTAL, 27, 16, MUTED, align=TA_LEFT, leading=22)
    c.setStrokeColor(RULE)
    c.setLineWidth(1)
    c.line(LEFT, H - 8100, RIGHT, H - 8100)
    paragraph(c, "JayTom DWS 当前项目 | 单页垂直泳道图 | 1 / 1", LEFT, 8130, 1000, 32,
              18, MUTED, align=TA_LEFT, leading=25)
    c.setFillColor(HexColor("#E3E8ED"))
    c.setFont("Helvetica-BoldOblique", 65)
    c.drawRightString(RIGHT, H - 8223, "JayTom DWS")


def validate_layout():
    """检查正文和活动框均未出页，且活动框之间没有重叠。"""
    for item in layout:
        if item["x"] < 0 or item["top"] < 0 or item["x"] + item["w"] > W + 0.01 or item["top"] + item["h"] > H + 0.01:
            raise ValueError(f"元素越过页面: {item}")
    boxes = [item for item in layout if item["kind"] == "box"]
    for index, a in enumerate(boxes):
        for b in boxes[index + 1:]:
            dx = min(a["x"] + a["w"], b["x"] + b["w"]) - max(a["x"], b["x"])
            dy = min(a["top"] + a["h"], b["top"] + b["h"]) - max(a["top"], b["top"])
            if dx > 0.1 and dy > 0.1:
                raise ValueError(f"步骤框相互重叠: {a['tag']} / {b['tag']}")


def main():
    """生成一份单页矢量 PDF，并保留布局检查记录供渲染核对。"""
    OUT.parent.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    c = Canvas(str(OUT), pagesize=(W, H), pageCompression=1)
    c.setTitle("JayTom DWS 下位机触发至落格垂直泳道全流程")
    c.setAuthor("JayTom DWS")
    c.setSubject("当前代码的下位机触发、相机拍照、保序融合、上传、协议落格及回执闭环")
    document(c)
    validate_layout()
    c.showPage()
    c.save()
    (QA / "layout.json").write_text(json.dumps(layout, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(OUT), "pages": 1, "size_points": [W, H],
                      "numbered_steps": 28, "lanes": 8, "layout_elements": len(layout),
                      "bytes": OUT.stat().st_size}, ensure_ascii=False))


if __name__ == "__main__":
    main()
