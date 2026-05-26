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
#endregion

public class Dashboard_Logic : BaseNetLogic
{
    private PeriodicTask periodicTask;
    private float[] tempHistorico = new float[100];
    private float[] corrienteHistorico = new float[100];
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

            GenerarHTML(rutaHtml.Uri, $"data_{instanceName}.json");
            ActualizarDatos(rutaData.Uri);

            var browser = (WebBrowser)Owner;
            browser.URL = rutaHtml;
            browser.Refresh();

            periodicTask = new PeriodicTask(Loop, 2500, LogicObject);
            periodicTask.Start();
        }
        catch (Exception ex)
        {
            Log.Error("BombaDashboard", $"Error al iniciar: {ex.Message}");
        }
    }

    public override void Stop()
    {
        if (periodicTask != null)
        {
            periodicTask.Dispose();
            periodicTask = null;
        }
        Log.Info("BombaDashboard", "Dashboard detenido");
    }

    private void Loop()
    {
        var instanceName = parentLine.Owner.Owner.GetAlias("Estacion").BrowseName;
        var rutaData = ResourceUri.FromProjectRelativePath($"External_Res/data_{instanceName}.json");
        ActualizarDatos(rutaData.Uri);
    }

    private void GenerarHTML(string rutaHtml, string dataJsNombre)
    {
        var h = new StringBuilder();

        h.AppendLine("<!DOCTYPE html>");
        h.AppendLine("<html lang='es'>");
        h.AppendLine("<head>");
        h.AppendLine("  <meta charset='UTF-8'>");
        h.AppendLine("  <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        h.AppendLine("  <title>Bomba Centrífuga | HMI Card</title>");

        h.AppendLine("  <style>");
        h.AppendLine("    *{margin:0;padding:0;box-sizing:border-box;}");
        h.AppendLine("    :root{");
        h.AppendLine("      --panel:#0a0e27;--panel2:#1a1f3a;--panel3:#2c3340;");
        h.AppendLine("      --border:rgba(100,181,246,0.2);--accent:#64b5f6;");
        h.AppendLine("      --green:#4caf50;--red:#f44336;--orange:#ff9800;--gray:#78909c;");
        h.AppendLine("      --text:#e8eaf6;--textd:rgba(255,255,255,0.6);");
        h.AppendLine("    }");
        h.AppendLine("    html,body{width:100%;height:100%;overflow:hidden;margin:0;padding:0;}");
        h.AppendLine("    body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;");
        h.AppendLine("      background:linear-gradient(135deg,var(--panel) 0%,var(--panel2) 100%);");
        h.AppendLine("      color:var(--text);}");

        h.AppendLine("    .card{width:100%;height:100%;");
        h.AppendLine("      background:linear-gradient(135deg,rgba(26,35,126,0.3),rgba(13,71,161,0.2));");
        h.AppendLine("      backdrop-filter:blur(12px);");
        h.AppendLine("      padding:0.5%;box-shadow:0 8px 32px rgba(0,0,0,0.4);");
        h.AppendLine("      display:flex;flex-direction:column;overflow:hidden;}");

        h.AppendLine("    .header{display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;");
        h.AppendLine("      margin-bottom:0.5%;padding-bottom:0.5%;border-bottom:1px solid var(--border);}");
        h.AppendLine("    .header-left h1{font-size:clamp(12px,2vmin,18px);font-weight:700;margin-bottom:0.2%;");
        h.AppendLine("      background:linear-gradient(135deg,#64b5f6 0%,#42a5f5 100%);");
        h.AppendLine("      -webkit-background-clip:text;-webkit-text-fill-color:transparent;}");
        h.AppendLine("    .header-left p{font-size:clamp(8px,1.2vmin,10px);color:var(--textd);}");

        h.AppendLine("    .status-badge{padding:1% 2%;border-radius:20px;font-size:clamp(12px,2vmin,16px);font-weight:700;");
        h.AppendLine("      display:flex;align-items:center;justify-content:center;gap:1%;text-transform:uppercase;");
        h.AppendLine("      letter-spacing:0.5px;transition:all 0.3s;white-space:nowrap;}");
        h.AppendLine("    .status-badge::before{content:'';width:10px;height:10px;border-radius:50%;flex-shrink:0;}");
        h.AppendLine("    .badge-running{background:rgba(46,125,50,0.2);border:2px solid var(--green);color:#a5d6a7;}");
        h.AppendLine("    .badge-running::before{background:var(--green);box-shadow:0 0 8px var(--green);animation:pulse 2s infinite;}");
        h.AppendLine("    .badge-stopped{background:rgba(69,90,100,0.2);border:2px solid var(--gray);color:#b0bec5;}");
        h.AppendLine("    .badge-stopped::before{background:var(--gray);}");
        h.AppendLine("    .badge-fault{background:rgba(198,40,40,0.2);border:2px solid var(--red);color:#ffcdd2;}");
        h.AppendLine("    .badge-fault::before{background:var(--red);box-shadow:0 0 8px var(--red);animation:blink 0.6s infinite;}");
        h.AppendLine("    .badge-maintenance{background:rgba(245,124,0,0.2);border:2px solid var(--orange);color:#ffe0b2;}");
        h.AppendLine("    .badge-maintenance::before{background:var(--orange);}");

        h.AppendLine("    @keyframes pulse{0%,100%{opacity:1;transform:scale(1);}50%{opacity:0.6;transform:scale(0.9);}}");
        h.AppendLine("    @keyframes blink{0%,100%{opacity:1;}50%{opacity:0.2;}}");
        h.AppendLine("    @keyframes spin{from{transform:rotate(0deg);}to{transform:rotate(360deg);}}");
        h.AppendLine("    @keyframes shake{0%,100%{transform:translate(0,0);}25%{transform:translate(-1.5px,1px);}");
        h.AppendLine("      50%{transform:translate(1.5px,-1px);}75%{transform:translate(-1px,1.5px);}}");
        h.AppendLine("    #impeller{transform-box:fill-box;transform-origin:center;}");

        h.AppendLine("    .main-grid{display:grid;grid-template-columns:1fr 2fr;gap:0.5%;margin-bottom:0.5%;flex-shrink:0;}");
        h.AppendLine("    @media(max-width:800px){.main-grid{grid-template-columns:1fr;}}");

        h.AppendLine("    .pump-visual{background:rgba(15,23,42,0.4);border:1px solid var(--border);");
        h.AppendLine("      border-radius:4px;padding:0.5%;display:flex;flex-direction:column;");
        h.AppendLine("      align-items:center;justify-content:center;}");
        h.AppendLine("    .pump-svg{width:100%;max-width:100%;height:auto;}");

        h.AppendLine("    .kpis-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:0.5%;}");
        h.AppendLine("    @media(max-width:1200px){.kpis-grid{grid-template-columns:repeat(2,1fr);}}");
        h.AppendLine("    .kpi-card{background:linear-gradient(135deg,rgba(30,41,59,0.5),rgba(15,23,42,0.4));");
        h.AppendLine("      border:1px solid var(--border);border-radius:4px;padding:1%;");
        h.AppendLine("      position:relative;overflow:hidden;transition:all 0.3s;");
        h.AppendLine("      display:flex;flex-direction:column;align-items:center;justify-content:center;}");
        h.AppendLine("    .kpi-card::before{content:'';position:absolute;top:0;left:0;right:0;height:3px;");
        h.AppendLine("      background:linear-gradient(90deg,transparent,var(--accent),transparent);");
        h.AppendLine("      opacity:0;transition:opacity 0.3s;}");
        h.AppendLine("    .kpi-card:hover::before{opacity:1;}");
        h.AppendLine("    .kpi-label{font-size:clamp(8px,1.3vmin,11px);color:var(--textd);text-transform:uppercase;");
        h.AppendLine("      letter-spacing:0.5px;margin-bottom:1%;text-align:center;}");
        h.AppendLine("    .kpi-value{font-size:clamp(20px,3.5vmin,28px);font-weight:700;font-family:monospace;");
        h.AppendLine("      color:var(--accent);line-height:1;display:flex;align-items:center;justify-content:center;}");
        h.AppendLine("    .kpi-unit{font-size:clamp(12px,1.8vmin,15px);color:var(--textd);margin-left:1.5%;}");

        h.AppendLine("    .charts-section{display:grid;grid-template-columns:repeat(2,1fr);gap:0.5%;flex-shrink:0;margin-bottom:0.5%;}");
        h.AppendLine("    @media(max-width:600px){.charts-section{grid-template-columns:1fr;}}");
        h.AppendLine("    .chart-card{background:linear-gradient(135deg,rgba(30,41,59,0.5),rgba(15,23,42,0.4));");
        h.AppendLine("      border:1px solid var(--border);border-radius:4px;padding:0.5%;}");
        h.AppendLine("    .chart-header{font-size:clamp(8px,1.2vmin,10px);color:var(--textd);text-transform:uppercase;");
        h.AppendLine("      letter-spacing:0.5px;margin-bottom:0.3%;}");
        h.AppendLine("    .chart-canvas{width:100%;height:clamp(120px,18vmin,180px);}");

        h.AppendLine("    .events-section{display:grid;grid-template-columns:1fr 1fr;gap:0.5%;margin-top:0.5%;flex-shrink:0;}");
        h.AppendLine("    .events-card{background:linear-gradient(135deg,rgba(30,41,59,0.5),rgba(15,23,42,0.4));");
        h.AppendLine("      border:1px solid var(--border);border-radius:4px;padding:0.5%;");
        h.AppendLine("      min-height:clamp(80px,15vmin,120px);}");
        h.AppendLine("    .events-title{font-size:clamp(9px,1.3vmin,11px);color:var(--textd);text-transform:uppercase;");
        h.AppendLine("      letter-spacing:0.5px;margin-bottom:0.3%;padding-bottom:0.3%;");
        h.AppendLine("      border-bottom:1px solid var(--border);}");
        h.AppendLine("    .events-log{height:clamp(70px,12vmin,100px);overflow-y:auto;font-size:clamp(8px,1.2vmin,10px);line-height:1.5;}");
        h.AppendLine("    .buttons-placeholder{background:transparent;border:none;padding:0.5%;");
        h.AppendLine("      display:flex;align-items:center;justify-content:center;}");
        h.AppendLine("    .event-row{display:grid;grid-template-columns:auto 1fr;gap:0.5%;");
        h.AppendLine("      padding:0.3% 0;border-bottom:1px solid rgba(255,255,255,0.05);}");
        h.AppendLine("    .event-row:last-child{border-bottom:none;}");
        h.AppendLine("    .event-time{color:var(--textd);font-family:monospace;font-size:clamp(7px,1vmin,9px);white-space:nowrap;}");
        h.AppendLine("    .event-message{color:var(--text);}");
        h.AppendLine("    .event-info .event-message{color:#90caf9;}");
        h.AppendLine("    .event-warning .event-message{color:#ffb74d;}");
        h.AppendLine("    .event-error .event-message{color:#ef5350;}");

        h.AppendLine("    .events-log::-webkit-scrollbar{width:4px;}");
        h.AppendLine("    .events-log::-webkit-scrollbar-track{background:rgba(255,255,255,0.05);border-radius:2px;}");
        h.AppendLine("    .events-log::-webkit-scrollbar-thumb{background:var(--accent);border-radius:2px;}");

        h.AppendLine("  </style>");
        h.AppendLine("</head>");
        h.AppendLine("<body>");

        h.AppendLine("  <div class='card'>");

        h.AppendLine("    <div class='header'>");
        h.AppendLine("      <div class='header-left'>");
        h.AppendLine("        <h1>Bomba Centrífuga P-101</h1>");
        h.AppendLine("        <p>Sistema de Agua Industrial | Monitoreo en Tiempo Real</p>");
        h.AppendLine("      </div>");
        h.AppendLine("      <div class='status-badge badge-running' id='statusBadge'>");
        h.AppendLine("        <span id='statusText'>EN OPERACIÓN</span>");
        h.AppendLine("      </div>");
        h.AppendLine("    </div>");

        h.AppendLine("    <div class='main-grid'>");

        GenerarSVGBomba(h);

        h.AppendLine("      <div class='kpis-grid'>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Temperatura</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiTemp'>68</span><span class='kpi-unit'>°C</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Corriente</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiCurrent'>78</span><span class='kpi-unit'>A</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Voltaje</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiVoltage'>460</span><span class='kpi-unit'>V</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Frecuencia</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiFreq'>60</span><span class='kpi-unit'>Hz</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Pot. Activa</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiPower'>35</span><span class='kpi-unit'>kW</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Pot. Reactiva</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiReactivePower'>12</span><span class='kpi-unit'>kVAR</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Pot. Aparente</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiApparentPower'>37</span><span class='kpi-unit'>kVA</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("        <div class='kpi-card'>");
        h.AppendLine("          <div class='kpi-label'>Consumo</div>");
        h.AppendLine("          <div class='kpi-value'><span id='kpiEnergy'>1250</span><span class='kpi-unit'>kWh</span></div>");
        h.AppendLine("        </div>");
        h.AppendLine("      </div>");
        h.AppendLine("    </div>");

        h.AppendLine("    <div class='charts-section'>");
        h.AppendLine("      <div class='chart-card'>");
        h.AppendLine("        <div class='chart-header'>Temperatura (°C)</div>");
        h.AppendLine("        <canvas id='chartTemp' class='chart-canvas'></canvas>");
        h.AppendLine("      </div>");
        h.AppendLine("      <div class='chart-card'>");
        h.AppendLine("        <div class='chart-header'>Corriente (A)</div>");
        h.AppendLine("        <canvas id='chartCurrent' class='chart-canvas'></canvas>");
        h.AppendLine("      </div>");
        h.AppendLine("    </div>");

        h.AppendLine("    <div class='events-section'>");
        h.AppendLine("      <div class='events-card'>");
        h.AppendLine("        <div class='events-title'>Registro de Eventos</div>");
        h.AppendLine("        <div class='events-log' id='eventsLog'></div>");
        h.AppendLine("      </div>");
        h.AppendLine("      <div class='buttons-placeholder'></div>");
        h.AppendLine("    </div>");

        h.AppendLine("  </div>");

        var instanceName = dataJsNombre.Replace(".json", "");
        instanceName = instanceName.Replace("data_", "");

        h.AppendLine($"<script src='./app_{instanceName}.js'></script>");
        h.AppendLine("</body></html>");
        File.WriteAllText(rutaHtml, h.ToString());

        string rutaAppJs = Path.Combine(Path.GetDirectoryName(rutaHtml), $"app_{instanceName}.js");
        GenerarJavaScript(rutaAppJs, dataJsNombre);
    }

    private void GenerarSVGBomba(StringBuilder h)
    {
        h.AppendLine("      <div class='pump-visual'>");
        h.AppendLine("        <svg class='pump-svg' viewBox='0 0 420 280' xmlns='http://www.w3.org/2000/svg' preserveAspectRatio='xMidYMid meet'>");
        h.AppendLine("          <defs>");
        h.AppendLine("            <linearGradient id='gradCasing' x1='0%' y1='0%' x2='0%' y2='100%'>");
        h.AppendLine("              <stop offset='0%' style='stop-color:#546e7a;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='50%' style='stop-color:#37474f;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='100%' style='stop-color:#263238;stop-opacity:1'/>");
        h.AppendLine("            </linearGradient>");
        h.AppendLine("            <linearGradient id='gradImpeller' x1='0%' y1='0%' x2='100%' y2='100%'>");
        h.AppendLine("              <stop offset='0%' style='stop-color:#ffd54f;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='50%' style='stop-color:#ffb300;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='100%' style='stop-color:#ff8f00;stop-opacity:1'/>");
        h.AppendLine("            </linearGradient>");
        h.AppendLine("            <linearGradient id='gradWater' x1='0%' y1='0%' x2='100%' y2='0%'>");
        h.AppendLine("              <stop offset='0%' style='stop-color:#29b6f6;stop-opacity:0.6'/>");
        h.AppendLine("              <stop offset='50%' style='stop-color:#039be5;stop-opacity:0.8'/>");
        h.AppendLine("              <stop offset='100%' style='stop-color:#0277bd;stop-opacity:0.6'/>");
        h.AppendLine("            </linearGradient>");
        h.AppendLine("            <linearGradient id='gradShaft' x1='0%' y1='0%' x2='100%' y2='0%'>");
        h.AppendLine("              <stop offset='0%' style='stop-color:#546e7a;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='50%' style='stop-color:#78909c;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='100%' style='stop-color:#546e7a;stop-opacity:1'/>");
        h.AppendLine("            </linearGradient>");
        h.AppendLine("            <linearGradient id='gradMotor' x1='0%' y1='0%' x2='0%' y2='100%'>");
        h.AppendLine("              <stop offset='0%' style='stop-color:#0d47a1;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='100%' style='stop-color:#01579b;stop-opacity:0.95'/>");
        h.AppendLine("            </linearGradient>");
        h.AppendLine("            <radialGradient id='gradMetallic'>");
        h.AppendLine("              <stop offset='0%' style='stop-color:#cfd8dc;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='70%' style='stop-color:#90a4ae;stop-opacity:1'/>");
        h.AppendLine("              <stop offset='100%' style='stop-color:#546e7a;stop-opacity:1'/>");
        h.AppendLine("            </radialGradient>");
        h.AppendLine("            <filter id='shadow'>");
        h.AppendLine("              <feDropShadow dx='0' dy='3' stdDeviation='4' flood-opacity='0.4'/>");
        h.AppendLine("            </filter>");
        h.AppendLine("            <filter id='glow'>");
        h.AppendLine("              <feGaussianBlur stdDeviation='3' result='coloredBlur'/>");
        h.AppendLine("              <feMerge><feMergeNode in='coloredBlur'/><feMergeNode in='SourceGraphic'/></feMerge>");
        h.AppendLine("            </filter>");
        h.AppendLine("          </defs>");
        h.AppendLine("          <g filter='url(#shadow)'>");
        h.AppendLine("            <rect x='60' y='195' width='140' height='25' rx='4' fill='url(#gradMetallic)' stroke='#37474f' stroke-width='2'/>");
        h.AppendLine("            <rect x='60' y='200' width='140' height='8' fill='rgba(0,0,0,0.3)'/>");
        h.AppendLine("          </g>");
        h.AppendLine("          <g id='pumpCasing' filter='url(#shadow)'>");
        h.AppendLine("            <ellipse cx='130' cy='140' rx='85' ry='75' fill='url(#gradCasing)' stroke='#78909c' stroke-width='3'/>");
        h.AppendLine("            <ellipse cx='130' cy='140' rx='75' ry='65' fill='rgba(0,0,0,0.2)'/>");
        h.AppendLine("            <circle cx='75' cy='100' r='5' fill='#263238' stroke='#546e7a' stroke-width='1.5'/>");
        h.AppendLine("            <circle cx='185' cy='100' r='5' fill='#263238' stroke='#546e7a' stroke-width='1.5'/>");
        h.AppendLine("            <circle cx='75' cy='180' r='5' fill='#263238' stroke='#546e7a' stroke-width='1.5'/>");
        h.AppendLine("            <circle cx='185' cy='180' r='5' fill='#263238' stroke='#546e7a' stroke-width='1.5'/>");
        h.AppendLine("            <rect x='35' y='125' width='50' height='30' rx='4' fill='url(#gradCasing)' stroke='#78909c' stroke-width='2.5'/>");
        h.AppendLine("            <rect x='30' y='130' width='15' height='20' rx='2' fill='#37474f' stroke='#546e7a' stroke-width='2'/>");
        h.AppendLine("            <rect x='115' y='45' width='30' height='35' rx='3' fill='url(#gradCasing)' stroke='#78909c' stroke-width='2.5'/>");
        h.AppendLine("            <ellipse cx='130' cy='40' rx='18' ry='10' fill='#37474f' stroke='#546e7a' stroke-width='2'/>");
        h.AppendLine("            <ellipse cx='130' cy='40' rx='12' ry='7' fill='rgba(0,0,0,0.3)'/>");
        h.AppendLine("          </g>");
        h.AppendLine("          <g id='waterFlow' opacity='0.85'>");
        h.AppendLine("            <path d='M 35,135 L 80,135 L 80,145 L 35,145 Z' fill='url(#gradWater)' opacity='0.7'>");
        h.AppendLine("              <animate attributeName='opacity' values='0.7;0.9;0.7' dur='1.5s' repeatCount='indefinite'/>");
        h.AppendLine("            </path>");
        h.AppendLine("            <ellipse cx='130' cy='140' rx='60' ry='50' fill='url(#gradWater)' opacity='0.5'/>");
        h.AppendLine("            <rect x='120' y='55' width='20' height='25' fill='url(#gradWater)' opacity='0.7'>");
        h.AppendLine("              <animate attributeName='opacity' values='0.7;0.9;0.7' dur='1.2s' repeatCount='indefinite'/>");
        h.AppendLine("            </rect>");
        h.AppendLine("            <circle r='3' fill='#81d4fa'>");
        h.AppendLine("              <animateMotion dur='2s' repeatCount='indefinite' path='M 40,140 Q 80,140 130,140'/>");
        h.AppendLine("              <animate attributeName='opacity' values='0;1;0' dur='2s' repeatCount='indefinite'/>");
        h.AppendLine("            </circle>");
        h.AppendLine("            <circle r='3' fill='#4fc3f7'>");
        h.AppendLine("              <animateMotion dur='2.5s' begin='0.5s' repeatCount='indefinite' path='M 40,140 Q 80,140 130,140'/>");
        h.AppendLine("              <animate attributeName='opacity' values='0;1;0' dur='2.5s' begin='0.5s' repeatCount='indefinite'/>");
        h.AppendLine("            </circle>");
        h.AppendLine("            <circle r='2.5' fill='#29b6f6'>");
        h.AppendLine("              <animateMotion dur='1.8s' begin='1s' repeatCount='indefinite' path='M 130,140 L 130,70'/>");
        h.AppendLine("              <animate attributeName='opacity' values='0;1;0' dur='1.8s' begin='1s' repeatCount='indefinite'/>");
        h.AppendLine("            </circle>");
        h.AppendLine("          </g>");
        h.AppendLine("          <g id='impeller' filter='url(#glow)'>");
        h.AppendLine("            <circle cx='130' cy='140' r='35' fill='url(#gradImpeller)' stroke='#f57c00' stroke-width='2'/>");
        h.AppendLine("            <g>");
        h.AppendLine("              <path d='M 130,105 Q 145,115 145,140 Q 145,125 130,115 Z' fill='rgba(255,179,0,0.9)' stroke='#f57c00' stroke-width='1'/>");
        h.AppendLine("              <path d='M 165,140 Q 155,155 130,155 Q 145,155 155,140 Z' fill='rgba(255,179,0,0.9)' stroke='#f57c00' stroke-width='1'/>");
        h.AppendLine("              <path d='M 130,175 Q 115,165 115,140 Q 115,155 130,165 Z' fill='rgba(255,179,0,0.9)' stroke='#f57c00' stroke-width='1'/>");
        h.AppendLine("              <path d='M 95,140 Q 105,125 130,125 Q 115,125 105,140 Z' fill='rgba(255,179,0,0.9)' stroke='#f57c00' stroke-width='1'/>");
        h.AppendLine("            </g>");
        h.AppendLine("            <circle cx='130' cy='140' r='15' fill='rgba(255,152,0,0.95)' stroke='#e65100' stroke-width='2'/>");
        h.AppendLine("            <circle cx='130' cy='140' r='8' fill='#ffd54f'/>");
        h.AppendLine("            <circle cx='127' cy='137' r='3' fill='rgba(255,255,255,0.7)'/>");
        h.AppendLine("          </g>");
        h.AppendLine("          <rect x='165' y='135' width='95' height='10' fill='url(#gradShaft)' stroke='#546e7a' stroke-width='1.5'/>");
        h.AppendLine("          <rect x='165' y='137' width='95' height='3' fill='rgba(255,255,255,0.2)'/>");
        h.AppendLine("          <g>");
        h.AppendLine("            <rect x='255' y='130' width='25' height='20' rx='2' fill='url(#gradMetallic)' stroke='#546e7a' stroke-width='2'/>");
        h.AppendLine("            <line x1='260' y1='132' x2='260' y2='148' stroke='#37474f' stroke-width='2'/>");
        h.AppendLine("            <line x1='267' y1='132' x2='267' y2='148' stroke='#37474f' stroke-width='2'/>");
        h.AppendLine("            <line x1='274' y1='132' x2='274' y2='148' stroke='#37474f' stroke-width='2'/>");
        h.AppendLine("          </g>");
        h.AppendLine("          <g id='motor' filter='url(#shadow)'>");
        h.AppendLine("            <rect x='280' y='175' width='115' height='20' rx='3' fill='url(#gradMetallic)' stroke='#546e7a' stroke-width='1.5'/>");
        h.AppendLine("            <rect x='285' y='105' width='105' height='70' rx='8' fill='url(#gradMotor)' stroke='#1976d2' stroke-width='2.5'/>");
        h.AppendLine("            <rect x='287' y='107' width='101' height='30' rx='6' fill='rgba(255,255,255,0.1)'/>");
        h.AppendLine("            <g opacity='0.6'>");
        for (int y = 112; y <= 168; y += 8)
            h.AppendLine($"              <line x1='290' y1='{y}' x2='385' y2='{y}' stroke='rgba(0,0,0,0.4)' stroke-width='2'/>");
        h.AppendLine("            </g>");
        h.AppendLine("            <ellipse cx='390' cy='140' rx='9' ry='35' fill='rgba(13,71,161,0.9)' stroke='#1976d2' stroke-width='2'/>");
        h.AppendLine("            <rect x='320' y='88' width='35' height='20' rx='3' fill='rgba(0,0,0,0.7)' stroke='#1976d2' stroke-width='1.5'/>");
        h.AppendLine("            <circle cx='337.5' cy='98' r='3.5' fill='#ffc107'/>");
        h.AppendLine("            <rect x='320' y='137' width='40' height='24' rx='3' fill='rgba(0,0,0,0.8)' stroke='#1976d2' stroke-width='2'/>");
        h.AppendLine("            <text x='340' y='152' text-anchor='middle' font-size='12' fill='#42a5f5' font-weight='bold'>Motor</text>");
        h.AppendLine("          </g>");
        h.AppendLine("          <g id='statusIndicator'>");
        h.AppendLine("            <circle cx='35' cy='50' r='18' fill='rgba(0,0,0,0.6)'/>");
        h.AppendLine("            <circle id='statusLight' cx='35' cy='50' r='14' fill='#4CAF50' opacity='0.95' filter='url(#glow)'>");
        h.AppendLine("              <animate attributeName='opacity' values='0.95;0.5;0.95' dur='2s' repeatCount='indefinite'/>");
        h.AppendLine("            </circle>");
        h.AppendLine("            <circle cx='35' cy='50' r='9' fill='rgba(255,255,255,0.7)'/>");
        h.AppendLine("            <circle cx='32' cy='47' r='4' fill='rgba(255,255,255,0.9)'/>");
        h.AppendLine("          </g>");
        h.AppendLine("        </svg>");
        h.AppendLine("      </div>");
    }

    private void GenerarJavaScript(string rutaAppJs, string dataJsNombre)
    {
        var js = new StringBuilder();

        // ═══════════════════════════════════════════════════════════
        // VARIABLES GLOBALES
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("// ESTADO GLOBAL");
        js.AppendLine("var currentState = '';");
        js.AppendLine("var eventLog = [];");
        js.AppendLine("var tempHistory = [];");
        js.AppendLine("var currentHistory = [];");
        js.AppendLine("");
        js.AppendLine("// CHARTS CON DOBLE BUFFER (anti-parpadeo)");
        js.AppendLine("var tempChartObj = null;");
        js.AppendLine("var currentChartObj = null;");
        js.AppendLine("var lastChartData = {};");
        js.AppendLine("var rafId = null;");
        js.AppendLine("");

        // ═══════════════════════════════════════════════════════════
        // INICIALIZAR CHART CON CANVAS OFFSCREEN
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("function initChart(canvasId) {");
        js.AppendLine("  var visible = document.getElementById(canvasId);");
        js.AppendLine("  if (!visible) return null;");
        js.AppendLine("  var offscreen = document.createElement('canvas');");
        js.AppendLine("  var dpr = window.devicePixelRatio || 1;");
        js.AppendLine("  offscreen.width = visible.width = visible.offsetWidth * dpr;");
        js.AppendLine("  offscreen.height = visible.height = visible.offsetHeight * dpr;");
        js.AppendLine("  return { visible: visible, offscreen: offscreen, initialized: true };");
        js.AppendLine("}");
        js.AppendLine("");

        // ═══════════════════════════════════════════════════════════
        // ACTUALIZAR CHART (con comparación de datos)
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("function updateChart(chartObj, newData, color, minVal, maxVal) {");
        js.AppendLine("  if (!chartObj || !chartObj.initialized) return;");
        js.AppendLine("  var dataKey = chartObj.visible.id;");
        js.AppendLine("  var dataStr = JSON.stringify(newData);");
        js.AppendLine("  if (dataStr === lastChartData[dataKey]) return;");
        js.AppendLine("  lastChartData[dataKey] = dataStr;");
        js.AppendLine("  if (rafId) cancelAnimationFrame(rafId);");
        js.AppendLine("  rafId = requestAnimationFrame(function() {");
        js.AppendLine("    drawChart(chartObj, newData, color, minVal, maxVal);");
        js.AppendLine("  });");
        js.AppendLine("}");
        js.AppendLine("");

        // ═══════════════════════════════════════════════════════════
        // DIBUJAR CHART EN OFFSCREEN Y COPIAR A VISIBLE (SIN clearRect)
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("function drawChart(chartObj, data, color, minVal, maxVal) {");
        js.AppendLine("  var off = chartObj.offscreen;");
        js.AppendLine("  var offCtx = off.getContext('2d');");
        js.AppendLine("  var w = off.width;");
        js.AppendLine("  var h = off.height;");
        js.AppendLine("  var dpr = window.devicePixelRatio || 1;");
        js.AppendLine("  var marginL = 45 * dpr;");
        js.AppendLine("  var marginR = 15 * dpr;");
        js.AppendLine("  var marginT = 20 * dpr;");
        js.AppendLine("  var marginB = 30 * dpr;");
        js.AppendLine("  var chartW = w - marginL - marginR;");
        js.AppendLine("  var chartH = h - marginT - marginB;");
        js.AppendLine("  var range = maxVal - minVal;");
        js.AppendLine("  if (data.length < 2) return;");
        js.AppendLine("  offCtx.strokeStyle = 'rgba(100, 181, 246, 0.1)';");
        js.AppendLine("  offCtx.lineWidth = 1;");
        js.AppendLine("  offCtx.font = Math.floor(9 * dpr) + 'px monospace';");
        js.AppendLine("  offCtx.fillStyle = '#78909c';");
        js.AppendLine("  offCtx.textAlign = 'right';");
        js.AppendLine("  offCtx.textBaseline = 'middle';");
        js.AppendLine("  for (var i = 0; i <= 4; i++) {");
        js.AppendLine("    var yVal = minVal + (range * i / 4);");
        js.AppendLine("    var y = marginT + chartH - (chartH * i / 4);");
        js.AppendLine("    offCtx.beginPath();");
        js.AppendLine("    offCtx.moveTo(marginL, y);");
        js.AppendLine("    offCtx.lineTo(marginL + chartW, y);");
        js.AppendLine("    offCtx.stroke();");
        js.AppendLine("    offCtx.fillText(yVal.toFixed(0), marginL - 5 * dpr, y);");
        js.AppendLine("  }");
        js.AppendLine("  var stepX = chartW / (data.length - 1);");
        js.AppendLine("  offCtx.strokeStyle = color;");
        js.AppendLine("  offCtx.lineWidth = 2 * dpr;");
        js.AppendLine("  offCtx.beginPath();");
        js.AppendLine("  for (var j = 0; j < data.length; j++) {");
        js.AppendLine("    var x = marginL + (j * stepX);");
        js.AppendLine("    var normalized = Math.max(0, Math.min(1, (data[j] - minVal) / range));");
        js.AppendLine("    var y = marginT + chartH - (normalized * chartH);");
        js.AppendLine("    if (j === 0) offCtx.moveTo(x, y);");
        js.AppendLine("    else offCtx.lineTo(x, y);");
        js.AppendLine("  }");
        js.AppendLine("  offCtx.stroke();");
        js.AppendLine("  offCtx.lineTo(marginL + chartW, marginT + chartH);");
        js.AppendLine("  offCtx.lineTo(marginL, marginT + chartH);");
        js.AppendLine("  offCtx.closePath();");
        js.AppendLine("  var r = parseInt(color.slice(1, 3), 16);");
        js.AppendLine("  var g = parseInt(color.slice(3, 5), 16);");
        js.AppendLine("  var b = parseInt(color.slice(5, 7), 16);");
        js.AppendLine("  offCtx.fillStyle = 'rgba(' + r + ',' + g + ',' + b + ', 0.2)';");
        js.AppendLine("  offCtx.fill();");
        js.AppendLine("  var visCtx = chartObj.visible.getContext('2d');");
        js.AppendLine("  visCtx.drawImage(off, 0, 0, w, h, 0, 0, chartObj.visible.width, chartObj.visible.height);");
        js.AppendLine("}");
        js.AppendLine("");

        // ═══════════════════════════════════════════════════════════
        // MANEJAR RESIZE
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("function handleChartResize() {");
        js.AppendLine("  if (tempChartObj && tempChartObj.initialized) {");
        js.AppendLine("    var v = tempChartObj.visible;");
        js.AppendLine("    var dpr = window.devicePixelRatio || 1;");
        js.AppendLine("    tempChartObj.offscreen.width = v.width = v.offsetWidth * dpr;");
        js.AppendLine("    tempChartObj.offscreen.height = v.height = v.offsetHeight * dpr;");
        js.AppendLine("    lastChartData[v.id] = null;");
        js.AppendLine("  }");
        js.AppendLine("  if (currentChartObj && currentChartObj.initialized) {");
        js.AppendLine("    var v2 = currentChartObj.visible;");
        js.AppendLine("    var dpr2 = window.devicePixelRatio || 1;");
        js.AppendLine("    currentChartObj.offscreen.width = v2.width = v2.offsetWidth * dpr2;");
        js.AppendLine("    currentChartObj.offscreen.height = v2.height = v2.offsetHeight * dpr2;");
        js.AppendLine("    lastChartData[v2.id] = null;");
        js.AppendLine("  }");
        js.AppendLine("}");
        js.AppendLine("");

        // ═══════════════════════════════════════════════════════════
        // RENDER PRINCIPAL
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("function render(data) {");
        js.AppendLine("  document.getElementById('kpiTemp').textContent = data.temperatura.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiPower').textContent = data.potencia.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiCurrent').textContent = data.corriente.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiFreq').textContent = data.frecuencia.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiVoltage').textContent = data.voltaje.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiReactivePower').textContent = data.potenciaReactiva.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiApparentPower').textContent = data.potenciaAparente.toFixed(1);");
        js.AppendLine("  document.getElementById('kpiEnergy').textContent = data.consumoElectrico.toFixed(1);");
        js.AppendLine("  tempHistory = data.temp_historico || [];");
        js.AppendLine("  currentHistory = data.corriente_historico || [];");
        js.AppendLine("  var newState = String(data.bomba_estado || 'stopped').trim().toLowerCase();");
        js.AppendLine("  updatePumpVisual(newState);");
        js.AppendLine("  updateStatusBadge(newState);");
        js.AppendLine("  if (newState !== currentState && currentState !== '') { addEvent(newState); }");
        js.AppendLine("  currentState = newState;");
        js.AppendLine("  updateChart(tempChartObj, tempHistory, '#ef5350', 0, 100);");
        js.AppendLine("  updateChart(currentChartObj, currentHistory, '#42a5f5', 0, 60);");
        js.AppendLine("}");
        js.AppendLine("");

        js.AppendLine("function updatePumpVisual(state) {");
        js.AppendLine("  var impeller = document.getElementById('impeller');");
        js.AppendLine("  var statusLight = document.getElementById('statusLight');");
        js.AppendLine("  var flowParticles = document.getElementById('waterFlow');");
        js.AppendLine("  var gradStops = document.querySelectorAll('#gradCasing stop');");
        js.AppendLine("  if (!impeller || !statusLight || !flowParticles || gradStops.length === 0) return;");
        js.AppendLine("  var configs = {");
        js.AppendLine("    'running': { animation: 'spin 1.5s linear infinite', lightColor: '#4caf50', casingColor: '#1e88e5', flowOpacity: '1' },");
        js.AppendLine("    'stopped': { animation: 'none', lightColor: '#78909c', casingColor: '#546e7a', flowOpacity: '0' },");
        js.AppendLine("    'fault': { animation: 'shake 0.3s infinite', lightColor: '#f44336', casingColor: '#d32f2f', flowOpacity: '0.3' },");
        js.AppendLine("    'maintenance': { animation: 'none', lightColor: '#ff9800', casingColor: '#f57c00', flowOpacity: '0' }");
        js.AppendLine("  };");
        js.AppendLine("  var config = configs[state] || configs['stopped'];");
        js.AppendLine("  impeller.style.transformBox = 'fill-box';");
        js.AppendLine("  impeller.style.transformOrigin = 'center';");
        js.AppendLine("  impeller.style.animation = 'none';");
        js.AppendLine("  void impeller.offsetWidth;");
        js.AppendLine("  impeller.style.animation = config.animation;");
        js.AppendLine("  statusLight.setAttribute('fill', config.lightColor);");
        js.AppendLine("  gradStops[0].style.stopColor = config.casingColor;");
        js.AppendLine("  gradStops[1].style.stopColor = '#37474f';");
        js.AppendLine("  gradStops[2].style.stopColor = '#263238';");
        js.AppendLine("  flowParticles.style.opacity = config.flowOpacity;");
        js.AppendLine("}");
        js.AppendLine("");

        js.AppendLine("function updateStatusBadge(state) {");
        js.AppendLine("  var badge = document.getElementById('statusBadge');");
        js.AppendLine("  var text = document.getElementById('statusText');");
        js.AppendLine("  var configs = {");
        js.AppendLine("    'running': { className: 'badge-running', text: ' EN OPERACIÓN' },");
        js.AppendLine("    'stopped': { className: 'badge-stopped', text: ' DETENIDA' },");
        js.AppendLine("    'fault': { className: 'badge-fault', text: ' FALLA DETECTADA' },");
        js.AppendLine("    'maintenance': { className: 'badge-maintenance', text: ' MANTENIMIENTO' }");
        js.AppendLine("  };");
        js.AppendLine("  var config = configs[state] || configs['stopped'];");
        js.AppendLine("  badge.className = 'status-badge ' + config.className;");
        js.AppendLine("  text.textContent = config.text;");
        js.AppendLine("}");
        js.AppendLine("");

        js.AppendLine("function addEvent(state) {");
        js.AppendLine("  var now = new Date();");
        js.AppendLine("  var time = now.toTimeString().slice(0, 8);");
        js.AppendLine("  var messages = {");
        js.AppendLine("    'running': { msg: 'Bomba iniciada - Operación normal', type: 'info' },");
        js.AppendLine("    'stopped': { msg: 'Bomba detenida', type: 'info' },");
        js.AppendLine("    'fault': { msg: 'Falla detectada - Revisión necesaria', type: 'error' },");
        js.AppendLine("    'maintenance': { msg: 'Modo mantenimiento activado', type: 'warning' }");
        js.AppendLine("  };");
        js.AppendLine("  var msgConfig = messages[state] || { msg: 'Cambio de estado', type: 'info' };");
        js.AppendLine("  eventLog.unshift({ time: time, message: msgConfig.msg, type: msgConfig.type });");
        js.AppendLine("  if (eventLog.length > 10) { eventLog.pop(); }");
        js.AppendLine("  renderEvents();");
        js.AppendLine("}");
        js.AppendLine("");

        js.AppendLine("function renderEvents() {");
        js.AppendLine("  var container = document.getElementById('eventsLog');");
        js.AppendLine("  var html = '';");
        js.AppendLine("  for (var i = 0; i < eventLog.length; i++) {");
        js.AppendLine("    var event = eventLog[i];");
        js.AppendLine("    html += '<div class=\"event-row event-' + event.type + '\">';");
        js.AppendLine("    html += '<div class=\"event-time\">' + event.time + '</div>';");
        js.AppendLine("    html += '<div class=\"event-message\">' + event.message + '</div>';");
        js.AppendLine("    html += '</div>';");
        js.AppendLine("  }");
        js.AppendLine("  container.innerHTML = html;");
        js.AppendLine("}");
        js.AppendLine("");

        js.AppendLine("function poll() {");
        js.AppendLine($"  fetch('./{dataJsNombre}?t=' + Date.now(), {{ cache: 'no-store' }})");
        js.AppendLine("    .then(function(response) { return response.json(); })");
        js.AppendLine("    .then(function(data) { render(data); })");
        js.AppendLine("    .catch(function(error) { console.error('Error:', error); });");
        js.AppendLine("}");
        js.AppendLine("");

        // ═══════════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ═══════════════════════════════════════════════════════════
        js.AppendLine("document.addEventListener('DOMContentLoaded', function() {");
        js.AppendLine("  tempChartObj = initChart('chartTemp');");
        js.AppendLine("  currentChartObj = initChart('chartCurrent');");
        js.AppendLine("  poll();");
        js.AppendLine("  setInterval(poll, 2500);");
        js.AppendLine("  addEvent('running');");
        js.AppendLine("  var resizeTimer;");
        js.AppendLine("  window.addEventListener('resize', function() {");
        js.AppendLine("    clearTimeout(resizeTimer);");
        js.AppendLine("    resizeTimer = setTimeout(handleChartResize, 250);");
        js.AppendLine("  });");
        js.AppendLine("});");

        File.WriteAllText(rutaAppJs, js.ToString());
    }

    private void ActualizarDatos(string rutaDataJson)
    {
        try
        {
            float temperatura = LogicObject.GetVariable("Temperatura").Value;
            float potencia = LogicObject.GetVariable("Potencia").Value;
            float corriente = LogicObject.GetVariable("Corriente").Value;
            float frecuencia = LogicObject.GetVariable("Frecuencia").Value;
            string estado = LogicObject.GetVariable("Estado").Value;
            float nuevaTemp = LogicObject.GetVariable("TempHistorico").Value;
            float nuevaCorriente = LogicObject.GetVariable("CorrienteHistorico").Value;

            float voltaje = LogicObject.GetVariable("Voltaje").Value;
            float potenciaReactiva = LogicObject.GetVariable("PotenciaReactiva").Value;
            float potenciaAparente = LogicObject.GetVariable("PotenciaAparente").Value;
            float consumoElectrico = LogicObject.GetVariable("ConsumoElectrico").Value;

            for (int i = 0; i < 99; i++)
            {
                tempHistorico[i] = tempHistorico[i + 1];
                corrienteHistorico[i] = corrienteHistorico[i + 1];
            }

            tempHistorico[99] = nuevaTemp;
            corrienteHistorico[99] = nuevaCorriente;

            var json = new StringBuilder();
            json.Append("{");
            json.AppendFormat("\"bomba_estado\":\"{0}\",", estado);
            json.AppendFormat("\"temperatura\":{0:F1},", temperatura);
            json.AppendFormat("\"potencia\":{0:F1},", potencia);
            json.AppendFormat("\"corriente\":{0:F1},", corriente);
            json.AppendFormat("\"frecuencia\":{0:F1},", frecuencia);
            json.AppendFormat("\"voltaje\":{0:F1},", voltaje);
            json.AppendFormat("\"potenciaReactiva\":{0:F1},", potenciaReactiva);
            json.AppendFormat("\"potenciaAparente\":{0:F1},", potenciaAparente);
            json.AppendFormat("\"consumoElectrico\":{0:F1},", consumoElectrico);

            json.Append("\"temp_historico\":[");
            for (int i = 0; i < 100; i++)
            {
                json.AppendFormat("{0:F1}", tempHistorico[i]);
                if (i < 99) json.Append(",");
            }
            json.Append("],");

            json.Append("\"corriente_historico\":[");
            for (int i = 0; i < 100; i++)
            {
                json.AppendFormat("{0:F1}", corrienteHistorico[i]);
                if (i < 99) json.Append(",");
            }
            json.Append("],");

            json.AppendFormat("\"timestamp\":\"{0:o}\"", DateTime.Now);
            json.Append("}");

            File.WriteAllText(rutaDataJson, json.ToString());
        }
        catch (Exception ex)
        {
            Log.Error("BombaDashboard", $"Error actualizando datos: {ex.Message}");
        }
    }
}