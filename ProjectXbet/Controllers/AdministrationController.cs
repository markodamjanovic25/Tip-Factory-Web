using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLibrary.Models;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.ViewModels;

namespace ProjectXbet.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdministrationController : BaseController
    {
        private readonly ILeagueRepository leagueRepository;
        private readonly IMatchRepository matchRepository;
        private readonly ITipRepository tipRepository;
        private readonly IPredictionRepository predictionRepository;
        private readonly IAccountRepository accountRepository;
        private readonly IBetRepository betRepository;
        public AdministrationViewModel viewModel;

        public AdministrationController(ILeagueRepository leagueRepository, 
            IMatchRepository matchRepository, ITipRepository tipRepository, IPredictionRepository predictionRepository, 
            IAccountRepository accountRepository, IBetRepository betRepository)
        {
            this.leagueRepository = leagueRepository;
            this.matchRepository = matchRepository;
            this.tipRepository = tipRepository;
            this.predictionRepository = predictionRepository;
            this.accountRepository = accountRepository;
            this.betRepository = betRepository;
            viewModel = new AdministrationViewModel();
        }

        public async Task<IActionResult> CreateViewModel()
        {
            viewModel.Leagues = await leagueRepository.GetLeagues();
            viewModel.Matches = await matchRepository.GetMatches();
            viewModel.Tips = await tipRepository.GetTips();
            viewModel.Roles = await accountRepository.GetRoles();
            viewModel.Predictions = await predictionRepository.GetPredictionsAll();
            return View("Index", viewModel);
        }

        [Route("administration")]
        [HttpGet]
        public async Task<IActionResult> Index() => await CreateViewModel();

        [HttpPost]
        public async Task<IActionResult> CreatePrediction(AdministrationViewModel model)
        {
            var prediction = new Prediction
            {
                Odds = model.Odds,
                Chance = model.Chance,
                MatchId = model.MatchId,
                TipId = model.TipId
            };
            await predictionRepository.AddPrediction(prediction, model.RoleId);

            return await CreateViewModel();
        }

        //This method creates a new Match
        [HttpPost]
        public async Task<IActionResult> CreateMatch(AdministrationViewModel model)
        {
            await matchRepository.AddMatch(new Match
            {
                MatchDateTime = model.MatchDateTime,
                ClubHomeName = model.ClubHomeName,
                ClubAwayName = model.ClubAwayName,
                LeagueId = model.LeagueId
            });
            return await CreateViewModel();
        }

        [HttpPost]
        public async Task<IActionResult> SetPredictionOutcome(int predictionId, bool isCorrect, AdministrationViewModel model)
        {
            await predictionRepository.SetPredictionOutcomeAsync(predictionId, model.ClubHomeGoalsHalf, model.ClubAwayGoalsHalf, model.ClubHomeGoals, model.ClubAwayGoals, isCorrect);
            await betRepository.RefreshAllTickets(predictionId);
            return await CreateViewModel();
        }

        [HttpPost]
        public async Task<IActionResult> DeletePrediction(int predictionId)
        {
            await predictionRepository.DeletePrediction(predictionId);
            return await CreateViewModel();
        }

        [HttpGet]
        public async Task<IActionResult> EditMatch(int matchId)
        {
            var match = await matchRepository.GetMatchByIdAsync(matchId);
            var matchViewModel = new MatchViewModel
            {
                MatchId = matchId,
                MatchDateTime = match.MatchDateTime,
                ClubHomeName = match.ClubHomeName,
                ClubAwayName = match.ClubAwayName,
                ClubHomeGoalsHalf = (int)match.ClubHomeGoalsHalf,
                ClubAwayGoalsHalf = (int)match.ClubAwayGoalsHalf,
                ClubHomeGoals = (int)match.ClubHomeGoals,
                ClubAwayGoals = (int)match.ClubAwayGoals,
                LeagueId = match.LeagueId,
                Leagues = await leagueRepository.GetLeagues()
            };

            return View("Match", matchViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditMatch(MatchViewModel model)
        {
            var match = new Match
            {
                MatchId = model.MatchId,
                MatchDateTime = model.MatchDateTime,
                ClubHomeName = model.ClubHomeName,
                ClubAwayName = model.ClubAwayName,
                ClubHomeGoalsHalf = model.ClubHomeGoalsHalf,
                ClubAwayGoalsHalf = model.ClubAwayGoalsHalf,
                ClubHomeGoals = model.ClubHomeGoals,
                ClubAwayGoals = model.ClubAwayGoals,
                LeagueId = model.LeagueId
            };
            await matchRepository.EditMatchAsync(match);

            return await CreateViewModel();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMatch(int matchId)
        {
            await matchRepository.DeleteMatchByIdAsync(matchId);
            return await CreateViewModel();
        }

        [HttpGet]
        public async Task<IActionResult> EditPrediction(int predictionId)
        {
            var prediction = await predictionRepository.GetPredictionByIdAsync(predictionId);
            var predictionViewModel = new PredictionViewModel() 
            {
                PredictionId = prediction.PredictionId,
                Odds = prediction.Odds,
                Chance = prediction.Chance,
                MatchId = prediction.MatchId,
                TipId = prediction.TipId,
                Matches = await matchRepository.GetMatches(),
                Tips = await tipRepository.GetTips(),
                Roles = await accountRepository.GetRoles()
            };

            return View("Prediction", predictionViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditPrediction(PredictionViewModel model)
        {
            var prediction = new Prediction
            {
                PredictionId = model.PredictionId,
                Odds = model.Odds,
                Chance = model.Chance,
                MatchId = model.MatchId,
                TipId = model.TipId
            };
            await predictionRepository.EditPredictionAsync(prediction);

            return await CreateViewModel();
        }

        [Route("StatsAdvanced")]
        [HttpGet]
        public async Task<IActionResult> GetOddsInLeaguesStats(int tipTypeId)
        {
            var viewModel = new AdminStatsViewModel()
            {
                Odds = await leagueRepository.GetAllOddsByTipType(tipTypeId),
                LeagueStatsByOdds = await leagueRepository.GetLeagueStatsByOdds(tipTypeId)
            };

            ViewData["TipTypeId"] = tipTypeId;
            return View("LeagueStatsAdvanced", viewModel);
        }
    }
}
