using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.Helpers;
using ProjectXbet.ViewModels;

namespace ProjectXbet.Controllers
{
    [Authorize]
    public class PredictionController : BaseController
    {
        private readonly IAccountRepository accountRepository;
        private readonly IPredictionRepository predictionRepository;
        private readonly IStatisticsRepository statisticsRepository;
        private readonly IBetRepository betRepository;
        private readonly IRandomRepository randomRepository;
        private readonly PredictionsViewModel viewModel;

        public PredictionController(IAccountRepository accountRepository, IPredictionRepository predictionRepository, 
            IStatisticsRepository statisticsRepository, IBetRepository betRepository, IRandomRepository randomRepository)
        {
            this.accountRepository = accountRepository;
            this.predictionRepository = predictionRepository;
            this.statisticsRepository = statisticsRepository;
            this.betRepository = betRepository;
            this.randomRepository = randomRepository;
            viewModel = new PredictionsViewModel();
        }

        public async Task<IActionResult> CreateViewModel(int tipTypeId)
        {
            viewModel.Predictions = await predictionRepository.GetPredictionsByTipType(GetUserRoleName(), tipTypeId);
            randomRepository.Predictions = randomRepository.FillPredictionDictionary(await predictionRepository.GetPredictionsByTipType(GetUserRoleName(), tipTypeId));
            viewModel.PredictionBoxes = randomRepository.Predictions;
            viewModel.PredictionsPrevious = await statisticsRepository.GetPredictionsPrevious(tipTypeId);
            viewModel.ControllerName = "Prediction";
            viewModel.TipTypeId = tipTypeId;

            ViewData["TipType"] = tipTypeId;
            return View("Index", viewModel);
        }

        [Route("tips")]
        [HttpGet]
        public async Task<IActionResult> Index(int tipTypeId)
        {
            await CheckSubscriptionStatus();
            return await CreateViewModel(tipTypeId);
        }

        //If User has Role linked to any paid Subscription, check if it has expired
        public async Task CheckSubscriptionStatus()
        {
            if (GetUserRoleName().In(Const.ROLE_ADVENTURER, Const.ROLE_MONOTONER, Const.ROLE_PROFESSIONAL))
                await accountRepository.CheckSubscriptionStatus(GetUserId(), await accountRepository.GetSubscriptionId(GetUserId()));
        }

        [HttpPost]
        public async Task AddTicketItem(int predictionId)
        {
            var prediction = await predictionRepository.GetPredictionByIdAsync(predictionId);
            await betRepository.AddTicketItem(GetUserId(), prediction);
        }

        [HttpGet]
        public IActionResult BetslipViewComponent() => ViewComponent("Betslip");

        [HttpPost]
        public async Task AddRandomTicketItem(int tipTypeId)
        {
            int predictionId = randomRepository.GetRandomPredictionId(tipTypeId);
            var prediction = await predictionRepository.GetPredictionByIdAsync(predictionId);
            await betRepository.AddTicketItem(GetUserId(), prediction);
        }

        [HttpPost]
        public async Task DeleteTicketItem(int predictionId) => await betRepository.DeleteTicketItem(GetUserId(), predictionId);

        [HttpPost]
        public async Task DeleteTicket(int ticketId) => await betRepository.DeleteTicketByIdAsync(ticketId);

        [HttpPost]
        public async Task SaveTicket(int ticketId) => await betRepository.SaveTicketAsync(ticketId);

        public async Task<IActionResult> GetTicketInfo(int ticketId)
        {
            var ticketViewModel = new TicketViewModel()
            {
                Ticket = await betRepository.GetTicketByIdAsync(ticketId),
                Predictions = await betRepository.GetBetPredictionsByTicketIdAsync(ticketId)
            };
            return View("Ticket", ticketViewModel);
        }
    }
}
