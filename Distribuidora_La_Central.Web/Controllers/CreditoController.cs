using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreditoController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CreditoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("GetCreditosPorCliente")]
        public IActionResult GetCreditosPorCliente()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = @"
                SELECT 
                    c.idCredito, 
                    c.codigoFactura,
                    cli.codigoCliente, 
                    cli.nombre as nombreCliente, 
                    c.saldoMaximo as saldoActual, 
                    c.fechaFinal as fechaVencimiento,
                    c.estado
                FROM Credito c
                JOIN Factura f ON c.codigoFactura = f.codigoFactura
                JOIN Cliente cli ON f.codigoCliente = cli.codigoCliente
                WHERE c.estado = 'Activo'";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    var creditos = new List<CreditoClienteView>();

                    foreach (DataRow row in dt.Rows)
                    {
                        creditos.Add(new CreditoClienteView
                        {
                            idCredito = Convert.ToInt32(row["idCredito"]),
                            codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                            codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                            nombreCliente = row["nombreCliente"].ToString(),
                            saldoActual = Convert.ToDecimal(row["saldoActual"]),
                            fechaVencimiento = Convert.ToDateTime(row["fechaVencimiento"]),
                            estado = row["estado"].ToString()
                        });
                    }

                    return Ok(creditos);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
        public class CreditoClienteView
        {
            public int idCredito { get; set; }
            public int codigoFactura { get; set; }  // <- Añadir esta propiedad
            public int codigoCliente { get; set; }
            public string nombreCliente { get; set; }
            public decimal saldoActual { get; set; }
            public DateTime fechaVencimiento { get; set; }
            public string estado { get; set; }
        }

        [HttpPost("CrearCreditoConFactura")]
        public IActionResult CrearCreditoConFactura([FromBody] NuevoCreditoConFacturaDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Verificar que el cliente existe
                        string verificarClienteQuery = "SELECT COUNT(1) FROM Cliente WHERE codigoCliente = @codigoCliente";
                        using (SqlCommand verificarCmd = new SqlCommand(verificarClienteQuery, con, transaction))
                        {
                            verificarCmd.Parameters.AddWithValue("@codigoCliente", request.codigoCliente);
                            int existe = (int)verificarCmd.ExecuteScalar();

                            if (existe == 0)
                            {
                                transaction.Rollback();
                                return NotFound($"Cliente con código {request.codigoCliente} no encontrado");
                            }
                        }

                        // 2. Insertar la nueva factura
                        string insertarFacturaQuery = @"
                    INSERT INTO Factura (
                        codigoCliente, 
                        fecha, 
                        totalFactura, 
                        saldo, 
                        tipo
                    ) 
                    VALUES (
                        @codigoCliente, 
                        @fecha, 
                        @totalFactura, 
                        @saldo, 
                        @tipo
                    );
                    SELECT SCOPE_IDENTITY();";

                        int codigoFactura;
                        using (SqlCommand facturaCmd = new SqlCommand(insertarFacturaQuery, con, transaction))
                        {
                            facturaCmd.Parameters.AddWithValue("@codigoCliente", request.codigoCliente);
                            facturaCmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                            facturaCmd.Parameters.AddWithValue("@totalFactura", request.totalFactura);
                            facturaCmd.Parameters.AddWithValue("@saldo", request.totalFactura); // El saldo inicial es igual al total
                            facturaCmd.Parameters.AddWithValue("@tipo", request.tipoFactura ?? "CREDITO");

                            codigoFactura = Convert.ToInt32(facturaCmd.ExecuteScalar());
                        }

                        // 3. Insertar el nuevo crédito asociado a la factura
                        string insertarCreditoQuery = @"
                    INSERT INTO Credito (
                        codigoFactura, 
                        saldoMaximo, 
                        fechaInicio, 
                        fechaFinal, 
                        estado
                    ) 
                    VALUES (
                        @codigoFactura, 
                        @saldoMaximo, 
                        @fechaInicio, 
                        @fechaFinal, 
                        'Activo'
                    );
                    SELECT SCOPE_IDENTITY();";

                        int idCredito;
                        using (SqlCommand creditoCmd = new SqlCommand(insertarCreditoQuery, con, transaction))
                        {
                            creditoCmd.Parameters.AddWithValue("@codigoFactura", codigoFactura);
                            creditoCmd.Parameters.AddWithValue("@saldoMaximo", request.saldoMaximo);
                            creditoCmd.Parameters.AddWithValue("@fechaInicio", DateTime.Now);
                            creditoCmd.Parameters.AddWithValue("@fechaFinal", request.fechaVencimiento);

                            idCredito = Convert.ToInt32(creditoCmd.ExecuteScalar());
                        }

                        transaction.Commit();

                        // Retornar la respuesta con los datos creados
                        return Ok(new
                        {
                            idCredito = idCredito,
                            codigoFactura = codigoFactura,
                            codigoCliente = request.codigoCliente,
                            saldoMaximo = request.saldoMaximo,
                            fechaVencimiento = request.fechaVencimiento,
                            totalFactura = request.totalFactura,
                            tipoFactura = request.tipoFactura ?? "CREDITO",
                            estado = "Activo"
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error al crear el crédito y factura: {ex.Message}");
                    }
                }
            }
        }

        // DTO para la solicitud
        public class NuevoCreditoConFacturaDto
        {
            public int codigoCliente { get; set; }
            public decimal totalFactura { get; set; }
            public decimal saldoMaximo { get; set; }
            public DateTime fechaVencimiento { get; set; }
            public string? tipoFactura { get; set; } // Opcional, por defecto será "CREDITO"
        }

        [HttpGet("GetFacturasConCreditoActivo")]
        public IActionResult GetFacturasConCreditoActivo()
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"
        SELECT f.codigoFactura, f.codigoCliente, f.fecha, f.totalFactura, 
               c.saldoMaximo as saldo, c.estado
        FROM Factura f
        JOIN Credito c ON f.codigoFactura = c.codigoFactura
        WHERE c.estado = 'Activo'";

            SqlDataAdapter da = new SqlDataAdapter(query, con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            List<FacturaConCredito> facturas = new List<FacturaConCredito>();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    FacturaConCredito factura = new FacturaConCredito
                    {
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                        fecha = Convert.ToDateTime(row["fecha"]),
                        totalFactura = Convert.ToDecimal(row["totalFactura"]),
                        saldo = Convert.ToDecimal(row["saldo"]), // Usamos saldoMaximo como saldo
                        estadoCredito = row["estado"].ToString()
                    };
                    facturas.Add(factura);
                }
            }

            return Ok(facturas);
        }

        // Modelo adicional para la respuesta
        public class FacturaConCredito
        {
            public int codigoFactura { get; set; }
            public int codigoCliente { get; set; }
            public DateTime fecha { get; set; }
            public decimal totalFactura { get; set; }
            public decimal saldo { get; set; }
            public string estadoCredito { get; set; }
        }
        // GET: api/Credito/GetAllCreditos
        [HttpGet("GetAllCreditos")]
        public async Task<IActionResult> GetAllCreditos()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Credito";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();

                await Task.Run(() => da.Fill(dt));

                var creditos = new System.Collections.Generic.List<Credito>();

                foreach (DataRow row in dt.Rows)
                {
                    creditos.Add(new Credito
                    {
                        idCredito = Convert.ToInt32(row["idCredito"]),
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        fechaInicial = Convert.ToDateTime(row["fechaInicial"]),
                        fechaFinal = Convert.ToDateTime(row["fechaFinal"]),
                        saldoMaximo = Convert.ToDecimal(row["saldoMaximo"]),
                        estado = row["estado"].ToString()
                    });
                }

                return Ok(creditos);
            }
        }

        // GET: api/Credito/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCredito(int id)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Credito WHERE idCredito = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                await con.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    var credito = new Credito
                    {
                        idCredito = Convert.ToInt32(reader["idCredito"]),
                        codigoFactura = Convert.ToInt32(reader["codigoFactura"]),
                        fechaInicial = Convert.ToDateTime(reader["fechaInicial"]),
                        fechaFinal = Convert.ToDateTime(reader["fechaFinal"]),
                        saldoMaximo = Convert.ToDecimal(reader["saldoMaximo"]),
                        estado = reader["estado"].ToString()
                    };

                    return Ok(credito);
                }

                return NotFound();
            }
        }

        // POST: api/Credito
        [HttpPost]
        public async Task<IActionResult> CreateCredito([FromBody] Credito credito)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"INSERT INTO Credito 
                                (codigoFactura, fechaInicial, fechaFinal, saldoMaximo, estado) 
                                VALUES (@CodigoFactura, @FechaInicial, @FechaFinal, @SaldoMaximo, @Estado);
                                SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CodigoFactura", credito.codigoFactura);
                cmd.Parameters.AddWithValue("@FechaInicial", credito.fechaInicial);
                cmd.Parameters.AddWithValue("@FechaFinal", credito.fechaFinal);
                cmd.Parameters.AddWithValue("@SaldoMaximo", credito.saldoMaximo);
                cmd.Parameters.AddWithValue("@Estado", credito.estado ?? "Activo");

                await con.OpenAsync();
                int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                credito.idCredito = newId;
                return CreatedAtAction(nameof(GetCredito), new { id = newId }, credito);
            }
        }

        // PUT: api/Credito/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCredito(int id, [FromBody] Credito credito)
        {
            if (id != credito.idCredito)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"UPDATE Credito SET 
                                codigoFactura = @CodigoFactura, 
                                fechaInicial = @FechaInicial, 
                                fechaFinal = @FechaFinal, 
                                saldoMaximo = @SaldoMaximo, 
                                estado = @Estado 
                                WHERE idCredito = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@CodigoFactura", credito.codigoFactura);
                cmd.Parameters.AddWithValue("@FechaInicial", credito.fechaInicial);
                cmd.Parameters.AddWithValue("@FechaFinal", credito.fechaFinal);
                cmd.Parameters.AddWithValue("@SaldoMaximo", credito.saldoMaximo);
                cmd.Parameters.AddWithValue("@Estado", credito.estado);

                await con.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
        }

        // DELETE: api/Credito/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCredito(int id)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "DELETE FROM Credito WHERE idCredito = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                await con.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
        }

        // GET: api/Credito/GetCreditosActivos
        [HttpGet("GetCreditosActivos")]
        public async Task<IActionResult> GetCreditosActivos()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Credito WHERE estado = 'Activo'";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();

                await Task.Run(() => da.Fill(dt));

                var creditos = new System.Collections.Generic.List<Credito>();

                foreach (DataRow row in dt.Rows)
                {
                    creditos.Add(new Credito
                    {
                        idCredito = Convert.ToInt32(row["idCredito"]),
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        fechaInicial = Convert.ToDateTime(row["fechaInicial"]),
                        fechaFinal = Convert.ToDateTime(row["fechaFinal"]),
                        saldoMaximo = Convert.ToDecimal(row["saldoMaximo"]),
                        estado = row["estado"].ToString()
                    });
                }

                return Ok(creditos);
            }
        }

        // GET: api/Credito/GetCreditosPorFactura/5
        [HttpGet("GetCreditosPorFactura/{facturaId}")]
        public async Task<IActionResult> GetCreditosPorFactura(int facturaId)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Credito WHERE codigoFactura = @FacturaId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FacturaId", facturaId);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                await Task.Run(() => da.Fill(dt));

                var creditos = new System.Collections.Generic.List<Credito>();

                foreach (DataRow row in dt.Rows)
                {
                    creditos.Add(new Credito
                    {
                        idCredito = Convert.ToInt32(row["idCredito"]),
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        fechaInicial = Convert.ToDateTime(row["fechaInicial"]),
                        fechaFinal = Convert.ToDateTime(row["fechaFinal"]),
                        saldoMaximo = Convert.ToDecimal(row["saldoMaximo"]),
                        estado = row["estado"].ToString()
                    });
                }

                return Ok(creditos);
            }
        }

        [HttpGet]
        [Route("DescargarReporteCreditos")]
        public IActionResult DescargarReporteCreditos()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta corregida - calculamos el saldo actual como saldoMaximo menos abonos
                string query = @"SELECT 
                c.idCredito,
                c.codigoFactura,
                cl.nombre + ' ' + cl.apellido AS cliente,
                c.fechaInicial,
                c.fechaFinal,
                c.saldoMaximo,
                (c.saldoMaximo - ISNULL((SELECT SUM(a.montoAbono) 
                                       FROM Abono a 
                                       WHERE a.codigoFactura = c.codigoFactura), 0)) AS saldoActual,
                c.estado
            FROM Credito c
            INNER JOIN Factura f ON c.codigoFactura = f.codigoFactura
            INNER JOIN Cliente cl ON f.codigoCliente = cl.codigoCliente";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                using (var stream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4.Rotate()); // Horizontal
                    PdfWriter.GetInstance(document, stream).CloseStream = false;
                    document.Open();

                    // Fuentes
                    var fontTitle = FontFactory.GetFont("Arial", 18, Font.BOLD);
                    var fontHeader = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
                    var fontCell = FontFactory.GetFont("Arial", 9);
                    var fontCellRed = FontFactory.GetFont("Arial", 9, Font.NORMAL, BaseColor.RED);
                    var fontCellGreen = FontFactory.GetFont("Arial", 9, Font.NORMAL, BaseColor.GREEN);

                    // Título
                    document.Add(new Paragraph("Reporte de Créditos", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 8 columnas
                    PdfPTable table = new PdfPTable(8);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1f, 1.5f, 3f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f };
                    table.SetWidths(columnWidths);

                    // Encabezados
                    AddHeaderCell(table, "ID Crédito", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Factura", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cliente", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha Inicio", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha Vencimiento", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Límite Crédito", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Saldo Actual", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Estado", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos y formato
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["idCredito"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["codigoFactura"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["cliente"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fechaInicial"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fechaFinal"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["saldoMaximo"]).ToString("C2"), fontCell));

                        // Saldo actual calculado
                        decimal saldoActual = Convert.ToDecimal(row["saldoActual"]);
                        table.AddCell(new Phrase(saldoActual.ToString("C2"),
                            saldoActual < 0 ? fontCellRed : fontCell));

                        // Resaltar estado
                        string estado = row["estado"]?.ToString() ?? "";
                        table.AddCell(new Phrase(estado,
                            estado == "Activo" ? fontCellGreen : fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Reporte_Creditos.pdf");
                }
            }
        }

        // Método auxiliar para celdas de encabezado
        private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            table.AddCell(cell);
        }



    }
}