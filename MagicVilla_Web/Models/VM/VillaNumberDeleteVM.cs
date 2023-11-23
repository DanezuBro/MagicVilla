using MagicVilla_Web.Models.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MagicVilla_Web.Models.VM
{
    public class VillaNumberDeleteVM
    {

        public VillaNumberDTO VillaNumber { get; set; }
        public IEnumerable<SelectListItem> VillaList { get; set; }

        public VillaNumberDeleteVM()
        {
            VillaNumber = new();
        }
    }
}
