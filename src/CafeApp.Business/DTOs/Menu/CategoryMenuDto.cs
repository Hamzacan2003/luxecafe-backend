using System;
using System.Collections.Generic;
using System.Text;

namespace CafeApp.Business.DTOs.Menu
{
    public class CategoryMenuDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<MenuItemDto> Items { get; set; } = new();
    }
}
