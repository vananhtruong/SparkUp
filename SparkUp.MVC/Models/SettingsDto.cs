using Microsoft.Extensions.Options;

namespace SparkUp.MVC.Models
{
    public class SettingsDto
    {
        public string Domain { get; set; }
        public float ExpiredCache { get; set; }

        //public SettingsDto(IOptions<SettingsDto> options)
        //{
        //    Domain = options.Value.Domain;
        //    ExpiredCache = options.Value.ExpiredCache;
        //}
    }
}
