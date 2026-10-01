from pathlib import Path
from reportlab.lib import colors
from reportlab.lib.colors import HexColor
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer, PageBreak,
    Table, TableStyle, LongTable, KeepTogether, Flowable
)

ROOT = Path(r"D:\WorkSpace\JayTom\JayTom.Dws")
OUTPUT = ROOT / "output" / "pdf" / "无限创科PLC_条码威视DWS协议桥_超级详细实施方案.pdf"
OUTPUT.parent.mkdir(parents=True, exist_ok=True)

pdfmetrics.registerFont(TTFont("CN", r"C:\Windows\Fonts\msyh.ttc", subfontIndex=0))
pdfmetrics.registerFont(TTFont("CNB", r"C:\Windows\Fonts\simhei.ttf"))
pdfmetrics.registerFont(TTFont("MONO", r"C:\Windows\Fonts\simfang.ttf"))

PAGE = landscape(A4)
PW, PH = PAGE
MARGIN_X = 16 * mm
MARGIN_TOP = 17 * mm
MARGIN_BOTTOM = 14 * mm
CONTENT_W = PW - 2 * MARGIN_X

NAVY = HexColor("#12233F")
MUTED = HexColor("#617089")
BLUE = HexColor("#4F7CFF")
TEAL = HexColor("#188C85")
PURPLE = HexColor("#7657F6")
ORANGE = HexColor("#D88719")
RED = HexColor("#C94646")
GREEN = HexColor("#2F8F60")
PALE_BLUE = HexColor("#EEF4FF")
PALE_TEAL = HexColor("#EAF8F6")
PALE_PURPLE = HexColor("#F2EEFF")
PALE_ORANGE = HexColor("#FFF5E8")
PALE_RED = HexColor("#FDEEEE")
GRID = HexColor("#CAD7E8")
LIGHT = HexColor("#F7F9FC")

styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name="BodyCN", fontName="CN", fontSize=9.1, leading=14.2, textColor=NAVY, wordWrap="CJK", spaceAfter=5))
styles.add(ParagraphStyle(name="SmallCN", fontName="CN", fontSize=7.4, leading=11.2, textColor=NAVY, wordWrap="CJK"))
styles.add(ParagraphStyle(name="TinyCN", fontName="CN", fontSize=6.4, leading=9.4, textColor=NAVY, wordWrap="CJK"))
styles.add(ParagraphStyle(name="H1CN", fontName="CNB", fontSize=20, leading=25, textColor=NAVY, spaceAfter=8, keepWithNext=True))
styles.add(ParagraphStyle(name="H2CN", fontName="CNB", fontSize=12.5, leading=17, textColor=NAVY, spaceBefore=4, spaceAfter=7, keepWithNext=True))
styles.add(ParagraphStyle(name="TitleCN", fontName="CNB", fontSize=28, leading=37, textColor=NAVY, alignment=TA_LEFT))
styles.add(ParagraphStyle(name="SubTitleCN", fontName="CN", fontSize=12, leading=19, textColor=MUTED))
styles.add(ParagraphStyle(name="Mono", fontName="MONO", fontSize=7.1, leading=10.2, textColor=NAVY, wordWrap="CJK"))
styles.add(ParagraphStyle(name="TableHead", fontName="CNB", fontSize=7.5, leading=10.5, textColor=NAVY, alignment=TA_CENTER, wordWrap="CJK"))
styles.add(ParagraphStyle(name="TableCell", fontName="CN", fontSize=7.1, leading=10.3, textColor=NAVY, wordWrap="CJK"))
styles.add(ParagraphStyle(name="Badge", fontName="CNB", fontSize=7.6, leading=10, textColor=NAVY, alignment=TA_CENTER))

def P(text, style="BodyCN"):
    return Paragraph(text, styles[style])

def mono(text):
    return Paragraph(text.replace("\n", "<br/>"), styles["Mono"])

def bullets(items, color=NAVY):
    out = []
    for item in items:
        out.append(Paragraph(f"<font color='{color.hexval()}'>●</font>&nbsp;&nbsp;{item}", styles["BodyCN"]))
    return out

def section(title, subtitle=None):
    out = [P(title, "H1CN")]
    if subtitle:
        out.append(Paragraph(subtitle, ParagraphStyle("sectsub", parent=styles["BodyCN"], textColor=MUTED, fontSize=8.5, leading=13)))
    out.append(Spacer(1, 3*mm))
    return out

def heading(title, accent=TEAL):
    bar = Table([["", P(title, "H2CN")]], colWidths=[2.2*mm, CONTENT_W-2.2*mm])
    bar.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (0,0), accent), ("VALIGN", (0,0), (-1,-1), "MIDDLE"),
        ("LEFTPADDING", (0,0), (0,0), 0), ("RIGHTPADDING", (0,0), (0,0), 0),
        ("LEFTPADDING", (1,0), (1,0), 6), ("RIGHTPADDING", (1,0), (1,0), 0),
        ("TOPPADDING", (0,0), (-1,-1), 2), ("BOTTOMPADDING", (0,0), (-1,-1), 3),
    ]))
    return bar

def make_table(headers, rows, widths=None, font="TableCell", header_bg=HexColor("#E7EFFA"), repeat=1):
    data = [[P(str(x), "TableHead") for x in headers]]
    for row in rows:
        data.append([x if isinstance(x, Flowable) else P(str(x), font) for x in row])
    t = LongTable(data, colWidths=widths, repeatRows=repeat, splitByRow=1, hAlign="LEFT")
    commands = [
        ("BACKGROUND", (0,0), (-1,0), header_bg), ("GRID", (0,0), (-1,-1), 0.45, GRID),
        ("VALIGN", (0,0), (-1,-1), "TOP"), ("LEFTPADDING", (0,0), (-1,-1), 5),
        ("RIGHTPADDING", (0,0), (-1,-1), 5), ("TOPPADDING", (0,0), (-1,-1), 4),
        ("BOTTOMPADDING", (0,0), (-1,-1), 4),
    ]
    for i in range(1, len(data)):
        if i % 2 == 0:
            commands.append(("BACKGROUND", (0,i), (-1,i), LIGHT))
    t.setStyle(TableStyle(commands))
    return t

def callout(title, text, tone="blue"):
    palette = {
        "blue": (PALE_BLUE, BLUE), "teal": (PALE_TEAL, TEAL), "purple": (PALE_PURPLE, PURPLE),
        "orange": (PALE_ORANGE, ORANGE), "red": (PALE_RED, RED), "green": (HexColor("#EDF8F1"), GREEN)
    }
    bg, border = palette[tone]
    cell = P(f"<b>{title}</b><br/>{text}", "BodyCN")
    t = Table([[cell]], colWidths=[CONTENT_W])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,-1), bg), ("BOX", (0,0), (-1,-1), 1, border),
        ("LEFTPADDING", (0,0), (-1,-1), 11), ("RIGHTPADDING", (0,0), (-1,-1), 11),
        ("TOPPADDING", (0,0), (-1,-1), 8), ("BOTTOMPADDING", (0,0), (-1,-1), 8),
    ]))
    return t

def codebox(text):
    t = Table([[mono(text)]], colWidths=[CONTENT_W])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,-1), HexColor("#F0F3F7")), ("BOX", (0,0), (-1,-1), .5, GRID),
        ("LEFTPADDING", (0,0), (-1,-1), 10), ("RIGHTPADDING", (0,0), (-1,-1), 10),
        ("TOPPADDING", (0,0), (-1,-1), 7), ("BOTTOMPADDING", (0,0), (-1,-1), 7),
    ]))
    return t

class ArchitectureDiagram(Flowable):
    def __init__(self):
        super().__init__(); self.width=CONTENT_W; self.height=190
    def draw(self):
        c=self.canv
        c.setStrokeColor(GRID); c.setFillColor(HexColor("#F5F8FC")); c.roundRect(0,0,self.width,self.height,12,fill=1,stroke=1)
        boxes=[
            (25,78,132,60,PALE_PURPLE,PURPLE,"无限创科 PLC","8 字节 / TCP或串口"),
            (207,66,165,84,PALE_BLUE,BLUE,"JayTom.Dws.WxkcBridge","独立 Windows 服务"),
            (424,98,135,55,PALE_TEAL,TEAL,"DWS 控制链路","TCP 3000"),
            (424,28,135,55,PALE_ORANGE,ORANGE,"DWS 分拣链路","TCP 3001"),
            (612,66,112,84,HexColor("#EDF8F1"),GREEN,"条码威视 DWS","协议服务端"),
        ]
        for x,y,w,h,bg,bd,t1,t2 in boxes:
            c.setFillColor(bg); c.setStrokeColor(bd); c.setLineWidth(1.1); c.roundRect(x,y,w,h,8,fill=1,stroke=1)
            c.setFillColor(NAVY); c.setFont("CNB",10); c.drawCentredString(x+w/2,y+h/2+7,t1)
            c.setFont("CN",7.4); c.setFillColor(MUTED); c.drawCentredString(x+w/2,y+h/2-10,t2)
        def arrow(x1,y1,x2,y2,color,label):
            c.setStrokeColor(color); c.setFillColor(color); c.setLineWidth(1.3); c.line(x1,y1,x2,y2)
            import math
            a=math.atan2(y2-y1,x2-x1); s=6
            pts=[(x2,y2),(x2-s*math.cos(a-.45),y2-s*math.sin(a-.45)),(x2-s*math.cos(a+.45),y2-s*math.sin(a+.45))]
            p=c.beginPath(); p.moveTo(*pts[0]); p.lineTo(*pts[1]); p.lineTo(*pts[2]); p.close(); c.drawPath(p,fill=1,stroke=0)
            c.setFont("CN",7); c.drawCentredString((x1+x2)/2,(y1+y2)/2+6,label)
        arrow(157,108,207,108,PURPLE,"协议终止 / 翻译")
        arrow(372,125,424,125,TEAL,"心跳、确认、结果")
        arrow(372,75,424,55,ORANGE,"触发、格口")
        arrow(559,125,612,125,TEAL,"独立长连接")
        arrow(559,55,612,85,ORANGE,"独立长连接")

class SequenceDiagram(Flowable):
    def __init__(self, participants, steps, height=350):
        super().__init__(); self.width=CONTENT_W; self.height=height; self.participants=participants; self.steps=steps
    def draw(self):
        c=self.canv; n=len(self.participants); gap=self.width/n; top=self.height-36; bottom=18
        c.setFillColor(HexColor("#F5F8FC")); c.setStrokeColor(GRID); c.roundRect(0,0,self.width,self.height,10,fill=1,stroke=1)
        xs=[]
        for i,name in enumerate(self.participants):
            x=gap*(i+.5); xs.append(x)
            c.setFillColor(PALE_BLUE if i%2 else PALE_TEAL); c.setStrokeColor(BLUE if i%2 else TEAL)
            c.roundRect(x-55,self.height-30,110,21,6,fill=1,stroke=1)
            c.setFillColor(NAVY); c.setFont("CNB",7.8); c.drawCentredString(x,self.height-23,name)
            c.setStrokeColor(GRID); c.setDash(2,2); c.line(x,top,x,bottom); c.setDash()
        usable=top-bottom-10; dy=usable/max(1,len(self.steps)); y=top-12
        import math
        for idx,(src,dst,label,tone) in enumerate(self.steps,1):
            x1=xs[src]; x2=xs[dst]; col={"ok":GREEN,"cmd":PURPLE,"dws":ORANGE,"warn":RED,"info":TEAL}.get(tone,BLUE)
            c.setStrokeColor(col); c.setFillColor(col); c.setLineWidth(1.15); c.line(x1,y,x2,y)
            a=0 if x2>x1 else math.pi; s=5
            pts=[(x2,y),(x2-s*math.cos(a-.45),y-s*math.sin(a-.45)),(x2-s*math.cos(a+.45),y-s*math.sin(a+.45))]
            p=c.beginPath(); p.moveTo(*pts[0]); p.lineTo(*pts[1]); p.lineTo(*pts[2]); p.close(); c.drawPath(p,fill=1,stroke=0)
            c.setFillColor(NAVY); c.setFont("CN",6.5); c.drawCentredString((x1+x2)/2,y+5,f"{idx}. {label}")
            y-=dy

