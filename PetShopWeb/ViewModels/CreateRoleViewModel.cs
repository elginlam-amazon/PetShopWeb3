using System.ComponentModel.DataAnnotations;

namespace PetShopWeb.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Role name is required")]
        public string? RoleName { get; set; }
    }
}
