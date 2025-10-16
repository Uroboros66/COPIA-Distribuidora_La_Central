using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

[Route("api/[controller]")]
[ApiController]
public class DetalleFacturaController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DetalleFacturaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("obtener-todos")]
    public string ObtenerTodos()
    {
        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
        SqlDataAdapter da = new("SELECT * FROM DetalleFactura", con);
        DataTable dt = new();
        da.Fill(dt);

        List<DetalleFactura> lista = new();
        Response response = new();

        foreach (DataRow row in dt.Rows)
        {
            DetalleFactura detalle = new()
            {
                idDetalle = Convert.ToInt32(row["idDetalle"]),
                codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                codigoProducto = Convert.ToInt32(row["codigoProducto"]),
                cantidad = Convert.ToInt32(row["cantidad"]),
                precioUnitario = Convert.ToDecimal(row["precioUnitario"]),
                subtotal = Convert.ToDecimal(row["subtotal"]),
                unidadMedida = row["unidadMedida"]?.ToString() // Nueva propiedad
            };
            lista.Add(detalle);
        }

        if (lista.Count > 0)
            return JsonConvert.SerializeObject(lista);
        else
        {
            response.StatusCode = 100;
            response.ErrorMessage = "No se encontraron datos.";
            return JsonConvert.SerializeObject(response);
        }
    }

    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] DetalleFactura detalle)
    {

        // Agrega logging temporal
        Console.WriteLine($"Recibido detalle - UnidadMedida: {detalle.unidadMedida}");

        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));

        SqlCommand cmd = new("INSERT INTO DetalleFactura (codigoFactura, codigoProducto, cantidad, precioUnitario, subtotal, unidadMedida) " +
                             "VALUES (@codigoFactura, @codigoProducto, @cantidad, @precioUnitario, @subtotal, @unidadMedida)", con);

        cmd.Parameters.AddWithValue("@codigoFactura", detalle.codigoFactura);
        cmd.Parameters.AddWithValue("@codigoProducto", detalle.codigoProducto);
        cmd.Parameters.AddWithValue("@cantidad", detalle.cantidad);
        cmd.Parameters.AddWithValue("@precioUnitario", detalle.precioUnitario);
        cmd.Parameters.AddWithValue("@subtotal", detalle.subtotal);
        cmd.Parameters.AddWithValue("@unidadMedida", detalle.unidadMedida ?? (object)DBNull.Value);

        con.Open();
        int result = cmd.ExecuteNonQuery();
        con.Close();

        if (result > 0)
            return Ok("Detalle registrado correctamente");
        else
            return StatusCode(500, "Error al registrar el detalle");
    }

    [HttpPut("actualizar/{id}")]
    public IActionResult Actualizar(int id, [FromBody] DetalleFactura detalle)
    {
        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));

        SqlCommand cmd = new(@"UPDATE DetalleFactura SET 
                                codigoFactura = @codigoFactura,
                                codigoProducto = @codigoProducto,
                                cantidad = @cantidad,
                                precioUnitario = @precioUnitario,
                                subtotal = @subtotal,
                                unidadMedida = @unidadMedida
                                WHERE idDetalle = @idDetalle", con);

        cmd.Parameters.AddWithValue("@idDetalle", id);
        cmd.Parameters.AddWithValue("@codigoFactura", detalle.codigoFactura);
        cmd.Parameters.AddWithValue("@codigoProducto", detalle.codigoProducto);
        cmd.Parameters.AddWithValue("@cantidad", detalle.cantidad);
        cmd.Parameters.AddWithValue("@precioUnitario", detalle.precioUnitario);
        cmd.Parameters.AddWithValue("@subtotal", detalle.subtotal);
        cmd.Parameters.AddWithValue("@unidadMedida", detalle.unidadMedida ?? (object)DBNull.Value);

        con.Open();
        int result = cmd.ExecuteNonQuery();
        con.Close();

        if (result > 0)
            return Ok("Detalle actualizado correctamente");
        else
            return NotFound("Detalle no encontrado");
    }

    [HttpDelete("eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {
        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));

        SqlCommand cmd = new("DELETE FROM DetalleFactura WHERE idDetalle = @id", con);
        cmd.Parameters.AddWithValue("@id", id);

        con.Open();
        int result = cmd.ExecuteNonQuery();
        con.Close();

        if (result > 0)
            return Ok("Detalle eliminado correctamente");
        else
            return NotFound("Detalle no encontrado");
    }
}
