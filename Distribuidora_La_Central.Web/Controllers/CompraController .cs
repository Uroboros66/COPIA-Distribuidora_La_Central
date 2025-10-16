using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
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
    public class CompraController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CompraController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPut]
        [Route("MarcarComoPagada/{id}")]
        public IActionResult MarcarComoPagada(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                string query = @"UPDATE Compra 
                        SET Estado = 'Pagado', 
                            FechaPago = @fechaPago
                        WHERE idCompra = @idCompra";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idCompra", id);
                cmd.Parameters.AddWithValue("@fechaPago", DateTime.Now);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { message = "Compra marcada como pagada correctamente" });
                }
                else
                {
                    return NotFound(new { message = "Compra no encontrada" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al marcar compra como pagada: {ex.Message}" });
            }
        }


        [HttpGet]
        [Route("GetFilteredCompras")]
        public IActionResult GetFilteredCompras(
    [FromQuery] int? proveedorId = null,
    [FromQuery] string? estado = null,
    [FromQuery] string? fechaInicio = null,
    [FromQuery] string? fechaFin = null)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                // Construir la consulta base con JOIN para incluir información del proveedor
                string query = @"SELECT c.*, p.nombre AS ProveedorNombre 
                        FROM Compra c
                        LEFT JOIN Proveedor p ON c.idProveedor = p.idProveedor
                        WHERE 1=1";

                // Lista de parámetros
                var parameters = new List<SqlParameter>();

                // Añadir filtros según los parámetros recibidos
                if (proveedorId.HasValue && proveedorId > 0)
                {
                    query += " AND c.idProveedor = @proveedorId";
                    parameters.Add(new SqlParameter("@proveedorId", proveedorId));
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    query += " AND c.Estado = @estado";
                    parameters.Add(new SqlParameter("@estado", estado));
                }

                if (DateTime.TryParse(fechaInicio, out DateTime fechaInicioParsed))
                {
                    query += " AND c.fechaCompra >= @fechaInicio";
                    parameters.Add(new SqlParameter("@fechaInicio", fechaInicioParsed));
                }

                if (DateTime.TryParse(fechaFin, out DateTime fechaFinParsed))
                {
                    // Añadir un día para incluir todas las compras del día final
                    query += " AND c.fechaCompra < @fechaFin";
                    parameters.Add(new SqlParameter("@fechaFin", fechaFinParsed.AddDays(1)));
                }

                query += " ORDER BY c.fechaCompra DESC";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddRange(parameters.ToArray());

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<CompraConProveedor> compras = new List<CompraConProveedor>();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var compra = new CompraConProveedor
                        {
                            idCompra = Convert.ToInt32(dt.Rows[i]["idCompra"]),
                            idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                            fechaCompra = Convert.ToDateTime(dt.Rows[i]["fechaCompra"]),
                            TotalCompra = Convert.ToDecimal(dt.Rows[i]["TotalCompra"]),
                            Estado = Convert.ToString(dt.Rows[i]["Estado"]),
                            FechaPago = dt.Rows[i]["FechaPago"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["FechaPago"]) : (DateTime?)null,
                            MetodoPago = Convert.ToString(dt.Rows[i]["MetodoPago"]),
                            Proveedor = new ProveedorInfo
                            {
                                idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                                nombre = Convert.ToString(dt.Rows[i]["ProveedorNombre"])
                            }
                        };
                        compras.Add(compra);
                    }
                }

                return Ok(compras);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener compras filtradas: {ex.Message}" });
            }
        }

        // Clases adicionales para la respuesta
        public class CompraConProveedor : Compra
        {
            public ProveedorInfo Proveedor { get; set; }
        }

        public class ProveedorInfo
        {
            public int idProveedor { get; set; }
            public string nombre { get; set; }
        }
        [HttpGet]
        [Route("GetAllCompras")]
        public IActionResult GetAllCompras()
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                string query = @"SELECT c.*, p.nombre AS ProveedorNombre 
                         FROM Compra c
                         LEFT JOIN Proveedor p ON c.idProveedor = p.idProveedor
                         ORDER BY c.fechaCompra DESC";

                using SqlCommand cmd = new SqlCommand(query, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<CompraConProveedor> compraList = new List<CompraConProveedor>();

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var compra = new CompraConProveedor
                    {
                        idCompra = Convert.ToInt32(dt.Rows[i]["idCompra"]),
                        idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                        fechaCompra = Convert.ToDateTime(dt.Rows[i]["fechaCompra"]),
                        TotalCompra = Convert.ToDecimal(dt.Rows[i]["TotalCompra"]),
                        Estado = Convert.ToString(dt.Rows[i]["Estado"]),
                        FechaPago = dt.Rows[i]["FechaPago"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["FechaPago"]) : (DateTime?)null,
                        MetodoPago = Convert.ToString(dt.Rows[i]["MetodoPago"]),
                        Proveedor = new ProveedorInfo
                        {
                            idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                            nombre = Convert.ToString(dt.Rows[i]["ProveedorNombre"])
                        }
                    };
                    compraList.Add(compra);
                }

                return Ok(compraList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener compras: {ex.Message}" });
            }
        }




        [HttpPost]
        [Route("AgregarCompra")]
        public IActionResult AgregarCompra([FromBody] CompraConDetalles compraConDetalles)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();
                using SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Insertar la compra principal
                    string queryCompra = @"INSERT INTO Compra (idProveedor, fechaCompra, TotalCompra, Estado, FechaPago, MetodoPago)
                         OUTPUT INSERTED.idCompra
                         VALUES (@idProveedor, @fechaCompra, @TotalCompra, @Estado, @FechaPago, @MetodoPago)";

                    int idCompra;
                    using (SqlCommand cmd = new SqlCommand(queryCompra, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@idProveedor", compraConDetalles.Compra.idProveedor);
                        cmd.Parameters.AddWithValue("@fechaCompra", compraConDetalles.Compra.fechaCompra);
                        cmd.Parameters.AddWithValue("@TotalCompra", compraConDetalles.Compra.TotalCompra);
                        cmd.Parameters.AddWithValue("@Estado", compraConDetalles.Compra.Estado ?? "Pendiente");
                        cmd.Parameters.AddWithValue("@FechaPago", compraConDetalles.Compra.FechaPago ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetodoPago", compraConDetalles.Compra.MetodoPago ?? (object)DBNull.Value);

                        idCompra = (int)cmd.ExecuteScalar();
                    }

                    // 2. Procesar detalles y actualizar cantidad en productos
                    if (compraConDetalles.Detalles != null && compraConDetalles.Detalles.Any())
                    {
                        string queryDetalle = @"INSERT INTO DetalleCompra 
                        (IdCompra, CodigoProducto, Cantidad, PrecioUnitario, Subtotal)
                        VALUES 
                        (@IdCompra, @CodigoProducto, @Cantidad, @PrecioUnitario, @Subtotal)";

                        // Usamos 'cantidad' que es el campo correcto según tu modelo Producto
                        string queryActualizarCantidad = @"UPDATE Producto 
                                              SET cantidad = cantidad + @Cantidad
                                              WHERE codigoProducto = @CodigoProducto";

                        foreach (var detalle in compraConDetalles.Detalles)
                        {
                            // Validar cantidad
                            if (detalle.Cantidad <= 0)
                            {
                                transaction.Rollback();
                                return BadRequest(new { error = $"La cantidad debe ser mayor que cero para el producto {detalle.CodigoProducto}" });
                            }

                            // Insertar detalle de compra
                            using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, con, transaction))
                            {
                                decimal subtotal = detalle.Cantidad * detalle.PrecioUnitario;

                                cmdDetalle.Parameters.AddWithValue("@IdCompra", idCompra);
                                cmdDetalle.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
                                cmdDetalle.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                                cmdDetalle.Parameters.AddWithValue("@Subtotal", subtotal);

                                cmdDetalle.ExecuteNonQuery();
                            }

                            // Actualizar cantidad en producto
                            using (SqlCommand cmdCantidad = new SqlCommand(queryActualizarCantidad, con, transaction))
                            {
                                cmdCantidad.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
                                cmdCantidad.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                int rowsAffected = cmdCantidad.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { error = $"Producto con código {detalle.CodigoProducto} no encontrado" });
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    return Ok(new
                    {
                        idCompra = idCompra,
                        message = "Compra registrada exitosamente",
                        detallesCount = compraConDetalles.Detalles?.Count ?? 0
                    });
                }
                catch (SqlException sqlEx)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        error = "Error en la base de datos",
                        details = sqlEx.Message,
                        columnError = sqlEx.Errors.Count > 0 ? sqlEx.Errors[0].ToString() : ""
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        error = $"Error al procesar la compra: {ex.Message}",
                        innerError = ex.InnerException?.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = $"Error de conexión: {ex.Message}",
                    innerError = ex.InnerException?.Message
                });
            }
        }
        // Clases DTO para la solicitud
        public class CompraConDetalles
        {
            public Compra Compra { get; set; }
            public List<DetalleCompraDto> Detalles { get; set; }
        }

        public class DetalleCompraDto
        {
            public int CodigoProducto { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        [HttpDelete("EliminarCompra/{id}")]
        public IActionResult EliminarCompra(int id)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            con.Open();
            using SqlTransaction transaction = con.BeginTransaction();

            try
            {
                // 1. Obtener los detalles de la compra para ajustar el inventario
                List<DetalleCompra> detalles = ObtenerDetallesCompra(con, transaction, id);

                // 2. Revertir los productos al inventario (restar las cantidades)
                foreach (var detalle in detalles)
                {
                    AjustarInventario(con, transaction, detalle.CodigoProducto, -detalle.Cantidad);
                }

                // 3. Eliminar los detalles de la compra
                EliminarDetallesCompra(con, transaction, id);

                // 4. Finalmente, eliminar la compra principal
                int filasAfectadas = EliminarCompraPrincipal(con, transaction, id);

                if (filasAfectadas > 0)
                {
                    transaction.Commit();
                    return Ok(new { success = true, message = "Compra eliminada correctamente" });
                }
                else
                {
                    transaction.Rollback();
                    return NotFound(new { success = false, message = "Compra no encontrada" });
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al eliminar la compra",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // Métodos auxiliares
        private List<DetalleCompra> ObtenerDetallesCompra(SqlConnection con, SqlTransaction transaction, int idCompra)
        {
            string query = "SELECT CodigoProducto, Cantidad FROM DetalleCompra WHERE IdCompra = @IdCompra";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@IdCompra", idCompra);

            var detalles = new List<DetalleCompra>();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                detalles.Add(new DetalleCompra
                {
                    CodigoProducto = reader.GetInt32(0),
                    Cantidad = reader.GetInt32(1)
                });
            }
            return detalles;
        }

        private void AjustarInventario(SqlConnection con, SqlTransaction transaction, int codigoProducto, int cantidad)
        {
            string query = "UPDATE Producto SET cantidad = cantidad + @Cantidad WHERE codigoProducto = @CodigoProducto";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@CodigoProducto", codigoProducto);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.ExecuteNonQuery();
        }

        private void EliminarDetallesCompra(SqlConnection con, SqlTransaction transaction, int idCompra)
        {
            string query = "DELETE FROM DetalleCompra WHERE IdCompra = @IdCompra";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@IdCompra", idCompra);
            cmd.ExecuteNonQuery();
        }

        private int EliminarCompraPrincipal(SqlConnection con, SqlTransaction transaction, int idCompra)
        {
            string query = "DELETE FROM Compra WHERE idCompra = @IdCompra";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@IdCompra", idCompra);
            return cmd.ExecuteNonQuery();
        }

        // Clase para los detalles de compra
        private class DetalleCompra
        {
            public int CodigoProducto { get; set; }
            public int Cantidad { get; set; }
        }

        [HttpPut]
        [Route("ActualizarCompra/{id}")]
        public IActionResult ActualizarCompra(int id, [FromBody] Compra compra)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Compra SET 
                                idProveedor = @idProveedor,
                                fechaCompra = @fechaCompra,
                                TotalCompra = @TotalCompra,
                                Estado = @Estado,
                                CreadoPor = @CreadoPor,
                                FechaPago = @FechaPago,
                                MetodoPago = @MetodoPago
                             WHERE idCompra = @idCompra";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@idCompra", id);
            cmd.Parameters.AddWithValue("@idProveedor", compra.idProveedor);
            cmd.Parameters.AddWithValue("@fechaCompra", compra.fechaCompra);
            cmd.Parameters.AddWithValue("@TotalCompra", compra.TotalCompra);
            cmd.Parameters.AddWithValue("@Estado", compra.Estado ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@FechaPago", compra.FechaPago ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MetodoPago", compra.MetodoPago ?? (object)DBNull.Value);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
                return Ok(new { message = "Compra actualizada correctamente." });
            else
                return NotFound(new { message = "Compra no encontrada." });
        }



        [HttpGet]
        [Route("DescargarReporteCompras")]
        public IActionResult DescargarReporteCompras()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta con los campos requeridos
                string query = @"SELECT 
                c.idCompra,
                c.idProveedor,
                p.nombre AS nombreProveedor,
                c.fechaCompra,
                c.TotalCompra,
                c.Estado,
                c.FechaPago,
                c.MetodoPago
            FROM Compra c
            INNER JOIN Proveedor p ON c.idProveedor = p.idProveedor";

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
                    document.Add(new Paragraph("Reporte de Compras", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 8 columnas
                    PdfPTable table = new PdfPTable(8);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1f, 1f, 2f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f };
                    table.SetWidths(columnWidths);

                    // Encabezados
                    AddHeaderCell(table, "ID Compra", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "ID Proveedor", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Proveedor", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha Compra", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Total", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Estado", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha Pago", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Método Pago", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos y formato
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["idCompra"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["idProveedor"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["nombreProveedor"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fechaCompra"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["TotalCompra"]).ToString("C2"), fontCell));
                        table.AddCell(new Phrase(row["Estado"]?.ToString() ?? "-", fontCell));

                        // Manejo de FechaPago nullable
                        if (row["FechaPago"] != DBNull.Value)
                        {
                            table.AddCell(new Phrase(Convert.ToDateTime(row["FechaPago"]).ToString("dd/MM/yyyy"), fontCell));
                        }
                        else
                        {
                            table.AddCell(new Phrase("-", fontCell));
                        }

                        table.AddCell(new Phrase(row["MetodoPago"]?.ToString() ?? "-", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Reporte_Compras.pdf");
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

        [HttpGet]
        [Route("DescargarReporteCompra/{idCompra?}")] // El parámetro es opcional
        public IActionResult DescargarReporteCompra(int? idCompra = null)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT 
                c.idCompra,
                c.Estado,
                c.TotalCompra,
                c.fechaCompra,
                p.nombre AS nombreProveedor
            FROM Compra c
            INNER JOIN Proveedor p ON c.idProveedor = p.idProveedor
            WHERE (@idCompra IS NULL OR c.idCompra = @idCompra)
            AND (@idCompra IS NOT NULL OR c.Estado = 'Pendiente')";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idCompra", idCompra ?? (object)DBNull.Value);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    return NotFound(idCompra.HasValue ?
                        $"No se encontró la compra con ID {idCompra}" :
                        "No hay deudas pendientes");
                }

                using (var stream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4.Rotate());
                    PdfWriter.GetInstance(document, stream).CloseStream = false;
                    document.Open();

                    var fontTitle = FontFactory.GetFont("Arial", 18, Font.BOLD);
                    var fontHeader = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
                    var fontCell = FontFactory.GetFont("Arial", 9);

                    // Título dinámico
                    string titulo = idCompra.HasValue ?
                        $"Detalle de Compra #{idCompra}" :
                        "Reporte de Deudas Pendientes";

                    document.Add(new Paragraph(titulo, fontTitle));
                    document.Add(Chunk.NEWLINE);

                    PdfPTable table = new PdfPTable(5);
                    table.WidthPercentage = 100;
                    float[] columnWidths = new float[] { 1f, 2f, 1.5f, 1.5f, 1.5f };
                    table.SetWidths(columnWidths);

                    AddHeaderCell(table, "ID Compra", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Proveedor", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha Compra", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Total", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Estado", fontHeader, BaseColor.DARK_GRAY);

                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["idCompra"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["nombreProveedor"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDateTime(row["fechaCompra"]).ToString("dd/MM/yyyy"), fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["TotalCompra"]).ToString("C2"), fontCell));
                        table.AddCell(new Phrase(row["Estado"]?.ToString() ?? "-", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;

                    string fileName = idCompra.HasValue ?
                        $"Compra_{idCompra}.pdf" :
                        "Reporte_Deudas_Pendientes.pdf";

                    return File(stream.ToArray(), "application/pdf", fileName);
                }
            }
        }




        [HttpGet]
        [Route("Deudas")]
        public IActionResult GetDeudasPendientes()
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                // Consulta actualizada para incluir los nuevos campos
                string query = @"SELECT 
                c.idCompra,
                c.idProveedor,
                p.nombre AS ProveedorNombre,
                c.fechaCompra,
                c.TotalCompra,
                c.Estado,
                c.FechaPago,
                c.MetodoPago
            FROM Compra c
            INNER JOIN Proveedor p ON c.idProveedor = p.idProveedor
            WHERE c.Estado = 'Pendiente'
            ORDER BY c.fechaCompra DESC";

                using SqlCommand cmd = new SqlCommand(query, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<CompraConProveedor> deudasList = new List<CompraConProveedor>();

                foreach (DataRow row in dt.Rows)
                {
                    var deuda = new CompraConProveedor
                    {
                        idCompra = Convert.ToInt32(row["idCompra"]),
                        idProveedor = Convert.ToInt32(row["idProveedor"]),
                        fechaCompra = Convert.ToDateTime(row["fechaCompra"]),
                        TotalCompra = Convert.ToDecimal(row["TotalCompra"]),
                        Estado = Convert.ToString(row["Estado"]),
                        FechaPago = row["FechaPago"] != DBNull.Value ? Convert.ToDateTime(row["FechaPago"]) : (DateTime?)null,
                        MetodoPago = Convert.ToString(row["MetodoPago"]),
                        Proveedor = new ProveedorInfo
                        {
                            idProveedor = Convert.ToInt32(row["idProveedor"]),
                            nombre = Convert.ToString(row["ProveedorNombre"])
                        }
                    };
                    deudasList.Add(deuda);
                }

                return Ok(deudasList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener deudas pendientes: {ex.Message}" });
            }
        }


    }
}