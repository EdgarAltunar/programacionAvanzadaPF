using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace programacionAvanzadaPF
{
    public partial class Form1 : Form
    {
        private SerialPort SP;
        private Timer SDTimer;
        private MySqlConnection SQLConnection;
        private string SQLString = "Server=localhost;Database=elevador;Uid=root;Pwd=8j97d32sY_;";

        // Variables para almacenar datos del elevador
        private int pisoActual = 1;
        private float pesoActual = 0;
        private bool limiteExcedido = false;
        private bool gasDetectado = false;
        private bool obstaculoDetectado = false;
        private float lm35_1 = 0;
        private float lm35_2 = 0;
        private bool excedido_12 = false;
        private float lm35_3 = 0;
        private float lm35_4 = 0;
        private bool excedido_34 = false;
        private float lm35_5 = 0;
        private float lm35_6 = 0;
        private bool excedido_56 = false;
        private double x = 0.00;
        private readonly double limite = 3.00;
        public Form1()
        {
            InitializeComponent();
            InitializeSP();
            InitializeDB();
            InitializeTimer();
        }

        private void InitializeSP()
        {
            SP = new SerialPort("COM4", 9600); // Cambia COM4 según corresponda
            SP.DataReceived += SP_DataReceived;
            SP.Open();
        }

        private void InitializeDB()
        {
            try
            {
                SQLConnection = new MySqlConnection(SQLString);
                SQLConnection.Open();
                MessageBox.Show("Conexión a la base de datos exitosa.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar a la base de datos: {ex.Message}");
            }
        }

        private void InitializeTimer()
        {
            SDTimer = new Timer();
            SDTimer.Interval = 30000;
            SDTimer.Tick += SDTimer_Marca;
            SDTimer.Start();
        }

        private void SP_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            string data = SP.ReadLine();
            
            Invoke(new Action(() => ParseMostrarDatos(data)));
        }

        private void ParseMostrarDatos(string data)
        {
            string[] lines = data.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("PISO:"))
                {
                    pisoActual = int.Parse(line.Substring(5));
                    lblPiso.Text = pisoActual.ToString();
                }
                else if (line.StartsWith("PESO:"))
                {
                    pesoActual = float.Parse(line.Substring(5));
                    lblPeso.Text = pesoActual.ToString("0.00");
                    limiteExcedido = pesoActual > 100;
                }
                else if (line.StartsWith("GAS:"))
                {
                    gasDetectado = line.Substring(4).Trim() == "DETECTADO";
                    lblGas.Text = gasDetectado ? "Sí" : "No";
                }
                else if (line.StartsWith("OBSTACULO:"))
                {
                    obstaculoDetectado = line.Substring(10).Trim() == "DETECTADO";
                    lblObstaculo.Text = obstaculoDetectado ? "Sí" : "No";
                }
                else if (line.StartsWith("LM35_1:"))
                {
                    lm35_1 = float.Parse(line.Substring(7));
                    lblLM35_1.Text = lm35_1.ToString();

                }
                else if (line.StartsWith("LM35_2:"))
                {
                    lm35_2 = float.Parse(line.Substring(7));
                    lblLM35_2.Text = lm35_2.ToString();
                    x = Math.Abs(lm35_1 - lm35_2);
                    excedido_12 = x > limite;
                    lblAlerta1.Text = excedido_12 ? "Fuego" : "Normal";
                }
                else if (line.StartsWith("LM35_3:"))
                {
                    lm35_3 = float.Parse(line.Substring(7));
                    lblLM35_3.Text = lm35_3.ToString();
                }
                else if (line.StartsWith("LM35_4:"))
                {
                    lm35_4 = float.Parse(line.Substring(7));
                    lblLM35_4.Text = lm35_4.ToString();
                    x = Math.Abs(lm35_3 - lm35_4);
                    excedido_34 = x > limite;
                    lblAlerta2.Text = excedido_34 ? "Fuego" : "Normal";
                }
                else if (line.StartsWith("LM35_5:"))
                {
                    lm35_5 = float.Parse(line.Substring(7));
                    lblLM35_5.Text = lm35_5.ToString();
                }
                else if (line.StartsWith("LM35_6:"))
                {
                    lm35_6 = float.Parse(line.Substring(7));
                    lblLM35_6.Text = lm35_6.ToString();
                    x = Math.Abs(lm35_5 - lm35_6);
                    excedido_56 = x > limite;
                    lblAlerta3.Text = excedido_56 ? "Fuego" : "Normal";
                }

                // Guarda en la base de datos si se cumplen condiciones
                if (limiteExcedido || gasDetectado || obstaculoDetectado || excedido_12 || excedido_34 || excedido_56)
                {
                    SDaBaseDatos();
                }

            }
        }
        private void SDTimer_Marca(object sender, EventArgs e)
        {
            SDaBaseDatos();
        }

        private void SDaBaseDatos()
        {
            string query = "INSERT INTO datos_elevador (fecha_hora, piso_actual, peso, limite_excedido, gas_detectado, obstaculo_detectado, lm35_1, lm35_2, lm35_3, lm35_4, lm35_5, lm35_6) " +
                           "VALUES (@fecha_hora, @piso_actual, @peso, @limite_excedido, @gas_detectado, @obstaculo_detectado, @lm35_1, @lm35_2, @lm35_3, @lm35_4, @lm35_5, @lm35_6)";
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(query, SQLConnection))
                {
                    cmd.Parameters.AddWithValue("@fecha_hora", DateTime.Now);
                    cmd.Parameters.AddWithValue("@piso_actual", pisoActual);
                    cmd.Parameters.AddWithValue("@peso", pesoActual);
                    cmd.Parameters.AddWithValue("@limite_excedido", limiteExcedido);
                    cmd.Parameters.AddWithValue("@gas_detectado", gasDetectado);
                    cmd.Parameters.AddWithValue("@obstaculo_detectado", obstaculoDetectado);
                    cmd.Parameters.AddWithValue("@lm35_1", lm35_1);
                    cmd.Parameters.AddWithValue("@lm35_2", lm35_2);
                    cmd.Parameters.AddWithValue("@lm35_3", lm35_3);
                    cmd.Parameters.AddWithValue("@lm35_4", lm35_4);
                    cmd.Parameters.AddWithValue("@lm35_5", lm35_5);
                    cmd.Parameters.AddWithValue("@lm35_6", lm35_6);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar en la base de datos: {ex.Message}");
            }
        }

        private void btnPiso1_Click(object sender, EventArgs e)
        {
            SP.WriteLine("PISO:1\n");
        }

        private void btnPiso2_Click(object sender, EventArgs e)
        {
            SP.WriteLine("PISO:2\n");
        }

        private void btnPiso3_Click(object sender, EventArgs e)
        {
            SP.WriteLine("PISO:3\n");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SP.Close();
            SQLConnection.Close();
        }
    }
}