class StateDiagram(Flowable):
    def __init__(self): super().__init__(); self.width=CONTENT_W; self.height=270
    def draw(self):
        c=self.canv; c.setFillColor(HexColor("#F5F8FC")); c.setStrokeColor(GRID); c.roundRect(0,0,self.width,self.height,10,fill=1,stroke=1)
        states=[("Created",15,190),("TriggerSent",130,190),("ChuteReceived",250,190),("ExitSent",385,190),("PlcAccepted",505,190),("AckSent",630,190),("Sorting",315,105),("Success",475,48),("Failure",315,48),("NeedsReconcile",125,48)]
        pos={}
        for name,x,y in states:
            w=100 if len(name)<12 else 120; pos[name]=(x,y,w)
            tone=PALE_RED if name in ("Failure","NeedsReconcile") else (HexColor("#EDF8F1") if name=="Success" else PALE_BLUE)
            border=RED if name in ("Failure","NeedsReconcile") else (GREEN if name=="Success" else BLUE)
            c.setFillColor(tone); c.setStrokeColor(border); c.roundRect(x,y,w,30,7,fill=1,stroke=1)
            c.setFillColor(NAVY); c.setFont("MONO",7.2); c.drawCentredString(x+w/2,y+11,name)
        def edge(a,b,label,col=TEAL):
            ax,ay,aw=pos[a]; bx,by,bw=pos[b]; x1=ax+aw/2; y1=ay; x2=bx+bw/2; y2=by+30
            if abs(ay-by)<5: x1=ax+aw; y1=ay+15; x2=bx; y2=by+15
            c.setStrokeColor(col); c.line(x1,y1,x2,y2); c.setFillColor(NAVY); c.setFont("CN",5.8); c.drawCentredString((x1+x2)/2,(y1+y2)/2+3,label)
        for a,b,l in [("Created","TriggerSent","0x12 / 0x68"),("TriggerSent","ChuteReceived","0x67"),("ChuteReceived","ExitSent","F9 11"),("ExitSent","PlcAccepted","0x16"),("PlcAccepted","AckSent","0x64"),("AckSent","Sorting","等待落格"),("Sorting","Success","0x21 / 0x66"),("Sorting","Failure","0x22/23 / 0x66")]: edge(a,b,l)
        edge("TriggerSent","NeedsReconcile","重启/未知发送结果",RED); edge("ExitSent","NeedsReconcile","断线/冲突",RED)

class BridgeDoc(BaseDocTemplate):
    def __init__(self, filename):
        super().__init__(filename,pagesize=PAGE,leftMargin=MARGIN_X,rightMargin=MARGIN_X,topMargin=MARGIN_TOP,bottomMargin=MARGIN_BOTTOM,title="无限创科PLC-条码威视DWS协议桥实施方案",author="OpenAI Codex")
        frame=Frame(self.leftMargin,self.bottomMargin,self.width,self.height,id="normal",leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)
        self.addPageTemplates(PageTemplate(id="main",frames=frame,onPage=self._page))
    def _page(self,c,doc):
        if doc.page>1:
            c.setStrokeColor(GRID); c.setLineWidth(.45); c.line(MARGIN_X,PH-10*mm,PW-MARGIN_X,PH-10*mm)
            c.setFont("CN",7); c.setFillColor(MUTED); c.drawString(MARGIN_X,PH-7.6*mm,"JayTom.Dws.WxkcBridge · 实施规格与协议映射")
        c.setStrokeColor(GRID); c.line(MARGIN_X,8.5*mm,PW-MARGIN_X,8.5*mm)
        c.setFont("CN",6.8); c.setFillColor(MUTED); c.drawString(MARGIN_X,5.2*mm,"内部实施资料 · 未确认项不得由开发者猜测")
        c.drawRightString(PW-MARGIN_X,5.2*mm,str(doc.page))

story=[]

def new_page(title, subtitle=None):
    if story: story.append(PageBreak())
    story.extend(section(title,subtitle))

def gap(h=4):
    story.append(Spacer(1, h*mm))

def add_table(headers, rows, widths=None, **kwargs):
    story.append(make_table(headers, rows, widths=widths, **kwargs))

def add_bullets(items):
    story.extend(bullets(items))

def divider():
    t = Table([[""]], colWidths=[CONTENT_W], rowHeights=[0.4])
    t.setStyle(TableStyle([("LINEABOVE", (0,0), (-1,-1), .5, GRID)]))
    story.append(t)

# 01 封面
story.append(Spacer(1, 13*mm))
story.append(P("无限创科 PLC — 条码威视 DWS", "TitleCN"))
story.append(P("协议桥新程序 · 超级详细实施方案", "TitleCN"))
story.append(Spacer(1, 4*mm))
story.append(P("供 Codex 直接实施的架构、字节协议、映射、状态机、时序、故障处理、配置、日志、测试与验收基线", "SubTitleCN"))
story.append(Spacer(1, 8*mm))
cover_meta = Table([
    [P("推荐解决方案名", "TableHead"), P("JayTom.Dws.WxkcBridge", "Mono"), P("Windows 服务名", "TableHead"), P("JayTomDwsWxkcBridge", "Mono")],
    [P("中文显示名", "TableHead"), P("无限创科 PLC - 条码威视 DWS 协议桥", "TableCell"), P("日志标识", "TableHead"), P("WXKC-DWS-BRIDGE", "Mono")],
    [P("实施基线", "TableHead"), P("v1.0 · 2026-09-22", "TableCell"), P("目标运行时", "TableHead"), P(".NET 10 / Windows Worker Service", "TableCell")],
], colWidths=[29*mm, 67*mm, 29*mm, 72*mm])
cover_meta.setStyle(TableStyle([
    ("BACKGROUND",(0,0),(-1,-1),LIGHT),("BOX",(0,0),(-1,-1),.8,GRID),("INNERGRID",(0,0),(-1,-1),.4,GRID),
    ("VALIGN",(0,0),(-1,-1),"MIDDLE"),("LEFTPADDING",(0,0),(-1,-1),7),("RIGHTPADDING",(0,0),(-1,-1),7),
    ("TOPPADDING",(0,0),(-1,-1),7),("BOTTOMPADDING",(0,0),(-1,-1),7),
]))
story.append(cover_meta)
story.append(Spacer(1, 7*mm))
story.append(ArchitectureDiagram())
story.append(Spacer(1, 5*mm))

# 02 阅读方式与结论
new_page("1. 先看结论：应该怎样实施", "本页是实施者的决策摘要；后续章节给出可直接编码的细节与验收门禁。")
story.append(heading("推荐决策", BLUE))
add_bullets([
    "新建独立 Windows Worker Service：<font name='MONO'>JayTom.Dws.WxkcBridge</font>，部署在工控机；不修改现有条码威视 DWS 主程序，不把桥接逻辑嵌入既有 UI。",
    "桥程序对无限创科 PLC 表现为上位机；对 DWS 表现为 PLC/TCP 客户端。它终止两端协议并做语义翻译，不做透明字节转发。",
    "同时维护 DWS 的两条独立长连接：控制链路 TCP 3000、分拣链路 TCP 3001；任一路断开都进入 <font name='MONO'>Degraded</font>，停止接纳新包裹。",
    "先完成“协议冻结包”和双方模拟器，再实现业务编排；未确认字段全部由功能开关关闭，禁止用经验值猜测后上线。",
])
gap(2)
story.append(heading("最短可落地闭环", TEAL))
add_table(["步骤", "无限创科侧", "桥接动作", "DWS 侧"], [
    ["1", "0x12 创建包裹", "创建唯一在途上下文；分配独立 16 位触发编号", "3001 发送 0x68"],
    ["2", "—", "用 DWS 触发编号关联包裹、条码、格口", "3001 接收 0x67"],
    ["3", "接收 F9 11 格口", "把 DWS 出口号转换为 2 字节大端并下发", "—"],
    ["4", "0x16 接收确认", "确认 PLC 真正接受后才回执 DWS", "3000 发送 0x64"],
    ["5", "0x21 完成 / 0x22、0x23 异常", "结束状态机；映射成功或失败原因", "3000 发送 0x66"],
], [12*mm, 50*mm, 91*mm, 44*mm])
gap(3)
story.append(callout("关键安全边界", "关联不到唯一包裹时不得向 PLC 下发格口；收到 DWS 0x67 时不得立即发送 0x64；重启后不得盲目重放 0x68、F9 11、0x64、0x66。", "orange"))

# 03 文档层级
new_page("2. 依据、边界与事实等级", "附件中的说明只作为资料；本方案以用户明确要求和官方协议事实为最高约束。")
add_table(["等级", "资料", "用途", "冲突时处理"], [
    ["A · 用户约束", "本次会话明确要求", "决定程序边界、无数据库、.log 日志、appsettings.json 配置", "无条件遵守"],
    ["B · DWS 官方", "《圆通速递矩阵标准协议文档-窄带》18 页", "DWS 帧、端口、方向、命令和示例", "同一文档内部矛盾列入阻断项"],
    ["C · 现有源码", "WxkcCommunicationProtocol.cs 及相关模型", "还原无限创科 8 字节报文的现状语义", "新程序重新实现严格编解码，不直接复制缺陷"],
    ["D · 参考版式", "《底扫前置方案(2)》5 页", "章节组织、卡片、时序和表格风格", "不继承其 Reader/PLC 协议内容"],
    ["E · 会话截图", "此前分析截图", "需求线索、名称和初版映射", "仅供核对，不能覆盖官方资料"],
], [24*mm, 55*mm, 74*mm, 44*mm])
gap(4)
story.append(heading("本期范围", TEAL))
add_bullets([
    "一条或多条分拣线的 PLC 与 DWS 双向桥接；每条线拥有独立连接、队列、编号空间、状态与日志上下文。",
    "无限创科固定 8 字节报文的严格收帧、校验、重同步；DWS 矩阵帧的长度驱动解析和端口命令白名单。",
    "包裹关联、格口下发、接收确认、最终结果、心跳、重连、超时、幂等、背压、冷启动与可观测性。",
    "现场模拟、金样报文、故障注入、24 小时稳定性试验、发布回滚与签字验收。",
])
story.append(heading("明确不在本期内", ORANGE))
add_bullets([
    "不改造条码威视 DWS 主程序，不替代其称重、扫码、视觉、矩阵或后台功能。",
    "不使用 SQLite、SQL Server、PostgreSQL、Redis、文件数据库或 EF Core；日志不作为事务数据库自动恢复在途任务。",
    "不自行实现协议未定义的业务成功语义；0x61、0x6B、0x71/0x70、0x73、0x75 默认关闭或仅记录。",
])

# 04 命名
new_page("3. 新程序命名、部署身份与版本规则", "名称同时满足当前项目识别、仓库约束和未来扩展；首版保持业务指向明确。")
add_table(["对象", "确定名称", "说明"], [
    ["解决方案 / 产品", "JayTom.Dws.WxkcBridge", "推荐主名称；包含 DWS 与 Wxkc，定位清晰"],
    ["Host 项目", "JayTom.Dws.WxkcBridge.Host", "Worker Service、依赖注入、生命周期、健康检查"],
    ["Core 项目", "JayTom.Dws.WxkcBridge.Core", "业务状态机、关联、命令编排、端口无关模型"],
    ["Protocols 项目", "JayTom.Dws.WxkcBridge.Protocols", "Wxkc 与 DWS 纯字节编解码；零业务副作用"],
    ["Tests 项目", "JayTom.Dws.WxkcBridge.Tests", "单元、属性、协议金样、状态机测试"],
    ["Integration 项目", "JayTom.Dws.WxkcBridge.IntegrationTests", "模拟 PLC/DWS、断线、时序、磁盘与部署测试"],
    ["Windows 服务", "JayTomDwsWxkcBridge", "无空格的 ServiceName；显示名可用中文"],
    ["进程 / 发布目录", "JayTom.Dws.WxkcBridge.exe / wxkc-dws-bridge", "win-x64 自包含、多文件发布，便于审计和回滚"],
], [42*mm, 73*mm, 82*mm])
gap(4)
story.append(callout("版本策略", "协议实现版本、应用版本、配置架构版本分别记录。每次启动把三者写入 bridge 日志；破坏性配置变更必须提升 <font name='MONO'>ConfigurationSchemaVersion</font> 并拒绝旧配置静默运行。", "purple"))
gap(3)
story.append(codebox("ProductVersion: 1.0.0\nProtocolProfile: Wxkc8-v1 + YtoMatrixNarrowband-v1\nConfigurationSchemaVersion: 1\nServiceName: JayTomDwsWxkcBridge\nLogContext: WXKC-DWS-BRIDGE"))

# 05 总体架构
new_page("4. 目标架构：两个协议端点，一个确定性编排器", "适配器只负责传输和字节协议；所有业务转换集中在 Core，避免映射散落。")
story.append(ArchitectureDiagram())
gap(4)
add_table(["层", "职责", "禁止事项"], [
    ["Host", "Windows 服务生命周期、配置验证、日志、健康状态、优雅停机", "不得解析业务字节或直接改包裹状态"],
    ["WxkcAdapter", "收发 8 字节帧、粘包拆包、F9/XOR 校验、断线重连", "不得决定 DWS 命令或失败原因"],
    ["DwsAdapter", "分别维护 3000/3001 连接、长度收帧、角色/命令/端口校验", "不得直接下发 PLC 格口"],
    ["TranslationEngine", "功能码、格口、失败原因、设备状态的显式映射", "不得在通信代码里写魔法值"],
    ["PackageCorrelationStore", "内存中的唯一关联、编号占用、去重指纹和过期索引", "不得落 SQLite；不得从普通日志自动重建状态"],
    ["BridgeWorker", "单线事件循环、状态迁移、超时、幂等、背压、发送确认", "不得并发改同一包裹；不得无限排队"],
], [34*mm, 91*mm, 72*mm])

