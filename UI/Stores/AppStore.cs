using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cortex.Net.Api;

namespace Templates.Blazor2.UI.Stores
{
    [Observable]
    public class AppStore
    {
        public string? CreatedAt { get; set; }

        [Action]
        public void OnCreate()
        {
            CreatedAt = DateTime.UtcNow.ToString();
        }
    }
}
