using System.Threading.Tasks;
using DataAccessLibrary.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using ProjectXbet.Helpers;
using ProjectXbet.ViewModels;

namespace ProjectXbet.Controllers
{
    public class LandingController : Controller
    {
        private readonly IPredictionRepository predictionRepository;
        private readonly IStatisticsRepository statisticsRepository;
        private readonly LandingViewModel viewModel;

        public LandingController(IPredictionRepository predictionRepository, IStatisticsRepository statisticsRepository)
        {
            this.predictionRepository = predictionRepository;
            this.statisticsRepository = statisticsRepository;
            viewModel = new LandingViewModel();
        }

        public async Task<IActionResult> CreateViewModel()
        {
            viewModel.PredictionsMonotonous = await predictionRepository.GetPredictionsByTipType(Const.ROLE_BASIC, Const.TIP_TYPE_MONOTONOUS);
            viewModel.PredictionsPreviousMonotonous = await statisticsRepository.GetPredictionsPrevious(Const.TIP_TYPE_MONOTONOUS);
            viewModel.TipsMonotonous = await statisticsRepository.GetTipStats(Const.TIP_TYPE_MONOTONOUS);
            viewModel.LeaguesMonotonous = await statisticsRepository.GetLeagueStats(Const.TIP_TYPE_MONOTONOUS);

            viewModel.PredictionsAdventurous = await predictionRepository.GetPredictionsByTipType(Const.ROLE_BASIC, Const.TIP_TYPE_ADVENTUROUS);
            viewModel.PredictionsPreviousAdventurous = await statisticsRepository.GetPredictionsPrevious(Const.TIP_TYPE_ADVENTUROUS);
            viewModel.TipsAdventurous = await statisticsRepository.GetTipStats(Const.TIP_TYPE_ADVENTUROUS);
            viewModel.LeaguesAdventurous = await statisticsRepository.GetLeagueStats(Const.TIP_TYPE_ADVENTUROUS);

            viewModel.PredictionsLudicrous = await predictionRepository.GetPredictionsByTipType(Const.ROLE_BASIC, Const.TIP_TYPE_LUDICROUS);
            viewModel.PredictionsPreviousLudicrous = await statisticsRepository.GetPredictionsPrevious(Const.TIP_TYPE_LUDICROUS);
            viewModel.TipsLudicrous = await statisticsRepository.GetTipStats(Const.TIP_TYPE_LUDICROUS);
            viewModel.LeaguesLudicrous = await statisticsRepository.GetLeagueStats(Const.TIP_TYPE_LUDICROUS);

            for (int i = 1; i < 4; i++)
                viewModel.TipTypeStats.Add(await statisticsRepository.GetTipTypeStats(i));

            return View("Index", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Index() => await CreateViewModel();
    }
}
