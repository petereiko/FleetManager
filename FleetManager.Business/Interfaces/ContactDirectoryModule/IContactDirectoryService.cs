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
        Task<MessageResponse<ContactRatingResultDto>> AddOrUpdateRatingAsync(ContactRatingDto dto);
        Task<MessageResponse<ContactRatingResultDto>> RemoveMyRatingAsync(long contactId);
        Task<(double avg, int count, Dictionary<int, int> distribution)> GetRatingStatsAsync(long contactId);
        List<SelectListItem> GetCategoryOptions();
    }

}
