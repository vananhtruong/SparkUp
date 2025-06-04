using Microsoft.Extensions.Options;

namespace SparkUp.MVC.Models
{
    public class EmailSenderDto
    {
        public string Email { get; set; }
        public string Password { get; set; }

        //public EmailSenderDto(IOptions<EmailSenderDto> options)
        //{
        //    Email = options.Value.Email;
        //    Password = options.Value.Password;
        //}
    }
}
