// 1. Using LIMPIOS - Apuntan a nuestros proyectos
using RedSismica.App.Controladores;
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
        public PantallaNuevaRevision(ManejadorRegistrarRespuesta manejador)
        {
            InitializeComponent();
            this.manejador = manejador; // Simplemente lo asignamos
        }

        // --- MÉTODOS PÚBLICOS (La "Interfaz" de la Vista) ---

        public void Habilitar()
        {
            this.Enabled = true;
        }

        // --- MÉTODO NUEVO (Llamado por FinCU) ---
        public void ReiniciarVistaParaNuevoCU()
        {
            // Oculta todos los controles
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl != btnIniciarCU)
                    ctrl.Visible = false;
            }

            // Muestra solo el botón de inicio
            btnIniciarCU.Visible = true;
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

        public void MostrarSismograma(string rutaImagen)
        {
            try
            {
                txtSismograma.Visible = false;
                picSismograma.Visible = true;
                if (picSismograma.Image != null)
                {
                    var old = picSismograma.Image;
                    picSismograma.Image = null;
                    old.Dispose();
                }
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
            // --- CORRECCIÓN ---
            // 1. Ocultamos el botón de inicio
            btnIniciarCU.Visible = false;

            // 2. Mostramos SOLO los controles del primer paso
            // (La grilla y los textboxes de detalle/sismograma)
            gridEventos.Visible = true;
            txtDetalleEvento.Visible = true;
            txtSismograma.Visible = true;

            // (El 'grpMapa' y los demás permanecen ocultos)

            // 3. Iniciamos el flujo
            opcionRegistrarResultadoRevisionManual();
        }

        public void opcionRegistrarResultadoRevisionManual()
        {
            Habilitar();
            manejador.RegistrarNuevaRevision(this);
        }

        private void gridEventos_SelectionChanged(object sender, EventArgs e)
        {
            if (gridEventos.SelectedRows.Count > 0)
            {
                int index = gridEventos.SelectedRows[0].Index;
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
            // --- REQUISITO 2: Revertir el estado ---
            // Le decimos al manejador que revierta el evento
            // actualmente "Bloqueado" a "Autodetectado"
            manejador.CancelarRevisionActual();

            // Reinicia la pantalla a su estado visual inicial
            RestaurarEstadoInicial();

            // Vuelve a cargar la lista de eventos (que ahora
            // incluirá el evento que acabamos de revertir)
            manejador.RegistrarNuevaRevision(this);
        }

        public void RestaurarEstadoInicial()
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

        private void picSismograma_Click(object sender, EventArgs e) { }
        private void gridEventos_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}