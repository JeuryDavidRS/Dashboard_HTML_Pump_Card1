#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.Modbus;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.UI;
using FTOptix.WebUI;
using System;
using System.IO;
using System.Linq;
using UAManagedCore;
using FTOptix.OPCUAClient;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class Analytics_Logic : BaseNetLogic
{
    private PeriodicTask tareaActualizacion;
    private string sourceFile;
    private string carpetaImagenes;
    
    public override void Start()
    {
        sourceFile = LogicObject.GetVariable("Path").Value;
        
        // Obtener carpeta External_Res
        int namespaceIndex = LogicObject.NodeId.NamespaceIndex;
        var projectUri = new ResourceUri($"ns={namespaceIndex};%PROJECTDIR%\\");
        string rutaProyecto = projectUri.Uri;
        carpetaImagenes = Path.Combine(rutaProyecto, "External_Res");
        
        tareaActualizacion = new PeriodicTask(UpdateAnalytics, 60000, LogicObject);
        tareaActualizacion.Start();
    }
    
    public override void Stop()
    {
        tareaActualizacion?.Dispose();
        tareaActualizacion = null;
    }
    
    public void UpdateAnalytics()
    {
        try
        {
            // Generar nombre único con timestamp
            string nombreOriginal = Path.GetFileNameWithoutExtension(sourceFile);
            string extension = Path.GetExtension(sourceFile);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string nombreArchivo = $"{nombreOriginal}_{timestamp}{extension}";
            
            string destino = Path.Combine(carpetaImagenes, nombreArchivo);
            Directory.CreateDirectory(carpetaImagenes);
            
            // Copiar nueva imagen
            File.Copy(sourceFile, destino, overwrite: true);
            
            // Actualizar componente Image
            var foto = (Image)Owner;
            var imagenUri = ResourceUri.FromProjectRelativePath($"External_Res/{nombreArchivo}");
            foto.Path = imagenUri;
            
            // Limpiar imágenes antiguas (mantener solo las últimas 5)
            LimpiarImagenesAntiguas(nombreOriginal, extension);
            
            Log.Info("Analytics", $"✓ Actualizada: {nombreArchivo}");
        }
        catch (Exception ex)
        {
            Log.Error("Analytics", $"Error: {ex.Message}");
        }
    }
    
    private void LimpiarImagenesAntiguas(string nombreBase, string extension)
    {
        try
        {
            // Buscar todas las imágenes con el mismo nombre base
            string patron = $"{nombreBase}_*{extension}";
            var archivos = Directory.GetFiles(carpetaImagenes, patron);
            
            // Si hay más de 5, borrar las más antiguas
            if (archivos.Length > 5)
            {
                var archivosOrdenados = archivos
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();
                
                // Mantener los 5 más recientes, borrar el resto
                foreach (var archivo in archivosOrdenados.Skip(5))
                {
                    archivo.Delete();
                    Log.Info("Analytics", $"🗑️ Eliminada imagen antigua: {archivo.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Analytics", $"No se pudo limpiar imágenes antiguas: {ex.Message}");
        }
    }
}
