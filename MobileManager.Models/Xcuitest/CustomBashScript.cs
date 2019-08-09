using System;
using System.ComponentModel.DataAnnotations;

namespace MobileManager.Models.Xcuitest
{
    public class CustomBashScript
    {
        [Key]
        public Guid Id { get; set; }

        public string WorkingDirectory { get; set; }

        public string[] ScriptLine { get; set; }
    }
}