# 06 部署网络
new_page("5. 部署拓扑、连接角色与防火墙矩阵", "DWS 为 TCP 服务端；桥程序主动连接。无限创科端的 TCP/串口角色必须由现场冻结。")
add_table(["链路", "客户端", "服务端", "默认端口/介质", "用途", "Ready 门禁"], [
    ["Wxkc", "待现场确认", "待现场确认", "TCP 或串口 · 禁止假设", "8 字节 PLC 命令", "连接 + 心跳正常"],
    ["DWS-Control", "Bridge", "DWS", "TCP 3000", "0x60/62、0x61、0x64、0x66、0x6B、0x70/71、0x73、0x75", "连接 + 心跳正常"],
    ["DWS-Sorting", "Bridge", "DWS", "TCP 3001", "0x68 触发、0x67 条码/三段码/格口", "连接正常"],
], [31*mm, 31*mm, 31*mm, 39*mm, 49*mm, 31*mm])
gap(4)
story.append(heading("网络与操作系统实施项", BLUE))
add_bullets([
    "为工控机、PLC、DWS 分配固定 IP；同一 VLAN 优先。禁止通过 NAT 改写长期连接；如跨网段，记录路由和 ACL 责任人。",
    "Windows 防火墙仅开放实际所需方向；桥程序本身对 DWS 不监听 3000/3001，而是主动发起连接。",
    "关闭网卡节能和会导致长连接休眠的电源策略；记录网卡、交换机端口、速率/双工、时钟同步源。",
    "DWS 3000 与 3001 分别进行端口探测和连接监控，不允许用“其中一条在线”冒充整体 Ready。",
    "服务账户只授予配置读取、日志目录写入、服务启动所需权限；不使用交互式管理员账户。",
])
story.append(callout("现场冻结项", "无限创科链路究竟是 TCP 客户端、TCP 服务端还是串口，现有资料不足以证明。必须拿到厂家通讯说明或抓包；在确认前仅定义可替换 Transport，不写死连接角色。", "red"))

# 07 项目结构
new_page("6. 解决方案与目录结构", "Core 不引用 Socket、SerialPort、NLog/Serilog、Windows Service 或 UI；Protocols 不引用 Core。")
story.append(codebox("JayTom.Dws.WxkcBridge.sln\n├─ src/\n│  ├─ JayTom.Dws.WxkcBridge.Host/\n│  │  ├─ Program.cs                 # 仅组合与启动\n│  │  ├─ Workers/BridgeSupervisor.cs\n│  │  ├─ Configuration/*.cs         # Options + ValidateOptions\n│  │  └─ appsettings.json           # 唯一配置入口\n│  ├─ JayTom.Dws.WxkcBridge.Core/\n│  │  ├─ Packages/                  # 状态、事件、关联键、指纹\n│  │  ├─ Orchestration/             # 单线状态机/命令编排\n│  │  ├─ Mapping/                   # 显式映射与结果类型\n│  │  └─ Ports/                     # IWxkcLink / IDwsLink / IClock\n│  └─ JayTom.Dws.WxkcBridge.Protocols/\n│     ├─ Wxkc/                      # 严格 8 字节 codec + framer\n│     └─ Dws/                       # 矩阵帧 codec + 双端口白名单\n├─ tests/\n│  ├─ JayTom.Dws.WxkcBridge.Tests/\n│  └─ JayTom.Dws.WxkcBridge.IntegrationTests/\n└─ tools/\n   ├─ WxkcPlcSimulator/\n   └─ DwsDualPortSimulator/"))
gap(4)
add_table(["依赖方向", "允许", "禁止"], [
    ["Host → Core / Protocols", "允许；Host 是组合根", "Core/Protocols 反向引用 Host"],
    ["Core → BCL", "允许", "Core 引用 TCP、串口、日志实现、数据库"],
    ["Protocols → BCL", "允许 Span、BinaryPrimitives、SequenceReader", "Protocols 引用状态机或包裹仓储"],
    ["Tests → 所有生产项目", "允许", "生产项目引用 Tests 或模拟器"],
], [44*mm, 72*mm, 81*mm])

# 08 模块责任
new_page("7. 模块责任与关键接口", "接口以不可变记录和明确结果类型传递事实；不得用 null/异常承担正常协议分支。")
add_table(["模块/接口", "输入", "输出", "关键不变量"], [
    ["IWxkcFrameDecoder", "任意字节块", "0..N 个 WxkcFrame / ParseError", "仅输出通过 F9、功能码、XOR 和范围校验的帧"],
    ["IDwsFrameDecoder", "任意字节块 + 端口角色", "0..N 个 DwsFrame / ParseError", "按 Length 成帧后验尾；命令必须符合端口/方向"],
    ["ITranslationEngine", "强类型协议事件", "Mapped / Unsupported / Invalid / NeedsConfirmation", "所有数字映射可追溯，无默认成功"],
    ["IPackageCorrelationStore", "关联键、事件、时间", "唯一上下文或歧义结果", "一个触发号最多一个活跃上下文；歧义绝不择一"],
    ["ILineOrchestrator", "单线有序事件", "待发送命令、状态迁移、审计事件", "一个事件处理完成后再处理下一事件"],
    ["IOutboundWriter", "有界发送队列", "Written / Failed / Unknown", "每连接单写者；成功仅在字节交给 Socket 后记录"],
    ["IClock", "—", "UTC 时间与单调时钟", "超时使用单调时钟；日志使用 DateTimeOffset"],
], [45*mm, 48*mm, 55*mm, 49*mm])
gap(3)
story.append(callout("实现原则", "编解码器不得触发 Socket 写入；状态机不得直接调用日志文件；日志失败不能被吞掉。每个外部副作用先形成待执行命令，由边界层执行并把确定结果作为新事件送回。", "teal"))

# 09 并发
new_page("8. 并发、队列与单写者模型", "目标不是最大并行度，而是保证同一线体、同一包裹的事件顺序可证明。")
add_table(["执行单元", "并发模型", "默认容量", "满载行为"], [
    ["Wxkc 接收", "1 个读取循环 → 解析 → 入 LineEvent 队列", "字节缓冲 64 KiB；事件 2,000", "停止接收新任务，进入 Degraded；不得丢帧"],
    ["DWS 3000 接收", "1 个读取循环 → 控制帧解析 → 入队", "字节缓冲 64 KiB；事件 2,000", "断开该连接并报警，触发重连"],
    ["DWS 3001 接收", "1 个读取循环 → 分拣帧解析 → 入队", "字节缓冲 64 KiB；事件 2,000", "停止新 0x68，断开并重连"],
    ["每条线体编排", "1 个 Channel 消费者 / Actor", "业务事件 10,000", "取消 Ready，拒绝新 0x12，保留已入队顺序"],
    ["每条连接发送", "1 个有界 Channel + 1 个写循环", "控制 2,000；分拣 2,000；PLC 2,000", "不得静默 Drop；向编排器回报 QueueFull"],
    ["日志", "异步批量写，但关键审计不可丢", "由 appsettings.json 配置", "写失败/磁盘低于阈值：fail-closed"],
], [41*mm, 65*mm, 44*mm, 47*mm])
gap(4)
story.append(heading("事件处理顺序", PURPLE))
story.append(codebox("Socket/Serial callback\n  → copy bytes into owned buffer\n  → strict framer + decoder\n  → immutable ProtocolEvent\n  → bounded LineEventChannel\n  → single LineOrchestrator\n  → StateTransition + OutboundCommand + AuditEvent\n  → per-connection bounded writer\n  → SendCompleted / SendFailed / SendOutcomeUnknown event"))
gap(3)
story.append(callout("禁止", "不得从多个 Socket 回调直接修改关联字典；不得让业务线程同时写同一 NetworkStream；不得使用无界 Channel；不得以 Task.Run 为每个报文创建无限任务。", "red"))

# 10 Wxkc frame
new_page("9. 无限创科 8 字节协议：现状规格与严格化要求", "本节由现有源码还原，属于待厂家文档/抓包确认的基线；新程序不能照搬现有宽松解析。")
add_table(["字节偏移", "长度", "名称", "编码", "新程序校验"], [
    ["0", "1", "帧头", "固定 F9", "必须等于 F9；否则逐字节扫描到下一个候选 F9"],
    ["1", "1", "功能码", "见方向命令表", "必须属于当前方向白名单"],
    ["2..3", "2", "PLC 序号", "UInt16，大端", "参与关联；不得格式化成超过 4 位后仍当作 UInt16"],
    ["4..5", "2", "数据/格口", "UInt16，大端", "按功能码解释；出口范围由配置校验"],
    ["6", "1", "交互/联车/异常", "功能相关", "异常语义读取本字节，不得误读校验字节"],
    ["7", "1", "XOR", "B1 ^ B2 ^ B3 ^ B4 ^ B5 ^ B6", "接收必须验证；发送必须重算；不包含 F9"],
], [24*mm, 18*mm, 33*mm, 45*mm, 77*mm])
gap(4)
add_table(["方向", "功能码", "现状名称", "桥中用途", "确定性"], [
    ["Bridge → PLC", "0x01", "Heartbeat", "桥侧 PLC 链路心跳", "待现场确认周期/应答关系"],
    ["Bridge → PLC", "0x11", "SendExit", "下发 DWS 格口", "核心必需"],
    ["Bridge → PLC", "0x13", "PreSignal", "预信号", "默认关闭；语义待确认"],
    ["Bridge → PLC", "0x15", "PackageInfoCompleted / PackageCenter", "现有源码出现同码双语义", "阻断：必须澄清"],
    ["PLC → Bridge", "0x02", "Heartbeat", "更新 PLC 在线状态", "待现场确认周期/应答关系"],
    ["PLC → Bridge", "0x12", "CreatePackage", "建立上下文并触发 DWS", "核心必需"],
    ["PLC → Bridge", "0x14", "PreSignalReply", "预信号确认", "默认关闭"],
    ["PLC → Bridge", "0x16", "SequenceBindingReply", "PLC 已接受格口，允许 0x64", "核心必需"],
    ["PLC → Bridge", "0x21", "RemovePackage", "正常分拣完成", "核心必需"],
    ["PLC → Bridge", "0x22 / 0x23", "PackageException", "异常完成并产生 0x66 失败", "核心必需；原因映射待冻"],
    ["PLC → Bridge", "0x31", "ResetButton", "仅记录/进入人工协调", "不得映射 DWS 0x75"],
], [34*mm, 25*mm, 47*mm, 63*mm, 28*mm])

# 11 Wxkc parser
new_page("10. 无限创科收帧、重同步与发送算法", "固定 8 字节并不等于每次 Read 恰好收到 8 字节；必须支持拆包、粘包和垃圾前缀。")
story.append(heading("接收算法", BLUE))
add_bullets([
    "每个连接拥有独立可增长但有上限的缓冲区；新连接建立时必须清空旧会话残留缓冲。",
    "查找首个 F9；其前所有字节记为 GarbagePrefix 并丢弃。若剩余少于 8 字节则等待下一次读取。",
    "取 8 字节候选帧，验证功能码方向白名单、字段范围及 XOR。全部通过才输出强类型事件并消费 8 字节。",
    "若候选无效，只消费该候选的第一个 F9，再继续寻找下一 F9；不得固定每 8 字节切片，否则一次掉字节会永久错位。",
    "连续错误次数、单位时间错误率、最大缓冲、最大垃圾字节均受配置限制；超过阈值主动断开并进入 Degraded。",
])
gap(3)
story.append(heading("发送算法", TEAL))
add_bullets([
    "先构造 0..6 字节，再对 B1..B6 计算 XOR 写入 B7；编码器必须返回恰好 8 字节。",
    "单写者循环按序写入；在写调用完成前不记录 SendCompleted。超时或连接断裂导致结果未知时进入 NeedsReconcile，不自动重发非幂等业务帧。",
    "原始发送帧、解析后的字段、包裹关联标识和状态前后值写入协议/审计 .log；业务日志可脱敏条码，但原始帧保留策略需现场安全确认。",
])
gap(3)
story.append(codebox("示意：F9 11 12 34 00 2A 00 XX\nXOR = 11 ^ 12 ^ 34 ^ 00 ^ 2A ^ 00 = 1D\n完整帧 = F9 11 12 34 00 2A 00 1D\n注意：该示意只展示算法，不代表现场有效序号/格口。"))
gap(3)
story.append(callout("现有实现缺口", "现有代码只检查长度为 8，未严格验证 F9 与 XOR；异常码字段还存在读取 B7（校验字节）的风险。新程序必须拥有独立 codec 与完整测试，不能直接复用原类作为生产解析器。", "orange"))

