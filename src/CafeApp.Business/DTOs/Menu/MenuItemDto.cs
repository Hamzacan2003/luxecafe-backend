using System;
using System.Collections.Generic;
using System.Text;

namespace CafeApp.Business.DTOs.Menu
{
    public class MenuItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;    
        public string Description { get; set; }= string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool InStock { get; set; }
    }
}
