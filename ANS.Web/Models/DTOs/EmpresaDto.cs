namespace ANS.Web.Models.DTOs
{
    /// <summary>
    /// DTO para empresas por buzón
    /// </summary>
    public class EmpresaDto
    {
        public string Empresa { get; set; }
        public int IdCuenta { get; set; }
        public string Cuenta { get; set; }
        public string Moneda { get; set; }
        public string Banco { get; set; }
        public int IdBanco { get; set; }
    }
}
