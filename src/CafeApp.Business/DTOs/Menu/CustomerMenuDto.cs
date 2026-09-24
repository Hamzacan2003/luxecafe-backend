using System;
using System.Collections.Generic;
using System.Text;

namespace CafeApp.Business.DTOs.Menu
{
    public class CustomerMenuDto
    {
        public string TableNumber { get; set; } = string.Empty;
        public List<CategoryMenuDto> Categories { get; set; } = new();
    }
}
