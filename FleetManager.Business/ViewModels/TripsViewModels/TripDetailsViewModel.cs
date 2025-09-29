using FleetManager.Business.DataObjects.TripsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class TripDetailsViewModel
    {
        public TripDto Trip { get; set; }
        public List<TripExpenseDto> Expenses { get; set; }
        public List<TripCheckpointDto> Checkpoints { get; set; }
        public decimal TotalExpenses => Expenses?.Sum(e => e.Amount) ?? 0;
        public int CheckpointCount => Checkpoints?.Count ?? 0;
    }
}