# 12 DWS frame
new_page("11. DWS 矩阵协议：通用帧与长度规则", "以下为官方窄带协议的帧基础；所有多字节整数按大端解析。")
add_table(["偏移", "长度", "字段", "规则"], [
    ["0..1", "2", "Head", "固定 0A 0B"],
    ["2..3", "2", "Length", "大端 UInt16；从 Sender 开始到 0D 0A 结束的字节数；整帧总长 = Length + 4"],
    ["4", "1", "Sender", "00 广播、01 PLC、02 后台、03 DWS；桥连接 DWS 时使用 PLC 身份 01"],
    ["5", "1", "Receiver", "按命令方向验证；禁止接受发给其他角色的帧"],
    ["6", "1", "Command", "按连接端口及方向白名单验证"],
    ["7..N-3", "变长", "Payload", "严格按命令字段和内部长度值解析；不可越界"],
    ["N-2..N-1", "2", "Tail", "固定 0D 0A；先由 Length 定位，再验尾"],
], [22*mm, 18*mm, 31*mm, 126*mm])
gap(4)
story.append(heading("流式收帧算法", BLUE))
add_bullets([
    "扫描 0A 0B；头前垃圾字节单独记录。若不足 4 字节，等待后续数据。",
    "读取 Length，先做数值边界验证。协议写 1..1024，但合法最短帧按字段计算至少为 5（Sender、Receiver、Command、Tail 两字节）；建议配置有效范围 5..1024。",
    "等待缓冲达到 Length + 4；仅切取该长度的候选帧，然后验证末尾恰为 0D 0A。禁止仅按尾标记分割，因为 Payload 可能包含标记值。",
    "再验证 Sender/Receiver、命令与端口白名单、固定字段、变长字段、ASCII 范围和剩余长度为 0；全部通过后输出事件。",
    "帧错误时执行有界重同步；错误率超过阈值断开会话。半帧断线后清空缓冲，不能与新连接拼接。",
])
story.append(callout("长度优先级", "编码时永远由实际序列化后的字段计算 Length；官方示例中的若干长度值互相矛盾，不能把示例常量写进代码。所有冲突在第 22 章单列。", "red"))

# 13 DWS commands
new_page("12. DWS 双端口命令矩阵", "端口是协议语义的一部分。命令出现在错误端口时拒绝并记录，不做兼容猜测。")
add_table(["端口", "命令", "方向（桥视角）", "用途", "本期策略"], [
    ["3001", "0x68", "Bridge → DWS", "触发编号 / 动态秤开始结束扩展", "核心发送；扩展字段默认关闭"],
    ["3001", "0x67", "DWS → Bridge", "触发号、条码、三段码、格口及扩展", "核心接收；按实际长度解析"],
    ["3000", "0x60", "Bridge → DWS", "每 10 秒发送：线体号2B、状态1B、模式1B", "独立于 Wxkc 心跳"],
    ["3000", "0x62", "DWS → Bridge", "收到 0x60 后回复同类字段", "验证线体、状态与模式"],
    ["3000", "0x61", "Bridge → DWS", "PLC 设备状态上报", "默认关闭；缺少可靠 Wxkc 来源"],
    ["3000", "0x64", "Bridge → DWS", "PLC 已接收格口的应答", "仅在收到匹配 0x16 后发送"],
    ["3000", "0x66", "Bridge → DWS", "分拣成功/失败结果", "核心发送；尾部扩展待确认"],
    ["3000", "0x6B", "DWS ↔ Bridge", "格口锁定/解锁状态", "默认关闭；无安全 Wxkc 映射"],
    ["3000", "0x71 / 0x70", "请求 / 应答", "进出口模式设置", "默认关闭；角色和语义需现场冻结"],
    ["3000", "0x73", "DWS → Bridge", "报警停机", "默认仅告警并进入 Degraded；不擅自控制 PLC"],
    ["3000", "0x75", "DWS → Bridge", "PDA 解锁格口", "默认拒绝；不得与 0x31 互映"],
], [23*mm, 29*mm, 39*mm, 60*mm, 46*mm])
gap(4)
story.append(callout("心跳", "官方说明为约每 10 秒一次、连续 3 次未收到视为异常并告警/断开重连。实现时把周期、允许丢失次数和响应超时放入 appsettings.json；默认值可以采用 10 秒/3 次，但必须可在协议冻结时调整。", "teal"))

# 14 mapping overview
new_page("13. 核心协议映射总表", "“直接”仅表示语义可建立；字段细节仍须通过金样测试。未列出的命令没有业务副作用。")
add_table(["无限创科事件", "桥接条件与动作", "DWS 命令", "结论"], [
    ["0x12 CreatePackage", "校验唯一性；创建上下文；分配 DWS 触发号；建立发送意图", "3001 / 0x68", "直接映射"],
    ["—", "接收 0x67 后用触发号定位唯一上下文；解析条码/格口；格口转 UInt16 大端", "3001 / 0x67", "DWS 入站"],
    ["F9 11 SendExit", "只有唯一关联、格口有效、状态允许时下发；缓存完整帧指纹", "来自 0x67", "直接映射"],
    ["0x16 BindingReply", "必须与已下发格口的序号/周期匹配；然后产生接收应答", "3000 / 0x64", "延迟确认"],
    ["0x21 RemovePackage", "只接受第一个终态；生成成功结果", "3000 / 0x66 Success", "直接映射"],
    ["0x22 / 0x23 Exception", "解析 B6 业务异常；映射失败原因；未确认映射则人工隔离", "3000 / 0x66 Failure", "条件映射"],
    ["0x01 / 0x02 Heartbeat", "仅维护 Wxkc 会话健康，不转发其字节", "0x60 / 0x62", "两套心跳独立"],
    ["0x31 Reset", "记录现场复位事实；改变服务状态需显式规则", "不得发送 0x75", "禁止猜测映射"],
], [45*mm, 92*mm, 35*mm, 25*mm])
gap(3)
story.append(callout("0x64 的正确时机", "0x64 表示 PLC 已经接收分拣格口，而不是桥程序已经收到 DWS 0x67。因此必须等待匹配的无限创科 0x16；如果 0x16 超时，不得伪造 0x64 成功。", "red"))

# 15 success sequence
new_page("14. 正常包裹完整时序", "每一步都先通过状态、关联、字段与链路健康门禁；图中的箭头不代表允许自动重放。")
story.append(SequenceDiagram(
    ["无限创科 PLC", "Bridge Core", "DWS 3001", "DWS 3000"],
    [
        (0,1,"0x12 创建包裹（PLC序号）","cmd"),
        (1,2,"0x68 触发（独立DWS触发号）","dws"),
        (2,1,"0x67 条码/三段码/格口","dws"),
        (1,0,"F9 11 下发格口","cmd"),
        (0,1,"0x16 接收格口确认","ok"),
        (1,3,"0x64 格口接收应答","dws"),
        (0,1,"0x21 正常完成","ok"),
        (1,3,"0x66 成功结果","dws"),
    ], height=270
))
gap(3)
add_table(["步骤", "提交状态", "必须写入审计 .log 的证据"], [
    ["0x68 已发送", "TriggerSent", "InstanceEpoch、线体、PLC 序号、DWS 触发号、原始帧、发送完成时间"],
    ["0x67 已接受", "ChuteReceived", "DWS 连接代次、触发号、条码哈希/掩码、台号、格口、原始帧"],
    ["F9 11 已发送", "ExitSent", "映射前格口、映射后 UInt16、Wxkc 帧与 XOR、发送结果"],
    ["0x16 已接受", "PlcAccepted", "匹配键、重复/冲突判定、状态迁移"],
    ["0x66 已发送", "ResultSent → Closed", "成功/失败、失败原因、首尾车字段策略、关闭时间、tombstone 到期时间"],
], [31*mm, 43*mm, 123*mm])

# 16 exception sequence
new_page("15. 异常包裹完整时序", "异常仍必须闭环到 DWS，但只允许发送已冻结的失败原因；未知原因进入人工隔离。")
story.append(SequenceDiagram(
    ["无限创科 PLC", "Bridge Core", "DWS 3001", "DWS 3000"],
    [
        (0,1,"0x12 创建包裹","cmd"),
        (1,2,"0x68 触发","dws"),
        (2,1,"0x67 格口","dws"),
        (1,0,"F9 11 下发格口","cmd"),
        (0,1,"0x16 确认","ok"),
        (1,3,"0x64 接收应答","dws"),
        (0,1,"0x22/0x23 + B6异常码","warn"),
        (1,3,"0x66 失败 + 已冻结原因","warn"),
    ], height=270
))
gap(3)
story.append(callout("未确认映射的处理", "如果无限创科 B6 无法安全映射为 DWS 原因，桥程序不得随意填 00 或 PLC 故障并假装闭环。默认进入 <font name='MONO'>NeedsReconcile</font>、停止该包裹后续自动动作、写 error 与 package-audit .log，并暴露告警。", "red"))
gap(3)
add_table(["情况", "桥接行为", "禁止行为"], [
    ["异常帧重复且内容相同", "按幂等策略重放已缓存的同一 0x66（仅在已确认安全时）", "新建第二个终态或改变原因"],
    ["先成功后失败 / 先失败后成功", "第一终态生效；后续冲突隔离并报警", "覆盖第一终态并再次发送不同结果"],
    ["异常到达但无上下文", "UnmatchedLateEvent；仅记录与告警", "仅凭 PLC 序号猜测旧任务"],
    ["0x66 写入结果未知", "NeedsReconcile；等待人工/双方查询能力", "自动无限重发"],
], [45*mm, 82*mm, 70*mm])

# 17 heartbeat and reconnect
new_page("16. 心跳、断线检测与重连时序", "Wxkc 与 DWS 心跳彼此独立；桥程序只翻译业务语义，不转发心跳字节。")
add_table(["链路", "发送方 / 响应方", "推荐初始值", "失效判定", "失效动作"], [
    ["DWS 3000", "Bridge 0x60 / DWS 0x62", "周期 10s；应答 3s", "连续 3 次未完成心跳", "关闭连接；整体 Degraded；指数退避重连"],
    ["DWS 3001", "协议无独立命令；用连接与业务超时监控", "空闲探测 30s；TCP keepalive 辅助", "Socket 错误/读 0/超时", "关闭连接；停止新 0x68；重连"],
    ["Wxkc", "0x01 / 0x02 关系待现场冻结", "暂不硬编码；全部配置化", "丢失阈值或 Socket/串口故障", "整体 Degraded；不再下发新格口"],
], [33*mm, 53*mm, 43*mm, 42*mm, 46*mm])
gap(4)
story.append(heading("统一重连规则", PURPLE))
add_bullets([
    "每条连接独立状态和退避计数：1s、2s、4s、8s、16s、30s 上限，加入 ±20% 抖动；稳定 60s 后清零退避。",
    "重连前取消旧读取/写入循环，等待退出，清空会话字节缓冲，生成新的 ConnectionEpoch；旧会话回调不得进入新会话队列。",
    "任一业务链路断开即取消整体 Ready。DWS 3000 断开时不能创建无法闭环确认/结果的新任务；3001 断开时不能发送触发；PLC 断开时不能下发格口。",
    "连接恢复不等于 Ready：必须完成身份、首个健康心跳、日志目录可写、队列低于恢复阈值以及冷启动屏障。",
    "默认不重放断线前非幂等业务帧。若未来需要查询/恢复协议，应作为新版本单独设计。",
])
story.append(callout("半开连接", "TCP KeepAlive 只是辅助；业务心跳才是 3000 的主要活性证据。必须测试拔网线、交换机黑洞、服务端进程终止、RST、FIN、只读不写与只写不读。", "orange"))

# 18 service state
new_page("17. 服务级状态机与 Ready 判定", "状态必须可查询并写入 bridge .log；不能以“进程仍在”代表可接单。")
add_table(["状态", "进入条件", "允许动作", "退出条件"], [
    ["Starting", "进程启动", "加载配置、创建日志目录、生成 InstanceEpoch", "配置/日志预检通过 → Connecting；否则退出"],
    ["Connecting", "至少一条链路未就绪", "连接、握手、心跳、清空旧缓冲", "全部健康且冷启动屏障通过 → Ready"],
    ["Ready", "PLC + 3000 + 3001 + 两侧心跳 + 日志可写 + 队列健康", "接受新 0x12 和完整业务闭环", "任一门禁失败 → Degraded"],
    ["Degraded", "链路、队列、日志、协议错误率等异常", "完成可证明安全的已入队动作；拒绝新包裹", "具备恢复条件 → Recovering"],
    ["Recovering", "所有故障条件初步消失", "稳定性观察、空队列/线体确认、重置统计", "持续健康窗口通过 → Ready；失败 → Degraded"],
    ["Draining", "Windows Stop / 部署升级", "立即取消 Ready；限时排空安全发送；写未完成清单", "到期或排空 → Stopped"],
    ["Stopped", "资源关闭", "刷新 .log，释放端口和锁", "仅由新进程重新开始"],
], [31*mm, 67*mm, 63*mm, 36*mm])
gap(4)
story.append(callout("Ready 的硬条件", "三条业务链路不是“二选一”；DWS 3000、DWS 3001 或 PLC 任一路未健康，均不得继续产生无法闭环的新分拣任务。日志目录不可写或剩余空间不足同样取消 Ready。", "red"))

