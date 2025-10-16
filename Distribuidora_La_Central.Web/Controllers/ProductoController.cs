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
    public class ProductoController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ProductoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllProductos")]
        public IActionResult GetProductos()
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlDataAdapter da = new SqlDataAdapter("SELECT codigoProducto, descripcion, cantidad, costo, items, idProveedor, idCategoria, descuento, idBodega, unidadMedida, margenGanancia FROM Producto", con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<Producto> productoList = new List<Producto>();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Producto producto = new Producto
                        {
                            codigoProducto = row["codigoProducto"] != DBNull.Value ? Convert.ToInt32(row["codigoProducto"]) : 0,
                            descripcion = row["descripcion"] != DBNull.Value ? Convert.ToString(row["descripcion"]) : string.Empty,
                            cantidad = row["cantidad"] != DBNull.Value ? Convert.ToInt32(row["cantidad"]) : 0,
                            costo = row["costo"] != DBNull.Value ? Convert.ToDecimal(row["costo"]) : 0m,
                            items = row["items"] != DBNull.Value ? Convert.ToInt32(row["items"]) : 0,
                            idProveedor = row["idProveedor"] != DBNull.Value ? Convert.ToInt32(row["idProveedor"]) : 0,
                            idCategoria = row["idCategoria"] != DBNull.Value ? Convert.ToInt32(row["idCategoria"]) : 0,
                            descuento = row["descuento"] != DBNull.Value ? Convert.ToDecimal(row["descuento"]) : 0m,
                            idBodega = row["idBodega"] != DBNull.Value ? Convert.ToInt32(row["idBodega"]) : 0,
                            unidadMedida = row["unidadMedida"] != DBNull.Value ? Convert.ToString(row["unidadMedida"]) : string.Empty,
                            margenGanancia = row["margenGanancia"] != DBNull.Value ? Convert.ToDecimal(row["margenGanancia"]) : 0m

                        };
                        productoList.Add(producto);
                    }
                    return Ok(productoList);
                }
                return NotFound(new { StatusCode = 404, Message = "No se encontraron productos" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al obtener productos", Error = ex.Message });
            }
        }

        [HttpGet("BuscarProducto")]
        public IActionResult BuscarProducto(
      [FromQuery] int? id = null,
      [FromQuery] int? items = null,
      [FromQuery] string? descripcion = null)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                string query = "SELECT * FROM Producto WHERE 1=1";
                var parameters = new List<SqlParameter>();

                if (id.HasValue)
                {
                    query += " AND codigoProducto = @id";
                    parameters.Add(new SqlParameter("@id", id));
                }

                if (items.HasValue)
                {
                    query += " AND items = @items";
                    parameters.Add(new SqlParameter("@items", items));
                }

                if (!string.IsNullOrWhiteSpace(descripcion))
                {
                    query += " AND descripcion LIKE @descripcion";
                    parameters.Add(new SqlParameter("@descripcion", $"%{descripcion}%"));
                }

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddRange(parameters.ToArray());

                SqlDataReader reader = cmd.ExecuteReader();

                List<Producto> productos = new List<Producto>();

                while (reader.Read())
                {
                    Producto producto = new Producto
                    {
                        codigoProducto = reader["codigoProducto"] != DBNull.Value ? Convert.ToInt32(reader["codigoProducto"]) : 0,
                        descripcion = reader["descripcion"] != DBNull.Value ? Convert.ToString(reader["descripcion"]) : string.Empty,
                        cantidad = reader["cantidad"] != DBNull.Value ? Convert.ToInt32(reader["cantidad"]) : 0,
                        costo = reader["costo"] != DBNull.Value ? Convert.ToDecimal(reader["costo"]) : 0m,
                        items = reader["items"] != DBNull.Value ? Convert.ToInt32(reader["items"]) : 0,
                        idProveedor = reader["idProveedor"] != DBNull.Value ? Convert.ToInt32(reader["idProveedor"]) : 0,
                        idCategoria = reader["idCategoria"] != DBNull.Value ? Convert.ToInt32(reader["idCategoria"]) : 0,
                        descuento = reader["descuento"] != DBNull.Value ? Convert.ToDecimal(reader["descuento"]) : 0m,
                        idBodega = reader["idBodega"] != DBNull.Value ? Convert.ToInt32(reader["idBodega"]) : 0,
                        unidadMedida = reader["descripcion"] != DBNull.Value ? Convert.ToString(reader["descripcion"]) : string.Empty,
                        margenGanancia = reader["margenGanancia"] != DBNull.Value ? Convert.ToDecimal(reader["margenGanancia"]) : 0m
                    };
                    productos.Add(producto);
                }

                if (productos.Any())
                    return Ok(productos);
                else
                    return NotFound(new { StatusCode = 404, Message = "No se encontraron productos con los criterios proporcionados" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al buscar productos", Error = ex.Message });
            }
        }


        [HttpPost]
        [Route("RegistrarProducto")]
        public IActionResult RegistrarProducto([FromBody] Producto producto)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                // Verificar si el producto ya existe
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM Producto WHERE descripcion = @descripcion AND idProveedor = @idProveedor",
                    con);
                checkCmd.Parameters.AddWithValue("@descripcion", producto.descripcion ?? string.Empty);
                checkCmd.Parameters.AddWithValue("@idProveedor", producto.idProveedor);

                con.Open();
                bool exists = (int)checkCmd.ExecuteScalar() > 0;
                if (exists)
                {
                    return Conflict(new { StatusCode = 409, Message = "El producto ya existe con este proveedor" });
                }

                // Insertar nuevo producto
                SqlCommand insertCmd = new SqlCommand(
                    @"INSERT INTO Producto 
                    (descripcion, cantidad, idCategoria, descuento, costo, idBodega, idProveedor, items, unidadMedida,margenGanancia) 
                    VALUES 
                    (@descripcion, @cantidad, @idCategoria, @descuento, @costo, @idBodega, @idProveedor, @items, @unidadMedida, @margenGanancia);
                    SELECT SCOPE_IDENTITY();",
                    con);

                insertCmd.Parameters.AddWithValue("@descripcion", producto.descripcion ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@cantidad", producto.cantidad);
                insertCmd.Parameters.AddWithValue("@idCategoria", producto.idCategoria);
                insertCmd.Parameters.AddWithValue("@descuento", producto.descuento);
                insertCmd.Parameters.AddWithValue("@costo", producto.costo);
                insertCmd.Parameters.AddWithValue("@idBodega", producto.idBodega);
                insertCmd.Parameters.AddWithValue("@idProveedor", producto.idProveedor);
                insertCmd.Parameters.AddWithValue("@items", producto.items);
                insertCmd.Parameters.AddWithValue("@unidadMedida", producto.unidadMedida ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@margenGanancia", producto.margenGanancia);

                int newId = Convert.ToInt32(insertCmd.ExecuteScalar());

                return Ok(new
                {
                    StatusCode = 200,
                    Message = "Producto registrado exitosamente",
                    ProductoId = newId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    StatusCode = 500,
                    Message = "Error al registrar producto",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpPut]
        [Route("ActualizarProducto")]
        public IActionResult ActualizarProducto([FromBody] Producto producto)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                SqlCommand cmd = new SqlCommand(
                    @"UPDATE Producto SET 
                        descripcion = @descripcion,
                        cantidad = @cantidad,
                        idCategoria = @idCategoria,
                        descuento = @descuento,
                        costo = @costo,
                        idBodega = @idBodega,
                        idProveedor = @idProveedor,
                        items = @items,
unidadMedida = @unidadMedida,
                        margenGanancia = @margenGanancia
                    WHERE codigoProducto = @codigoProducto",
                    con);

                cmd.Parameters.AddWithValue("@codigoProducto", producto.codigoProducto);
                cmd.Parameters.AddWithValue("@descripcion", producto.descripcion ?? string.Empty);
                cmd.Parameters.AddWithValue("@cantidad", producto.cantidad);
                cmd.Parameters.AddWithValue("@idCategoria", producto.idCategoria);
                cmd.Parameters.AddWithValue("@descuento", producto.descuento);
                cmd.Parameters.AddWithValue("@costo", producto.costo);
                cmd.Parameters.AddWithValue("@idBodega", producto.idBodega);
                cmd.Parameters.AddWithValue("@idProveedor", producto.idProveedor);
                cmd.Parameters.AddWithValue("@items", producto.items);
                cmd.Parameters.AddWithValue("@unidadMedida", producto.unidadMedida ?? string.Empty);
                cmd.Parameters.AddWithValue("@margenGanancia", producto.margenGanancia);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { StatusCode = 200, Message = "Producto actualizado exitosamente" });
                }
                return NotFound(new { StatusCode = 404, Message = "Producto no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al actualizar producto", Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult EliminarProducto(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                SqlCommand cmd = new SqlCommand("DELETE FROM Producto WHERE codigoProducto = @id", con);
                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { StatusCode = 200, Message = "Producto eliminado exitosamente" });
                }
                return NotFound(new { StatusCode = 404, Message = "Producto no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al eliminar producto", Error = ex.Message });
            }


        }


        [HttpGet("obtener-todas-categorias")]
        public string ObtenerTodasCategorias()
        {
            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new("SELECT * FROM CategoriaProducto", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            List<CategoriaProducto> categoriaList = new List<CategoriaProducto>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    CategoriaProducto categoria = new CategoriaProducto();
                    categoria.idCategoria = Convert.ToInt32(dt.Rows[i]["idCategoria"]);
                    categoria.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                    categoria.descripcion = Convert.ToString(dt.Rows[i]["descripcion"]);
                    categoriaList.Add(categoria);
                }
            }

            if (categoriaList.Count > 0)
                return JsonConvert.SerializeObject(categoriaList);
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No se encontraron categorías.";
                return JsonConvert.SerializeObject(response);
            }
        }



        [HttpGet]
        [Route("DescargarReporteProductos")]
        public IActionResult DescargarReporteProductos()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta con joins para obtener nombres de categoría, bodega y proveedor
                string query = @"SELECT 
    p.codigoProducto,
    p.descripcion,
    p.cantidad,
    cp.nombre AS categoria,
    p.descuento,
    p.costo,
    b.nombre AS bodega,
    prov.nombre AS proveedor,
    p.idProveedor
