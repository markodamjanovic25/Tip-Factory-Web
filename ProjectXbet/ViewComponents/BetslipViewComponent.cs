using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectXbet.ViewComponents
{
    [ViewComponent(Name = "Betslip")]
    public class BetslipViewComponent : ViewComponent
    {
        private readonly IBetRepository betRepository;

        public BetslipViewComponent(IBetRepository betRepository)
        {
            this.betRepository = betRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = new BetslipViewModel {
                TicketId = betRepository.GetCurrentTicketId(GetUserId()),
                Predictions = await betRepository.GetCurrentBetPredictionsAsync(GetUserId()),
                TotalOdds = betRepository.GetCurrentTicketOdds(GetUserId())
            };

            return View(result);
        }

        public string GetUserId() => (User as ClaimsPrincipal).FindFirst(ClaimTypes.NameIdentifier).Value;
    }
}