# 19 package state
new_page("18. 包裹级状态机", "图示为主干；完整实现还包括 Queued、ResultSent、Closed、TimedOut、Quarantined 等审计状态。")
story.append(StateDiagram())
gap(4)
add_table(["状态组", "状态", "允许的下一步", "超时默认"], [
    ["触发", "Created → TriggerQueued → TriggerSent", "接收匹配 0x67", "TimedOut / NeedsReconcile；不重发 0x68"],
    ["格口", "ChuteReceived → ExitQueued → ExitSent", "接收匹配 0x16", "NeedsReconcile；不伪造 0x64"],
    ["确认", "PlcAccepted → DwsAckSent → Sorting", "0x21 或 0x22/0x23", "TimedOut；不伪造 0x66"],
    ["终态", "CompletedSuccess / CompletedFailure → ResultSent → Closed", "进入 tombstone 隔离", "发送结果未知则 NeedsReconcile"],
    ["安全分支", "TimedOut / Quarantined / NeedsReconcile", "人工处置或明确协议恢复", "无自动业务发送"],
], [29*mm, 58*mm, 66*mm, 44*mm])

# 20 correlation
new_page("19. 包裹关联模型：防止 A 包裹收到 B 格口", "不允许只按条码、16 位序号或“最近一个包裹”关联；必须使用复合上下文和代次。")
add_table(["字段", "来源", "作用", "规则"], [
    ["InstanceEpoch", "每次进程启动生成", "隔离重启前后事件", "不可复用；写入每条审计日志"],
    ["LineId", "appsettings.json / 连接", "隔离不同线体", "任何入站事件在解析后立即绑定"],
    ["DeviceNo", "DWS 0x67 / 配置", "区分左1、右2设备", "0x64 返回规则需冻结"],
    ["DwsTriggerId", "桥独立分配 UInt16", "0x68 ↔ 0x67 主关联", "活跃期 + tombstone 期内唯一"],
    ["WxkcSequence", "Wxkc B2..B3", "PLC 事件链关联", "同一值可跨周期复用，不能单独做主键"],
    ["PackageCycle", "同线体递增 UInt64", "区分序号回绕/复用", "仅内存；每次创建递增"],
    ["ConnectionEpochs", "三条链路每次重连生成", "拒绝旧会话迟到回调", "事件必须携带对应代次"],
    ["PayloadFingerprints", "规范化字段 + 原始帧哈希", "判定重复与冲突", "相同标识不同指纹 → Quarantined"],
], [41*mm, 46*mm, 59*mm, 51*mm])
gap(4)
story.append(codebox("CorrelationKey = (InstanceEpoch, LineId, DeviceNo, DwsTriggerId, WxkcSequence, PackageCycle)\nDWS lookup     = (InstanceEpoch, LineId, DeviceNo, DwsTriggerId)\nPLC lookup     = (InstanceEpoch, LineId, WxkcSequence, active PackageCycle)\n规则：lookup 结果必须恰好 1 条；0 条=Unmatched，>1 条=Ambiguous，二者都禁止输出格口。"))
gap(3)
story.append(callout("关联验收结论", "最重要的生产验收不是“能通信”，而是通过重复、乱序、延迟、断线、回绕和重启测试证明：系统不会把 A 包裹的格口下发给 B 包裹。", "green"))

# 21 trigger allocator
new_page("20. DWS 触发编号分配、回绕与隔离", "DWS 触发号是桥生成的独立 UInt16，不应简单复用 PLC 序号。")
add_bullets([
    "维护 0..65535 的游标与占用集合。分配时从游标起查找第一个既不活跃、又不在 tombstone 隔离期的编号。",
    "成功占用后游标前进；65535 后回绕到 0。编号耗尽时进入 Degraded 并拒绝新包裹，禁止覆盖活跃记录。",
    "包裹 Closed 后释放活跃占用，但保留 tombstone 至 <font name='MONO'>TriggerIdReuseDelay</font> 到期，以吸收迟到 0x67。",
    "触发号只在事件循环内分配，或使用单独的原子分配器；不得先分配后异步失败却忘记释放/标记。",
    "0x68 写入结果未知时，触发号必须继续隔离，不能立即复用。",
])
gap(4)
add_table(["场景", "期望结果"], [
    ["游标在 65534，65535 可用，0 可用", "依次分配 65535、0；都建立不同上下文"],
    ["65535 仍处于 tombstone", "跳过 65535，继续寻找下一个安全编号"],
    ["全部 65536 个编号活跃/隔离", "拒绝新任务，写容量告警；不得无限循环或覆盖"],
    ["0x67 在 tombstone 内迟到", "识别为已关闭任务的 LateDuplicate，不下发 PLC 格口"],
    ["0x67 在重启后迟到", "InstanceEpoch 不匹配；UnmatchedLateEvent，不猜测关联"],
], [71*mm, 126*mm])

# 22 idempotency
new_page("21. 幂等、重复、乱序与冲突处理", "每个外部报文先做“身份 + 状态 + 指纹”判定，再决定是否产生副作用。")
add_table(["输入", "同标识同内容", "同标识不同内容", "乱序/迟到"], [
    ["Wxkc 0x12", "返回已有上下文，不再发送 0x68", "Quarantined；停止该序号", "若旧周期已关闭且过隔离期，可建立新周期"],
    ["DWS 0x67", "未下发则继续；已下发则不重复 F9 11", "Quarantined；绝不择一格口", "无唯一上下文则 Unmatched，不下发"],
    ["Wxkc 0x16", "若 0x64 已安全发送，可重放缓存应答或忽略", "序号/字段冲突报警", "未处于 ExitSent 时拒绝副作用"],
    ["Wxkc 0x21", "只保留第一成功终态；必要时重放同一 0x66", "与已失败终态冲突并隔离", "没有 Sorting 上下文则 Unmatched"],
    ["Wxkc 0x22/0x23", "同原因重复按第一终态处理", "不同原因或与成功冲突报警", "没有 Sorting 上下文则 Unmatched"],
    ["DWS 0x62", "刷新同一次心跳状态", "字段异常记录并降级", "旧 ConnectionEpoch 响应丢弃"],
], [38*mm, 55*mm, 55*mm, 49*mm])
gap(4)
story.append(heading("允许自动重试的范围", TEAL))
add_bullets([
    "连接建立、心跳、明确无业务副作用的健康检查可以自动重试。",
    "0x68、F9 11、0x64、0x66 默认不因超时而盲目重发；只有协议冻结后证明对端支持幂等键，才可按命令分别开启。",
    "所有重放都必须复用原始业务标识与完全相同的规范化载荷，不能生成新的触发号或改变结果。",
])

# 23 failure mapping
new_page("22. 失败原因映射：已确认与待确认必须分开", "DWS 0x66 原因：00无失败、01摆轮故障、02间距过小、03分拣迷失、04 PLC故障、05锁格。")
add_table(["Wxkc B6", "现状语义", "建议 DWS 原因", "置信度", "默认实施"], [
    ["0x01", "DistanceTooClose / 包裹间距过小", "0x02 间距过小", "高：语义直接一致", "启用，但仍用抓包金样确认"],
    ["0x02", "LockExit / 格口锁定", "0x05 锁格", "高：语义直接一致", "启用，但确认格口字段来源"],
    ["0x03", "VehicleMismatch / 小车编号不匹配", "候选 0x03 分拣迷失", "低：并非同一表述", "默认关闭；厂家确认后启用"],
    ["0x04", "UnstableLineSpeed / 线速度不稳定", "候选 0x04 PLC故障", "低：范围过宽", "默认关闭；厂家确认后启用"],
    ["无", "—", "0x01 摆轮故障", "无可靠 Wxkc 来源", "不得主动生成"],
    ["未知值", "未知 / 新固件扩展", "不得填 0x00 或任意失败码", "不适用", "Quarantined + 告警"],
], [25*mm, 59*mm, 45*mm, 39*mm, 29*mm])
gap(4)
story.append(heading("0x66 编码规则", ORANGE))
add_bullets([
    "设备号、条码和格口必须来自此前同一上下文的 0x67；结果 01=成功、00=失败；成功时失败原因用 00。",
    "官方第 13 页完整示例在失败原因后附加 <font name='MONO'>FF + 首车2B + 尾车2B</font>，但字段定义未说明是否必填或何时出现。本期默认不开启该 5 字节扩展。",
    "如果现场 DWS 要求首尾车字段，必须明确数据来源、无小车号表示法、大小端、成功/失败是否都带，并加入双方金样后再打开功能开关。",
])
story.append(callout("配置策略", "Mapping.FailureReasons 只能包含经过签字确认的条目。启动验证发现启用条目缺少映射、映射到未定义 DWS 码或多个 Wxkc 原因冲突时，服务启动失败。", "red"))

# 24 official contradictions
new_page("23. 官方协议内部矛盾与实施阻断项", "这些不是排版笔误可以随意修正的问题；应保留原例、计算值和厂家答复三份证据。")
add_table(["命令 / 页", "文档写法", "按统一规则实算", "本期安全处理"], [
    ["0x68 动态秤 · p7", "追加 00 01 / 00 02 后 Length 仍写 0008", "若两字节属于 Payload，应为 000A", "动态秤扩展禁用；确认字段位置、计长与触发位置关系"],
    ["0x64 六位条码 · p10-11", "Length 写 0009", "3头字段+设备1+长度1+条码6+7C1+格口1+尾2 = 000F", "编码器动态计算；原错误例作为负面测试"],
    ["0x66 · p11-13", "p12 写 0009；完整例 0021 且多出 FF+首车2B+尾车2B", "完整例 0021 可实算，但额外 5B 未定义", "扩展默认关闭；厂家确认必填性和数据来源"],
    ["0x6B · p13-14", "Length 写 0009", "3头字段+状态1+格口1+尾2 = 0007", "功能默认关闭；现场样报确认"],
    ["0x71 · p14-15", "字段分解写 0009；完整帧写 0006", "3头字段+模式1+尾2 = 0006", "采用动态计算；0006 纳入金样"],
    ["0x67 触发号 · p8-9", "列出字段但未写宽度；样例为 00 05", "由样例推断 2B 大端", "标为“由样例推断”；联调前确认"],
    ["0x64 设备号 · p10", "先说左1右2，又红字说现固定01", "无法由协议消解", "确认右台回 01 还是镜像 02"],
    ["0x61 后续设备 · p5", "摆臂、窄带仅有标题", "没有可编码字段", "不实现，不猜测"],
], [38*mm, 59*mm, 50*mm, 50*mm])
gap(3)
story.append(callout("编码总原则", "Length、条码长度、三段码长度、扩展长度都由实际序列化结果计算；解析时信任边界已验证的长度字段而不是示例固定值。矛盾例作为“应拒绝/应修正”测试，不被删除。", "orange"))

# 25 unsupported commands
new_page("24. 尚无安全映射的 DWS 功能", "默认策略是关闭或只告警；没有协议来源时，桥程序不得伪造 PLC 已执行。")
add_table(["DWS 命令", "缺口", "默认行为", "启用前所需证据"], [
    ["0x61 设备状态", "Wxkc 仅有有限心跳/异常，无法构造线体速度、模式、摆轮信息", "FeatureFlags.SendDwsDeviceStatus=false", "PLC 正式字段、更新触发、单位、故障码、摆轮列表"],
    ["0x6B 锁/解格", "Wxkc 0x02 仅像锁格异常，不等同完整状态变化", "不发送；仅可从已知异常记录", "双方状态机、解锁来源、格口范围"],
    ["0x71 / 0x70 模式", "Wxkc 没有明确进港/出港设置与应答", "拒绝副作用并报警", "PLC 对应命令、权限、安全停机规则"],
    ["0x73 报警停机", "没有确认的 PLC 停机命令", "进入 Degraded、写高优先级告警；不发送 PLC 控制", "厂家定义的安全停机接口和恢复流程"],
    ["0x75 PDA 解锁", "Wxkc 0x31 是复位按钮，语义不等价", "拒绝并记录 Unsupported", "明确解锁指令、操作权限、审计及幂等"],
], [32*mm, 70*mm, 52*mm, 43*mm])
gap(4)
story.append(callout("Fail-closed", "未知命令、未知枚举、错误端口、错误 Sender/Receiver、长度冲突或未启用功能，都不得产生 PLC 输出或 DWS 成功应答。记录原始帧与原因后按错误率策略降级/断线。", "red"))

