using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class FineTollDetailsViewModel
    {
        public long Id { get; set; }
        public string DriverName { get; set; }
        public string VehicleDescription { get; set; }
        public FineTollType Type { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public bool IsMinimal { get; set; }
        public FineTollStatus Status { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public FineTollDetailsViewModel() { }

        public FineTollDetailsViewModel(FineAndTollDto dto)
        {
            Id = dto.Id;
            DriverName = dto.DriverName;
            VehicleDescription = dto.VehicleDescription;
            Type = dto.Type;
            Title = dto.Title;
            Amount = dto.Amount;
            Currency = dto.Currency;
            Reason = dto.Reason;
            Notes = dto.Notes;
            IsMinimal = dto.IsMinimal;
            Status = dto.Status;
            PaidDate = dto.PaidDate;
            CreatedDate = dto.CreatedDate;
            CreatedBy = dto.CreatedBy;
            ModifiedDate = dto.ModifiedDate;
            ModifiedBy = dto.ModifiedBy;
        }
    }


}
