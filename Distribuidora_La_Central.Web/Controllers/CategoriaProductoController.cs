using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

[Route("api/[controller]")]
[ApiController]
public class CategoriaProductoController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public CategoriaProductoController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("obtener-todas-categorias")]
    public IActionResult ObtenerTodasCategorias()
    {
        try
        {
            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new("SELECT * FROM CategoriaProducto", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            List<CategoriaProducto> categoriaList = new();

            foreach (DataRow row in dt.Rows)
            {
                categoriaList.Add(new CategoriaProducto
                {
                    idCategoria = Convert.ToInt32(row["idCategoria"]),
                    nombre = row["nombre"].ToString(),
                    descripcion = row["descripcion"].ToString()
                });
            }

            return Ok(categoriaList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                StatusCode = 500,
                Message = "Error al obtener categorías",
                Error = ex.Message
            });
        }
    }

    [HttpPost("registrar-categoria")]
    public IActionResult RegistrarCategoria([FromBody] CategoriaProducto categoria)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoria.nombre))
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "El nombre de la categoría es requerido"
                });
            }

            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            con.Open();

            // Verificar si la categoría ya existe
            SqlCommand checkCmd = new(
                "SELECT COUNT(*) FROM CategoriaProducto WHERE nombre = @Nombre",
                con);
            checkCmd.Parameters.AddWithValue("@Nombre", categoria.nombre);
            int exists = (int)checkCmd.ExecuteScalar();

            if (exists > 0)
            {
                return Conflict(new
                {
                    StatusCode = 409,
                    Message = "Ya existe una categoría con este nombre"
                });
            }

            // Insertar nueva categoría
            SqlCommand cmd = new(
                "INSERT INTO CategoriaProducto (nombre, descripcion) " +
                "VALUES (@Nombre, @Descripcion); " +
                "SELECT SCOPE_IDENTITY();",
                con);

            cmd.Parameters.AddWithValue("@Nombre", categoria.nombre);
            cmd.Parameters.AddWithValue("@Descripcion", categoria.descripcion ?? (object)DBNull.Value);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());

            return Ok(new
            {
                StatusCode = 200,
                Message = "Categoría registrada con éxito",
                Data = new
                {
                    idCategoria = newId,
                    nombre = categoria.nombre,
                    descripcion = categoria.descripcion
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                StatusCode = 500,
                Message = "Error al registrar la categoría",
                Error = ex.Message
            });
        }
    }


    [HttpGet]
    [Route("DescargarReporteCategorias")]
    public IActionResult DescargarReporteCategorias()
    {
        using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            // Consulta con solo los campos requeridos
            string query = @"SELECT 
                idCategoria, 
                nombre, 
                descripcion
            FROM CategoriaProducto";

            SqlDataAdapter da = new SqlDataAdapter(query, con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4.Rotate()); // Horizontal
                PdfWriter.GetInstance(document, stream).CloseStream = false;
                document.Open();

                // Fuentes (consistentes con los otros reportes)
                var fontTitle = FontFactory.GetFont("Arial", 18, Font.BOLD);
                var fontHeader = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
                var fontCell = FontFactory.GetFont("Arial", 9);

                // Título
                document.Add(new Paragraph("Reporte de Categorías", fontTitle));
                document.Add(Chunk.NEWLINE);

                // Tabla con 3 columnas
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;

                // Anchos de columnas optimizados
                float[] columnWidths = new float[] { 1f, 2f, 4f };
                table.SetWidths(columnWidths);

                // Encabezados
                AddHeaderCell(table, "ID", fontHeader, BaseColor.DARK_GRAY);
                AddHeaderCell(table, "Nombre", fontHeader, BaseColor.DARK_GRAY);
                AddHeaderCell(table, "Descripción", fontHeader, BaseColor.DARK_GRAY);

                // Datos con manejo de nulos
                foreach (DataRow row in dt.Rows)
                {
                    table.AddCell(new Phrase(row["idCategoria"].ToString(), fontCell));
                    table.AddCell(new Phrase(row["nombre"]?.ToString() ?? "-", fontCell));
                    table.AddCell(new Phrase(row["descripcion"]?.ToString() ?? "-", fontCell));
                }

                document.Add(table);
                document.Close();

                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", "Reporte_Categorias.pdf");
            }
        }
    }

    // Método auxiliar para celdas de encabezado (reutilizado)
    private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bgColor)
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.BackgroundColor = bgColor;
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.Padding = 5;
        table.AddCell(cell);
    }


}