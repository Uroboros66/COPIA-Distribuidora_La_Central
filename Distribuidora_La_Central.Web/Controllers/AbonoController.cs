using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbonoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public AbonoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllAbonos")]
        public string GetAbonos()
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Abono;", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<Abono> abonoList = new List<Abono>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    Abono abono = new Abono
                    {
                        idAbono = Convert.ToInt32(row["idAbono"]),
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        montoAbono = Convert.ToDecimal(row["montoAbono"]),
                        fechaAbono = Convert.ToDateTime(row["fechaAbono"])
                    };
                    abonoList.Add(abono);
                }
            }

            if (abonoList.Count > 0)
            {
                return JsonConvert.SerializeObject(abonoList);
            }
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No se encontraron abonos.";
                return JsonConvert.SerializeObject(response);
            }
        }

        [HttpPost("registrar-abono")]
        public IActionResult RegistrarAbono([FromBody] Abono abono)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Verificar que existe el crédito asociado a la factura
                        SqlCommand cmdGetCredito = new SqlCommand(
                            @"SELECT saldoMaximo, estado FROM Credito 
                      WHERE codigoFactura = @codigoFactura",
                            con, transaction);
                        cmdGetCredito.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);

                        decimal saldoActual = 0;
                        string estadoActual = "";

                        using (var reader = cmdGetCredito.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                transaction.Rollback();
                                return BadRequest("No se encontró crédito asociado a esta factura");
                            }

                            saldoActual = reader.GetDecimal(0);
                            estadoActual = reader.GetString(1);
                        }

                        // 2. Validaciones
                        if (estadoActual != "Activo")
                        {
                            transaction.Rollback();
                            return BadRequest("El crédito asociado no está activo");
                        }

                        if (abono.montoAbono > saldoActual)
                        {
                            transaction.Rollback();
                            return BadRequest("El monto del abono excede el saldo disponible");
                        }

                        // 3. Registrar el abono
                        SqlCommand cmdInsertAbono = new SqlCommand(
                            @"INSERT INTO Abono (codigoFactura, montoAbono, fechaAbono) 
                      VALUES (@codigoFactura, @montoAbono, @fechaAbono);
                      SELECT SCOPE_IDENTITY();", // Para obtener el ID generado
                            con, transaction);
                        cmdInsertAbono.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);
                        cmdInsertAbono.Parameters.AddWithValue("@montoAbono", abono.montoAbono);
                        cmdInsertAbono.Parameters.AddWithValue("@fechaAbono", abono.fechaAbono);

                        int idAbono = Convert.ToInt32(cmdInsertAbono.ExecuteScalar());


                        decimal nuevoSaldo = saldoActual - abono.montoAbono;
                        string nuevoEstado = nuevoSaldo <= 0 ? "Cancelado" : "Activo";

                        SqlCommand cmdUpdateCredito = new SqlCommand(
                            @"UPDATE Credito 
                      SET saldoMaximo = @nuevoSaldo,
                          estado = @nuevoEstado
                      WHERE codigoFactura = @codigoFactura",
                            con, transaction);
                        cmdUpdateCredito.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                        cmdUpdateCredito.Parameters.AddWithValue("@nuevoEstado", nuevoEstado);
                        cmdUpdateCredito.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);
                        cmdUpdateCredito.ExecuteNonQuery();

                        transaction.Commit();

                        return Ok(new
                        {
                            success = true,
                            message = "Abono registrado exitosamente",
                            idAbono,
                            nuevoSaldo,
                            nuevoEstado
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error al registrar el abono: {ex.Message}");
                    }
                }
            }
        }
        [HttpGet("GetByFactura/{codigoFactura}")]
        public string GetAbonosPorFactura(int codigoFactura)
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new SqlDataAdapter(
                "SELECT * FROM Abono WHERE codigoFactura = @codigoFactura",
                con);
            da.SelectCommand.Parameters.AddWithValue("@codigoFactura", codigoFactura);

            DataTable dt = new DataTable();
            da.Fill(dt);

            List<Abono> abonoList = new List<Abono>();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    Abono abono = new Abono
                    {
                        idAbono = Convert.ToInt32(row["idAbono"]),
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        montoAbono = Convert.ToDecimal(row["montoAbono"]),
                        fechaAbono = Convert.ToDateTime(row["fechaAbono"])
                    };
                    abonoList.Add(abono);
                }
            }

            return JsonConvert.SerializeObject(abonoList);
        }

        [HttpPost("Registrar")]
        public IActionResult RegistrarNuevoAbono([FromBody] Abono abono)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlTransaction transaccion = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Verificar crédito asociado
                        var cmdVerificarCredito = new SqlCommand(
                            @"SELECT saldoMaximo, estado FROM Credito 
                              WHERE codigoFactura = @codigoFactura",
                            con, transaccion);
                        cmdVerificarCredito.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);

                        using (var lector = cmdVerificarCredito.ExecuteReader())
                        {
                            if (!lector.Read())
                            {
                                transaccion.Rollback();
                                return BadRequest("No existe crédito asociado a esta factura");
                            }

                            decimal saldoActual = lector.GetDecimal(0);
                            string estadoActual = lector.GetString(1);
                            lector.Close();

                            // 2. Validaciones
                            if (estadoActual != "Activo")
                                return BadRequest("El crédito no está activo");

                            if (abono.montoAbono > saldoActual)
                                return BadRequest("Monto excede el saldo disponible");

                            // 3. Registrar abono
                            var cmdRegistrarAbono = new SqlCommand(
                                @"INSERT INTO Abono (codigoFactura, montoAbono, fechaAbono) 
                                  VALUES (@codigoFactura, @montoAbono, @fechaAbono);
                                  SELECT SCOPE_IDENTITY();",
                                con, transaccion);

                            cmdRegistrarAbono.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);
                            cmdRegistrarAbono.Parameters.AddWithValue("@montoAbono", abono.montoAbono);
                            cmdRegistrarAbono.Parameters.AddWithValue("@fechaAbono", abono.fechaAbono);

                            int idAbono = Convert.ToInt32(cmdRegistrarAbono.ExecuteScalar());

                            // 4. Actualizar crédito
                            decimal nuevoSaldo = saldoActual - abono.montoAbono;
                            string nuevoEstado = nuevoSaldo <= 0 ? "Cancelado" : "Activo";

                            var cmdActualizarCredito = new SqlCommand(
                                @"UPDATE Credito 
                                  SET saldoMaximo = @nuevoSaldo,
                                      estado = @nuevoEstado
                                  WHERE codigoFactura = @codigoFactura",
                                con, transaccion);

                            cmdActualizarCredito.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                            cmdActualizarCredito.Parameters.AddWithValue("@nuevoEstado", nuevoEstado);
                            cmdActualizarCredito.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);
                            cmdActualizarCredito.ExecuteNonQuery();

                            transaccion.Commit();

                            return Ok(new
                            {
                                Exito = true,
                                Mensaje = "Abono registrado correctamente",
                                IdAbono = idAbono,
                                NuevoSaldo = nuevoSaldo,
                                EstadoActual = nuevoEstado
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        return StatusCode(500, $"Error al procesar el abono: {ex.Message}");
                    }
                }
            }
        }



        [HttpPut("actualizar-abono/{idAbono}")]
        public IActionResult ActualizarAbono(int idAbono, [FromBody] Abono abono)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            SqlDataAdapter checkAbono = new SqlDataAdapter("SELECT * FROM Abono WHERE idAbono = @idAbono", con);
            checkAbono.SelectCommand.Parameters.AddWithValue("@idAbono", idAbono);
            DataTable dt = new DataTable();
            checkAbono.Fill(dt);

            if (dt.Rows.Count == 0)
                return NotFound("No se encontró el abono a actualizar.");

            SqlCommand cmd = new SqlCommand(@"UPDATE Abono 
                                               SET codigoFactura = @codigoFactura,
                                                   montoAbono = @montoAbono,
                                                   fechaAbono = @fechaAbono
                                             WHERE idAbono = @idAbono", con);

            cmd.Parameters.AddWithValue("@codigoFactura", abono.codigoFactura);
            cmd.Parameters.AddWithValue("@montoAbono", abono.montoAbono);
            cmd.Parameters.AddWithValue("@fechaAbono", abono.fechaAbono);
            cmd.Parameters.AddWithValue("@idAbono", idAbono);

            con.Open();
            int i = cmd.ExecuteNonQuery();
            con.Close();

            if (i > 0)
                return Ok("Abono actualizado exitosamente.");
            else
                return StatusCode(500, "Error al actualizar el abono.");
        }

        [HttpDelete("eliminar-abono/{idAbono}")]
        public IActionResult EliminarAbono(int idAbono)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            SqlDataAdapter checkAbono = new SqlDataAdapter("SELECT * FROM Abono WHERE idAbono = @idAbono", con);
            checkAbono.SelectCommand.Parameters.AddWithValue("@idAbono", idAbono);
            DataTable dt = new DataTable();
            checkAbono.Fill(dt);

            if (dt.Rows.Count == 0)
                return NotFound("No se encontró el abono a eliminar.");

            SqlCommand cmd = new SqlCommand("DELETE FROM Abono WHERE idAbono = @idAbono", con);
            cmd.Parameters.AddWithValue("@idAbono", idAbono);

            con.Open();
            int i = cmd.ExecuteNonQuery();
            con.Close();

            if (i > 0)
                return Ok("Abono eliminado exitosamente.");
            else
                return StatusCode(500, "Error al eliminar el abono.");
        }


        [HttpGet]
        [Route("DescargarReporteAbonos")]
        public IActionResult DescargarReporteAbonos()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta con los campos necesarios
                string query = @"SELECT 
                a.idAbono, 
                a.codigoFactura, 
                c.nombre + ' ' + c.apellido as Cliente,
                a.montoAbono, 
                a.fechaAbono,
                cr.saldoMaximo as SaldoRestante
            FROM Abono a
            INNER JOIN Factura f ON a.codigoFactura = f.codigoFactura
            INNER JOIN Cliente c ON f.codigoCliente = c.codigoCliente
            INNER JOIN Credito cr ON a.codigoFactura = cr.codigoFactura";

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

                    // Título
                    document.Add(new Paragraph("Reporte de Abonos", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 6 columnas
                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1f, 1.5f, 3f, 2f, 2f, 2f };
                    table.SetWidths(columnWidths);

                    // Encabezados
                    AddHeaderCell(table, "ID Abono", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Factura", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cliente", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Monto", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Saldo Restante", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos y formato
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["idAbono"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["codigoFactura"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["Cliente"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["montoAbono"]).ToString("C2"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fechaAbono"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["SaldoRestante"]).ToString("C2"), fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Reporte_Abonos.pdf");
                }
            }
        }

        // Método auxiliar para celdas de encabezado (el mismo que ya tienes)
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