# 26 restart
new_page("25. 不使用 SQLite 时的重启与冷启动策略", "关联只在有界内存中存在；日志用于审计和取证，不作为事务状态库。")
add_table(["阶段", "必须执行", "业务副作用"], [
    ["进程停止前", "取消 Ready；限时排空已确定安全的发送；记录所有未完成上下文摘要；刷新 .log", "不新建任务；不延长到无限等待"],
    ["进程启动", "生成新 InstanceEpoch；关联表为空；验证 appsettings.json 和日志目录", "不得从 .log 自动恢复在途状态"],
    ["连接恢复", "分别建立 PLC、3000、3001；清空旧缓冲；完成心跳；记录 ConnectionEpoch", "不得重放旧 0x68/F9 11/0x64/0x66"],
    ["冷启动屏障", "确认线体处于可接单状态；建议现场清线/清周期并由操作员确认", "屏障通过前拒绝 0x12 或主动断开 PLC 业务会话"],
    ["迟到事件", "0x67、0x16、0x21/22/23 无新代次唯一上下文时记 UnmatchedLateEvent", "不得仅凭相同 16 位序号猜测旧包裹"],
    ["重新 Ready", "三链路、两侧心跳、日志、队列、冷启动确认均健康并稳定一个窗口", "才可接受新包裹"],
], [34*mm, 107*mm, 56*mm])
gap(4)
story.append(callout("明确的能力边界", "没有数据库意味着进程崩溃后不能无损恢复在途任务。该限制必须被现场接受并写入验收；如果未来要求自动恢复，需要新增可审计的持久化设计和协议级查询/幂等能力，不能偷偷把 .log 当数据库。", "purple"))
gap(3)
story.append(heading("发布包负面扫描", TEAL))
story.append(codebox("不得出现包/文件：\nMicrosoft.EntityFrameworkCore.Sqlite | Microsoft.Data.Sqlite | SQLitePCLRaw | e_sqlite3.dll\n*.db | *.sqlite | *.sqlite3 | *-wal | *-shm\n不得引用：JayTom.Dws.Infrastructure（若其引入数据库基础设施）"))

# 27 fault matrix
new_page("26. 故障矩阵与统一降级动作", "先取消 Ready，再保护当前任务；任何“继续跑但不记日志”的模式都不允许。")
add_table(["故障", "立即状态", "新任务", "在途任务", "恢复门禁"], [
    ["PLC 断开/心跳失效", "Degraded", "拒绝", "不下发格口；未决任务 NeedsReconcile", "新会话 + 心跳 + 清线确认"],
    ["DWS 3000 断开", "Degraded", "拒绝", "不得产生无法确认/结果闭环的新动作", "连接 + 0x60/0x62 + 稳定窗口"],
    ["DWS 3001 断开", "Degraded", "拒绝", "不发送 0x68；等待格口任务超时隔离", "连接 + 可用性探测 + 稳定窗口"],
    ["队列满", "Degraded", "拒绝", "保留已入队顺序；不得 DropOldest/DropWrite", "低于恢复水位并稳定"],
    ["日志目录不可写", "Degraded → 可配置停止服务", "拒绝", "不继续无审计业务；尽力写系统错误通道", "写测试成功 + 磁盘阈值正常"],
    ["磁盘低于阈值", "Degraded", "拒绝", "尽快安全闭环或隔离", "清理旧 .log 后达到恢复阈值"],
    ["协议错误率过高", "Degraded / 断开对应链路", "拒绝", "合法已入队事件按状态处理", "新连接 + 错误率窗口清零"],
    ["配置错误", "Starting 失败", "无", "无", "修正 appsettings.json 并重启"],
], [39*mm, 40*mm, 22*mm, 59*mm, 37*mm])

# 28 timing
new_page("27. 时序参数、超时与重试默认值", "除 DWS 10 秒心跳外，下列均为推荐起始值，不是官方事实；必须用现场节拍和压力测试冻结。")
add_table(["配置键", "推荐起始值", "作用", "超时动作"], [
    ["Timeouts.Connect", "5s", "单次 TCP 连接尝试", "按退避计划重试；不阻塞服务停止"],
    ["Dws.Heartbeat.Interval", "10s", "发送 0x60；官方建议", "等待 ResponseTimeout"],
    ["Dws.Heartbeat.ResponseTimeout", "3s", "等待匹配 0x62", "记一次 Miss"],
    ["Dws.Heartbeat.AllowedMisses", "3", "连续心跳失败阈值；官方说明", "关闭 3000，整体 Degraded"],
    ["Timeouts.WaitForDwsChute", "5s", "0x68 后等待 0x67", "TimedOut；不重发 0x68"],
    ["Timeouts.WaitForPlcAcceptance", "2s", "F9 11 后等待 0x16", "NeedsReconcile；不发送 0x64"],
    ["Timeouts.WaitForSortingResult", "30s", "0x64 后等待 0x21/22/23", "TimedOut；不伪造 0x66"],
    ["Timeouts.Send", "2s", "单次 NetworkStream 写入", "结果未知时 NeedsReconcile"],
    ["Timeouts.Drain", "10s", "停服排空安全命令", "记录剩余任务后强制停止"],
    ["Correlation.TriggerIdReuseDelay", "120s", "关闭后触发号隔离", "到期才允许复用"],
    ["Health.RecoveryStableWindow", "30s", "Degraded → Ready 稳定观察", "窗口内再故障则重新计时"],
], [61*mm, 31*mm, 65*mm, 40*mm])
gap(3)
story.append(callout("时间来源", "超时比较必须使用单调时钟（如 Stopwatch 时间戳），避免 NTP/人工调时造成倒退或瞬间超时；日志时间使用带时区的 DateTimeOffset，统一输出 ISO 8601。", "teal"))

# 29 capacities
new_page("28. 容量、背压与资源上限", "所有容器、缓冲、队列、日志和连接错误窗口都有上限；容量达到上限是业务故障，不是可静默丢弃的性能问题。")
add_table(["资源", "推荐起始值", "高水位", "恢复水位", "上限动作"], [
    ["每线活跃包裹", "10,000", "80%", "50%", "拒绝新 0x12，Degraded"],
    ["每线事件 Channel", "10,000", "80%", "50%", "停止上游读取/断开，禁止 Drop"],
    ["每连接发送 Channel", "2,000", "80%", "50%", "向编排器回报 QueueFull"],
    ["每连接接收缓冲", "64 KiB", "48 KiB", "8 KiB", "协议异常，断开并重连"],
    ["单 DWS 帧", "≤ 1,028B 总长", "按 Length 1024 + 4", "—", "拒绝并计协议错误"],
    ["关闭 tombstone", "按 2 分钟峰值测算", "80%", "按过期清理", "编号耗尽则拒绝新任务"],
    ["协议错误窗口", "60s 内 20 次", "阈值即触发", "新连接清零", "断开对应链路并报警"],
], [47*mm, 37*mm, 32*mm, 32*mm, 49*mm])
gap(4)
story.append(heading("内存估算和压测要求", BLUE))
add_bullets([
    "为每个 PackageContext 设目标上限（建议 < 2 KiB，不重复保存大对象）；原始帧只保存必要副本和哈希，完整十六进制由日志承担。",
    "压测采用不少于现场峰值 2 倍的创建速率和最大允许响应延迟；同时注入 10% 拆包、10% 粘包、重复和乱序。",
    "24 小时试验观察私有字节、GC 堆、线程、句柄、Channel 深度与日志吞吐；要求无持续线性增长。",
])
story.append(callout("容量值不是硬编码", "所有容量和水位都在 appsettings.json；但变更队列、关联和编号隔离参数需要重启并重新评估峰值，不能运行时热改。", "purple"))

# 30 logging
new_page("29. 日志设计：全部落盘为 .log", "日志既是故障取证证据，也是运行安全门禁；不写数据库、不写 Windows EventLog、不使用独立 Nlog.config。")
add_table(["类别", "路径模式（示例）", "内容", "级别"], [
    ["主运行", "logs/bridge/bridge-YYYYMMDD-NNN.log", "启动、配置摘要、服务/链路状态、容量、健康", "Info+"],
    ["Wxkc 协议", "logs/protocol/wxkc-YYYYMMDD-NNN.log", "方向、命令、字段、原始 HEX、校验、连接代次", "Debug/Trace"],
    ["DWS 控制", "logs/protocol/dws-control-YYYYMMDD-NNN.log", "3000 的 0x60/62/64/66 等", "Debug/Trace"],
    ["DWS 分拣", "logs/protocol/dws-sorting-YYYYMMDD-NNN.log", "3001 的 0x68/67", "Debug/Trace"],
    ["包裹审计", "logs/audit/package-YYYYMMDD-NNN.log", "创建、状态迁移、映射、外部副作用、关闭", "Info · 不得丢"],
    ["错误", "logs/error/error-YYYYMMDD-NNN.log", "解析失败、冲突、超时、异常、堆栈", "Warn+"],
], [31*mm, 70*mm, 67*mm, 29*mm])
gap(3)
story.append(heading("共同字段", TEAL))
story.append(codebox("timestamp | level | serviceVersion | instanceEpoch | lineId | deviceNo | connectionName | connectionEpoch\ndirection | command | wxkcSequence | dwsTriggerId | packageCycle | stateBefore | stateAfter\nresult | reason | latencyMs | queueDepth | rawHex | payloadFingerprint | message | exception"))
gap(3)
add_bullets([
    "UTF-8；按日且按 100 MiB 滚动；归档也使用 <font name='MONO'>name-YYYYMMDD-NNN.log</font>，禁止生成 <font name='MONO'>.log.1</font>。保留天数、最大文件数、总容量和最小磁盘空间全在 appsettings.json。",
    "条码默认写掩码或哈希；若联调必须写明文，以配置开关、受控目录和较短保留期管理。原始帧是否包含明文条码必须纳入数据安全评审。",
    "关键 package-audit 写失败、目录不可写、磁盘低于阈值或日志组件异常时，立即取消 Ready 并拒绝新包裹。",
])

# 31 appsettings sections
new_page("30. appsettings.json：唯一配置入口（结构一）", "文件名只有一个点。以下结构是实现契约；JSON 必须由标准解析器验证，禁止注释与尾逗号。")
story.append(codebox("{\n  \"ConfigurationSchemaVersion\": 1,\n  \"Bridge\": {\n    \"ServiceName\": \"JayTomDwsWxkcBridge\",\n    \"InstanceName\": \"LINE-A-BRIDGE-01\",\n    \"LineId\": 1,\n    \"ShutdownDrainSeconds\": 10\n  },\n  \"Wxkc\": {\n    \"Transport\": {\n      \"Mode\": \"TcpClient|TcpServer|Serial\",\n      \"Host\": \"REPLACE_BEFORE_PRODUCTION\",\n      \"Port\": 0,\n      \"SerialPort\": \"COM0\",\n      \"BaudRate\": 9600\n    },\n    \"Heartbeat\": { \"Enabled\": true, \"IntervalMs\": 10000, \"AllowedMisses\": 3 },\n    \"Protocol\": { \"FrameHeadHex\": \"F9\", \"ValidateXor\": true, \"MaxErrorsPerMinute\": 20 }\n  },\n  \"Dws\": {\n    \"Host\": \"REPLACE_BEFORE_PRODUCTION\",\n    \"ControlConnection\": { \"Port\": 3000 },\n    \"SortingConnection\": { \"Port\": 3001 },\n    \"Identity\": { \"SenderId\": 1, \"ReceiverId\": 3, \"LineNumber\": 1, \"DeviceNumber\": 1 },\n    \"Heartbeat\": { \"IntervalMs\": 10000, \"ResponseTimeoutMs\": 3000, \"AllowedMisses\": 3 }\n  }") )
gap(3)
story.append(callout("示例值不能直接投产", "REPLACE_BEFORE_PRODUCTION、Port=0、COM0 等占位值必须被启动校验拒绝。选择 Wxkc Transport.Mode 后，只允许并要求该模式相关字段，其他模式字段不得悄悄生效。", "orange"))

