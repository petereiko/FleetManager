using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ContactDirectoryModule
{
    public interface IContactDirectoryService
    {
        Task<MessageResponse> AddContactAsync(ContactDirectoryDto dto);
        Task<MessageResponse<ContactDirectoryDto>> UpdateContactAsync(ContactDirectoryDto dto);
        Task<MessageResponse> DeleteContactAsync(long id);
        Task<ContactDirectoryDto?> GetContactByIdAsync(long id);
        Task<List<ContactDirectoryDto>> GetAllContactsAsync();
        List<SelectListItem> GetCategoryOptions();
    }

}
