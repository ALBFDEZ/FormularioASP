using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Data;

namespace FormularioASP.Pages
{
    public class FormularioModel : PageModel
    {
        private readonly IConfiguration _config;

        public FormularioModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]public string Documento { get; set; }
        [BindProperty]public string Telefono1 { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string? FechaNacimiento { get; set; }
        [BindProperty] public string? FechaDemanda { get; set; }
        [BindProperty] public IFormFile? ArchivoCV { get; set; }
        public string? ArchivoNombre { get; set; }
        [BindProperty] public bool PoliticaAceptada { get; set; }

        public List<Categoria> Categorias { get; set; } = new();
        public List<Nacionalidad> Nacionalidades { get; set; } = new();

        public class Categoria { public int ID { get; set; } public string Nombre { get; set; } }
        public class Profesion { public int ID { get; set; } public string Nombre { get; set; } }
        public class Nacionalidad { public int nClave { get; set; } public string sDescripcion { get; set; } }

        public void OnGet()
        {
            CargarCategorias();
            CargarNacionalidades();
        }

        public IActionResult OnPostCancelar() => RedirectToPage("/Index");

        public JsonResult OnGetProfesiones(int idCategoria)
        {
            List<Profesion> profesiones = new();
            string connString = _config.GetConnectionString("BolsaEmpleo");

            using SqlConnection conn = new(connString);
            conn.Open();

            string sql = "SELECT ID, PROFESION FROM AYTO_PROFESIONES WHERE CATEGORIA = @id ORDER BY PROFESION";
            SqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@id", idCategoria);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                profesiones.Add(new Profesion
                {
                    ID = (int)reader["ID"],
                    Nombre = reader["PROFESION"].ToString()
                });
            }