# 32 appsettings continued
new_page("31. appsettings.json：唯一配置入口（结构二）", "所有超时、容量、重试、映射、功能开关与日志都继续位于同一文件中。")
story.append(codebox("  ,\"Correlation\": {\n    \"MaxActivePackagesPerLine\": 10000,\n    \"TriggerIdReuseDelaySeconds\": 120,\n    \"ClosedTombstoneSeconds\": 120\n  },\n  \"Timeouts\": {\n    \"ConnectMs\": 5000, \"SendMs\": 2000, \"WaitForDwsChuteMs\": 5000,\n    \"WaitForPlcAcceptanceMs\": 2000, \"WaitForSortingResultMs\": 30000\n  },\n  \"Queues\": { \"LineEvents\": 10000, \"WxkcWrites\": 2000, \"DwsControlWrites\": 2000, \"DwsSortingWrites\": 2000 },\n  \"Retry\": { \"InitialDelayMs\": 1000, \"MaximumDelayMs\": 30000, \"Multiplier\": 2.0, \"JitterPercent\": 20, \"ResetAfterStableSeconds\": 60 },\n  \"Mapping\": {\n    \"FailureReasons\": { \"01\": \"02\", \"02\": \"05\" },\n    \"DwsExitMinimum\": 0, \"DwsExitMaximum\": 255,\n    \"WxkcExitMinimum\": 0, \"WxkcExitMaximum\": 65535\n  },\n  \"FeatureFlags\": {\n    \"SendDwsDeviceStatus\": false, \"EnableDwsLockState\": false,\n    \"EnableDwsModeChange\": false, \"EnableDwsAlarmStop\": false,\n    \"EnableDwsPdaUnlock\": false, \"EnableDwsVehicleExtension\": false,\n    \"EnableDwsDynamicScaleExtension\": false\n  },\n  \"Logging\": {\n    \"RootDirectory\": \"logs\", \"Encoding\": \"utf-8\",\n    \"RollSizeMiB\": 100, \"RetentionDays\": 30, \"MaxTotalSizeGiB\": 20,\n    \"MinimumFreeSpaceGiB\": 5, \"BarcodeMode\": \"Hash\"\n  },\n  \"NLog\": { \"throwConfigExceptions\": true, \"targets\": { \"...\": \"全部 File target 与 archive 规则写在这里\" }, \"rules\": [ \"...\" ] }\n}"))
gap(3)
story.append(callout("禁止旁路配置", "不得再创建 Nlog.config、INI/XML、注册表项或数据库配置；环境变量/命令行也不作为业务参数覆盖源。Windows 服务账户、服务恢复策略和防火墙属于部署动作，不冒充应用配置。", "red"))

# 33 config validation
new_page("32. 启动配置验证与变更规则", "所有问题应在连接设备前一次性暴露；配置错误不允许带病进入后台运行。")
add_table(["验证域", "必须验证", "失败结果"], [
    ["架构版本", "ConfigurationSchemaVersion 恰为当前支持值", "Fatal 日志后进程非 0 退出"],
    ["端点", "有效 IP/DNS；端口 1..65535；3000≠3001；无示例占位", "拒绝启动"],
    ["Wxkc Transport", "枚举有效；选中模式的字段完整；未选模式字段不得冲突", "拒绝启动并指出 JSON 路径"],
    ["身份", "Sender=01、Receiver=03；线体/设备号在已确认范围", "拒绝启动"],
    ["超时/重试", "所有值 >0；响应超时 < 心跳间隔；初始退避 ≤ 最大退避", "拒绝启动"],
    ["容量/水位", "容量 >0；恢复水位 < 高水位；活跃包裹 ≤ 触发号安全容量", "拒绝启动"],
    ["映射", "键值都是合法字节；只包含已确认映射；出口范围非空", "拒绝启动"],
    ["功能开关", "开启功能所需字段与映射均已配置", "拒绝启动，不自动降级为“猜测”"],
    ["日志", "目录可创建/写入；滚动/保留/容量合理；所有文件模板以 .log 结尾", "拒绝启动"],
], [35*mm, 114*mm, 48*mm])
gap(4)
story.append(heading("配置变更", PURPLE))
add_bullets([
    "端点、身份、协议、映射、超时、容量和功能开关不热更新；修改 appsettings.json 后通过受控重启生效。",
    "启动时记录脱敏的有效配置摘要和 SHA-256 配置哈希；不得把密码/敏感条码写入摘要。",
    "发布目录只有一个运行配置入口。环境专属值由部署流程在发布前生成完整 appsettings.json，而不是多层覆盖。",
])

# 34 implementation steps
new_page("33. Codex 实施顺序：阶段与门禁", "严格按阶段推进；上一个 Gate 未通过时，不进入下一阶段。")
add_table(["阶段", "实施内容", "交付物", "Gate"], [
    ["0 协议冻结", "厂家问答、现场抓包、端口/方向/长度/异常/重启规则确认", "ProtocolDecisionRecord.md、金样 HEX、签字映射表", "所有 P0 阻断项有结论或保持禁用"],
    ["1 工程骨架", "新 solution/projects、Options、日志、Worker 生命周期、健康状态", "可安装/启动/停服的空桥服务", "无数据库依赖；错误配置启动失败"],
    ["2 纯协议层", "Wxkc 与 DWS codec/framer、端口白名单、属性测试", "Protocols + 单元测试", "全部金样/负样通过，任意拆包/粘包通过"],
    ["3 核心状态机", "关联、编号、幂等、超时、单线事件循环", "Core + 状态迁移测试", "无 Socket 依赖；全状态覆盖"],
    ["4 适配器", "PLC transport、DWS 双连接、单写者、重连与心跳", "Host adapters + 模拟器", "三链路独立故障测试通过"],
    ["5 完整桥接", "映射、0x64 延迟确认、0x66 结果、日志与降级", "可端到端运行的候选版", "正常/异常/重复/乱序/重启闭环"],
    ["6 现场联调", "镜像流量/空跑、单件、连续件、故障注入、节拍调参", "联调记录、抓包、参数冻结", "双方签字，无错配"],
    ["7 上线验收", "24h 压测、发布扫描、服务安装、回滚演练", "发布包、运维手册、验收单", "P0 清单全部通过"],
], [25*mm, 67*mm, 63*mm, 42*mm])

# 35 implementation checklist
new_page("34. Codex 每阶段的具体执行清单", "本页可直接转成 issue / task；每个任务都必须带测试和可观察证据。")
add_bullets([
    "创建 solution 和 5 个项目；设置 Nullable、WarningsAsErrors、分析器；确保 Core/Protocols 依赖方向符合第 6 章。",
    "定义强类型 Options 与 ValidateOptions；实现完整启动预检；日志目标/规则全部从 appsettings.json 绑定。",
    "先写协议 DTO、结果类型和错误枚举，再写纯函数 Encoder/Decoder；每个官方正确示例和矛盾示例先写测试。",
    "实现 PipeReader/SequenceReader 或等价有界缓冲收帧；逐字节拆分、粘包、垃圾前缀、错误头尾、错误长度、错误 XOR 全覆盖。",
    "定义 PackageState 与 ServiceState 的显式迁移表；非法迁移返回拒绝原因，不产生副作用。",
    "实现 InMemoryPackageCorrelationStore、TriggerIdAllocator 和 tombstone；加入回绕、耗尽、并发与重启代次测试。",
    "实现每线单消费者事件循环和每连接单写者；所有 Channel 有界且 FullMode 不得丢业务事件。",
    "实现 DWS 3000/3001 两连接与 Wxkc 可替换 Transport；每次会话生成 ConnectionEpoch，旧回调被拒绝。",
    "把映射放在独立 TranslationEngine；0x03/0x04 异常映射、DWS 扩展和未覆盖命令保持默认关闭。",
    "实现故障注入模拟器、Windows 服务安装脚本、健康检查命令、日志容量预检和发布扫描。",
])
gap(3)
story.append(callout("代码评审关注点", "评审者必须能沿着一个 PackageCycle 从 0x12 追踪到 0x66，并证明每次外部发送都只有一个明确的允许状态、唯一关联和审计日志。", "green"))

# 36 protocol tests
new_page("35. 协议层测试矩阵", "两套编解码器独立测试；禁止只依赖端到端测试覆盖字节边界。")
add_table(["测试类", "Wxkc", "DWS"], [
    ["金样", "每个已知功能码的 Encode/Decode；XOR 结果", "官方每个正确完整帧；Length 动态计算"],
    ["拆包", "对 8 字节在 1..7 每个位置拆分", "对每个样例在每个字节边界拆分"],
    ["粘包", "2..100 帧一次读入", "3000/3001 各自多帧粘连；不同命令连续"],
    ["垃圾/重同步", "F9 前垃圾；错误候选后合法帧；连续 F9", "0A0B 前垃圾；错误长度/尾后合法帧"],
    ["字段错误", "错误帧头、功能码、XOR、范围", "错误 Length、Sender、Receiver、Command、Tail、7C、ASCII、内部长度"],
    ["会话边界", "半帧断线，新连接缓冲为空", "半帧断线、旧 ConnectionEpoch 回调拒绝"],
    ["端口白名单", "方向命令白名单", "0x68/67 出现在 3000、0x64/66 出现在 3001 均拒绝"],
    ["矛盾样例", "0x15 双语义保持阻断", "0009 的 0x64/0x6B、动态秤 0008、0x66 扩展作为负面/待决测试"],
    ["模糊/属性", "任意字节输入不崩溃、不越界、不无限循环", "任意 Length/Payload 不越界；Encode→Decode 保持字段"],
], [35*mm, 76*mm, 86*mm])

# 37 end-to-end tests
new_page("36. 状态机与端到端故障测试", "每个用例必须断言：外部发送次数、原始字节、状态迁移、日志证据与最终健康状态。")
add_table(["类别", "必测用例", "核心断言"], [
    ["正常", "0x12→0x68→0x67→F9 11→0x16→0x64→0x21→0x66", "顺序、字段、一次性副作用、Closed"],
    ["异常", "0x22/23 + 每个 B6；未知 B6；DWS 原因扩展开/关", "只发送已确认原因；未知进入隔离"],
    ["重复", "每个入站业务帧重复 1/10/100 次", "状态不重复创建；外部副作用至多一次或安全重放"],
    ["乱序", "0x67 早于 0x68 完成、0x16 早于 ExitSent、终态早于确认", "不允许跃迁，不发送成功应答"],
    ["冲突", "同触发号不同格口/条码；同序号不同异常；成功后失败", "Quarantined；第一终态保留"],
    ["超时", "等待格口、PLC确认、结果、写入分别超时", "不伪造 0x64/0x66；正确 NeedsReconcile/TimedOut"],
    ["断线", "三条链路在每个包裹状态各断开一次", "立即 Degraded；不盲目重放；会话缓冲隔离"],
    ["重启", "每个状态强杀进程后重启；旧消息迟到", "新 InstanceEpoch；空关联；无重复格口/结果"],
    ["容量", "队列满、编号耗尽、活跃包裹满、接收缓冲超限", "拒绝新任务；无静默丢失/覆盖"],
    ["日志", "目录不可写、磁盘满、滚动失败、文件占用", "取消 Ready；不在无审计状态继续接单"],
], [29*mm, 105*mm, 63*mm])

# 38 load and acceptance
new_page("37. 性能、稳定性与生产验收指标", "性能指标服从安全不变量；绝不以丢帧或跳过日志换吞吐。")
add_table(["指标", "门槛", "证明方式"], [
    ["正确关联", "0 次 A 包裹收到 B 格口", "含回绕、重复、乱序、长延迟、断线、重启的自动化用例"],
    ["吞吐", "不少于现场峰值的 2 倍", "模拟 PLC+DWS 连续运行，记录 p50/p95/p99"],
    ["格口转发延迟", "由现场节拍冻结；推荐 p99 < 100ms（桥内）", "单调时钟测量 0x67 接收至 F9 11 写完成"],
    ["稳定性", "连续 24h 无崩溃、死锁、线性内存增长、静默丢帧", "性能计数器 + 日志 + 模拟器统计三方核对"],
    ["恢复", "任一连接恢复后经过稳定窗口才 Ready", "拔线/RST/黑洞/重启测试"],
    ["审计", "每个外部副作用可由 .log 还原因果链", "按任意 10 个 PackageCycle 做人工追溯"],
    ["无数据库", "无 SQLite 包、DLL、.db/.sqlite/WAL/SHM", "包清单、发布目录扫描、运行目录扫描"],
    ["配置", "运行配置只来自 appsettings.json", "发布目录审计；启动配置哈希"],
    ["日志后缀", "当前/归档全部以 .log 结尾", "滚动后目录扫描"],
], [39*mm, 85*mm, 73*mm])

