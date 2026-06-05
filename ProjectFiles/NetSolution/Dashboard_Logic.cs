#region Using directives
using System;
using System.IO;
using System.Text;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.OPCUAServer;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
#endregion

public class Dashboard_Logic : BaseNetLogic
{
    private PeriodicTask tareaActualizacion;
    private IUAObject parentLine;

    public override void Start()
    {
        try
        {
            parentLine = (IUAObject)LogicObject.Owner;

            var instanceName = parentLine.Owner.Owner.GetAlias("Estacion").BrowseName;

            var rutaHtml = ResourceUri.FromProjectRelativePath($"External_Res/index_{instanceName}.html");
            var rutaData = ResourceUri.FromProjectRelativePath($"External_Res/data_{instanceName}.json");

            string folder = Path.GetDirectoryName(rutaHtml.Uri);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            GenerarHtml(rutaHtml.Uri, $"data_{instanceName}.json");
            ActualizarDatos(rutaData.Uri);

            var browser = (WebBrowser)Owner;
            browser.URL = rutaHtml;
            browser.Refresh();

            tareaActualizacion = new PeriodicTask(Loop, 600, LogicObject);
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
        var instanceName = parentLine.Owner.Owner.GetAlias("Estacion").BrowseName;
        var rutaData = ResourceUri.FromProjectRelativePath($"External_Res/data_{instanceName}.json");
        ActualizarDatos(rutaData.Uri);
    }

    private void ActualizarDatos(string rutaJson)
    {
        float Ff(string n) => LogicObject.GetVariable(n).Value;
        int Fi(string n) => LogicObject.GetVariable(n).Value;

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"temperatura\":{Ff("Temperatura").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"corriente\":{Ff("Corriente").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"voltaje\":{Ff("Voltaje").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"frecuencia\":{Ff("Frecuencia").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"pot_activa\":{Ff("Potencia").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"pot_reactiva\":{Ff("PotenciaReactiva").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"pot_aparente\":{Ff("PotenciaAparente").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"consumo\":{Ff("ConsumoElectrico").ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
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
        h.AppendLine("<title>Bomba P-101</title>");
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

        h.AppendLine(".hdr{display:flex;align-items:center;justify-content:space-between;padding:10px 18px;background:linear-gradient(180deg,#0d2244 0%,#0a1628 100%);border-bottom:1px solid var(--border);}");
        h.AppendLine(".hdr-left h1{font-size:clamp(13px,1.8vw,18px);font-weight:700;color:#a8d4ff;letter-spacing:.5px;}");
        h.AppendLine(".hdr-left p{font-size:10px;color:var(--textd);margin-top:2px;}");
        h.AppendLine(".hdr-center{font-size:clamp(16px,2.5vw,22px);font-weight:300;color:var(--text);letter-spacing:4px;}");
        h.AppendLine(".status-pill{padding:6px 18px;border-radius:6px;font-size:clamp(11px,1.5vw,14px);font-weight:700;letter-spacing:1px;cursor:default;display:flex;align-items:center;gap:8px;transition:all .3s;border:1.5px solid;}");
        h.AppendLine(".pill-off{background:rgba(15,35,70,.9);border-color:rgba(60,100,180,.4);color:var(--textd);}");
        h.AppendLine(".pill-run{background:rgba(10,60,30,.9);border-color:rgba(38,217,107,.5);color:var(--green);}");
        h.AppendLine(".pill-fault{background:rgba(60,10,10,.9);border-color:rgba(232,64,64,.6);color:var(--red);animation:pfault .8s step-end infinite;}");
        h.AppendLine("@keyframes pfault{0%,100%{opacity:1}50%{opacity:.5}}");
        h.AppendLine(".pill-dot{width:8px;height:8px;border-radius:50%;background:currentColor;}");

        h.AppendLine(".main-row{display:grid;grid-template-columns:clamp(200px,28%,340px) 1fr;gap:0;min-height:0;}");

        h.AppendLine(".pump-panel{background:var(--bg2);border-right:1px solid var(--border);display:flex;align-items:center;justify-content:center;padding:clamp(10px,3%,24px);}");
        h.AppendLine(".pump-panel svg{width:100%;max-width:300px;height:auto;}");

        h.AppendLine(".kpi-area{display:grid;grid-template-rows:1fr 1fr;background:var(--bg3);}");
        h.AppendLine(".kpi-row{display:grid;grid-template-columns:repeat(4,1fr);border-bottom:1px solid var(--border);}");
        // CAMBIO: align-items:center + text-align:center
        h.AppendLine(".kpi-card{border-right:1px solid var(--border);display:flex;flex-direction:column;align-items:center;justify-content:center;padding:clamp(10px,2%,22px) clamp(12px,2.5%,28px);gap:6px;text-align:center;}");
        h.AppendLine(".kpi-card:last-child{border-right:none;}");
        h.AppendLine(".kpi-lbl{font-size:clamp(9px,1vw,11px);color:#ffffff;letter-spacing:2px;text-transform:uppercase;font-weight:600;}");
        // CAMBIO: font-size mayor + font-weight:700
        h.AppendLine(".kpi-val{font-size:clamp(28px,4.2vw,48px);font-weight:700;color:#ffffff;line-height:1;letter-spacing:-1px;}");
        h.AppendLine(".kpi-val span{font-size:clamp(11px,1.3vw,15px);color:#ffffff;margin-left:3px;font-weight:400;}");

        h.AppendLine(".charts-row{display:grid;grid-template-columns:1fr 1fr;border-top:1px solid var(--border);background:var(--bg2);height:clamp(180px,32vh,280px);}");
        h.AppendLine(".chart-card{border-right:1px solid var(--border);padding:clamp(10px,1.8%,18px) clamp(12px,2%,20px);display:flex;flex-direction:column;gap:6px;overflow:hidden;}");
        h.AppendLine(".chart-card:last-child{border-right:none;}");
        h.AppendLine(".chart-title{font-size:10px;color:#ffffff;letter-spacing:2px;text-transform:uppercase;flex-shrink:0;}");
        h.AppendLine(".chart-canvas{flex:1;min-height:0;width:100%;}");

        h.AppendLine("</style></head><body>");

        h.AppendLine("<div class='shell'>");

        h.AppendLine("  <div class='hdr'>");
        h.AppendLine("    <div class='hdr-left'>");
        h.AppendLine("      <h1>Bomba Centrífuga</h1>");
        h.AppendLine("      <p>Sistema de Agua Industrial | Monitoreo en Tiempo Real</p>");
        h.AppendLine("    </div>");
        h.AppendLine("    <div class='status-pill pill-off' id='status-pill'>");
        h.AppendLine("      <div class='pill-dot'></div>");
        h.AppendLine("      <span id='status-txt'>DETENIDA</span>");
        h.AppendLine("    </div>");
        h.AppendLine("  </div>");

        h.AppendLine("  <div class='main-row'>");

        h.AppendLine("    <div class='pump-panel'>");
        h.AppendLine(PumpSvg());
        h.AppendLine("    </div>");

        h.AppendLine("    <div class='kpi-area'>");

        h.AppendLine("      <div class='kpi-row'>");
        KpiCard(h, "kpi-temp", "Temperatura", "25.0", "°C");
        KpiCard(h, "kpi-curr", "Corriente", "0.0", "A");
        KpiCard(h, "kpi-volt", "Voltaje", "0.0", "V");
        KpiCard(h, "kpi-freq", "Frecuencia", "0.0", "Hz");
        h.AppendLine("      </div>");

        h.AppendLine("      <div class='kpi-row'>");
        KpiCard(h, "kpi-kw", "Pot. Activa", "0.0", "kW");
        KpiCard(h, "kpi-kvar", "Pot. Reactiva", "0.0", "kVAR");
        KpiCard(h, "kpi-kva", "Pot. Aparente", "0.0", "kVA");
        KpiCard(h, "kpi-kwh", "Consumo", "0.0", "kWh");
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

        var instanceName = dataJsNombre.Replace(".json", "");
        instanceName = instanceName.Replace("data_", "");

        h.AppendLine($"<script src='./app_{instanceName}.js'></script>");
        h.AppendLine("</body></html>");
        File.WriteAllText(rutaHtml, h.ToString());

        string rutaAppJs = Path.Combine(Path.GetDirectoryName(rutaHtml), $"app_{instanceName}.js");
        GenerarJs(rutaAppJs, dataJsNombre);
    }

    private static void KpiCard(StringBuilder h, string id, string label, string defaultVal, string unit)
    {
        h.AppendLine($"        <div class='kpi-card'>");
        h.AppendLine($"          <div class='kpi-lbl'>{label}</div>");
        h.AppendLine($"          <div class='kpi-val' id='{id}'>{defaultVal}<span>{unit}</span></div>");
        h.AppendLine($"        </div>");
    }

    private static string PumpSvg()
    {
        var s = new StringBuilder();
        s.Append("<svg id='pump-svg' viewBox='0 0 320 240' xmlns='http://www.w3.org/2000/svg'>");

        s.Append("<defs>");
        s.Append("<radialGradient id='gBomba' cx='40%' cy='40%'>");
        s.Append("<stop offset='0%' stop-color='#3a4560'/>");
        s.Append("<stop offset='100%' stop-color='#1a2030'/>");
        s.Append("</radialGradient>");
        s.Append("<linearGradient id='gMotor' x1='0' y1='0' x2='0' y2='1'>");
        s.Append("<stop offset='0%' stop-color='#3a5a9a'/>");
        s.Append("<stop offset='100%' stop-color='#1a3060'/>");
        s.Append("</linearGradient>");
        s.Append("<radialGradient id='gImpulsor' cx='50%' cy='50%'>");
        s.Append("<stop offset='0%' stop-color='#f0a830'/>");
        s.Append("<stop offset='60%' stop-color='#c07010'/>");
        s.Append("<stop offset='100%' stop-color='#804800'/>");
        s.Append("</radialGradient>");
        s.Append("</defs>");

        s.Append("<rect x='40' y='190' width='240' height='12' rx='4' fill='#1a2535' stroke='#2a3a55' stroke-width='1.5'/>");
        s.Append("<rect x='55' y='185' width='30' height='10' rx='2' fill='#1e2d45'/>");
        s.Append("<rect x='230' y='185' width='50' height='10' rx='2' fill='#1e2d45'/>");

        s.Append("<circle cx='105' cy='135' r='72' fill='url(#gBomba)' stroke='#2a3a55' stroke-width='2.5'/>");
        s.Append("<circle cx='105' cy='135' r='58' fill='none' stroke='#2a3a55' stroke-width='1'/>");
        s.Append("<circle cx='105' cy='135' r='40' fill='none' stroke='#252f45' stroke-width='1.5'/>");
        for (int i = 0; i < 6; i++)
        {
            double ang = i * 60.0 * Math.PI / 180.0;
            double bx = 105 + 66 * Math.Cos(ang);
            double by = 135 + 66 * Math.Sin(ang);
            s.Append($"<circle cx='{bx:F1}' cy='{by:F1}' r='4' fill='#1a2535' stroke='#3a4a65' stroke-width='1'/>");
            s.Append($"<line x1='{bx - 2.5:F1}' y1='{by:F1}' x2='{bx + 2.5:F1}' y2='{by:F1}' stroke='#4a5a75' stroke-width='.8'/>");
        }

        s.Append("<g id='pump-impeller' style='transform-origin:105px 135px'>");
        for (int i = 0; i < 8; i++)
        {
            double ang = i * 45.0 * Math.PI / 180.0;
            double r1 = 14, r2 = 34;
            double x1 = 105 + r1 * Math.Cos(ang);
            double y1 = 135 + r1 * Math.Sin(ang);
            double x2 = 105 + r2 * Math.Cos(ang + 0.5);
            double y2 = 135 + r2 * Math.Sin(ang + 0.5);
            double cx1 = 105 + r1 * 1.6 * Math.Cos(ang + 0.25);
            double cy1 = 135 + r1 * 1.6 * Math.Sin(ang + 0.25);
            s.Append($"<path d='M{x1:F1},{y1:F1} Q{cx1:F1},{cy1:F1} {x2:F1},{y2:F1}' fill='none' stroke='url(#gImpulsor)' stroke-width='5' stroke-linecap='round'/>");
        }
        s.Append("<circle cx='105' cy='135' r='15' fill='url(#gImpulsor)'/>");
        s.Append("<circle cx='105' cy='135' r='7' fill='#c09030' stroke='#f0d060' stroke-width='1.5'/>");
        s.Append("<circle cx='105' cy='135' r='3' fill='#f0e080'/>");
        s.Append("</g>");

        s.Append("<circle id='pump-led' cx='48' cy='75' r='8' fill='#1a2535' stroke='#2a3a55' stroke-width='1.5'/>");

        s.Append("<rect x='20' y='128' width='18' height='14' rx='2' fill='#1e2d45' stroke='#2a3a55' stroke-width='1.5'/>");
        s.Append("<rect x='10' y='125' width='12' height='20' rx='2' fill='#2a3a55' stroke='#3a4a65' stroke-width='1'/>");
        s.Append("<circle cx='13' cy='128' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");
        s.Append("<circle cx='13' cy='142' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");
        s.Append("<circle cx='20' cy='128' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");
        s.Append("<circle cx='20' cy='142' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");

        s.Append("<rect x='98' y='55' width='14' height='18' rx='2' fill='#1e2d45' stroke='#2a3a55' stroke-width='1.5'/>");
        s.Append("<rect x='95' y='42' width='20' height='14' rx='2' fill='#2a3a55' stroke='#3a4a65' stroke-width='1'/>");
        s.Append("<circle cx='97' cy='44' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");
        s.Append("<circle cx='113' cy='44' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");
        s.Append("<circle cx='97' cy='54' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");
        s.Append("<circle cx='113' cy='54' r='2' fill='#1a2535' stroke='#4a5a70' stroke-width='.8'/>");

        s.Append("<rect x='175' y='130' width='20' height='10' rx='2' fill='#2a3a55' stroke='#3a5070' stroke-width='1.5'/>");
        s.Append("<rect x='192' y='128' width='8' height='14' rx='1' fill='#1e3050' stroke='#3a5070' stroke-width='1'/>");

        s.Append("<rect x='198' y='102' width='100' height='76' rx='10' fill='url(#gMotor)' stroke='#2a4a80' stroke-width='2'/>");
        for (int i = 0; i < 7; i++)
        {
            int fy = 108 + i * 10;
            s.Append($"<rect x='200' y='{fy}' width='96' height='4' rx='1' fill='#1a3060' stroke='#2a4a80' stroke-width='.5'/>");
        }
        s.Append("<rect x='222' y='120' width='54' height='28' rx='4' fill='rgba(10,20,50,0.8)' stroke='#2a4a80' stroke-width='1'/>");
        s.Append("<text x='249' y='138' text-anchor='middle' font-size='11' font-weight='700' fill='#5090e0' font-family='Segoe UI,sans-serif' letter-spacing='1'>Motor</text>");
        s.Append("<circle cx='200' cy='114' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<circle cx='200' cy='166' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<circle cx='296' cy='114' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<circle cx='296' cy='166' r='3' fill='#1a2540' stroke='#3a5070' stroke-width='.8'/>");
        s.Append("<rect x='230' y='100' width='26' height='10' rx='2' fill='#1e3050' stroke='#2a4a80' stroke-width='1'/>");
        s.Append("<circle id='motor-led' cx='243' cy='105' r='4' fill='#c07010' stroke='#f0a030' stroke-width='1'/>");

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

        // CAMBIO: drawChart acepta fixedMin y fixedMax opcionales
        js.AppendLine("function drawChart(canvasId, data, color, fixedMin, fixedMax){");
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
        // Escala fija si se pasan parámetros, sino auto
        js.AppendLine("  var min = (fixedMin !== undefined) ? fixedMin : data[0];");
        js.AppendLine("  var max = (fixedMax !== undefined) ? fixedMax : data[0];");
        js.AppendLine("  if(fixedMin === undefined || fixedMax === undefined){");
        js.AppendLine("    for(var i=1;i<data.length;i++){ if(data[i]<min) min=data[i]; if(data[i]>max) max=data[i]; }");
        js.AppendLine("  }");
        js.AppendLine("  var range = max - min;");
        js.AppendLine("  if(range < 1) range = 1;");
        js.AppendLine("  var pad = h * 0.12;");
        js.AppendLine("  var chartH = h - pad*2;");
        js.AppendLine("  ctx.strokeStyle = 'rgba(41,98,180,0.18)';");
        js.AppendLine("  ctx.lineWidth = 1;");
        js.AppendLine("  for(var g=0;g<=4;g++){");
        js.AppendLine("    var gy = pad + (chartH/4)*g;");
        js.AppendLine("    ctx.beginPath(); ctx.moveTo(0,gy); ctx.lineTo(w,gy); ctx.stroke();");
        js.AppendLine("    var labelVal = max - (range/4)*g;");
        js.AppendLine("    ctx.fillStyle='#5a7aaa'; ctx.font='9px monospace';");
        js.AppendLine("    ctx.fillText(labelVal.toFixed(0), 3, gy-2);");
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

        js.AppendLine("  setKpi('kpi-temp',  d.temperatura.toFixed(1), '°C');");
        js.AppendLine("  setKpi('kpi-curr',  d.corriente.toFixed(1),   'A');");
        js.AppendLine("  setKpi('kpi-volt',  d.voltaje.toFixed(1),     'V');");
        js.AppendLine("  setKpi('kpi-freq',  d.frecuencia.toFixed(1),  'Hz');");
        js.AppendLine("  setKpi('kpi-kw',    d.pot_activa.toFixed(1),  'kW');");
        js.AppendLine("  setKpi('kpi-kvar',  d.pot_reactiva.toFixed(1),'kVAR');");
        js.AppendLine("  setKpi('kpi-kva',   d.pot_aparente.toFixed(1),'kVA');");
        js.AppendLine("  setKpi('kpi-kwh',   d.consumo.toFixed(1),     'kWh');");

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
        // CAMBIO: escala fija — temperatura 0-100, corriente 0-60
        js.AppendLine("  drawChart('chart-temp', histTemp, 'rgba(232,100,100,1)', 0, 100);");
        js.AppendLine("  drawChart('chart-curr', histCurr, 'rgba(41,182,246,1)',  0, 60);");

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
        js.AppendLine("  setInterval(poll, 2000);");
        js.AppendLine("});");

        File.WriteAllText(rutaAppJs, js.ToString());
    }
}
