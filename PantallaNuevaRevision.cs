// 1. Using LIMPIOS - Apuntan a nuestros proyectos
using RedSismica.App.Controladores; // Crearemos esta carpeta
using RedSismica.App.Services;
using RedSismica.Core.Entities;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System;

// 2. Namespace CORRECTO
namespace RedSismica.App
{
    // Es una 'partial class', la otra mitad es el .Designer.cs
    public partial class PantallaNuevaRevision : Form
    {
        // 3. El Manejador es privado
        private ManejadorRegistrarRespuesta manejador;

        // 4. CONSTRUCTOR LIMPIO (Inyección de Dependencias)
        // El Manejador se "inyecta" desde Program.cs
        public PantallaNuevaRevision(ManejadorRegistrarRespuesta manejador)
        {
            InitializeComponent();
            this.manejador = manejador; // Simplemente lo asignamos
        }

        // --- MÉTODOS PÚBLICOS (La "Interfaz" de la Vista) ---
        // Estos métodos los llama el Manejador

        public void Habilitar()
        {
            this.Enabled = true;
        }

        public void SolicitarSeleccionEvento(List<object> eventos)
        {
            gridEventos.AutoGenerateColumns = true;
            gridEventos.DataSource = null;
            gridEventos.Columns.Clear();
            gridEventos.DataSource = eventos;
            gridEventos.Refresh();
        }

        public void MostrarDetalleEventoSismico(string detalle)
        {
            txtDetalleEvento.Text = detalle;
        }

        // 5. Método 'MostrarSismograma' SIMPLIFICADO
        // Solo muestra la imagen que le pasa el manejador.
        public void MostrarSismograma(string rutaImagen)
        {
            try
            {
                txtSismograma.Visible = false;
                picSismograma.Visible = true;

                // Liberar imagen anterior si existe
                if (picSismograma.Image != null)
                {
                    var old = picSismograma.Image;
                    picSismograma.Image = null;
                    old.Dispose();
                }

                // Cargar la nueva imagen desde el archivo
                // Usamos FileStream para evitar bloqueos del archivo
                using (var fs = new FileStream(rutaImagen, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    picSismograma.Image = Image.FromStream(fs);
                }
                picSismograma.BringToFront();
            }
            catch (Exception ex)
            {
                picSismograma.Visible = false;
                txtSismograma.Visible = true;
                txtSismograma.Text = $"[Error al cargar sismograma]: {ex.Message}";
            }
        }

        public void opcionMostrarMapa()
        {
            grpMapa.Visible = true;
        }

        public void OpcionModificacionAlcance()
        {
            grpModificarAlcance.Enabled = true;
        }

        public void OpcionModificacionMagnitud()
        {
            grpModificarMagnitud.Enabled = true;
        }

        public void OpcionModificacionOrigen()
        {
            grpModificarOrigen.Enabled = true;
        }

        public void SolicitarSeleccionAcciones()
        {
            cmbAccion.Enabled = true;
            btnConfirmar.Enabled = true;
        }

        public void MostrarMensaje(string texto)
        {
            MessageBox.Show(texto);
        }

        public void MostrarBotonCancelar()
        {
            btnCancelar.Visible = true;
            btnConfirmar.Visible = true;
        }

        // --- EVENT HANDLERS (Los "Disparadores" de la UI) ---
        // Estos métodos llaman al Manejador

        private void PantallaNuevaRevision_Load(object sender, EventArgs e)
        {
            // Oculta todo menos el botón de inicio
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl != btnIniciarCU)
                    ctrl.Visible = false;
            }
            btnIniciarCU.Left = (this.ClientSize.Width - btnIniciarCU.Width) / 2;
            btnIniciarCU.Top = (this.ClientSize.Height - btnIniciarCU.Height) / 2;
        }

        private void btnIniciarCU_Click(object sender, EventArgs e)
        {
            // Muestra todos los controles y oculta el botón de inicio
            foreach (Control ctrl in this.Controls)
                ctrl.Visible = true;

            btnIniciarCU.Visible = false;

            // Inicia el caso de uso
            opcionRegistrarResultadoRevisionManual();
        }

        public void opcionRegistrarResultadoRevisionManual()
        {
            Habilitar();
            // Llama al manejador para que inicie el flujo
            manejador.RegistrarNuevaRevision(this);
        }

        private void gridEventos_SelectionChanged(object sender, EventArgs e)
        {
            if (gridEventos.SelectedRows.Count > 0)
            {
                int index = gridEventos.SelectedRows[0].Index;
                // Informa al manejador la selección
                manejador.TomarSeleccionEvento(index, this);
            }
        }

        private void rbtnMapaNo_Click(object sender, EventArgs e)
        {
            manejador.TomarDecisionVisualizarMapa(false, this);
        }

        private void rbtnMapaSi_Click(object sender, EventArgs e)
        {
            manejador.TomarDecisionVisualizarMapa(true, this);
        }

        private void rbtnModAlcanceNo_Click(object sender, EventArgs e)
        {
            manejador.TomarOpcionModificacionAlcance(false, this);
        }

        private void rbtnModAlcanceSi_Click(object sender, EventArgs e)
        {
            manejador.TomarOpcionModificacionAlcance(true, this);
        }

        private void rbtnModMagnitudNo_Click(object sender, EventArgs e)
        {
            manejador.TomarOpcionModificacionMagnitud(false, this);
        }

        private void rbtnModMagnitudSi_Click(object sender, EventArgs e)
        {
            manejador.TomarOpcionModificacionMagnitud(true, this);
        }

        private void rbtnModOrigenNo_Click(object sender, EventArgs e)
        {
            manejador.TomarOpcionModificacionOrigen(false, this);
        }

        private void rbtnModOrigenSi_Click(object sender, EventArgs e)
        {
            manejador.TomarOpcionModificacionOrigen(true, this);
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            int opcion = cmbAccion.SelectedIndex + 1;
            // Valida que se haya seleccionado algo
            if (opcion > 0)
            {
                manejador.TomarOpcionAccion(opcion, this);
            }
            else
            {
                MostrarMensaje("Por favor, seleccione una acción.");
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            // Reinicia la pantalla y el manejador
            RestaurarEstadoInicial();
            manejador.RegistrarNuevaRevision(this);
        }

        private void RestaurarEstadoInicial()
        {
            grpMapa.Visible = false;
            rbtnMapaSi.Checked = false;
            rbtnMapaNo.Checked = false;

            grpModificarAlcance.Enabled = false;
            rbtnModAlcanceSi.Checked = false;
            rbtnModAlcanceNo.Checked = false;

            grpModificarMagnitud.Enabled = false;
            rbtnModMagnitudSi.Checked = false;
            rbtnModMagnitudNo.Checked = false;

            grpModificarOrigen.Enabled = false;
            rbtnModOrigenSi.Checked = false;
            rbtnModOrigenNo.Checked = false;

            cmbAccion.SelectedIndex = -1;
            cmbAccion.Enabled = false;

            btnConfirmar.Enabled = false;
            btnConfirmar.Visible = false;
            btnCancelar.Visible = false;

            txtDetalleEvento.Clear();
            txtSismograma.Clear();
            lblMapa.Text = "";

            if (picSismograma.Image != null)
            {
                var old = picSismograma.Image;
                picSismograma.Image = null;
                old.Dispose();
            }
            picSismograma.Visible = false;
            txtSismograma.Visible = true;

            gridEventos.DataSource = null;
            gridEventos.ClearSelection();
        }

        // --- Eventos vacíos (necesarios para el Designer) ---
        private void picSismograma_Click(object sender, EventArgs e) { }
        private void gridEventos_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}