using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using static Distribuidora_La_Central.Shared.Pages.GestionUsuarios;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedorController : ControllerBase
    {
        public readonly IConfiguration _configuration;
        public ProveedorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("obtener-todos")]
        public string ObtenerTodosProveedores()
        {
            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new("SELECT * FROM Proveedor", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            List<Proveedor> proveedorList = new List<Proveedor>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Proveedor proveedor = new Proveedor();
                    proveedor.idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]);
                    proveedor.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                    proveedor.razonSocial = Convert.ToString(dt.Rows[i]["razonSocial"]);
                    proveedor.contacto = Convert.ToString(dt.Rows[i]["contacto"]);
                    proveedor.telefono = Convert.ToString(dt.Rows[i]["telefono"]);
                    proveedor.diaIngreso = Convert.ToDateTime(dt.Rows[i]["diaIngreso"]);
                    proveedorList.Add(proveedor);
                }
            }

            if (proveedorList.Count > 0)
                return JsonConvert.SerializeObject(proveedorList);
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No se encontraron proveedores.";
                return JsonConvert.SerializeObject(response);
            }

        }

        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Proveedor proveedor)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            // Verificar si ya existe un proveedor con el mismo nombre
            SqlDataAdapter checkProveedor = new SqlDataAdapter("SELECT * FROM Proveedor WHERE nombre = @nombre", con);
            checkProveedor.SelectCommand.Parameters.AddWithValue("@nombre", proveedor.nombre);

            DataTable dt = new DataTable();
            checkProveedor.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                return BadRequest("El proveedor ya existe");
            }

            // Insertar el nuevo proveedor
            SqlCommand cmd = new SqlCommand(@"INSERT INTO Proveedor (nombre, razonSocial, contacto, telefono, diaIngreso) 
                                      VALUES (@nombre, @razonSocial, @contacto, @telefono, @diaIngreso)", con);

            cmd.Parameters.AddWithValue("@nombre", proveedor.nombre);
            cmd.Parameters.AddWithValue("@razonSocial", proveedor.razonSocial);
            cmd.Parameters.AddWithValue("@contacto", proveedor.contacto);
            cmd.Parameters.AddWithValue("@telefono", proveedor.telefono);
            cmd.Parameters.AddWithValue("@diaIngreso", proveedor.diaIngreso);

            con.Open();
            int i = cmd.ExecuteNonQuery();
            con.Close();

            if (i > 0)
            {
                return Ok("Proveedor registrado exitosamente");
            }
            else
            {
                return StatusCode(500, "Error al registrar proveedor");
            }
        }


        [HttpDelete]
        [Route("EliminarProveedor/{id}")]
        public IActionResult EliminarProveeddor(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                string query = "DELETE FROM Proveedor WHERE idProveedor = @id";
                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                    return Ok(new { message = "Proveedor eliminado correctamente" });
                else
                    return NotFound(new { message = "Proveedor no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al eliminar proveedor: {ex.Message}" });
            }
        }

        [HttpPut]
        [Route("ActualizarProveedor/{id}")]
        public IActionResult ActualizarProveedor(int id, [FromBody] Proveedor proveedor)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Proveedor SET 
                    nombre = @nombre,
                    razonSocial = @razonSocial,
                    contacto = @contacto,
                    telefono = @telefono,
                    diaIngreso = @diaIngreso
                 WHERE idProveedor = @idProveedor";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@idProveedor", id);
            cmd.Parameters.AddWithValue("@nombre", proveedor.nombre);
            cmd.Parameters.AddWithValue("@razonSocial", proveedor.razonSocial);
            cmd.Parameters.AddWithValue("@contacto", proveedor.contacto);
            cmd.Parameters.AddWithValue("@telefono", proveedor.telefono);
            cmd.Parameters.AddWithValue("@diaIngreso", proveedor.diaIngreso);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
                return Ok(new { message = "Proveedor actualizado correctamente." });
            else
                return NotFound(new { message = "Proveedor no encontrado." });
        }


        [HttpGet]
        [Route("DescargarReporteProveedores")]
        public IActionResult DescargarReporteProveedores()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta específica con solo los campos requeridos
                string query = @"SELECT idProveedor, nombre, razonSocial, contacto, telefono, diaIngreso
                FROM Proveedor";

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
                    document.Add(new Paragraph("Reporte de Proveedores", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 6 columnas
                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1f, 2f, 2.5f, 2f, 1.5f, 1f };
                    table.SetWidths(columnWidths);

                    // Encabezados (mismo estilo que facturas)
                    AddHeaderCell(table, "ID", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Nombre", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Razón Social", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Contacto", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Teléfono", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Día Ingreso", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["idProveedor"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["nombre"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["razonSocial"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["contacto"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["telefono"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["diaIngreso"]?.ToString() ?? "-", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Reporte_Proveedores.pdf");
                }
            }
        }

        // Método auxiliar idéntico al original
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