using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChatbotApiNet9.Models;

public class Passenger
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "O RG é obrigatório")]
    [RegularExpression(@"^\d{1,2}\.\d{3}\.\d{3}-[0-9A-Za-z]$|^\d{8,9}$", ErrorMessage = "Formato de RG inválido")]
    public string RG { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "O CPF é obrigatório")]
    [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$|^\d{11}$", ErrorMessage = "Formato de CPF inválido")]
    public string CPF { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A data de nascimento é obrigatória")]
    public DateTime BirthDate { get; set; }
    
    public bool ValidateCPF()
    {
        // Remove caracteres não numéricos
        string cpf = Regex.Replace(CPF, "[^0-9]", "");
        
        // Verifica se tem 11 dígitos
        if (cpf.Length != 11)
            return false;
            
        // Verifica se todos os dígitos são iguais
        bool allEqual = true;
        for (int i = 1; i < cpf.Length; i++)
        {
            if (cpf[i] != cpf[0])
            {
                allEqual = false;
                break;
            }
        }
        if (allEqual)
            return false;
            
        // Calcula o primeiro dígito verificador
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);
            
        int remainder = sum % 11;
        int digit1 = remainder < 2 ? 0 : 11 - remainder;
        
        // Verifica o primeiro dígito verificador
        if (int.Parse(cpf[9].ToString()) != digit1)
            return false;
            
        // Calcula o segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);
            
        remainder = sum % 11;
        int digit2 = remainder < 2 ? 0 : 11 - remainder;
        
        // Verifica o segundo dígito verificador
        return int.Parse(cpf[10].ToString()) == digit2;
    }
    
    public bool ValidateRG()
    {
        // Remove caracteres não alfanuméricos
        string rg = Regex.Replace(RG, "[^0-9A-Za-z]", "");
        
        // Verifica se tem pelo menos 8 caracteres
        return rg.Length >= 8;
    }
    
    public bool ValidateBirthDate()
    {
        // Verifica se a data de nascimento é válida (não é futura e a pessoa tem pelo menos 2 anos)
        return BirthDate <= DateTime.Today && BirthDate.AddYears(2) <= DateTime.Today;
    }
}
