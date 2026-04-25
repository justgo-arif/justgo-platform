using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.ClassTerm.Commands.RemoveTermHoliday
{
    public class RemoveJustGoBookingTermHolidayCommand : IRequest<string>
    {
        public RemoveJustGoBookingTermHolidayCommand(int termHolidayId)
        {
            TermHolidayId = termHolidayId;
        }

        public int TermHolidayId { get; set; }
    }
}
