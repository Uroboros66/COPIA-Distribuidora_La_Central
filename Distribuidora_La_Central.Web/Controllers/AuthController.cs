using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] Usuario usuario)
        {
            try
            {
                if (usuario == null || string.IsNullOrEmpty(usuario.nombre) || string.IsNullOrEmpty(usuario.codigoAcceso))
                {
                    return BadRequest("Datos de usuario inválidos");
                }

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Usuario WHERE nombre = @nombre AND codigoAcceso = @codigoAcceso", con);
                    da.SelectCommand.Parameters.AddWithValue("@nombre", usuario.nombre);
                    da.SelectCommand.Parameters.AddWithValue("@codigoAcceso", usuario.codigoAcceso);

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        // Crear objeto Usuario desde los datos del DataTable
                        Usuario usuarioEncontrado = new Usuario
                        {
                            idUsuario = Convert.ToInt32(dt.Rows[0]["idUsuario"]),
                            nombre = dt.Rows[0]["nombre"].ToString(),
                            rol = dt.Rows[0]["rol"].ToString(),
                            codigoAcceso = dt.Rows[0]["codigoAcceso"].ToString()
                        };

                        _logger.LogInformation($"Login exitoso para usuario: {usuario.nombre}");
                        return Ok(usuarioEncontrado); // ✅ Devuelve un JSON con el usuario
                    }
                    else
                    {
                        _logger.LogWarning($"Intento de login fallido para usuario: {usuario.nombre}");
                        return Unauthorized("Nombre o código incorrecto");
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de base de datos en Login");
                return StatusCode(500, "Error de conexión con la base de datos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en Login");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("Registrar")]
        public IActionResult Registrar([FromBody] Usuario usuario)
        {
            try
            {
                if (usuario == null || string.IsNullOrEmpty(usuario.nombre) || string.IsNullOrEmpty(usuario.rol) || string.IsNullOrEmpty(usuario.codigoAcceso))
                {
                    return BadRequest("Todos los campos son obligatorios");
                }

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    // Verificar si el usuario ya existe
                    SqlDataAdapter checkUser = new SqlDataAdapter("SELECT * FROM Usuario WHERE nombre = @nombre", con);
                    checkUser.SelectCommand.Parameters.AddWithValue("@nombre", usuario.nombre);

                    DataTable dt = new DataTable();
                    checkUser.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return BadRequest("El usuario ya existe");
                    }

                    // Insertar nuevo usuario
                    SqlCommand cmd = new SqlCommand("INSERT INTO Usuario (nombre, rol, codigoAcceso) VALUES (@nombre, @rol, @codigoAcceso)", con);
                    cmd.Parameters.AddWithValue("@nombre", usuario.nombre);
                    cmd.Parameters.AddWithValue("@rol", usuario.rol);
                    cmd.Parameters.AddWithValue("@codigoAcceso", usuario.codigoAcceso);

                    con.Open();
                    int i = cmd.ExecuteNonQuery();
                    con.Close();

                    if (i > 0)
                    {
                        _logger.LogInformation($"Usuario registrado exitosamente: {usuario.nombre}");
                        return Ok("Registro exitoso");
                    }
                    else
                    {
                        _logger.LogWarning($"No se pudo registrar el usuario: {usuario.nombre}");
                        return StatusCode(500, "Error al registrar usuario");
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de base de datos en Registrar");
                return StatusCode(500, "Error de conexión con la base de datos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en Registrar");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("Lista")]
        public IActionResult GetUsuarios()
        {
            try
            {
                List<Usuario> usuarios = new List<Usuario>();

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SELECT * FROM Usuario", con);
                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        usuarios.Add(new Usuario
                        {
                            idUsuario = Convert.ToInt32(reader["idUsuario"]),
                            nombre = reader["nombre"].ToString(),
                            rol = reader["rol"].ToString(),
                            codigoAcceso = reader["codigoAcceso"].ToString()
                        });
                    }
                }

                _logger.LogInformation($"Se obtuvieron {usuarios.Count} usuarios");
                return Ok(usuarios);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de base de datos en GetUsuarios");
                return StatusCode(500, "Error de conexión con la base de datos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetUsuarios");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}