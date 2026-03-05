using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace FormularioASP.Pages
{
    public class FormularioModel : PageModel
    {
        private readonly IConfiguration _config;

        public FormularioModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public string? FechaNacimiento { get; set; }

        [BindProperty]
        public string? FechaDemanda { get; set; }

        [BindProperty]
        public IFormFile? ArchivoCV { get; set; }

        public string? ArchivoNombre { get; set; }

        [BindProperty]
        public bool PoliticaAceptada { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPostCancelar()
        {
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostHola()
        {
            TempData["msg"] = "Hola";
            return Page();
        }

        //Validar fecha
        private bool ValidarFecha(string? fechaTexto)
        {
            if (fechaTexto == null)
                return false;

            if (!DateTime.TryParseExact(fechaTexto, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                return false;

            int edad = DateTime.Today.Year - fecha.Year;
            if (fecha > DateTime.Today.AddYears(-edad)) edad--;

            return edad >= 16 && edad <= 100;
        }

        //Validar Captcha
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

            if (!await ValidarReCaptcha())
            {
                ModelState.AddModelError("", "Debes verificar que no eres un robot.");
                return Page();
            }

            if (!PoliticaAceptada)
            {
                ModelState.AddModelError("", "Debes aceptar la política de privacidad.");
                return Page();
            }

            if (!ValidarFecha(FechaNacimiento) || !ValidarFecha(FechaDemanda))
            {
                ModelState.AddModelError("", "Alguna fecha no es válida.");
                return Page();
            }

            if (ArchivoCV != null)
                ArchivoNombre = ArchivoCV.FileName;

            TempData["msg"] = "Formulario enviado correctamente.";
            return Page();
        }
    }
}