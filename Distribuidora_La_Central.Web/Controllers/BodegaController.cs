using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

[Route("api/[controller]")]
[ApiController]
public class BodegaController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public BodegaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("obtener-todos")]
    public string ObtenerTodasBodegas()
    {
        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
        SqlDataAdapter da = new("SELECT * FROM Bodega", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        List<Bodega> bodegaList = new List<Bodega>();
        Response response = new Response();

        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Bodega bodega = new Bodega();
                bodega.idBodega = Convert.ToInt32(dt.Rows[i]["idBodega"]);
                bodega.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                bodega.ubicacion = Convert.ToString(dt.Rows[i]["ubicacion"]);

                // Manejo del valor NULL para responsable
                bodega.responsable = dt.Rows[i]["responsable"] == DBNull.Value
                    ? 0  // Valor por defecto cuando es NULL
                    : Convert.ToInt32(dt.Rows[i]["responsable"]);

                // Manejo del valor NULL para fecha
                bodega.fecha = dt.Rows[i]["fecha"] == DBNull.Value
                    ? DateTime.MinValue  // Valor por defecto cuando es NULL
                    : Convert.ToDateTime(dt.Rows[i]["fecha"]);

                bodegaList.Add(bodega);
            }
        }

        if (bodegaList.Count > 0)
            return JsonConvert.SerializeObject(bodegaList);
        else
        {
            response.StatusCode = 100;
            response.ErrorMessage = "No se encontraron bodegas.";
            return JsonConvert.SerializeObject(response);
        }
    }


    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] Bodega bodega)
    {
        using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        // Verificar si ya existe una bodega con el mismo nombre
        SqlDataAdapter checkBodega = new SqlDataAdapter("SELECT * FROM Bodega WHERE nombre = @nombre", con);
        checkBodega.SelectCommand.Parameters.AddWithValue("@nombre", bodega.nombre);

        DataTable dt = new DataTable();
        checkBodega.Fill(dt);

        if (dt.Rows.Count > 0)
        {
            return BadRequest("La bodega ya existe");
        }

        // Insertar la nueva bodega
        SqlCommand cmd = new SqlCommand(@"INSERT INTO Bodega (nombre, ubicacion, responsable, fecha) 
                                      VALUES (@nombre, @ubicacion, @responsable, @fecha)", con);

        cmd.Parameters.AddWithValue("@nombre", bodega.nombre);
        cmd.Parameters.AddWithValue("@ubicacion", bodega.ubicacion);
        cmd.Parameters.AddWithValue("@responsable", bodega.responsable);
        cmd.Parameters.AddWithValue("@fecha", bodega.fecha);

        con.Open();
        int i = cmd.ExecuteNonQuery();
        con.Close();

        if (i > 0)
        {
            return Ok("Bodega registrada exitosamente");
        }
        else
        {
            return StatusCode(500, "Error al registrar bodega");
        }
    }


    [HttpGet]
    [Route("DescargarReporteBodegas")]
    public IActionResult DescargarReporteBodegas()
    {
        using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            // Consulta básica con solo los campos de Bodega
            string query = @"SELECT 
                idBodega, 
                nombre, 
                ubicacion,
                responsable,
                fecha
            FROM Bodega";

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
                document.Add(new Paragraph("Reporte de Bodegas", fontTitle));
                document.Add(Chunk.NEWLINE);

                // Tabla con 5 columnas
                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;

                // Anchos de columnas optimizados
                float[] columnWidths = new float[] { 1f, 2f, 3f, 2f, 2f };
                table.SetWidths(columnWidths);

                // Encabezados
                AddHeaderCell(table, "ID", fontHeader, BaseColor.DARK_GRAY);
                AddHeaderCell(table, "Nombre", fontHeader, BaseColor.DARK_GRAY);
                AddHeaderCell(table, "Ubicación", fontHeader, BaseColor.DARK_GRAY);
                AddHeaderCell(table, "Responsable ID", fontHeader, BaseColor.DARK_GRAY);
                AddHeaderCell(table, "Fecha Registro", fontHeader, BaseColor.DARK_GRAY);

                // Datos con manejo de nulos y formato
                foreach (DataRow row in dt.Rows)
                {
                    table.AddCell(new Phrase(row["idBodega"].ToString(), fontCell));
                    table.AddCell(new Phrase(row["nombre"]?.ToString() ?? "-", fontCell));
                    table.AddCell(new Phrase(row["ubicacion"]?.ToString() ?? "-", fontCell));

                    // Manejo del responsable (ID numérico)
                    if (row["responsable"] == DBNull.Value)
                    {
                        table.AddCell(new Phrase("-", fontCell));
                    }
                    else
                    {
                        table.AddCell(new Phrase(row["responsable"].ToString(), fontCell));
                    }

                    // Formatear fecha o mostrar "-" si es nula/minima
                    if (row["fecha"] == DBNull.Value)
                    {
                        table.AddCell(new Phrase("-", fontCell));
                    }
                    else
                    {
                        DateTime fecha = Convert.ToDateTime(row["fecha"]);
                        table.AddCell(new Phrase(fecha.ToString("dd/MM/yyyy"), fontCell));
                    }
                }

                document.Add(table);
                document.Close();

                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", "Reporte_Bodegas.pdf");
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