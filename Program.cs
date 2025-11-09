// En: RedSismica.App/Program.cs

// 1. Usings necesarios para la Inyección de Dependencias
using Microsoft.EntityFrameworkCore; // Para DbContextOptionsBuilder
using RedSismica.App.Controladores;
using RedSismica.App.Services;
using RedSismica.Infrastructure;
using RedSismica.Infrastructure.Data;
using RedSismica.Infrastructure.Repositories;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RedSismica.App
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // --- 1. CONFIGURACIÓN DE LA BASE DE DATOS ---
            // Usaremos la BBDD SQLite real (rs.db), no 'InMemory'
            var options = new DbContextOptionsBuilder<RedSismicaContext>()
                .UseSqlite("Data Source=rs.db") // Tu cadena de conexión
                .Options;

            // --- 2. SEED (Poblado inicial de la BBDD) ---
            // Usamos un bloque 'using' para el contexto de inicialización
            using (var ctx_init = new RedSismicaContext(options))
            {
                // Aseguramos que la BBDD exista
                ctx_init.Database.EnsureCreated();

                // Revisamos si ya se importó o si ya hay datos
                if (!ctx_init.EventosSismicos.Any())
                {
                    try
                    {
                        // (IMPORTANTE: Debes crear una carpeta "import"
                        // en la carpeta de salida (bin/Debug/net6.0-windows)
                        // y poner ahí los .txt que definimos)
                        string importPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import");
                        if (Directory.Exists(importPath))
                        {
                            BulkTxtImporter.Run(ctx_init, importPath);
                        }
                        else
                        {
                            MessageBox.Show(
                                $"Carpeta 'import' no encontrada en {importPath}.\n" +
                                "La aplicación se ejecutará sin datos iniciales.",
                                "Advertencia de Importación",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Si la importación falla, informamos al usuario
                        MessageBox.Show(
                            $"Error fatal al importar datos: {ex.Message}\n" +
                            "La aplicación se cerrará.",
                            "Error de Inicialización",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return; // Cerramos la app si no se pueden cargar datos
                    }
                }
            }

            // --- 3. INYECCIÓN DE DEPENDENCIAS (El "Armado") ---

            // Creamos las instancias que se usarán durante la vida de la app
            var context = new RedSismicaContext(options);
            var repository = new EventoRepositoryEF(context);
            var generadorSismograma = new CU_GenerarSismograma();

            // Creamos el "cerebro" y le pasamos sus herramientas
            var manejador = new ManejadorRegistrarRespuesta(
                context,
                repository,
                generadorSismograma
            );

            // --- 4. EJECUCIÓN DE LA APLICACIÓN ---
            ApplicationConfiguration.Initialize();

            // Creamos la pantalla principal y le "inyectamos" el manejador
            var pantallaPrincipal = new PantallaNuevaRevision(manejador);

            Application.Run(pantallaPrincipal);
        }
    }
}