            return new JsonResult(profesiones);
        }

        //Cargamos las categorias de la BBDD
        private void CargarCategorias()
        {
            Categorias = new();
            string connString = _config.GetConnectionString("BolsaEmpleo");

            using SqlConnection conn = new(connString);
            conn.Open();

            string sql = "SELECT ID, CATEGORIA FROM AYTO_CATEGORIA ORDER BY CATEGORIA";
            SqlCommand cmd = new(sql, conn);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Categorias.Add(new Categoria
                {
                    ID = (int)reader["ID"],
                    Nombre = reader["CATEGORIA"].ToString()
                });
            }
        }

        //CARGAMOS LAS NACIONALIDADES DE LA BBDD
        private void CargarNacionalidades()
        {
            Nacionalidades = new();
            string connString = _config.GetConnectionString("BolsaEmpleo");

            using SqlConnection conn = new(connString);
            conn.Open();

            string sql = "SELECT nClave, sDescripcion FROM tAuxNacionalidad ORDER BY sDescripcion";
            SqlCommand cmd = new(sql, conn);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Nacionalidades.Add(new Nacionalidad
                {
                    nClave = (int)reader["nClave"],
                    sDescripcion = reader["sDescripcion"].ToString()
                });
            }
        }

        //VALIDAMOS PARAMETROS DE FECHA NACIMIENTO
        private bool FechaNacimientoValida(string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha)) return false;

            var partes = fecha.Split('/');
            if (partes.Length != 3) return false;

            if (!int.TryParse(partes[0], out int dia)) return false;
            if (!int.TryParse(partes[1], out int mes)) return false;
            if (!int.TryParse(partes[2], out int año)) return false;

            DateTime fechaNac;
            if (!DateTime.TryParse($"{año}-{mes}-{dia}", out fechaNac)) return false;

            var hoy = DateTime.Today;
            int edad = hoy.Year - fechaNac.Year;
            if (fechaNac > hoy.AddYears(-edad)) edad--;

            return edad >= 16 && edad <= 80;
        }

        //VALIDAMOS APRAMETROS DE FECHA DEMANDA
        private bool FechaDemandaValida(string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha)) return false;

            var partes = fecha.Split('/');
            if (partes.Length != 3) return false;

            if (!int.TryParse(partes[0], out int dia)) return false;
            if (!int.TryParse(partes[1], out int mes)) return false;
            if (!int.TryParse(partes[2], out int año)) return false;

            DateTime fechaDem;
            if (!DateTime.TryParse($"{año}-{mes}-{dia}", out fechaDem)) return false;

            return fechaDem <= DateTime.Today;
        }

        //VALIDAMOS UNICAMENTE EL DNI
        private bool DocumentoValido(string doc)
        {
            if (string.IsNullOrWhiteSpace(doc))
                return false;

            doc = doc.Trim().ToUpper();

            // DNI ÚNICAMENTE
            if (System.Text.RegularExpressions.Regex.IsMatch(doc, @"^[0-9]{8}[A-Z]$"))
            {
                string letras = "TRWAGMYFPDXBNJZSQVHLCKE";
                int numero = int.Parse(doc.Substring(0, 8));
                char letraCorrecta = letras[numero % 23];
                return doc[8] == letraCorrecta;
            }

            return false;
        }
        //SI EL DOCUEMTO ESTA REGISTRADO SALTA ERROR
        private bool ExisteDocumento(string dni)
        {
            string connString = _config.GetConnectionString("BolsaEmpleo");

            using SqlConnection conn = new(connString);
            conn.Open();

            string query = @"
        SELECT COUNT(*) 
        FROM AYTO_PERSONA
        WHERE DOCUMENTO = @dni";

            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@dni", dni);

            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }


        private bool TelefonoValido(string tel)
        {
            if (string.IsNullOrWhiteSpace(tel))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(
                tel.Trim(),
                @"^[0-9]{3} [0-9]{2} [0-9]{2} [0-9]{2}$"
            );
        }


        private async Task<bool> ValidarReCaptcha()
        {
            var secret = _config["GoogleReCaptcha:SecretKey"];
            var response = Request.Form["g-recaptcha-response"];

            using var client = new HttpClient();
            var result = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={response}",
                null);

            var json = await result.Content.ReadAsStringAsync();
            return json.Contains("\"success\": true");
        }

        public async Task<IActionResult> OnPostEnviar()
        {
            //VALIDACION CAPTCHA
            if (!await ValidarReCaptcha())
            {
                ModelState.AddModelError("", "Debes verificar que no eres un robot.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //VALDIAR POLÍTICA DE PRIVACIDAD
            if (!PoliticaAceptada)
            {
                ModelState.AddModelError("", "Debes aceptar la política de privacidad.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //TELEFONO 1
            if (!TelefonoValido(Telefono1))
            {
                ModelState.AddModelError("", "El teléfono introducido no tiene un formato válido.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //FECHA NACIMIENTO
            if (!FechaNacimientoValida(FechaNacimiento))
            {
                ModelState.AddModelError("", "La fecha de nacimiento no es válida. Debe tener entre 16 y 80 años.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //FECHA DEMANDA
            if (!FechaDemandaValida(FechaDemanda))
            {
                ModelState.AddModelError("", "La fecha de demanda no puede ser posterior al día de hoy.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //DNI
            if (!DocumentoValido(Documento))
            {
                ModelState.AddModelError("", "El documento introducido no es válido. Debe ser un DNI correcto.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //SI EL DOCUMENTO INTRODUCIDO, YA SEA DNI O NO, ESTA DUPLICADO
            if (ExisteDocumento(Documento))
            {
                ModelState.AddModelError("", "El documento introducido ya existe y está registrado en la base de datos.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }

            //CV
            if (ArchivoCV != null)
                ArchivoNombre = ArchivoCV.FileName;

            //INSERTAR EN LA BBDD
            string connString = _config.GetConnectionString("BolsaEmpleo");

            using SqlConnection conn = new(connString);
            conn.Open();

            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                // PA1 INSERTAR PERSONA -> spAytoInsertarPersona

                SqlCommand cmd = new("spAytoInsertarPersona", conn, tran);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@docu", Request.Form["Documento"].ToString());
                cmd.Parameters.AddWithValue("@nom", Request.Form["Nombre"].ToString());
                cmd.Parameters.AddWithValue("@ape1", Request.Form["Apellido1"].ToString());
                cmd.Parameters.AddWithValue("@ape2", Request.Form["Apellido2"].ToString());

                // FECHA NACIMIENTO
                string fechaNacForm = Request.Form["FechaNacimiento"];
                DateTime fechaNac = DateTime.ParseExact(fechaNacForm, "dd/MM/yyyy", null);
                cmd.Parameters.AddWithValue("@fnac", fechaNac.ToString("yyyyMMdd"));

                cmd.Parameters.AddWithValue("@sex",
                    Request.Form["sexo"].ToString() == "Hombre" ? 1 : 2);

                string domicilioSQL =
                    $"c/ {Request.Form["Calle"]}, piso {Request.Form["Piso"]} puerta {Request.Form["Puerta"]}";
                cmd.Parameters.AddWithValue("@dire", domicilioSQL);

                cmd.Parameters.AddWithValue("@pob", Request.Form["Poblacion"].ToString());
                cmd.Parameters.AddWithValue("@telf", Request.Form["Telefono1"].ToString());
                cmd.Parameters.AddWithValue("@mov", Request.Form["Telefono2"].ToString());
                cmd.Parameters.AddWithValue("@mail", Request.Form["Email"].ToString());

                cmd.Parameters.AddWithValue("@naci",
                    int.TryParse(Request.Form["Nacionalidad"], out int nac) ? nac : 0);

                cmd.Parameters.AddWithValue("@desem",
                    Request.Form["desempleo"].ToString() == "Sí" ? 1 : 2);

                // FECHA DEMANDA
                string fechaDemForm = Request.Form["FechaDemanda"];
                if (!string.IsNullOrWhiteSpace(fechaDemForm))
                {
                    DateTime fechaDem = DateTime.ParseExact(fechaDemForm, "dd/MM/yyyy", null);
                    cmd.Parameters.AddWithValue("@femple", fechaDem.ToString("yyyyMMdd"));
                }
                else
                {
                    cmd.Parameters.AddWithValue("@femple", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@mej",
                    Request.Form["mejora"].ToString() == "Sí" ? 1 : 2);

                cmd.Parameters.AddWithValue("@pwd", "1234");
                cmd.Parameters.AddWithValue("@acep", PoliticaAceptada ? 1 : 0);
                cmd.Parameters.AddWithValue("@baja", 0);

                // DISCAPACIDADES
                cmd.Parameters.AddWithValue("@dfis", Request.Form["DFisica"].ToString() == "1" ? 1 : 0);
                cmd.Parameters.AddWithValue("@dpsi", Request.Form["DPsiquica"].ToString() == "1" ? 1 : 0);
                cmd.Parameters.AddWithValue("@dsen", Request.Form["DSensorial"].ToString() == "1" ? 1 : 0);

                // PERMISOS
                cmd.Parameters.AddWithValue("@sPermisos", "");

                // CATEGORÍAS Y PROFESIONES
                var categorias = Request.Form["Categoria[]"].ToList();
                var profesiones = Request.Form["Profesion[]"].ToList();

                int GetValue(List<string> lista, int index)
                {
                    if (index < lista.Count && int.TryParse(lista[index], out int val))
                        return val;
                    return 0;
                }

                cmd.Parameters.AddWithValue("@cat1", GetValue(categorias, 0));
                cmd.Parameters.AddWithValue("@pro1", GetValue(profesiones, 0));
                cmd.Parameters.AddWithValue("@cat2", GetValue(categorias, 1));
                cmd.Parameters.AddWithValue("@pro2", GetValue(profesiones, 1));
                cmd.Parameters.AddWithValue("@cat3", GetValue(categorias, 2));
                cmd.Parameters.AddWithValue("@pro3", GetValue(profesiones, 2));
                cmd.Parameters.AddWithValue("@cat4", GetValue(categorias, 3));
                cmd.Parameters.AddWithValue("@pro4", GetValue(profesiones, 3));
                cmd.Parameters.AddWithValue("@cat5", GetValue(categorias, 4));
                cmd.Parameters.AddWithValue("@pro5", GetValue(profesiones, 4));
                cmd.Parameters.AddWithValue("@cat6", GetValue(categorias, 5));
                cmd.Parameters.AddWithValue("@pro6", GetValue(profesiones, 5));

                // Obtener ID devuelto por el SP
                cmd.Parameters.Add("@ReturnValue", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                cmd.ExecuteNonQuery();

                int personaID = (int)cmd.Parameters["@ReturnValue"].Value;

                //================================================
                //PA 2 INSERTAR DOCUMENTO -> spDemandanteDocumento

                var archivoCV = Request.Form.Files["ArchivoCV"];

                if (archivoCV != null && archivoCV.Length > 0)
                {
                    SqlCommand cmdDoc = new("spDemandanteDocumento", conn, tran);
                    cmdDoc.CommandType = CommandType.StoredProcedure;

                    cmdDoc.Parameters.AddWithValue("@ccAccion", 1);
                    cmdDoc.Parameters.AddWithValue("@nClave", "");
                    cmdDoc.Parameters.AddWithValue("@nClaveDemandante", personaID);
                    cmdDoc.Parameters.AddWithValue("@sDescripcion", "CURRICULUM WEB");
                    cmdDoc.Parameters.AddWithValue("@sRuta", archivoCV.FileName);
                    cmdDoc.Parameters.AddWithValue("@nTipoDoc", 7);

                    cmdDoc.ExecuteNonQuery();
                }

                //INSERTAR PERMISOS REALES
                var permisosSeleccionados = Request.Form["permisos"]
                    .Select(int.Parse)
                    .ToList();

                foreach (int permiso in permisosSeleccionados)
                {
                    SqlCommand cmdPerm = new SqlCommand(
                        "INSERT INTO tRelDemandantePermiso (nClaveDemandante, nClavePermiso) VALUES (@demandante, @permiso)",
                        conn,
                        tran
                    );

                    cmdPerm.Parameters.AddWithValue("@demandante", personaID);
                    cmdPerm.Parameters.AddWithValue("@permiso", permiso);

                    cmdPerm.ExecuteNonQuery();
                }

                //SE CONFIRMA SI TODO HA SALIDO BIEN
                tran.Commit();

                TempData["SuccessMessage"] = "Formulario enviado correctamente.";
                return RedirectToPage();

            }
            catch (Exception ex)
            {
                //SI FALLA SE HACE ROLLBACK PARA QUE NO GUARDE
                tran.Rollback();

                ModelState.AddModelError("", "Error al guardar los datos.");
                CargarCategorias();
                CargarNacionalidades();
                return Page();
            }
        }
    }
}