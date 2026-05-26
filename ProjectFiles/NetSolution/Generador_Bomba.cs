#region Using directives
using System;
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

public class Generador_Bomba : BaseNetLogic
{
    public int Seed;
    public int PeriodoMs = 1000;

    private PeriodicTask _task;
    private Random _rng;
    private int _tick;

    private double _corrienteObjetivo;
    private double _voltajeObjetivo;

    private int _holdCorriente;
    private int _holdVoltaje;

    private IUAObject parentBomba;

    // Estados actuales con inercia
    private double _voltajeActual;
    private double _corrienteActual;
    private double _frecuenciaActual;
    private double _temperaturaActual;

    public double Voltaje { get; private set; }
    public double Corriente { get; private set; }
    public double Potencia_Activa { get; private set; }
    public double Potencia_Aparente { get; private set; }
    public double Potencia_Reactiva { get; private set; }
    public double Frecuencia { get; private set; }
    public double Temperatura { get; private set; }
    public double Consumo_Electrico { get; private set; }

    public int Control { get; set; }

    public override void Start()
    {
        parentBomba = (IUAObject)LogicObject.Owner;
        Seed = LogicObject.GetVariable("Seed").Value;
        _rng = new Random(Seed);
        _tick = 0;

        InicializarValores();

        _task = new PeriodicTask(Actualizar, PeriodoMs, LogicObject);
        _task.Start();
    }

    public override void Stop()
    {
        _task?.Dispose();
        _task = null;
    }

    private void Actualizar()
    {
        _tick++;
        double u = Clamp(Control / 100.0, -1.0, 1.0);

        // ===== VOLTAJE BASE (de la red) =====
        if (_holdVoltaje-- <= 0)
        {
            double salto = Noise(1.5);
            _voltajeObjetivo = Clamp(Voltaje + salto, 220.0, 240.0);
            _holdVoltaje = _rng.Next(8, 15);
        }
        Voltaje = Clamp(Voltaje + ((_voltajeObjetivo - Voltaje) * 0.3) + Noise(0.2), 220.0, 240.0);

        // ===== CORRIENTE BASE (variación reducida) =====
        if (_holdCorriente-- <= 0)
        {
            double salto = Noise(0.8) + (u * 12.0);
            if (_rng.NextDouble() < 0.15) salto += Noise(1.5);
            _corrienteObjetivo = Clamp(Corriente + salto, 10.0, 65.0);
            _holdCorriente = _rng.Next(5, 12); // Cambios más espaciados
        }
        Corriente = Clamp(Corriente + ((_corrienteObjetivo - Corriente) * 0.35) + Noise(0.15), 10.0, 65.0);

        // ===== FRECUENCIA BASE =====
        Frecuencia = Clamp(60.0 + Noise(0.02), 59.90, 60.10);

        // ===== BOMBA CON INERCIA =====
        bool estadoActual = parentBomba.GetVariable("OnOff").Value;

        if (estadoActual)
        {
            // Encendida: valores normales con transición suave
            double voltajeObjetivo = Clamp(Voltaje + Noise(1.5), 210.0, 245.0);
            double corrienteObjetivo = Clamp(Corriente + Noise(1.0), 5.0, 90.0); // Ruido reducido
            double frecuenciaObjetivo = Clamp(Frecuencia + Noise(2.0), 55, 60.2);

            _voltajeActual += (voltajeObjetivo - _voltajeActual) * 0.6;
            _corrienteActual += (corrienteObjetivo - _corrienteActual) * 0.5;
            _frecuenciaActual += (frecuenciaObjetivo - _frecuenciaActual) * 0.3;

            // ===== TEMPERATURA: equilibrio térmico basado en corriente actual =====
            double cargaNorm = Clamp(_corrienteActual / 90.0, 0.0, 1.0);
            double temperaturaEquilibrio = 45.0 + (cargaNorm * 30.0) + (u * 3.0) + Noise(1.5);
            temperaturaEquilibrio = Clamp(temperaturaEquilibrio, 40.0, 90.0);

            _temperaturaActual += (temperaturaEquilibrio - _temperaturaActual) * 0.08;

            // ===== CÁLCULO DE POTENCIAS =====
            double fp = 0.85;

            Potencia_Aparente = (_voltajeActual * _corrienteActual) / 1000.0;
            Potencia_Activa = Potencia_Aparente * fp;
            Potencia_Reactiva = Potencia_Aparente * Math.Sin(Math.Acos(fp));

            // ===== ACUMULACIÓN DE CONSUMO =====
            double dtHoras = PeriodoMs / 1000.0 / 3600.0;
            double incrementoConsumo = Potencia_Activa * dtHoras;
            Consumo_Electrico = Consumo_Electrico + incrementoConsumo;
        }
        else
        {
            // Apagada: todo a cero
            _voltajeActual = 0;
            _corrienteActual = 0;
            _frecuenciaActual = 0;

            _temperaturaActual += (25.0 - _temperaturaActual) * 0.05;

            Potencia_Activa = 0.0;
            Potencia_Aparente = 0.0;
            Potencia_Reactiva = 0.0;
        }

        // ===== ESCRITURA DE TAGS =====
        parentBomba.GetVariable("Voltaje").Value = Clamp(_voltajeActual, 0.0, 245.0);
        parentBomba.GetVariable("Corriente").Value = Clamp(_corrienteActual, 0.0, 90.0);
        parentBomba.GetVariable("Frecuencia").Value = Clamp(_frecuenciaActual, 0.0, 60.2);
        parentBomba.GetVariable("Temperatura").Value = Clamp(_temperaturaActual, 25.0, 90.0);

        parentBomba.GetVariable("Potencia Activa").Value = Potencia_Activa;
        parentBomba.GetVariable("Potencia Aparente").Value = Potencia_Aparente;
        parentBomba.GetVariable("Potencia Reactiva").Value = Potencia_Reactiva;
        parentBomba.GetVariable("Consumo Electrico").Value = Consumo_Electrico;
    }

    private void InicializarValores()
    {
        Voltaje = 228.0 + _rng.NextDouble() * 8.0;
        Corriente = 25.0 + _rng.NextDouble() * 15.0;
        Frecuencia = 60.0;

        Potencia_Activa = 0.0;
        Potencia_Aparente = 0.0;
        Potencia_Reactiva = 0.0;

        Consumo_Electrico = 1.0 + _rng.NextDouble() * 45.0;

        _voltajeObjetivo = Voltaje;
        _corrienteObjetivo = Corriente;

        _holdVoltaje = _rng.Next(8, 15);
        _holdCorriente = _rng.Next(5, 12);

        _voltajeActual = Voltaje;
        _corrienteActual = 0.0;
        _frecuenciaActual = 0.0;
        _temperaturaActual = 25.0;
    }

    private double Noise(double amplitude)
    {
        return (_rng.NextDouble() - 0.5) * 2.0 * amplitude;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    [ExportMethod]
    public void CambioControl(int nuevoControl)
    {
        Control = nuevoControl;
        LogicObject.GetVariable("Control").Value = Control;
    }
}
