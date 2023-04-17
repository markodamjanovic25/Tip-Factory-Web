using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using DataAccessLibrary.Repository.SqlRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.ViewModels;

namespace ProjectXbet.Controllers
{
    [Authorize]
    public class StatisticsController : BaseController
    {
        private readonly IStatisticsRepository repository;
        private readonly ILeagueRepository leagueRepository;
        private readonly ITipRepository tipRepository;
        private readonly StatisticsViewModel viewModel;

        public StatisticsController(IStatisticsRepository repository, ILeagueRepository leagueRepository, ITipRepository tipRepository)
        {
            this.repository = repository;
            this.leagueRepository = leagueRepository;
            this.tipRepository = tipRepository;
            viewModel = new StatisticsViewModel();
        }

        public async Task<IActionResult> CreateViewModel(int tipTypeId)
        {
            viewModel.TipStats = await repository.GetTipStats(tipTypeId);
            viewModel.Predictions = await repository.GetPredictionsPrevious(tipTypeId);
            viewModel.LeagueStats = await repository.GetLeagueStats(tipTypeId);
            viewModel.TipTypeStats = await repository.GetTipTypeStats(tipTypeId);
            viewModel.ControllerName = "Statistics";
            viewModel.TipTypeId = tipTypeId;

            ViewData["TipType"] = tipTypeId;
            return View("Index", viewModel);
        }

        [Route("stats")]
        [HttpGet]
        public async Task<IActionResult> Index(int tipTypeId) => await CreateViewModel(tipTypeId);

        [Route("league-stats")]
        [HttpGet]
        public async Task<IActionResult> ShowLeagueStats(int tipTypeId, int leagueId)
        {
            decimal totalPlayed = await leagueRepository.GetLeagueTotalPlayed(leagueId, tipTypeId);
            decimal wins = await leagueRepository.GetLeagueWins(leagueId, tipTypeId);
            decimal odds = await leagueRepository.GetLeagueAverageOdds(leagueId, tipTypeId);
            decimal percentage = PercentageCalculator.CalculatePercentage(totalPlayed, wins);
            decimal roi = PercentageCalculator.CalculateRoi(totalPlayed, wins, odds);

            var viewModel = new LeagueViewModel { 
               LeagueTotalPlayed = totalPlayed,
               LeagueWins = wins,
               LeaguePercentage = percentage,
               LeagueAverageOdds = odds,
               LeagueRoi = roi,
               League = await leagueRepository.GetLeagueByIdAsync(leagueId),
               Predictions = await leagueRepository.GetPredictionsByLeagueAndTipType(leagueId, tipTypeId),
               TipStats = await repository.GetTipStatsByLeague(tipTypeId, leagueId),
               ControllerName = "Statistics",
               TipTypeId = tipTypeId
            };

            ViewData["TipTypeId"] = tipTypeId;
            return View("League", viewModel);
        }

        [Route("league-tip-stats")]
        [HttpGet]
        public async Task<IActionResult> ShowTipsByLeagueAndTip(int tipTypeId, int tipId, int leagueId)
        {
            decimal totalPlayed = await leagueRepository.GetLeagueTotalPlayedByTip(leagueId, tipId);
            decimal wins = await leagueRepository.GetLeagueWinsByTip(leagueId, tipId);
            decimal odds = await leagueRepository.GetLeagueAverageOddsByTip(leagueId, tipId);
            decimal percentage = PercentageCalculator.CalculatePercentage(totalPlayed, wins);
            decimal roi = PercentageCalculator.CalculateRoi(totalPlayed, wins, odds);

            var viewModel = new LeagueTipDetailedViewModel
            {
                Tip = await tipRepository.GetTipByTipId(tipId),
                League = await leagueRepository.GetLeagueByIdAsync(leagueId),
                LeagueTotalPlayed = totalPlayed,
                LeagueWins = wins,
                LeagueAverageOdds = odds,
                LeaguePercentage = percentage,
                LeagueRoi = roi,
                Predictions = await leagueRepository.GetPredictionsByLeagueAndTip(leagueId, tipId),
                ControllerName = "Statistics",
                TipTypeId = tipTypeId
            };

            ViewData["TipTypeId"] = tipTypeId;
            return View("LeagueTipDetailed", viewModel);
        }

        [Route("tip-stats")]
        [HttpGet]
        public async Task <IActionResult> ShowPredictionsByTip(int tipTypeId, int tipId)
        {
            var viewModel = new TipViewModel
            {
                Tip = await tipRepository.GetTipByTipId(tipId),
                Predictions = await repository.GetPredictionsByTipId(tipId),
                TipStats = await repository.GetTipStatsByTipId(tipId),
                LeagueStats = await repository.GetLeagueStatsByTip(tipId),
                ControllerName = "Statistics",
                TipTypeId = tipTypeId
            };

            ViewData["TipTypeId"] = tipTypeId;
            return View("Tip", viewModel);
        }


        

    }
}
