
namespace ANS.Model.Interfaces
{
    public interface IServicioEmail
    {
        bool enviarExcelPorMail(string excelPath,string asunto,string cuerpo);
    }
}
