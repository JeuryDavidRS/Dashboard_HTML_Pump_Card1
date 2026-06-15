#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.Modbus;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.WebUI;
using System;
using System.IO;
using System.Net;
using System.Text;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class Dashboard_Logic : BaseNetLogic
{
    private PeriodicTask tareaActualizacion;
    private IUAObject parentLine;
    private string instanceName = Guid.NewGuid().ToString().Replace("-", "");

    public override void Start()
    {
        try
        {
            parentLine = (IUAObject)LogicObject.Owner;

            var rutaHtml = ResourceUri.FromProjectRelativePath($"External_Res/index_{instanceName}.html");
            var rutaData = ResourceUri.FromProjectRelativePath($"External_Res/data_{instanceName}.json");

            var IPAdress = Project.Current.GetVariable("Root/Types/ObjectTypes/BaseObjectType/SessionType/UISession/IpAddress").Value;

            string folder = Path.GetDirectoryName(rutaHtml.Uri);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            GenerarHtml(rutaHtml.Uri, $"data_{instanceName}.json");
            ActualizarDatos(rutaData.Uri);

            var browser = (WebBrowser)Owner;
            browser.URL = rutaHtml;
            browser.Refresh();

            tareaActualizacion = new PeriodicTask(Loop, 200, LogicObject);
            tareaActualizacion.Start();
        }
        catch (Exception ex)
        {
            Log.Error("BombaDashboard", $"Error al iniciar: {ex.Message}");
        }
    }

    public override void Stop()
    {
        tareaActualizacion?.Dispose();
        tareaActualizacion = null;
    }

    private void Loop()
    {
        //var instanceName = parentLine.Owner.Owner.GetAlias("Estacion").BrowseName;
        var rutaData = ResourceUri.FromProjectRelativePath($"External_Res/data_{instanceName}.json");
        ActualizarDatos(rutaData.Uri);
    }

    private void ActualizarDatos(string rutaJson)
    {
        float Ff(string n) => LogicObject.GetVariable(n).Value;
        int Fi(string n) => LogicObject.GetVariable(n).Value;

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"temperatura\":{Ff("Temperatura").ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"corriente\":{Ff("Corriente").ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"voltaje\":{Ff("Voltaje").ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"frecuencia\":{Ff("Frecuencia").ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"pot_activa\":{Ff("Potencia").ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"consumo\":{Ff("ConsumoElectrico").ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"estado\":{Fi("Estado")}");
        sb.AppendLine("}");

        File.WriteAllText(rutaJson, sb.ToString());
    }

    private static void GenerarHtml(string rutaHtml, string dataJsNombre)
    {
        var h = new StringBuilder();

        h.AppendLine("<!DOCTYPE html>");
        h.AppendLine("<html lang='es'><head>");
        h.AppendLine("<meta charset='UTF-8'>");
        h.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        h.AppendLine("<title>Motor - Ventilador M-101</title>");
        h.AppendLine("<style>");

        h.AppendLine(":root{");
        h.AppendLine("  --bg:#07132a;");
        h.AppendLine("  --bg2:#0d1f3c;");
        h.AppendLine("  --bg3:#0a1628;");
        h.AppendLine("  --card:rgba(10,30,65,0.85);");
        h.AppendLine("  --border:rgba(41,98,180,0.25);");
        h.AppendLine("  --blue:#2979d0;");
        h.AppendLine("  --blue-l:#4da3ff;");
        h.AppendLine("  --cyan:#29b6f6;");
        h.AppendLine("  --green:#26d96b;");
        h.AppendLine("  --red:#e84040;");
        h.AppendLine("  --orange:#f5a623;");
        h.AppendLine("  --text:#c8d8f0;");
        h.AppendLine("  --textd:#5a7aaa;");
        h.AppendLine("  --accent:#3a8fdd;");
        h.AppendLine("}");

        h.AppendLine("*{box-sizing:border-box;margin:0;padding:0;}");
        h.AppendLine("html,body{width:100%;height:100%;background:var(--bg);overflow:hidden;font-family:'Segoe UI',sans-serif;color:var(--text);}");

        h.AppendLine(".shell{width:100%;height:100%;display:grid;grid-template-rows:auto 1fr auto;gap:0;background:var(--bg);}");

        h.AppendLine(".hdr{display:flex;align-items:center;justify-content:space-between;padding:10px 18px;background:linear-gradient(180deg,#0d2244 0%,#0a1628 100%);border-bottom:1px solid var(--border);box-shadow:0 1px 12px rgba(41,182,246,0.15);}");
        h.AppendLine(".hdr-left h1{font-size:clamp(13px,1.8vw,18px);font-weight:700;color:#a8d4ff;letter-spacing:.5px;text-shadow:0 0 10px rgba(77,163,255,0.5);}");
        h.AppendLine(".hdr-left p{font-size:10px;color:var(--textd);margin-top:2px;}");
        h.AppendLine(".hdr-center{font-size:clamp(16px,2.5vw,22px);font-weight:300;color:var(--text);letter-spacing:4px;}");
        h.AppendLine(".status-pill{padding:6px 18px;border-radius:6px;font-size:clamp(11px,1.5vw,14px);font-weight:700;letter-spacing:1px;cursor:default;display:flex;align-items:center;gap:8px;transition:all .3s;border:1.5px solid;}");
        h.AppendLine(".pill-off{background:rgba(15,35,70,.9);border-color:rgba(60,100,180,.4);color:var(--textd);}");
        h.AppendLine(".pill-run{background:rgba(10,60,30,.9);border-color:rgba(38,217,107,.5);color:var(--green);box-shadow:0 0 14px rgba(38,217,107,0.45);}");
        h.AppendLine(".pill-fault{background:rgba(60,10,10,.9);border-color:rgba(232,64,64,.6);color:var(--red);animation:pfault .8s step-end infinite;box-shadow:0 0 14px rgba(232,64,64,0.5);}");
        h.AppendLine("@keyframes pfault{0%,100%{opacity:1}50%{opacity:.5}}");
        h.AppendLine(".pill-dot{width:8px;height:8px;border-radius:50%;background:currentColor;}");

        h.AppendLine(".main-row{display:grid;grid-template-columns:clamp(320px,44%,560px) 1fr;gap:0;min-height:0;}");

        h.AppendLine(".pump-panel{background:var(--bg2);border-right:1px solid var(--border);display:flex;align-items:center;justify-content:center;padding:clamp(10px,3%,28px);position:relative;overflow:hidden;}");
        h.AppendLine(".pump-panel::before{content:'';position:absolute;width:70%;height:70%;background:radial-gradient(circle,rgba(41,182,246,0.18) 0%,transparent 70%);pointer-events:none;}");
        h.AppendLine(".pump-panel svg{width:100%;max-width:520px;height:auto;filter:drop-shadow(0 0 18px rgba(41,182,246,0.25));position:relative;z-index:1;}");

        h.AppendLine(".kpi-area{display:grid;grid-template-rows:1fr 1fr;background:var(--bg3);}");
        h.AppendLine(".kpi-row{display:grid;grid-template-columns:repeat(3,1fr);border-bottom:1px solid var(--border);}");
        h.AppendLine(".kpi-card{border-right:1px solid var(--border);display:flex;flex-direction:column;align-items:center;justify-content:center;padding:clamp(10px,2%,22px) clamp(12px,2.5%,28px);gap:6px;text-align:center;transition:background .3s;}");
        h.AppendLine(".kpi-card:hover{background:rgba(41,182,246,0.05);}");
        h.AppendLine(".kpi-card:last-child{border-right:none;}");
        h.AppendLine(".kpi-lbl{font-size:clamp(18px,1vw,20px);color:var(--cyan);letter-spacing:3px;text-transform:uppercase;font-weight:600;}");
        h.AppendLine(".kpi-val{font-size:clamp(28px,4.2vw,48px);font-weight:700;color:#ffffff;line-height:1;letter-spacing:-1px;text-shadow:0 0 12px rgba(77,163,255,0.55);}");
        h.AppendLine(".kpi-val span{font-size:clamp(11px,1.3vw,15px);color:var(--cyan);margin-left:3px;font-weight:400;text-shadow:none;}");

        h.AppendLine(".charts-row{display:grid;grid-template-columns:1fr 1fr;border-top:1px solid var(--border);background:var(--bg2);height:clamp(180px,32vh,280px);}");
        h.AppendLine(".chart-card{border-right:1px solid var(--border);padding:clamp(10px,1.8%,18px) clamp(12px,2%,20px);display:flex;flex-direction:column;gap:6px;overflow:hidden;}");
        h.AppendLine(".chart-card:last-child{border-right:none;}");
        h.AppendLine(".chart-title{font-size:10px;color:var(--cyan);letter-spacing:2px;text-transform:uppercase;flex-shrink:0;text-shadow:0 0 8px rgba(41,182,246,0.5);}");
        h.AppendLine(".chart-canvas{flex:1;min-height:0;width:100%;}");

        h.AppendLine("</style></head><body>");

        h.AppendLine("<div class='shell'>");

        h.AppendLine("  <div class='hdr'>");
        h.AppendLine("    <div class='hdr-left'>");
        h.AppendLine("      <h1>Motor Eléctrico + Ventilador</h1>");
        h.AppendLine("      <p>Sistema de Ventilación Industrial | Monitoreo en Tiempo Real</p>");
        h.AppendLine("    </div>");
        h.AppendLine("    <div class='status-pill pill-off' id='status-pill'>");
        h.AppendLine("      <div class='pill-dot'></div>");
        h.AppendLine("      <span id='status-txt'>DETENIDA</span>");
        h.AppendLine("    </div>");
        h.AppendLine("  </div>");

        h.AppendLine("  <div class='main-row'>");

        h.AppendLine("    <div class='pump-panel'>");
        h.AppendLine(MotorFanSvg());
        h.AppendLine("    </div>");

        h.AppendLine("    <div class='kpi-area'>");

        h.AppendLine("      <div class='kpi-row'>");
        KpiCard(h, "kpi-temp", "Temperatura", "0.00", "°C");
        KpiCard(h, "kpi-curr", "Corriente", "0.00", "A");
        KpiCard(h, "kpi-volt", "Voltaje", "0.00", "V");
        h.AppendLine("      </div>");

        h.AppendLine("      <div class='kpi-row'>");
        KpiCard(h, "kpi-freq", "Frecuencia", "0.00", "Hz");
        KpiCard(h, "kpi-kw", "Pot. Activa", "0.00", "W");
        KpiCard(h, "kpi-kwh", "Consumo", "0.00", "Wh");
        h.AppendLine("      </div>");

        h.AppendLine("    </div>");
        h.AppendLine("  </div>");

        h.AppendLine("  <div class='charts-row'>");
        h.AppendLine("    <div class='chart-card'>");
        h.AppendLine("      <div class='chart-title'>TEMPERATURA (°C)</div>");
        h.AppendLine("      <canvas class='chart-canvas' id='chart-temp'></canvas>");
        h.AppendLine("    </div>");
        h.AppendLine("    <div class='chart-card'>");
        h.AppendLine("      <div class='chart-title'>CORRIENTE (A)</div>");
        h.AppendLine("      <canvas class='chart-canvas' id='chart-curr'></canvas>");
        h.AppendLine("    </div>");
        h.AppendLine("  </div>");

        h.AppendLine("</div>");

        var instanceNames = dataJsNombre.Replace(".json", "");
        instanceNames = instanceNames.Replace("data_", "");

        h.AppendLine($"<script src='./app_{instanceNames}.js'></script>");
        h.AppendLine("</body></html>");
        File.WriteAllText(rutaHtml, h.ToString());

        string rutaAppJs = Path.Combine(Path.GetDirectoryName(rutaHtml), $"app_{instanceNames}.js");
        GenerarJs(rutaAppJs, dataJsNombre);
    }

    private static void KpiCard(StringBuilder h, string id, string label, string defaultVal, string unit)
    {
        h.AppendLine($"        <div class='kpi-card'>");
        h.AppendLine($"          <div class='kpi-lbl'>{label}</div>");
        h.AppendLine($"          <div class='kpi-val' id='{id}'>{defaultVal}<span>{unit}</span></div>");
        h.AppendLine($"        </div>");
    }

    private static string MotorFanSvg()
    {
        var s = new StringBuilder();
        s.Append("<svg id='pump-svg' viewBox='0 0 320 240' xmlns='http://www.w3.org/2000/svg'>");

        s.Append("<defs>");
        s.Append("<radialGradient id='gFanHub' cx='40%' cy='40%'>");
        s.Append("<stop offset='0%' stop-color='#3a4560'/>");
        s.Append("<stop offset='100%' stop-color='#1a2030'/>");
        s.Append("</radialGradient>");
        s.Append("<linearGradient id='gMotor' x1='0' y1='0' x2='0' y2='1'>");
        s.Append("<stop offset='0%' stop-color='#3a5a9a'/>");
        s.Append("<stop offset='100%' stop-color='#1a3060'/>");
        s.Append("</linearGradient>");
        s.Append("<radialGradient id='gBlade' cx='50%' cy='50%'>");
        s.Append("<stop offset='0%' stop-color='#eef4ff'/>");
        s.Append("<stop offset='55%' stop-color='#9ab0d0'/>");
        s.Append("<stop offset='100%' stop-color='#5a6a82'/>");
        s.Append("</radialGradient>");
        s.Append("</defs>");

        // base
        s.Append("<rect x='30' y='190' width='260' height='12' rx='4' fill='#1a2535' stroke='#2a3a55' stroke-width='1.5'/>");
        s.Append("<rect x='45' y='185' width='30' height='10' rx='2' fill='#1e2d45'/>");
        s.Append("<rect x='225' y='185' width='50' height='10' rx='2' fill='#1e2d45'/>");

        // Ventilador (carcasa circular)
        s.Append("<circle cx='105' cy='130' r='80' fill='url(#gFanHub)' stroke='var(--cyan)' stroke-width='2.5' opacity='0.95'/>");
        s.Append("<circle cx='105' cy='130' r='80' fill='none' stroke='var(--cyan)' stroke-width='3' opacity='0.18'/>");
        s.Append("<circle cx='105' cy='130' r='66' fill='none' stroke='#2a3a55' stroke-width='1'/>");
        s.Append("<circle cx='105' cy='130' r='66' fill='none' stroke='#252f45' stroke-width='10' stroke-dasharray='4 6' opacity='.5'/>");

        // pernos de la carcasa
        for (int i = 0; i < 8; i++)
        {
            double ang = i * 45.0 * Math.PI / 180.0;
            double bx = 105 + 74 * Math.Cos(ang);
            double by = 130 + 74 * Math.Sin(ang);
            s.Append($"<circle cx='{bx:F1}' cy='{by:F1}' r='4' fill='#1a2535' stroke='#3a4a65' stroke-width='1'/>");
        }

        // aspas del ventilador (giratorio, más gruesas)
        s.Append("<g id='pump-impeller' style='transform-origin:105px 130px'>");
        for (int i = 0; i < 5; i++)
        {
            double ang = i * 72.0 * Math.PI / 180.0;
            double r1 = 10, r2 = 60;
            double x1 = 105 + r1 * Math.Cos(ang);
            double y1 = 130 + r1 * Math.Sin(ang);
            double x2 = 105 + r2 * Math.Cos(ang + 0.55);
            double y2 = 130 + r2 * Math.Sin(ang + 0.55);
            double cx1 = 105 + r1 * 2.2 * Math.Cos(ang + 0.28);
            double cy1 = 130 + r1 * 2.2 * Math.Sin(ang + 0.28);
            s.Append($"<path d='M{x1:F1},{y1:F1} Q{cx1:F1},{cy1:F1} {x2:F1},{y2:F1}' fill='none' stroke='url(#gBlade)' stroke-width='14' stroke-linecap='round'/>");
            s.Append($"<path d='M{x1:F1},{y1:F1} Q{cx1:F1},{cy1:F1} {x2:F1},{y2:F1}' fill='none' stroke='#3a4a65' stroke-width='14' stroke-linecap='round' opacity='0.15'/>");
        }
        s.Append("<circle cx='105' cy='130' r='18' fill='#2a3550' stroke='var(--cyan)' stroke-width='1.5'/>");
        s.Append("<circle cx='105' cy='130' r='7' fill='#cfe0ff'/>");
        s.Append("</g>");

        // led de estado ventilador
        s.Append("<circle id='pump-led' cx='48' cy='70' r='8' fill='#1a2535' stroke='#2a3a55' stroke-width='1.5'/>");

        // eje de acople ventilador-motor
        s.Append("<rect x='180' y='122' width='34' height='16' rx='3' fill='#2a3a55' stroke='#3a5070' stroke-width='1.5'/>");
        s.Append("<rect x='210' y='118' width='10' height='24' rx='2' fill='#1e3050' stroke='#3a5070' stroke-width='1'/>");

        // Motor electrico
        s.Append("<rect x='218' y='90' width='84' height='80' rx='10' fill='url(#gMotor)' stroke='#2a4a80' stroke-width='2'/>");
        for (int i = 0; i < 7; i++)
        {
            int fy = 96 + i * 10;
            s.Append($"<rect x='220' y='{fy}' width='80' height='4' rx='1' fill='#1a3060' stroke='#2a4a80' stroke-width='.5'/>");
        }
        s.Append("<rect x='238' y='112' width='48' height='28' rx='4' fill='rgba(10,20,50,0.8)' stroke='#2a4a80' stroke-width='1'/>");
        s.Append("<text x='262' y='130' text-anchor='middle' font-size='11' font-weight='700' fill='#5090e0' font-family='Segoe UI,sans-serif' letter-spacing='1'>Motor</text>");
        s.Append("<circle cx='220' cy='98' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<circle cx='220' cy='162' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<circle cx='300' cy='98' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<circle cx='300' cy='162' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");

        // caja de bornes / led motor
        s.Append("<rect x='248' y='78' width='28' height='12' rx='2' fill='#1e3050' stroke='#2a4a80' stroke-width='1'/>");
        s.Append("<circle id='motor-led' cx='262' cy='84' r='4' fill='#c07010' stroke='#f0a030' stroke-width='1'/>");

        // patas del motor
        s.Append("<rect x='228' y='170' width='16' height='14' rx='2' fill='#1e2d45' stroke='#2a3a55' stroke-width='1.5'/>");
        s.Append("<rect x='278' y='170' width='16' height='14' rx='2' fill='#1e2d45' stroke='#2a3a55' stroke-width='1.5'/>");

        s.Append("</svg>");
        return s.ToString();
    }

    private static void GenerarJs(string rutaAppJs, string dataJsNombre)
    {
        var js = new StringBuilder();

        js.AppendLine("var MAX_POINTS = 60;");
        js.AppendLine("var histTemp = [];");
        js.AppendLine("var histCurr = [];");

        js.AppendLine("function setKpi(id, val, unit){");
        js.AppendLine("  var el = document.getElementById(id);");
        js.AppendLine("  if(el) el.innerHTML = val + \"<span>\"+unit+\"</span>\";");
        js.AppendLine("}");

        // drawChart acepta fixedMin, fixedMax y cantidad de decimales para las etiquetas
        js.AppendLine("function drawChart(canvasId, data, color, fixedMin, fixedMax, decimals){");
        js.AppendLine("  if(decimals === undefined) decimals = 0;");
        js.AppendLine("  var canvas = document.getElementById(canvasId);");
        js.AppendLine("  if(!canvas) return;");
        js.AppendLine("  var w = canvas.clientWidth;");
        js.AppendLine("  var h = canvas.clientHeight;");
        js.AppendLine("  if(w===0||h===0) return;");
        js.AppendLine("  canvas.width  = w;");
        js.AppendLine("  canvas.height = h;");
        js.AppendLine("  var ctx = canvas.getContext('2d');");
        js.AppendLine("  ctx.clearRect(0,0,w,h);");
        js.AppendLine("  if(data.length < 2) return;");
        js.AppendLine("  var min = (fixedMin !== undefined) ? fixedMin : data[0];");
        js.AppendLine("  var max = (fixedMax !== undefined) ? fixedMax : data[0];");
        js.AppendLine("  if(fixedMin === undefined || fixedMax === undefined){");
        js.AppendLine("    for(var i=1;i<data.length;i++){ if(data[i]<min) min=data[i]; if(data[i]>max) max=data[i]; }");
        js.AppendLine("  }");
        js.AppendLine("  var range = max - min;");
        js.AppendLine("  if(range <= 0) range = 1;");
        js.AppendLine("  var pad = h * 0.12;");
        js.AppendLine("  var chartH = h - pad*2;");
        js.AppendLine("  ctx.strokeStyle = 'rgba(41,98,180,0.18)';");
        js.AppendLine("  ctx.lineWidth = 1;");
        js.AppendLine("  for(var g=0;g<=4;g++){");
        js.AppendLine("    var gy = pad + (chartH/4)*g;");
        js.AppendLine("    ctx.beginPath(); ctx.moveTo(0,gy); ctx.lineTo(w,gy); ctx.stroke();");
        js.AppendLine("    var labelVal = max - (range/4)*g;");
        js.AppendLine("    ctx.fillStyle='#5a7aaa'; ctx.font='9px monospace';");
        js.AppendLine("    ctx.fillText(labelVal.toFixed(decimals), 3, gy-2);");
        js.AppendLine("  }");

        js.AppendLine("  var n = data.length;");
        js.AppendLine("  function xp(i){ return (i/(MAX_POINTS-1))*w; }");
        js.AppendLine("  function yp(v){ return pad + chartH - ((v-min)/range)*chartH; }");
        js.AppendLine("  var startIdx = MAX_POINTS - n;");
        js.AppendLine("  ctx.beginPath();");
        js.AppendLine("  ctx.moveTo(xp(startIdx), h);");
        js.AppendLine("  for(var j=0;j<n;j++){ ctx.lineTo(xp(startIdx+j), yp(data[j])); }");
        js.AppendLine("  ctx.lineTo(xp(startIdx+n-1), h);");
        js.AppendLine("  ctx.closePath();");
        js.AppendLine("  var grad = ctx.createLinearGradient(0,pad,0,h);");
        js.AppendLine("  grad.addColorStop(0, color.replace('1)','0.25)'));");
        js.AppendLine("  grad.addColorStop(1, color.replace('1)','0.02)'));");
        js.AppendLine("  ctx.fillStyle = grad;");
        js.AppendLine("  ctx.fill();");

        js.AppendLine("  ctx.beginPath();");
        js.AppendLine("  ctx.moveTo(xp(startIdx), yp(data[0]));");
        js.AppendLine("  for(var k=1;k<n;k++){ ctx.lineTo(xp(startIdx+k), yp(data[k])); }");
        js.AppendLine("  ctx.strokeStyle = color;");
        js.AppendLine("  ctx.lineWidth = 1.8;");
        js.AppendLine("  ctx.lineJoin = 'round';");
        js.AppendLine("  ctx.stroke();");
        js.AppendLine("}");

        js.AppendLine("function render(d){");
        js.AppendLine("  var estado = d.estado || 0;");

        js.AppendLine("  setKpi('kpi-temp',  d.temperatura.toFixed(2), '°C');");
        js.AppendLine("  setKpi('kpi-curr',  d.corriente.toFixed(2),   'A');");
        js.AppendLine("  setKpi('kpi-volt',  d.voltaje.toFixed(2),     'V');");
        js.AppendLine("  setKpi('kpi-freq',  d.frecuencia.toFixed(2),  'Hz');");
        js.AppendLine("  setKpi('kpi-kw',    d.pot_activa.toFixed(2),  'W');");
        js.AppendLine("  setKpi('kpi-kwh',   d.consumo.toFixed(2),     'Wh');");

        js.AppendLine("  var pill = document.getElementById('status-pill');");
        js.AppendLine("  var ptxt = document.getElementById('status-txt');");
        js.AppendLine("  if(pill && ptxt){");
        js.AppendLine("    pill.className = 'status-pill';");
        js.AppendLine("    if(estado===2){ pill.classList.add('pill-fault'); ptxt.textContent='FALLA'; }");
        js.AppendLine("    else if(estado===1){ pill.classList.add('pill-run'); ptxt.textContent='EN MARCHA'; }");
        js.AppendLine("    else { pill.classList.add('pill-off'); ptxt.textContent='DETENIDA'; }");
        js.AppendLine("  }");

        js.AppendLine("  var led  = document.getElementById('pump-led');");
        js.AppendLine("  var mled = document.getElementById('motor-led');");
        js.AppendLine("  var ledFill   = estado===2?'#e84040':estado===1?'#26d96b':'#1a2535';");
        js.AppendLine("  var ledStroke = estado===2?'#ff6060':estado===1?'#50ffa0':'#2a3a55';");
        js.AppendLine("  var mFill     = estado===2?'#e84040':estado===1?'#f0a030':'#304060';");
        js.AppendLine("  if(led){  led.setAttribute('fill',ledFill);   led.setAttribute('stroke',ledStroke); }");
        js.AppendLine("  if(mled){ mled.setAttribute('fill',mFill);    mled.setAttribute('stroke',ledStroke); }");

        js.AppendLine("  var imp = document.getElementById('pump-impeller');");
        js.AppendLine("  if(imp) imp.style.animation = estado===1 ? 'spin-cw .8s linear infinite' : 'none';");

        js.AppendLine("  histTemp.push(d.temperatura);");
        js.AppendLine("  histCurr.push(d.corriente);");
        js.AppendLine("  if(histTemp.length > MAX_POINTS) histTemp.shift();");
        js.AppendLine("  if(histCurr.length > MAX_POINTS) histCurr.shift();");
        // temperatura: 0-60, 0 decimales | corriente: 0-1, 2 decimales
        js.AppendLine("  drawChart('chart-temp', histTemp, 'rgba(232,100,100,1)', 0, 60, 0);");
        js.AppendLine("  drawChart('chart-curr', histCurr, 'rgba(41,182,246,1)',  0, 1, 2);");

        js.AppendLine("}");

        js.AppendLine("(function(){");
        js.AppendLine("  var s = document.createElement('style');");
        js.AppendLine("  s.textContent = '@keyframes spin-cw{to{transform:rotate(360deg)}}';");
        js.AppendLine("  document.head.appendChild(s);");
        js.AppendLine("})();");

        js.AppendLine("function poll(){");
        js.AppendLine($"  fetch('./{dataJsNombre}?t='+Date.now(),{{cache:'no-store'}})");
        js.AppendLine("    .then(function(r){ return r.json(); })");
        js.AppendLine("    .then(function(data){ render(data); })");
        js.AppendLine("    .catch(function(e){ console.warn('poll error:',e); });");
        js.AppendLine("}");

        js.AppendLine("document.addEventListener('DOMContentLoaded', function(){");
        js.AppendLine("  poll();");
        js.AppendLine("  setInterval(poll, 200);");
        js.AppendLine("});");

        File.WriteAllText(rutaAppJs, js.ToString());
    }
}