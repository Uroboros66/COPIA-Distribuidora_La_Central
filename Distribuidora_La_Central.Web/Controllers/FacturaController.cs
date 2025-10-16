using Distribuidora_La_Central.Web.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;


namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturaController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FacturaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("GetAllFacturas")]
        public IActionResult GetAllFacturas()
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            // Asegúrate de incluir el campo 'estado' en la consulta
            SqlDataAdapter da = new SqlDataAdapter("SELECT codigoFactura, codigoCliente, fecha, totalFactura, saldo, tipo, estado FROM Factura", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<Factura> facturas = new List<Factura>();

            foreach (DataRow row in dt.Rows)
            {
                facturas.Add(new Factura
                {
                    codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                    codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                    fecha = Convert.ToDateTime(row["fecha"]),
                    totalFactura = Convert.ToDecimal(row["totalFactura"]),
                    saldo = Convert.ToDecimal(row["saldo"]),
                    tipo = row["tipo"].ToString(),
                    estado = row["estado"]?.ToString() // Asegúrate que tu modelo Factura tenga esta propiedad
                });
            }

            return Ok(facturas);
        }
        [HttpGet("GetFacturaPorCodigo/{codigo}")]
        public IActionResult GetFacturaPorCodigo(int codigo)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Factura WHERE codigoFactura = @codigo", con);
            da.SelectCommand.Parameters.AddWithValue("@codigo", codigo);

            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count == 0)
                return NotFound();

            DataRow row = dt.Rows[0];

            var factura = new Factura
            {
                codigoFactura = Convert.ToInt32(row["codigoFactura"]), // Corregido el typo "codigoFactura"
                codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                fecha = Convert.ToDateTime(row["fecha"]),
                totalFactura = Convert.ToDecimal(row["totalFactura"]),
                saldo = Convert.ToDecimal(row["saldo"]),
                tipo = row["tipo"].ToString(),
                estado = row["estado"]?.ToString()
            };

            return Ok(factura);
        }




        [HttpPost("AgregarFactura")]
        public IActionResult AgregarFactura([FromBody] FacturaConDetalles facturaConDetalles)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();
                using SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Validar stock antes de procesar la factura
                    foreach (var detalle in facturaConDetalles.Detalles)
                    {
                        if (!ValidarStockDisponible(con, transaction, detalle.codigoProducto, detalle.cantidad))
                        {
                            transaction.Rollback();
                            return BadRequest(new { error = $"Stock insuficiente para el producto {detalle.codigoProducto}" });
                        }
                    }

                    // 2. Insertar la factura
                    int codigoFactura = InsertarFactura(con, transaction, facturaConDetalles.Factura);

                    // 3. Insertar detalles y actualizar inventario
                    foreach (var detalle in facturaConDetalles.Detalles)
                    {
                        // Calcular subtotal
                        detalle.subtotal = detalle.cantidad * detalle.precioUnitario;

                        InsertarDetalleFactura(con, transaction, codigoFactura, detalle);
                        ActualizarInventario(con, transaction, detalle.codigoProducto, detalle.cantidad);
                    }

                    // 4. Si es crédito, registrar en tabla de créditos
                    if (facturaConDetalles.Factura.tipo == "Crédito")
                    {
                        RegistrarCredito(con, transaction, codigoFactura, facturaConDetalles.Factura);
                    }

                    transaction.Commit();
                    return Ok(new { codigoFactura, message = "Factura registrada exitosamente" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new { error = "Error al registrar la factura", details = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error de conexión", details = ex.Message });
            }
        }

        #region Métodos Auxiliares

        private bool ValidarStockDisponible(SqlConnection con, SqlTransaction transaction, int codigoProducto, int cantidadRequerida)
        {
            string query = "SELECT cantidad FROM Producto WHERE codigoProducto = @codigoProducto";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoProducto", codigoProducto);

            int stockActual = Convert.ToInt32(cmd.ExecuteScalar());
            return stockActual >= cantidadRequerida;
        }

        private int InsertarFactura(SqlConnection con, SqlTransaction transaction, Factura factura)
        {
            string query = @"INSERT INTO Factura 
                          (codigoCliente, fecha, totalFactura, saldo, tipo, estado)
                          OUTPUT INSERTED.codigoFactura
                          VALUES (@codigoCliente, @fecha, @totalFactura, @saldo, @tipo, @estado)";

            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoCliente", factura.codigoCliente);
            cmd.Parameters.AddWithValue("@fecha", factura.fecha);
            cmd.Parameters.AddWithValue("@totalFactura", factura.totalFactura);

            decimal saldoInicial = factura.tipo == "Contado" ? 0 : factura.totalFactura;
            cmd.Parameters.AddWithValue("@saldo", saldoInicial);

            cmd.Parameters.AddWithValue("@tipo", factura.tipo);
            cmd.Parameters.AddWithValue("@estado", factura.tipo == "Contado" ? "Pagado" : "Pendiente");

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void InsertarDetalleFactura(SqlConnection con, SqlTransaction transaction, int codigoFactura, DetalleFactura detalle)
        {
            string query = @"INSERT INTO DetalleFactura 
                         (codigoFactura, codigoProducto, cantidad, precioUnitario, subtotal)
                         VALUES (@codigoFactura, @codigoProducto, @cantidad, @precioUnitario, @subtotal)";

            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoFactura", codigoFactura);
            cmd.Parameters.AddWithValue("@codigoProducto", detalle.codigoProducto);
            cmd.Parameters.AddWithValue("@cantidad", detalle.cantidad);
            cmd.Parameters.AddWithValue("@precioUnitario", detalle.precioUnitario);
            cmd.Parameters.AddWithValue("@subtotal", detalle.subtotal);

            cmd.ExecuteNonQuery();
        }

        private void ActualizarInventario(SqlConnection con, SqlTransaction transaction, int codigoProducto, int cantidadVendida)
        {
            string query = "UPDATE Producto SET cantidad = cantidad - @cantidad WHERE codigoProducto = @codigoProducto";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoProducto", codigoProducto);
            cmd.Parameters.AddWithValue("@cantidad", cantidadVendida);
            cmd.ExecuteNonQuery();
        }

        private void RegistrarCredito(SqlConnection con, SqlTransaction transaction, int codigoFactura, Factura factura)
        {
            DateTime fechaVencimiento = factura.fecha.AddDays(30);

            string query = @"INSERT INTO Credito 
                         (codigoFactura, fechaInicial, fechaFinal, saldoMaximo, estado)
                         VALUES (@codigoFactura, @fechaInicial, @fechaFinal, @saldoMaximo, @estado)";

            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoFactura", codigoFactura);
            cmd.Parameters.AddWithValue("@fechaInicial", factura.fecha);
            cmd.Parameters.AddWithValue("@fechaFinal", fechaVencimiento);
            cmd.Parameters.AddWithValue("@saldoMaximo", factura.totalFactura);
            cmd.Parameters.AddWithValue("@estado", "Activo");
            cmd.ExecuteNonQuery();
        }
        #endregion

        // En el controlador FacturaController.cs
        [HttpGet("GetFacturasFiltradas")]
        public IActionResult GetFacturasFiltradas(
            [FromQuery] int? codigoCliente = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] decimal? montoMinimo = null,
            [FromQuery] decimal? montoMaximo = null,
            [FromQuery] string tipo = null,
            [FromQuery] string estado = null,
            [FromQuery] bool? pendientes = null)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            string query = "SELECT * FROM Factura WHERE 1=1";
            var parameters = new List<SqlParameter>();

            if (codigoCliente.HasValue)
            {
                query += " AND codigoCliente = @codigoCliente";
                parameters.Add(new SqlParameter("@codigoCliente", codigoCliente));
            }

            if (fechaDesde.HasValue)
            {
                query += " AND fecha >= @fechaDesde";
                parameters.Add(new SqlParameter("@fechaDesde", fechaDesde));
            }

            if (fechaHasta.HasValue)
            {
                query += " AND fecha <= @fechaHasta";
                parameters.Add(new SqlParameter("@fechaHasta", fechaHasta));
            }

            if (montoMinimo.HasValue)
            {
                query += " AND totalFactura >= @montoMinimo";
                parameters.Add(new SqlParameter("@montoMinimo", montoMinimo));
            }

            if (montoMaximo.HasValue)
            {
                query += " AND totalFactura <= @montoMaximo";
                parameters.Add(new SqlParameter("@montoMaximo", montoMaximo));
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                query += " AND tipo = @tipo";
                parameters.Add(new SqlParameter("@tipo", tipo));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                query += " AND estado = @estado";
                parameters.Add(new SqlParameter("@estado", estado));
            }

            if (pendientes.HasValue && pendientes.Value)
            {
                query += " AND saldo > 0";
            }

            SqlDataAdapter da = new SqlDataAdapter(query, con);
            da.SelectCommand.Parameters.AddRange(parameters.ToArray());

            DataTable dt = new DataTable();
            da.Fill(dt);

            List<Factura> facturas = new List<Factura>();
            foreach (DataRow row in dt.Rows)
            {
                facturas.Add(new Factura
                {
                    codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                    codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                    fecha = Convert.ToDateTime(row["fecha"]),
                    totalFactura = Convert.ToDecimal(row["totalFactura"]),
                    saldo = Convert.ToDecimal(row["saldo"]),
                    tipo = row["tipo"].ToString(),
                    estado = row["estado"]?.ToString()
                });
            }

            return Ok(facturas);
        }

        [HttpDelete("EliminarFactura/{id}")]
        public IActionResult EliminarFactura(int id)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            con.Open();
            using SqlTransaction transaction = con.BeginTransaction();

            try
            {
                // 1. Obtener los detalles de la factura para devolver los productos al inventario
                List<DetalleFactura> detalles = ObtenerDetallesFactura(con, transaction, id);

                // 2. Devolver los productos al inventario
                foreach (var detalle in detalles)
                {
                    DevolverProductoAlInventario(con, transaction, detalle.codigoProducto, detalle.cantidad);
                }

                // 3. Eliminar los detalles de la factura
                EliminarDetallesFactura(con, transaction, id);

                // 4. Si es crédito, eliminar el registro de crédito asociado
                EliminarCreditoAsociado(con, transaction, id);

                // 5. Finalmente, eliminar la factura
                int filasAfectadas = EliminarFacturaPrincipal(con, transaction, id);

                if (filasAfectadas > 0)
                {
                    transaction.Commit();
                    return Ok(new { success = true, message = "Factura eliminada correctamente" });
                }
                else
                {
                    transaction.Rollback();
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { success = false, message = "Error al eliminar la factura", error = ex.Message });
            }
        }

        #region Métodos auxiliares para EliminarFactura

        private List<DetalleFactura> ObtenerDetallesFactura(SqlConnection con, SqlTransaction transaction, int idFactura)
        {
            string query = "SELECT codigoProducto, cantidad FROM DetalleFactura WHERE codigoFactura = @idFactura";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@idFactura", idFactura);

            var detalles = new List<DetalleFactura>();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                detalles.Add(new DetalleFactura
                {
                    codigoProducto = reader.GetInt32(0),
                    cantidad = reader.GetInt32(1)
                });
            }
            return detalles;
        }

        private void DevolverProductoAlInventario(SqlConnection con, SqlTransaction transaction, int codigoProducto, int cantidad)
        {
            string query = "UPDATE Producto SET cantidad = cantidad + @cantidad WHERE codigoProducto = @codigoProducto";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoProducto", codigoProducto);
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            cmd.ExecuteNonQuery();
        }

        private void EliminarDetallesFactura(SqlConnection con, SqlTransaction transaction, int idFactura)
        {
            string query = "DELETE FROM DetalleFactura WHERE codigoFactura = @idFactura";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@idFactura", idFactura);
            cmd.ExecuteNonQuery();
        }

        private void EliminarCreditoAsociado(SqlConnection con, SqlTransaction transaction, int idFactura)
        {
            string query = "DELETE FROM Credito WHERE codigoFactura = @idFactura";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@idFactura", idFactura);
            cmd.ExecuteNonQuery();
        }

        private int EliminarFacturaPrincipal(SqlConnection con, SqlTransaction transaction, int id)
        {
            string query = "DELETE FROM Factura WHERE codigoFactura = @id";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery();
        }

        #endregion

        [HttpPut("ActualizarFactura/{id}")]
        public IActionResult ActualizarFactura(int id, [FromBody] Factura factura)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Factura SET 
                                codigoCliente = @codigoCliente,
                                fecha = @fecha,
                                totalFactura = @totalFactura,
                                saldo = @saldo,
                                tipo = @tipo
                             WHERE codigoFactura = @codigoFactura";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@codigoFactura", id);
            cmd.Parameters.AddWithValue("@codigoCliente", factura.codigoCliente);
            cmd.Parameters.AddWithValue("@fecha", factura.fecha);
            cmd.Parameters.AddWithValue("@totalFactura", factura.totalFactura);
            cmd.Parameters.AddWithValue("@saldo", factura.saldo);
            cmd.Parameters.AddWithValue("@tipo", factura.tipo);

            con.Open();
            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                return Ok(new { message = "Factura actualizada correctamente" });
            else
                return NotFound(new { message = "Factura no encontrada" });
        }

        public class FacturaConDetalles
        {
            public Factura Factura { get; set; }
            public List<DetalleFactura> Detalles { get; set; }
        }

        [HttpGet("GetFacturasPendientes")]
        public IActionResult GetFacturasPendientes()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT * FROM Factura 
                        WHERE saldo > 0 
                        AND (estado IS NULL OR estado != 'Pagado')";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<Factura> facturas = new List<Factura>();

                foreach (DataRow row in dt.Rows)
                {
                    facturas.Add(new Factura
                    {
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        totalFactura = Convert.ToDecimal(row["totalFactura"]),
                        saldo = Convert.ToDecimal(row["saldo"]),
                        estado = row["estado"]?.ToString()
                    });
                }

                return Ok(facturas);
            }
        }

        [HttpGet]
        [Route("DescargarReporteFacturas")]
        public IActionResult DescargarReporteFacturas(string filtro = "")
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta base con JOIN para obtener el nombre del cliente
                string query = @"SELECT 
                f.codigoFactura,
                f.codigoCliente,
                cl.nombre + ' ' + cl.apellido AS cliente,
                f.fecha,
                f.totalFactura,
                f.saldo,
                f.tipo,
                f.estado
            FROM Factura f
            INNER JOIN Cliente cl ON f.codigoCliente = cl.codigoCliente";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                // Aplicar filtro si se especificó
                if (!string.IsNullOrEmpty(filtro))
                {
                    var filteredRows = dt.AsEnumerable().Where(row =>
                        row.Field<string>("codigoFactura").Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        row.Field<string>("codigoCliente").Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        row.Field<DateTime>("fecha").ToString().Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        row.Field<decimal>("totalFactura").ToString().Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        row.Field<decimal>("saldo").ToString().Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        row.Field<string>("tipo").Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        row.Field<string>("estado").Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

                    dt = filteredRows.Any() ? filteredRows.CopyToDataTable() : dt.Clone();
                }

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

                    // Título con filtro aplicado (si existe)
                    string titulo = string.IsNullOrEmpty(filtro)
                        ? "Reporte de Facturas"
                        : $"Reporte de Facturas (Filtrado por: '{filtro}')";

                    document.Add(new Paragraph(titulo, fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 8 columnas
                    PdfPTable table = new PdfPTable(8);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1.5f, 1.5f, 3f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f };
                    table.SetWidths(columnWidths);

                    // Encabezados
                    AddHeaderCell(table, "N° Factura", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cód. Cliente", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cliente", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Total", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Saldo", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Tipo", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Estado", fontHeader, BaseColor.DARK_GRAY);

                    // Datos
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["codigoFactura"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["codigoCliente"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["cliente"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fecha"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["totalFactura"]).ToString("C2"), fontCell));

                        // Saldo con color según valor
                        decimal saldo = Convert.ToDecimal(row["saldo"]);
                        table.AddCell(new Phrase(saldo.ToString("C2"),
                            saldo > 0 ? fontCellRed : fontCell));

                        table.AddCell(new Phrase(row["tipo"]?.ToString() ?? "-", fontCell));

                        // Estado con color
                        string estado = row["estado"]?.ToString() ?? "";
                        table.AddCell(new Phrase(estado,
                            estado == "Pagado" ? fontCellGreen : fontCell));
                    }

                    document.Add(table);

                    // Pie de página con fecha de generación
                    document.Add(Chunk.NEWLINE);
                    document.Add(new Paragraph($"Generado el: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}", fontCell));

                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", $"Reporte_Facturas_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }
            }
        }

        // Método auxiliar para celdas de encabezado (el mismo que antes)
        private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            table.AddCell(cell);
        }


        [HttpGet]
        [Route("DescargarReporteFactura/{codigoFactura?}")] // El parámetro es opcional y debe ser int
        public IActionResult DescargarReporteFactura(int? codigoFactura = null)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT 
                f.codigoFactura,
                f.estado,
                f.totalFactura,
                f.fecha,
                c.nombre + ' ' + c.apellido AS nombreCliente,
                f.saldo,
                f.tipo
            FROM Factura f
            INNER JOIN Cliente c ON f.codigoCliente = c.codigoCliente
            WHERE (@codigoFactura IS NULL OR f.codigoFactura = @codigoFactura)
            AND (@codigoFactura IS NOT NULL OR f.estado = 'Pendiente')";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@codigoFactura", codigoFactura ?? (object)DBNull.Value);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    return NotFound(codigoFactura.HasValue ?
                        $"No se encontró la factura con código {codigoFactura}" :
                        "No hay facturas pendientes");
                }

                using (var stream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4.Rotate());
                    PdfWriter.GetInstance(document, stream).CloseStream = false;
                    document.Open();

                    var fontTitle = FontFactory.GetFont("Arial", 18, Font.BOLD);
                    var fontHeader = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
                    var fontCell = FontFactory.GetFont("Arial", 9);

                    string titulo = codigoFactura.HasValue ?
                        $"Detalle de Factura #{codigoFactura}" :
                        "Reporte de Facturas Pendientes";

                    document.Add(new Paragraph(titulo, fontTitle));
                    document.Add(Chunk.NEWLINE);

                    PdfPTable table = new PdfPTable(7);
                    table.WidthPercentage = 100;
                    float[] columnWidths = new float[] { 1.5f, 2f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f };
                    table.SetWidths(columnWidths);

                    AddHeaderCell(table, "N° Factura", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cliente", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Total", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Saldo", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Tipo", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Estado", fontHeader, BaseColor.DARK_GRAY);

                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["codigoFactura"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["nombreCliente"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fecha"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["totalFactura"]).ToString("C2"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["saldo"]).ToString("C2"), fontCell));
                        table.AddCell(new Phrase(row["tipo"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["estado"]?.ToString() ?? "-", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;

                    string fileName = codigoFactura.HasValue ?
                        $"Factura_{codigoFactura}.pdf" :
                        "Reporte_Facturas_Pendientes.pdf";

                    return File(stream.ToArray(), "application/pdf", fileName);
                }
            }
        }




    }
}