FROM Producto p
LEFT JOIN CategoriaProducto cp ON p.idCategoria = cp.idCategoria
LEFT JOIN Bodega b ON p.idBodega = b.idBodega
LEFT JOIN Proveedor prov ON p.idProveedor = prov.idProveedor";

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

                    // Título
                    document.Add(new Paragraph("Reporte de Productos", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 8 columnas
                    PdfPTable table = new PdfPTable(8);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1f, 3f, 1f, 1.5f, 1f, 1.5f, 1.5f, 2f };
                    table.SetWidths(columnWidths);

                    // Encabezados
                    AddHeaderCell(table, "Código", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Descripción", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cantidad", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Categoría", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Descuento", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Costo", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Bodega", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Proveedor", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos y formato
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["codigoProducto"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["descripcion"]?.ToString() ?? "-", fontCell));

                        // Resaltar cantidad baja en rojo
                        int cantidad = Convert.ToInt32(row["cantidad"]);
                        table.AddCell(new Phrase(cantidad.ToString(),
                            cantidad < 10 ? fontCellRed : fontCell));

                        table.AddCell(new Phrase(row["categoria"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(Convert.ToDecimal(row["descuento"]).ToString("P0"), fontCell)); // Formato porcentaje
                        table.AddCell(new Phrase(Convert.ToDecimal(row["costo"]).ToString("C2"), fontCell));
                        table.AddCell(new Phrase(row["bodega"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["proveedor"]?.ToString() ?? "-", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Reporte_Productos.pdf");
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
        [Route("DescargarReporteInventario")]
        public IActionResult DescargarReporteInventario()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta con joins para obtener nombres de categoría y bodega
                string query = @"
            SELECT 
                p.codigoProducto,
                p.descripcion,
                p.cantidad,
                p.unidadMedida,
                cp.nombre AS categoria,
                p.descuento,
                p.costo,
                p.items,
                b.nombre AS bodega
            FROM Producto p
            LEFT JOIN CategoriaProducto cp ON p.idCategoria = cp.idCategoria
            LEFT JOIN Bodega b ON p.idBodega = b.idBodega";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                using (var stream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4.Rotate()); // Horizontal
                    PdfWriter.GetInstance(document, stream).CloseStream = false;
                    document.Open();

                    // Fuentes (consistentes con el otro reporte)
                    var fontTitle = FontFactory.GetFont("Arial", 18, Font.BOLD);
                    var fontHeader = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
                    var fontCell = FontFactory.GetFont("Arial", 9);

                    // Título
                    document.Add(new Paragraph("Reporte de Inventario", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con las columnas necesarias
                    PdfPTable table = new PdfPTable(9);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados para los datos de productos
                    float[] columnWidths = new float[] { 1f, 3f, 1f, 1f, 2f, 1f, 1.5f, 1f, 2f };
                    table.SetWidths(columnWidths);

                    // Encabezados (mismo estilo que clientes)
                    AddHeaderCell(table, "Código", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Descripción", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cantidad", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Unidad", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Categoría", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Descuento", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Costo", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Items", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Bodega", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos y formato específico
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["codigoProducto"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["descripcion"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["cantidad"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["unidadMedida"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["categoria"]?.ToString() ?? "N/A", fontCell));

                        // Formatear descuento como porcentaje
                        var descuento = row["descuento"] != DBNull.Value ? Convert.ToDecimal(row["descuento"]) : 0m;
                        table.AddCell(new Phrase(descuento.ToString("P1"), fontCell));

                        // Formatear costo con formato monetario
                        var costo = row["costo"] != DBNull.Value ? Convert.ToDecimal(row["costo"]) : 0m;
                        table.AddCell(new Phrase(costo.ToString("C2"), fontCell));

                        table.AddCell(new Phrase(row["items"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["bodega"]?.ToString() ?? "N/A", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", $"Reporte_Inventario_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }
            }
        }



    }
}