# 39 deployment
new_page("38. Windows 部署、升级与回滚运行手册", "生产变更先在模拟环境和现场空跑验证；服务安装不是应用内配置。")
add_table(["步骤", "执行内容", "验证证据"], [
    ["1 发布", "dotnet publish win-x64 self-contained；生成版本清单和 SHA-256", "发布目录、依赖清单、无 SQLite 扫描"],
    ["2 目录", "bin、appsettings.json、logs 分离；服务账户只读程序、读配置、写日志", "ACL 导出；日志写入测试"],
    ["3 配置", "由双人复核填写唯一 appsettings.json；执行离线 validation 命令", "配置哈希、验证报告"],
    ["4 网络", "验证 PLC 链路、DWS:3000、DWS:3001、时钟同步和防火墙", "端口测试、抓包、设备签字"],
    ["5 安装", "注册 JayTomDwsWxkcBridge；设置自动启动与受控恢复策略", "sc.exe/PowerShell 查询结果"],
    ["6 冷启动", "确认清线/周期、启动服务、观察 Connecting→Ready", "启动日志、三链路心跳、操作员确认"],
    ["7 单件/连续件", "先单件成功与异常，再逐步提高节拍", "PackageCycle 因果链与 DWS/PLC 对账"],
    ["8 回滚", "取消 Ready、停服、保留日志、恢复上一发布目录和配置哈希", "旧版本启动成功；不恢复在途任务"],
], [20*mm, 112*mm, 65*mm])
gap(4)
story.append(callout("Windows 服务恢复策略", "可以配置进程异常后的延迟重启，但自动拉起不等于自动恢复业务。新进程仍必须执行新 InstanceEpoch、空关联表、三链路检查和冷启动屏障。", "orange"))

# 40 operations
new_page("39. 运行监控与现场处置", "即使没有数据库，也必须能从内存指标、健康状态和 .log 快速判断是否安全接单。")
add_table(["信号", "正常", "告警", "现场动作"], [
    ["ServiceState", "Ready", "Degraded / Recovering 超时", "暂停上件；按连接、日志、队列顺序排查"],
    ["三链路", "全部 Connected，ConnectionEpoch 稳定", "频繁重连/半开/错误率高", "查交换机、网卡、电源策略与对端进程"],
    ["心跳", "0x60/0x62 延迟稳定；Wxkc 心跳正常", "连续 Miss 或字段异常", "抓包并比对线体/模式；不要跳过心跳门禁"],
    ["队列", "长期低于 50%", "达到 80% 或持续增长", "停止新任务；定位慢消费者/日志/网络"],
    ["活跃包裹", "随流量上下波动并及时关闭", "单调增长/大量超时", "停止上件，按状态分布定位断点"],
    ["磁盘/日志", "高于最小空间；滚动成功", "写异常、目录不可写、空间低", "保留最新证据，清理旧 .log，验证后恢复"],
    ["冲突/歧义", "0", ">0 即高优先级", "立即暂停相关线体，导出原始帧和 PackageCycle"],
], [36*mm, 55*mm, 55*mm, 51*mm])
gap(4)
story.append(heading("一次问题的最小证据包", TEAL))
add_bullets([
    "发生时间区间、线体/设备、服务版本、配置哈希、InstanceEpoch 和三条 ConnectionEpoch。",
    "相关 PackageCycle 的 package-audit、wxkc、dws-control、dws-sorting、error .log 片段及双方抓包。",
    "服务状态和队列/活跃包裹快照；操作员动作时间；PLC 与 DWS 当时状态。",
])

# 41 acceptance checklist
new_page("40. P0 生产验收清单", "任何一项未通过，都不得把候选版标记为可生产。")
add_table(["类别", "必须通过的门禁"], [
    ["范围", "□ 新独立程序；□ 不修改 DWS 主程序；□ 未确认功能默认无副作用"],
    ["协议", "□ 两侧金样/负样；□ 任意拆包粘包；□ F9/XOR；□ 0A0B/Length/0D0A；□ 端口白名单"],
    ["映射", "□ 0x64 等待 0x16；□ 原因映射签字；□ 0x66 扩展策略签字；□ 设备号策略签字"],
    ["关联", "□ 复合键；□ 触发号独立分配；□ 回绕/耗尽；□ 重复/冲突/迟到；□ A 不会收到 B 格口"],
    ["状态机", "□ 所有允许/拒绝迁移；□ 三类业务超时；□ 第一终态；□ NeedsReconcile 无自动副作用"],
    ["连接", "□ PLC/3000/3001 独立断线；□ 心跳；□ 半开；□ 抖动退避；□ 恢复稳定窗口"],
    ["重启", "□ 新 InstanceEpoch；□ 空关联；□ 不从日志恢复；□ 不盲目重放；□ 迟到消息隔离"],
    ["资源", "□ 所有队列有界；□ 满载 Degraded；□ 磁盘满/日志不可写 fail-closed；□ 24h 无泄漏"],
    ["无数据库", "□ 无 SQLite/EF 包；□ 无 sqlite DLL；□ 无 .db/.sqlite/WAL/SHM；□ 不引用数据库基础设施"],
    ["配置", "□ 仅 appsettings.json；□ 启动强校验；□ 无占位 IP；□ 配置哈希；□ 关键项不热更新"],
    ["日志", "□ 当前/归档均 .log；□ UTF-8；□ 六类日志；□ 滚动/保留/容量；□ 每个副作用可追溯"],
    ["版式/文档", "□ 全 PDF 逐页 PNG 复核；□ 无重叠/裁切/错位；□ 中文字体正常；□ 表格与图形可读"],
], [31*mm, 166*mm])

# 42 open questions
new_page("41. 实施前必须向厂家/现场确认的问题", "问题应形成书面答复或可复现抓包，并更新协议决策记录；不得只靠口头记忆。")
add_table(["编号", "问题", "责任方/证据", "未答复时默认"], [
    ["Q01", "Wxkc 物理链路与连接角色：TCP 客户端/服务端还是串口？心跳周期和 0x01/0x02 关系？", "无限创科文档 + 抓包", "Transport 可替换；不可投产"],
    ["Q02", "Wxkc 8 字节 XOR 是否确为 B1..B6？0x15 双语义如何区分？", "无限创科确认 + 金样", "严格按现状候选测试；0x15 禁用"],
    ["Q03", "0x12/0x16/0x21/22/23 各字段、序号复用周期和 0x22/0x23 差异？", "无限创科字段表", "仅核心已证实字段可用"],
    ["Q04", "0x68 动态秤 00 01/00 02 的正式位置和 Length；触发位置字段宽度？", "DWS 厂家 + 抓包", "动态秤扩展关闭"],
    ["Q05", "0x67 触发号是否固定 2B 大端？扩展段上限、编码、重复规则？", "DWS 厂家 + 金样", "2B 仅标推断；扩展关闭"],
    ["Q06", "右台设备收到 0x67 后，0x64 DeviceNo 回 02 还是固定 01？", "DWS 厂家", "右台不上线或功能阻断"],
    ["Q07", "0x66 的 FF+首车+尾车是否必填、何时出现、无车号如何表示？", "DWS 厂家", "扩展关闭"],
    ["Q08", "Wxkc 0x03/0x04 异常能否分别映射 DWS 03/04？DWS 01 来源是什么？", "双方共同签字", "0x03/0x04 映射关闭；不生成 01"],
    ["Q09", "重启、断线和结果未知时，双方是否提供查询或幂等重放能力？", "双方协议/联调", "fail-closed，不重放"],
    ["Q10", "现场峰值节拍、最长机械延迟、格口范围、清线流程和可接受停机策略？", "现场工艺负责人", "推荐超时仅实验，不投产"],
], [15*mm, 100*mm, 44*mm, 38*mm])

# 43 source map
new_page("42. DWS 官方协议命令索引", "页码来自《圆通速递矩阵标准协议文档-窄带.pdf》PDF 页码；3000 端口多依第 1 页“其余协议”总则。")
add_table(["命令", "方向", "端口", "Payload 摘要", "页码", "事实状态"], [
    ["0x60", "PLC→DWS", "3000", "线体2B、状态1B、模式1B", "2-3", "文档明确"],
    ["0x62", "DWS→PLC", "3000", "同 0x60", "3-4", "文档明确"],
    ["0x61", "PLC→DWS", "3000", "线体、速度、状态、模式、可重复摆轮", "4-5", "后续设备定义缺失"],
    ["0x68", "PLC→DWS", "3001", "设备1B、触发2B、位置/扩展", "6-7", "动态秤长度冲突"],
    ["0x67", "DWS→PLC", "3001", "设备、条码、三段码、格口1B、触发、扩展", "8-9", "触发宽度由样例推断"],
    ["0x64", "PLC→DWS", "3000", "设备、条码、7C、格口1B", "9-11", "长度和设备号冲突"],
    ["0x66", "PLC→DWS", "3000", "设备、条码、格口、结果、原因、可疑车号扩展", "11-13", "扩展未定义"],
    ["0x6B", "PLC→DWS", "3000", "锁格状态1B、格口1B", "13-14", "长度冲突"],
    ["0x71/0x70", "DWS→PLC / PLC→DWS", "3000", "模式1B", "14-15", "0x71 分解长度冲突"],
    ["0x73", "DWS→PLC", "3000", "LED、停机、错误号", "16-17", "字段明确"],
    ["0x75", "DWS→PLC", "3000", "格口2B 大端", "17-18", "字段明确但无 Wxkc 映射"],
], [24*mm, 38*mm, 25*mm, 67*mm, 18*mm, 25*mm])

# 44 core payloads
new_page("43. 核心 DWS Payload 字节布局", "B 表示单字节；N/M 为前置长度字段给出的字节数。分隔符均为 7C。")
add_table(["命令", "Payload 布局", "桥接取值规则"], [
    ["0x68", "DeviceNo(B) | TriggerId(2B BE) | [Position?] | [7C Len Ext]*", "DeviceNo 来自线体配置；TriggerId 由桥分配；可选项未冻结前不编码"],
    ["0x67", "DeviceNo(B) | BarcodeLen(B) | Barcode(N) | 7C | SectionLen(B) | Section(M) | 7C | Exit(B) | 7C | TriggerId(推断2B BE) | [Ext]*", "所有长度动态读取；Exit=00 表示无格口；TriggerId 唯一关联"],
    ["0x64", "DeviceNo(B) | BarcodeLen(B) | Barcode(N) | 7C | Exit(B)", "Barcode/Exit 从同一 0x67 上下文取；只在 0x16 后编码"],
    ["0x66", "DeviceNo(B) | BarcodeLen(B) | Barcode(N) | 7C | Exit(B) | 7C | Result(B) | 7C | Reason(B) | [FF FirstCar(2B) LastCar(2B)]", "扩展默认关闭；Result 01成功/00失败；原因见第22章"],
    ["0x60/62", "LineNo(2B BE) | SystemStatus(B) | SystemMode(B)", "状态/模式值域需按官方枚举验证；0x62 必须匹配当前会话"],
], [27*mm, 105*mm, 65*mm])
gap(4)
story.append(callout("格口宽度差异", "0x67、0x64、0x66 的格口字段是 1 字节，而 Wxkc SendExit 使用 2 字节大端。核心流程只能接收 0..255 的 DWS 格口并无损扩展为 Wxkc UInt16；不得把 >255 的配置值塞入这些 DWS 命令。0x75 的 2 字节格口是另一条未启用协议。", "red"))

# 45 codex prompt
new_page("44. 交给 Codex 的实施指令", "可把本页连同整份 PDF 作为新任务输入；若协议冻结记录缺失，Codex 应只实现默认关闭的安全骨架。")
story.append(codebox("目标：在现有仓库中新建独立解决方案 JayTom.Dws.WxkcBridge，不修改现有 DWS 主程序。\n\n必须：\n1. 先生成协议决策记录、项目骨架和测试骨架；后实现协议与业务。\n2. 使用 .NET 10 Windows Worker Service；Core/Protocols 保持平台无关。\n3. 不使用 SQLite、EF Core 或任何数据库；关联仅内存，重启 fail-closed。\n4. 所有配置只在 appsettings.json；不得创建 Nlog.config/INI/XML。\n5. 所有运行、协议、审计、错误与归档日志均为 .log。\n6. DWS 为服务端；Bridge 以 PLC 身份分别连接 3000 和 3001。\n7. 0x68/0x67 只在 3001；0x64 必须等待匹配 Wxkc 0x16；0x66 在 3000。\n8. 所有队列有界；每线单事件循环；每连接单写者；歧义关联绝不下发格口。\n9. 协议矛盾项和未映射命令默认关闭，禁止凭经验补全。\n10. 先提交测试、模拟器和验证报告，再申请现场联调。\n\n每个阶段输出：变更文件、设计决定、已运行测试、残余风险、下一 Gate。"))
gap(5)
story.append(heading("完成定义", GREEN))
add_bullets([
    "候选程序通过第 35-37 章测试和第 40 章全部 P0 门禁。",
    "现场双方对第 41 章问题给出书面结论；映射和功能开关与答复一致。",
    "发布包、唯一 appsettings.json、六类 .log 规则、模拟器、测试报告、运维/回滚手册齐全。",
    "最终现场签字证明：连续运行中没有错格、串包、重复闭环或无审计继续运行。",
])

# 构建
doc = BridgeDoc(str(OUTPUT))
doc.build(story)
print(str(OUTPUT))
