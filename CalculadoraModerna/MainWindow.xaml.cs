using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace CalculadoraModerna
{
    public partial class MainWindow : Window
    {
        private string operador = "";
        private string ultimoOperador = "";
        private double resultado = 0;
        private double ultimoNumero = 0;
        private bool nuevaOperacion = false;

        // Lista para guardar el historial
        // Usar ObservableCollection para la lista de historial
        private const string HistorialKey = "Historial"; // Clave en el archivo App.config

        private List<string> historial = new List<string>();



        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainWindow()
        {
            InitializeComponent();
            // Suscribirse al evento Loaded para asegurar que el tema oscuro se aplique al cargar
            Loaded += MainWindow_Loaded;
            CargarHistorial();
            // Suscribirse a cambios del tema del sistema
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        }
        // Método para guardar el historial en el archivo de configuración
        private void GuardarHistorial()
        {
            // Convertir el historial a una cadena (separada por comas)
            string historialStr = string.Join(",", historial);
            // Guardar la cadena en el archivo de configuración
            ConfigurationManager.AppSettings[HistorialKey] = historialStr;
        }
        // Método para cargar el historial desde el archivo de configuración
        private void CargarHistorial()
        {
            string historialStr = ConfigurationManager.AppSettings[HistorialKey];
            if (!string.IsNullOrEmpty(historialStr))
            {
                // Convertir la cadena a una lista de historial
                historial = historialStr.Split(',').ToList();
                lstHistorial.ItemsSource = historial; // Mostrar en la interfaz
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyDarkMode(); // Aplicar tema oscuro una vez que la ventana esté completamente cargada
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                ApplyDarkMode();
            }
        }

        private void ApplyDarkMode()
        {
            bool isDarkMode = IsWindowsInDarkMode();

            // Obtener el handle de la ventana actual
            var hwnd = new WindowInteropHelper(this).Handle;
            int darkMode = isDarkMode ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Cambiar colores de fondo y texto de la ventana para que coincidan con el tema
            //Background = isDarkMode ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
            Foreground = isDarkMode ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
        }

        private bool IsWindowsInDarkMode()
        {
            // Leer el valor del registro para detectar si el sistema está en modo oscuro
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var registryValueObject = key.GetValue("AppsUseLightTheme");
                if (registryValueObject != null)
                {
                    int registryValue = (int)registryValueObject;
                    return registryValue == 0; // 0 significa modo oscuro activado
                }
            }
            return false; // Por defecto, modo claro
        }




        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Identificar si la tecla presionada es un número
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                int numero = e.Key - Key.D0;
                btnNumero_Click(new Button { Content = numero.ToString() }, null);
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                int numero = e.Key - Key.NumPad0;
                btnNumero_Click(new Button { Content = numero.ToString() }, null);
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Add:
                        btnOperador_Click(new Button { Content = "+" }, null);
                        break;
                    case Key.Subtract:
                        btnOperador_Click(new Button { Content = "-" }, null);
                        break;
                    case Key.Multiply:
                        btnOperador_Click(new Button { Content = "×" }, null);
                        break;
                    case Key.Divide:
                        btnOperador_Click(new Button { Content = "÷" }, null);
                        break;
                    case Key.Enter:
                        btnIgual_Click(null, null);
                        e.Handled = true; // Evita que otros controles manejen el evento
                        break;
                    case Key.Escape:
                        btnLimpiar_Click(null, null);
                        break;
                    case Key.Back:
                        // Borra el último carácter si el texto es mayor que 1 carácter, si no, establece en "0"
                        if (txtDisplay.Text.Length > 1)
                        {
                            txtDisplay.Text = txtDisplay.Text.Substring(0, txtDisplay.Text.Length - 1);
                        }
                        else
                        {
                            txtDisplay.Text = "0";
                        }
                        e.Handled = true; // Evita que el control TextBox maneje Backspace por defecto
                        break;
                    case Key.Decimal:
                        if (!txtDisplay.Text.Contains("."))
                            txtDisplay.Text += ".";
                        break;
                }
            }
        }




        private void btnNumero_Click(object sender, RoutedEventArgs e)
        {
            Button boton = (Button)sender;

            // Si es una nueva operación, reinicia el texto en el display
            if (nuevaOperacion)
            {
                txtDisplay.Text = "";
                lblPreview.Content = "";
                nuevaOperacion = false;
            }

            // Evitar agregar múltiples ceros antes del punto decimal o cualquier otro número
            if (txtDisplay.Text == "0" && boton.Content.ToString() != "0" && boton.Content.ToString() != ".")
            {
                txtDisplay.Text = boton.Content.ToString();
            }
            else
            {
                txtDisplay.Text += boton.Content.ToString();
            }

            // Asegurarse de que solo haya un punto decimal
            if (boton.Content.ToString() == "." && txtDisplay.Text.Count(c => c == '.') > 1)
            {
                txtDisplay.Text = txtDisplay.Text.Remove(txtDisplay.Text.Length - 1); // Eliminar el último punto decimal agregado
            }

            // Actualizar lblPreview solo si hay una operación en curso
            if (operador != "")
            {
                lblPreview.Content = $"{resultado} {operador} {txtDisplay.Text}";
            }
        }




        private void btnOperador_Click(object sender, RoutedEventArgs e)
        {
            Button boton = (Button)sender;

            if (!nuevaOperacion && operador != "")
            {
                // Si ya hay un operador, resolver la operación antes de continuar
                btnIgual_Click(sender, e);
            }

            operador = boton.Content.ToString();
            if (double.TryParse(txtDisplay.Text.Replace(",", ""), out resultado))
            {
                nuevaOperacion = true;
            }
            else
            {
                MessageBox.Show("Por favor, ingrese un número válido.");
            }

            // Actualizar lblPreview con el operador seleccionado
            lblPreview.Content = $"{resultado} {operador}";
        }

        // Método para el botón de cambio de signo (±)
        private void btnCambioSigno_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtDisplay.Text, out double numeroActual))
            {
                numeroActual = -numeroActual;
                txtDisplay.Text = numeroActual.ToString("N0");
            }
        }

        // Método para el botón de porcentaje (%)
        private void btnPorcentaje_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtDisplay.Text, out double numeroActual))
            {
                numeroActual = numeroActual / 100;
                txtDisplay.Text = numeroActual.ToString("N2");
            }
        }

        private void btnIgual_Click(object sender, RoutedEventArgs e)
        {
            double segundoNumero;

            // Intentar convertir el texto del display a un número decimal
            if (!double.TryParse(txtDisplay.Text, out segundoNumero))
            {
                txtDisplay.Text = "Error";
                return;
            }

            // Si operador está vacío, significa que el usuario ha presionado "=" varias veces.
            if (string.IsNullOrEmpty(operador))
            {
                if (string.IsNullOrEmpty(ultimoOperador))
                {
                    return;
                }
                operador = ultimoOperador;
                segundoNumero = ultimoNumero;
            }
            else
            {
                ultimoOperador = operador;
                ultimoNumero = segundoNumero;
            }

            // Guardar la operación en lblPreview antes de actualizar el resultado
            lblPreview.Content = $"{resultado} {operador} {segundoNumero} =";

            // Realizar el cálculo
            switch (operador)
            {
                case "+":
                    resultado += segundoNumero;
                    break;
                case "-":
                    resultado -= segundoNumero;
                    break;
                case "×":
                    resultado *= segundoNumero;
                    break;
                case "÷":
                    if (segundoNumero != 0)
                        resultado /= segundoNumero;
                    else
                    {
                        txtDisplay.Text = "Error";
                        return;
                    }
                    break;
            }

            // Mostrar el resultado formateado en el txtDisplay
            txtDisplay.Text = resultado % 1 == 0
                              ? resultado.ToString("N0") // Si es entero, sin decimales
                              : resultado.ToString("N2"); // Si es decimal, con 2 decimales

            operador = "";
            nuevaOperacion = true; // Permitir nueva operación

            // Agregar la operación y resultado al historial
            AddToHistory(lblPreview.Content.ToString(), txtDisplay.Text);
        }

        private void AddToHistory(string operacion, string resultado)
        {
            string historialItem = $"{operacion} = {resultado}";
            historial.Insert(0, historialItem); // Insertar al inicio de la lista para mostrar las operaciones más recientes arriba
            lstHistorial.ItemsSource = null; // Resetear la fuente de datos
            lstHistorial.ItemsSource = historial; // Vincular la lista actualizada al ListBox
            GuardarHistorial();
        }

        private void btnHistorial_Click(object sender, RoutedEventArgs e)
        {
            // Toggle visibility del historial
            if (pnlHistorial.Visibility == Visibility.Collapsed)
            {
                // Mostrar historial
                pnlHistorial.Visibility = Visibility.Visible;

                // Ajustar la altura de la ventana para mostrar el historial
                this.Height = 742; // Aquí puedes ajustar el valor según tus necesidades
            }
            else
            {
                // Ocultar historial
                pnlHistorial.Visibility = Visibility.Collapsed;

                // Ajustar la altura de la ventana de vuelta
                this.Height = 550; // Altura original
            }
        }

        private void lstHistorial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstHistorial.SelectedItem != null)
            {
                string operacion = lstHistorial.SelectedItem.ToString();
                string[] partes = operacion.Split('=');
                lblPreview.Content = partes[0].Trim(); // Mostrar la operación
                txtDisplay.Text = partes.Last();
                // Mostrar el resultado
            }
        }

        private void btnLimpiarHistorial_Click(object sender, RoutedEventArgs e)
        {
            historial.Clear();
            lstHistorial.ItemsSource = null;
            ConfigurationManager.AppSettings[HistorialKey] = ""; // Limpiar el historial en la configuración

        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            txtDisplay.Text = "0";
            resultado = 0;
            ultimoOperador = "";

            operador = "";
            nuevaOperacion = false;

            // Limpiar lblPreview también
            lblPreview.Content = "";
        }
    }
}
